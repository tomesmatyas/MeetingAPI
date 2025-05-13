using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingAPI.Data;
using MeetingAPI.Models;
using System.Text.Json;

namespace MeetingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MeetingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Meeting>>> GetMeetings()
    {
        return await _context.Meetings
            .Include(m => m.Recurrence)
            .Include(m => m.Participants)
                .ThenInclude(mp => mp.User)
            .Include(m => m.CreatedByUser)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Meeting>> GetMeetingById(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Recurrence)
            .Include(m => m.Participants)
                .ThenInclude(mp => mp.User)
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null) return NotFound();
        return meeting;
    }

    [HttpPost]
    public async Task<ActionResult<Meeting>> CreateMeeting(Meeting meeting)
    {
        if (meeting.EndTime <= meeting.StartTime)
            return BadRequest("EndTime must be after StartTime.");

        if (meeting.CreatedByUserId == 0)
            return BadRequest("Missing CreatedByUserId.");

        meeting.CreatedAt = DateTime.UtcNow;

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMeetingById), new { id = meeting.Id }, meeting);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMeeting(int id, Meeting updatedMeeting)
    {
        if (id != updatedMeeting.Id) return BadRequest();

        var existingMeeting = await _context.Meetings
            .Include(m => m.Recurrence)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (existingMeeting == null) return NotFound();

        if (updatedMeeting.EndTime <= updatedMeeting.StartTime)
            return BadRequest("EndTime must be after StartTime.");

        existingMeeting.Title = updatedMeeting.Title;
        existingMeeting.Date = updatedMeeting.Date;
        existingMeeting.StartTime = updatedMeeting.StartTime;
        existingMeeting.EndTime = updatedMeeting.EndTime;
        existingMeeting.ColorHex = updatedMeeting.ColorHex;
        existingMeeting.IsRegular = updatedMeeting.IsRegular;
        existingMeeting.EndDate = updatedMeeting.EndDate;
        existingMeeting.UpdatedAt = DateTime.UtcNow;

        if (updatedMeeting.IsRegular && updatedMeeting.Recurrence != null)
        {
            existingMeeting.RecurrenceId = updatedMeeting.Recurrence.Id;
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
    public async Task<ActionResult<IEnumerable<User>>> GetParticipants(int meetingId)
    {
        return await _context.MeetingParticipants
            .Where(mp => mp.MeetingId == meetingId)
            .Include(mp => mp.User)
            .Select(mp => mp.User!)
            .ToListAsync();
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
    public async Task<IActionResult> AddParticipantViaBody(int meetingId, [FromBody] JsonElement data)
    {
        if (!data.TryGetProperty("userId", out var userIdElement))
            return BadRequest("Missing userId");

        int userId = userIdElement.GetInt32();

        var user = await _context.Users.FindAsync(userId);
        var meeting = await _context.Meetings.FindAsync(meetingId);

        if (user == null || meeting == null)
            return NotFound();

        bool exists = await _context.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.UserId == userId);

        if (exists)
            return Conflict("Participant already added.");

        var link = new MeetingParticipant { MeetingId = meetingId, UserId = userId };
        _context.MeetingParticipants.Add(link);

        await _context.SaveChangesAsync();

        return Ok();
    }
}
