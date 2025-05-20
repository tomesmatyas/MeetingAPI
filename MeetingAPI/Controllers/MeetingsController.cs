using MeetingAPI.Data;
using MeetingAPI.Models;
using MeetingAPI.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace MeetingAPI.Controllers { 

[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public MeetingsController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
        [Authorize]
        [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingDto>>> GetMeetings()
    {
            Console.WriteLine($" Authenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"User: {User.Identity?.Name}");

            foreach (var c in User.Claims)
                Console.WriteLine($" Claim: {c.Type} = {c.Value}");
            var meetings = await _context.Meetings
            .Include(m => m.Recurrence)
            .Include(m => m.Participants).ThenInclude(mp => mp.User)
            .Include(m => m.CreatedByUser)
            .ToListAsync();

        return Ok(_mapper.Map<List<MeetingDto>>(meetings));
    }
        [Authorize]
        [HttpGet("{id}")]
    public async Task<ActionResult<MeetingDto>> GetMeetingById(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Recurrence)
            .Include(m => m.Participants).ThenInclude(mp => mp.User)
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null) return NotFound();
        return Ok(_mapper.Map<MeetingDto>(meeting));
    }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<MeetingDto>> CreateMeeting(MeetingDto meetingDto)
        {
            var meeting = _mapper.Map<Meeting>(meetingDto);

            if (meeting.EndTime <= meeting.StartTime)
                return BadRequest("EndTime must be after StartTime.");

            if (meeting.CreatedByUserId == 0)
                return BadRequest("Missing CreatedByUserId.");

            meeting.CreatedAt = DateTime.UtcNow;
            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync(); 
            if (meetingDto.Participants?.Any() == true)
            {
                foreach (var p in meetingDto.Participants)
                {
                    _context.MeetingParticipants.Add(new MeetingParticipant
                    {
                        MeetingId = meeting.Id,
                        UserId = p.UserId
                    });
                }
                await _context.SaveChangesAsync();
            }

            var resultDto = _mapper.Map<MeetingDto>(meeting);
            return CreatedAtAction(nameof(GetMeetingById), new { id = resultDto.Id }, resultDto);
        }
        [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, MeetingDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var existingMeeting = await _context.Meetings
                .Include(m => m.Recurrence)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingMeeting == null) return NotFound();

            if (dto.EndTime <= dto.StartTime)
                return BadRequest("EndTime must be after StartTime.");

            existingMeeting.Title = dto.Title;
            existingMeeting.Date = dto.Date;
            existingMeeting.StartTime = dto.StartTime;
            existingMeeting.EndTime = dto.EndTime;
            existingMeeting.ColorHex = dto.ColorHex;
            existingMeeting.IsRegular = dto.IsRegular;
            existingMeeting.EndDate = dto.EndDate;
            existingMeeting.UpdatedAt = DateTime.UtcNow;

            if (dto.IsRegular && dto.Recurrence != null)
            {
                if (existingMeeting.Recurrence == null)
                {
                    existingMeeting.Recurrence = new MeetingRecurrence();
                    _context.MeetingRecurrences.Add(existingMeeting.Recurrence);
                }

                existingMeeting.Recurrence.Pattern = dto.Recurrence.Pattern;
                existingMeeting.Recurrence.Interval = dto.Recurrence.Interval;

                // ❗ NEPŘIŘAZUJ RecurrenceId RUČNĚ
                // EF Core si to automaticky propojí přes navigation property
            }
            else
            {
                // ❗ Odstranění Recurrence pokud se už nehodí
                if (existingMeeting.Recurrence != null)
                    _context.MeetingRecurrences.Remove(existingMeeting.Recurrence);

                existingMeeting.Recurrence = null;
                existingMeeting.RecurrenceId = null;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMeeting(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null) return NotFound();

        _context.Meetings.Remove(meeting);
        await _context.SaveChangesAsync();
        return NoContent();
    }
        [Authorize(Roles = "Admin")]
        [HttpPost("{meetingId}/users/{userId}")]
    public async Task<IActionResult> AddParticipant(int meetingId, int userId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        var user = await _context.Users.FindAsync(userId);
        if (meeting == null || user == null) return NotFound();

        bool exists = await _context.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.UserId == userId);

        if (exists)
            return Conflict("Participant already added.");

        var link = new MeetingParticipant { MeetingId = meetingId, UserId = userId };
        _context.MeetingParticipants.Add(link);
        await _context.SaveChangesAsync();
        return Ok();
    }
        [Authorize]
        [HttpGet("{meetingId}/participants")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetParticipants(int meetingId)
    {
        var users = await _context.MeetingParticipants
            .Where(mp => mp.MeetingId == meetingId)
            .Include(mp => mp.User)
            .Select(mp => mp.User!)
            .ToListAsync();

        return Ok(_mapper.Map<List<UserDto>>(users));
    }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{meetingId}/users/{userId}")]
    public async Task<IActionResult> RemoveParticipant(int meetingId, int userId)
    {
        var link = await _context.MeetingParticipants
            .FirstOrDefaultAsync(mp => mp.MeetingId == meetingId && mp.UserId == userId);

        if (link == null) return NotFound();

        _context.MeetingParticipants.Remove(link);
        await _context.SaveChangesAsync();
        return NoContent();
    }
        [Authorize(Roles = "Admin")]
        [HttpPost("{meetingId}/participants")]
    public async Task<IActionResult> AddParticipantViaBody(int meetingId, [FromBody] MeetingParticipantDto data)
    {
        var user = await _context.Users.FindAsync(data.UserId);
        var meeting = await _context.Meetings.FindAsync(meetingId);

        if (user == null || meeting == null)
            return NotFound();

        bool exists = await _context.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.UserId == data.UserId);

        if (exists)
            return Conflict("Participant already added.");

        var link = new MeetingParticipant { MeetingId = meetingId, UserId = data.UserId };
        _context.MeetingParticipants.Add(link);

        await _context.SaveChangesAsync();

        return Ok();
    }
}
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public UsersController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsAdmin)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || user.IsAdmin) return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, _mapper.Map<UserDto>(user));
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var existing = await _context.Users.FindAsync(id);
        if (existing == null || existing.IsAdmin) return NotFound();

        existing.FirstName = dto.FirstName;
        existing.LastName = dto.LastName;
        existing.Email = dto.Email;

        await _context.SaveChangesAsync();
        return NoContent();
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || user.IsAdmin) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    } 
}
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || user.PasswordHash != dto.Password) // POZOR: nezabezpečeno!
            return Unauthorized();

        var token = GenerateJwtToken(user);
        return Ok(new LoginResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }
        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return Conflict("Uživatel již existuje.");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = dto.PasswordHash, // ❗️ Později zašifrovat!
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null) return NotFound();

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }


        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
                            {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("role", user.Role)
                            };
            Console.WriteLine($"[LOGIN] Uživatelské jméno: {user.Username}");
            Console.WriteLine($"[LOGIN] Role: {user.Role}");

            foreach (var c in claims)
                Console.WriteLine($"[LOGIN] Claim: {c.Type} = {c.Value}");

            var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: creds
                    );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 
