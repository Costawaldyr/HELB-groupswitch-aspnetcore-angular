using GroupSwitch.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping.TestSupport
{
    public class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _sqliteConnection;

        public GroupSwitchDbContext _dbContext { get; }

        public SqliteTestDatabase()
        {
            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            _sqliteConnection.Open();

            var options = new DbContextOptionsBuilder<GroupSwitchDbContext>()
                .UseSqlite(_sqliteConnection)
                .Options;

            _dbContext = new GroupSwitchDbContext(options);
            _dbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _sqliteConnection.Dispose();
        }

        public Task<int> CountSessionsOfGroupAsync(string groupName)
        {
            return _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .SelectMany(group => group.SessionList)
                .CountAsync();
        }

        public Task<int> CountSessionsOfGroupWeekAsync(string groupName, int week)
        {
            return _dbContext.StudentGroups
                .Where(group => group.Name == groupName)
                .SelectMany(group => group.SessionList)
                .CountAsync(session => session.WeekNumber == week);
        }
    }
}
