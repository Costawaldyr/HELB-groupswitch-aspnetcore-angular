using GroupSwitch.Api.Mapping;
using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;
using GroupSwitch.Data.Enums;

namespace GroupSwitch.Tests.Scraping.Mapping
{
    public class ImportDtoMapperTests
    {
        private readonly ImportDtoMapper mapper = new();

        private ImportRun NewRun()
        {
            return new ImportRun
            {
                Id = 7,
                StartedAt = new DateTime(2026, 11, 8, 10, 0, 0),
                FinishedAt = new DateTime(2026, 11, 8, 10, 5, 0),
                Status = ImportStatus.Finished,
                GroupsImported = 2,
                SessionsImported = 39,
                LogEntriesList = new List<ImportLogEntry>
                {
                    new() { Id = 2, Level = ImportLogLevels.Info, Message = "second" },
                    new() { Id = 1, Level = ImportLogLevels.Info, Message = "first" }
                }
            };
        }

        [Fact]
        public void ToDto_MapsFieldsAndWritesTheStatusAsText()
        {
            var dto = mapper.ToDto(NewRun());

            Assert.Equal(7, dto.Id);
            Assert.Equal("Finished", dto.Status);
            Assert.Equal(2, dto.GroupsImported);
            Assert.Equal(39, dto.SessionsImported);
        }

        [Fact]
        public void ToDetailsDto_OrdersTheLogByEntryId()
        {
            var details = mapper.ToDetailsDto(NewRun());

            Assert.Equal(new[] { "first", "second" }, details.Log.Select(entry => entry.Message));
        }
    }
}