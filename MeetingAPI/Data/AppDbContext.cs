using Microsoft.EntityFrameworkCore;
using MeetingAPI.Models;

namespace MeetingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Participant> Participants { get; set; }
    }
}