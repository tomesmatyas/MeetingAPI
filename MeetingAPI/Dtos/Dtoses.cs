﻿namespace MeetingAPI.Dtos
{

    public class CreateMeetingDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? ColorHex { get; set; }
        public bool IsRegular { get; set; }
        public int? RecurrenceId { get; set; }  // volitelné, lepší RecurrenceId
        public int? Interval { get; set; } = 1;        // NOVĚ!      // nově můžeš předat i RecurrenceId
        public DateTime? EndDate { get; set; }
        public int CreatedByUserId { get; set; }
        public List<int>? Participants { get; set; }
    }
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }
    public class MeetingDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ColorHex { get; set; }
        public bool IsRegular { get; set; }
        public int? Interval { get; set; } = 1;
        public string? RecurrencePattern { get; set; }
        public int? RecurrenceId { get; set; }
        public List<UserDto> Participants { get; set; } = new();
    }
    public class UpdateMeetingDto : CreateMeetingDto
    {
        public int Id { get; set; }
    }
    public class MeetingDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ColorHex { get; set; }
        public bool IsRegular { get; set; }

        public int? RecurrenceId { get; set; }
        public MeetingRecurrenceDto? Recurrence { get; set; }

        public int? Interval { get; set; } = 1; // přidej zde

        public int CreatedByUserId { get; set; }
        public UserDto? CreatedByUser { get; set; }

        public List<MeetingParticipantDto> Participants { get; set; } = new();
    }

    public class MeetingRecurrenceDto
    {
        public int Id { get; set; }
        public string Pattern { get; set; } = "None";
       
    }

    public class MeetingParticipantDto
    {
        public int MeetingId { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; }
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }


    public class UpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Id { get; set; }
    }
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }
}
