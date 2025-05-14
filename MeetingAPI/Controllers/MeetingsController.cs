using MeetingAPI.Data;
using MeetingAPI.Models;
using MeetingAPI.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace MeetingAPI.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingDto>>> GetMeetings()
    {
        var meetings = await _context.Meetings
            .Include(m => m.Recurrence)
            .Include(m => m.Participants).ThenInclude(mp => mp.User)
            .Include(m => m.CreatedByUser)
            .ToListAsync();

        return Ok(_mapper.Map<List<MeetingDto>>(meetings));
    }

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

        var resultDto = _mapper.Map<MeetingDto>(meeting);
        return CreatedAtAction(nameof(GetMeetingById), new { id = resultDto.Id }, resultDto);
    }

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
                _context.Recurrences.Add(existingMeeting.Recurrence);
            }

            existingMeeting.Recurrence.Pattern = dto.Recurrence.Pattern;
            existingMeeting.Recurrence.Interval = dto.Recurrence.Interval;
            existingMeeting.RecurrenceId = dto.Recurrence.Id;
        }
        else
        {
            existingMeeting.RecurrenceId = null;
            existingMeeting.Recurrence = null;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMeeting(int id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null) return NotFound();

        _context.Meetings.Remove(meeting);
        await _context.SaveChangesAsync();
        return NoContent();
    }

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

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || user.IsAdmin) return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, _mapper.Map<UserDto>(user));
    }

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
