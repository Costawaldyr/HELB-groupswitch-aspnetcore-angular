namespace GroupSwitch.Business.Scraping.Models
{
    public class FailedGroupStreak
    {
        private string? _currentGroupName;
        private bool _isCurrentGroupFailing;

        public int CompletedFailedGroups { get; private set; }

        public void Add(WeekAssessment assessment)
        {
            if (assessment.GroupName != _currentGroupName)
            {
                CloseCurrentGroup();
                _currentGroupName = assessment.GroupName;
                _isCurrentGroupFailing = true;
            }

            if (assessment.Status != WeekResponseStatus.RetrievalFailed)
            {
                _isCurrentGroupFailing = false;
            }
        }

        private void CloseCurrentGroup()
        {
            if (_currentGroupName is null)
            {
                return;
            }

            CompletedFailedGroups = _isCurrentGroupFailing ? CompletedFailedGroups + 1 : 0;
        }
    }
}
