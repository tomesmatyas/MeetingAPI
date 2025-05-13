using Microsoft.EntityFrameworkCore;
using MeetingAPI.Models;

namespace MeetingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MeetingRecurrence> MeetingRecurrences { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MeetingParticipant: složený primární klíč
            modelBuilder.Entity<MeetingParticipant>()
                .HasKey(mp => new { mp.MeetingId, mp.UserId });

            modelBuilder.Entity<MeetingParticipant>()
                .HasOne(mp => mp.Meeting)
                .WithMany(m => m.Participants)
                .HasForeignKey(mp => mp.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MeetingParticipant>()
                .HasOne(mp => mp.User)
                .WithMany(u => u.Participations)
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User: validace délky jména
            modelBuilder.Entity<User>()
                .Property(u => u.FirstName)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.LastName)
                .HasMaxLength(100);

            // Meeting: CHECK constraints
            modelBuilder.Entity<Meeting>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_EndTimeAfterStartTime", "[EndTime] > [StartTime]");
                table.HasCheckConstraint("CK_EndDateAfterDate", "[EndDate] IS NULL OR [EndDate] >= [Date]");
            });

            // Meeting: vztah k autorovi
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.CreatedByUser)
                .WithMany(u => u.CreatedMeetings)
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Meeting: vztah k MeetingRecurrence
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.Recurrence)
                .WithMany(r => r.Meetings)
                .HasForeignKey(m => m.RecurrenceId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
