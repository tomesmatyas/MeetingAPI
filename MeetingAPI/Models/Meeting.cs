using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetingAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string Role { get; set; } = "User";

        public ICollection<Meeting> CreatedMeetings { get; set; } = new List<Meeting>();
        public ICollection<MeetingParticipant> Participations { get; set; } = new List<MeetingParticipant>();

        // Pro vazbu admin-user
        public ICollection<AdminUser> AdminOfUsers { get; set; } = new List<AdminUser>();
        public ICollection<AdminUser> UserOfAdmins { get; set; } = new List<AdminUser>();
    }

    public class AdminUser
    {
        public int AdminId { get; set; }
        public User Admin { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }



    public class MeetingRecurrence
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        [RegularExpression("None|Weekly|Monthly")]
        public string Pattern { get; set; } = "None";

        public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
    }


    public class Meeting
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan StartTime { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan EndTime { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [MaxLength(7)]
        public string? ColorHex { get; set; }

        public bool IsRegular { get; set; }

        public int? Interval { get; set; } = 1;

        public int? RecurrenceId { get; set; }
        public MeetingRecurrence? Recurrence { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    }

    public class MeetingParticipant
    {
        public int MeetingId { get; set; }
        public Meeting Meeting { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

}
