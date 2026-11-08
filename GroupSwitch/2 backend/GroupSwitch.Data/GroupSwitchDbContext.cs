using GroupSwitch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;


namespace GroupSwitch.Data
{
    public class GroupSwitchDbContext : DbContext
    {
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ScheduledSession> ScheduledSessions { get; set; }
        public DbSet<ImportRun> ImportRuns { get; set; }
        public DbSet<ImportLogEntry> ImportLogEntries { get; set; }

        public GroupSwitchDbContext(DbContextOptions<GroupSwitchDbContext> options) : base(options)
        {
        }
    }
}
