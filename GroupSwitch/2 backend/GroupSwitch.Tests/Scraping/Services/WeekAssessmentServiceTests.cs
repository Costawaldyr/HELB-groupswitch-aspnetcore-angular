using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Data.Entities;
using GroupSwitch.Tests.Scraping.TestSupport;

namespace GroupSwitch.Tests.Scraping.Services
{
    public class WeekAssessmentServiceTests
    {
        private const string GroupName = "IODA B3";

        private readonly string week8Json = FixtureReader.Read("week8.json");
        private readonly string week11Json = FixtureReader.Read("week11.json");
        private readonly string emptyWeekJson = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[],'ListeAnnulationsCours':[]}}}".Replace('\'', '"');

        private WeekAssessment Assess(WeekRetrieval retrieval)
        {
            return TestObjects.NewWeekAssessmentService().Assess(retrieval);
        }

        private string OneCourseJson(int week, int position, int duration, string subject)
        {
            var json = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[{'G':" + week
                + ",'p':" + position + ",'d':" + duration
                + ",'listeC':[{'G':0,'C':{'L':'" + subject + "'}}]}],'ListeAnnulationsCours':[]}}}";

            return json.Replace('\'', '"');
        }

        private string RequestBodyFor(string groupName, int week)
        {
            var body = "{'donneesSec':{'_Signature_':{'listeRecherche':[{'L':'" + groupName
                + "'}]},'donnees':{'Domaine':{'_T':8,'V':'[" + week + "]'}}}}";

            return body.Replace('\'', '"');
        }

        [Fact]
        public void Assess_FullWeek_IsComplete()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 11, week11Json));

            Assert.Equal(WeekResponseStatus.Complete, assessment.Status);
            Assert.Equal(15, assessment.Sessions.Count);
        }

        [Fact]
        public void Assess_WeekWithOneCancellation_KeepsTheCancelledSession()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, week8Json));

            Assert.Equal(WeekResponseStatus.Complete, assessment.Status);
            Assert.Equal(9, assessment.Sessions.Count);
            Assert.Single(assessment.Sessions, session => session.IsCancelled);
        }

        [Fact]
        public void Assess_PresentButEmptyLists_IsCompleteEmpty()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 7, emptyWeekJson));

            Assert.Equal(WeekResponseStatus.CompleteEmpty, assessment.Status);
            Assert.True(assessment.IsAccepted);
            Assert.Empty(assessment.Sessions);
        }

        [Fact]
        public void Assess_MissingListeCours_IsInvalid()
        {
            var json = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{}}}".Replace('\'', '"');

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 7, json));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
            Assert.False(assessment.IsAccepted);
        }

        [Fact]
        public void Assess_ListeCoursIsNotAnArray_IsInvalid()
        {
            var json = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':'x'}}}".Replace('\'', '"');

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 7, json));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
        }

        [Fact]
        public void Assess_BrokenJson_IsInvalid()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, "{ not json"));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
        }

        [Fact]
        public void Assess_OtherHyperplanningFunction_IsInvalid()
        {
            var json = "{'nom':'FonctionRenvoyerListeDeRessource','donneesSec':{}}".Replace('\'', '"');

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, json));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
        }

        [Fact]
        public void Assess_SessionsOfAnotherWeek_IsUncorrelated()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, week11Json));

            Assert.Equal(WeekResponseStatus.Uncorrelated, assessment.Status);
            Assert.Empty(assessment.Sessions);
        }

        [Fact]
        public void Assess_RequestForAnotherWeek_IsUncorrelated()
        {
            var retrieval = WeekRetrieval.Succeeded(GroupName, 11, week11Json, RequestBodyFor(GroupName, 8));

            Assert.Equal(WeekResponseStatus.Uncorrelated, Assess(retrieval).Status);
        }

        [Fact]
        public void Assess_RequestForAnotherGroup_IsUncorrelated()
        {
            var retrieval = WeekRetrieval.Succeeded(GroupName, 11, week11Json, RequestBodyFor("SI 3", 11));

            Assert.Equal(WeekResponseStatus.Uncorrelated, Assess(retrieval).Status);
        }

        [Fact]
        public void Assess_RequestOfTheRightGroupAndWeek_IsComplete()
        {
            var retrieval = WeekRetrieval.Succeeded(GroupName, 11, week11Json, RequestBodyFor(GroupName, 11));

            Assert.Equal(WeekResponseStatus.Complete, Assess(retrieval).Status);
        }

        [Fact]
        public void Assess_FailedRetrieval_KeepsTheReason()
        {
            var assessment = Assess(WeekRetrieval.Failed(GroupName, 8, "Timeout"));

            Assert.Equal(WeekResponseStatus.RetrievalFailed, assessment.Status);
            Assert.Contains("Timeout", assessment.Diagnostic);
        }

        [Theory]
        [InlineData(392, 4)]
        [InlineData(-1, 4)]
        [InlineData(10, 0)]
        [InlineData(10, -2)]
        [InlineData(50, 10)]
        public void Assess_CourseOutsideTheGrid_IsInvalid(int position, int duration)
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, OneCourseJson(8, position, duration, "Projets II")));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
        }

        [Fact]
        public void Assess_CourseWithoutSubject_IsInvalid()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, OneCourseJson(8, 116, 4, string.Empty)));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
        }

        private string CourseWithFieldsJson(string teacher, string room)
        {
            var json = "{'nom':'FonctionEmploiDuTemps','donneesSec':{'donnees':{'ListeCours':[{'G':8,'p':116,'d':4,"
                + "'listeC':[{'G':0,'C':{'L':'Projets II'}},{'G':1,'C':[{'L':'" + teacher + "'}]},{'G':3,'C':[{'L':'" + room + "'}]}]}],"
                + "'ListeAnnulationsCours':[]}}}";

            return json.Replace('\'', '"');
        }

        [Fact]
        public void Assess_RoomLongerThanItsColumn_IsInvalidAndNamesTheField()
        {
            var tooLongRoom = new string('R', ScheduledSession.RoomMaxLength + 1);

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, CourseWithFieldsJson("MEDOL", tooLongRoom)));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
            Assert.Empty(assessment.Sessions);
            Assert.Contains("Room", assessment.Diagnostic);
            Assert.Contains((ScheduledSession.RoomMaxLength + 1).ToString(), assessment.Diagnostic);
        }

        [Fact]
        public void Assess_TeacherLongerThanItsColumn_IsInvalid()
        {
            var tooLongTeacher = new string('T', ScheduledSession.TeacherMaxLength + 1);

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, CourseWithFieldsJson(tooLongTeacher, "2106")));

            Assert.Equal(WeekResponseStatus.Invalid, assessment.Status);
            Assert.Contains("Teacher", assessment.Diagnostic);
        }

        [Fact]
        public void Assess_RoomExactlyAtItsLimit_IsCompleteAndNotTruncated()
        {
            var longestRoom = new string('R', ScheduledSession.RoomMaxLength);

            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, CourseWithFieldsJson("MEDOL", longestRoom)));

            Assert.Equal(WeekResponseStatus.Complete, assessment.Status);
            Assert.Equal(longestRoom, Assert.Single(assessment.Sessions).Room);
        }

        [Fact]
        public void Assess_ValidSingleCourse_IsComplete()
        {
            var assessment = Assess(WeekRetrieval.Succeeded(GroupName, 8, OneCourseJson(8, 116, 4, "Projets II")));

            Assert.Equal(WeekResponseStatus.Complete, assessment.Status);
            Assert.Single(assessment.Sessions);
        }
    }
}
