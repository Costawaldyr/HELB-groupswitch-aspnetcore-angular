using GroupSwitch.Business.Scraping.Models;

namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IWeekAssessmentService
    {
        WeekAssessment Assess(WeekRetrieval retrieval);
    }
}
