using System;
using System.ComponentModel.DataAnnotations;

namespace MeetingAPI.Models
{

    public class Meeting
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime Date { get; set; }
        public bool IsRegular { get; set; } // Pravidelná/nepravidelná schůzka
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? ColorHex { get; set; }
        public List<Participant> Participants { get; set; } = new();
    }

    public class Participant
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int MeetingId { get; set; }
        public Meeting? Meeting { get; set; }
    }
}
