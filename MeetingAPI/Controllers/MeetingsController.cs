using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingAPI.Data;
using MeetingAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeetingAPI.Controllers
{
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
            return await _context.Meetings.Include(m => m.Participants).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Meeting>> GetMeetingById(int id)
        {
            var meeting = await _context.Meetings.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == id);
            if (meeting == null) return NotFound();
            return meeting;
        }

        [HttpPost]
        public async Task<ActionResult<Meeting>> CreateMeeting(Meeting meeting)
        {
            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMeetingById), new { id = meeting.Id }, meeting);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, Meeting updatedMeeting)
        {
            if (id != updatedMeeting.Id) return BadRequest();
            _context.Entry(updatedMeeting).State = EntityState.Modified;
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

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Meeting>>> SearchMeetings(string? title)
        {
            return await _context.Meetings
                .Where(m => title == null || m.Title.Contains(title))
                .Include(m => m.Participants)
                .ToListAsync();
        }

        [HttpGet("bydate")]
        public async Task<ActionResult<IEnumerable<Meeting>>> GetMeetingsByDate(DateTime date)
        {
            return await _context.Meetings
                .Where(m => m.Date.Date == date.Date)
                .Include(m => m.Participants)
                .ToListAsync();
        }

        [HttpPost("{meetingId}/participants")]
        public async Task<ActionResult> AddParticipant(int meetingId, Participant participant)
        {
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null) return NotFound();

            participant.MeetingId = meetingId;
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{meetingId}/participants")]
        public async Task<ActionResult<IEnumerable<Participant>>> GetParticipants(int meetingId)
        {
            return await _context.Participants.Where(p => p.MeetingId == meetingId).ToListAsync();
        }

        [HttpPut("participants/{participantId}")]
        public async Task<IActionResult> UpdateParticipant(int participantId, Participant updated)
        {
            if (participantId != updated.Id) return BadRequest();
            _context.Entry(updated).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("participants/{participantId}")]
        public async Task<IActionResult> DeleteParticipant(int participantId)
        {
            var participant = await _context.Participants.FindAsync(participantId);
            if (participant == null) return NotFound();
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
