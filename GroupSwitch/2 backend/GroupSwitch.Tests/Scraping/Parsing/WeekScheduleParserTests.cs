using GroupSwitch.Business.Scraping.Models;
using GroupSwitch.Business.Scraping.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Tests.Scraping.Parsing
{
    public class WeekScheduleParserTests
    {
        private readonly WeekScheduleParser _parser = new();

        private const string OneCourseAndOneCancellation = """
            {"donneesSec":{"donnees":{
                "ListeCours":[
                    {"G":11,"p":116,"d":4,"listeC":[
                        {"G":0,"C":{"L":"Dév. 4 : Plateforme.net"}},
                        {"G":1,"C":[{"L":"MEDOL"}]},
                        {"G":3,"C":[{"L":"Teams"}]},
                        {"G":5,"C":{}}
                    ]},
                    {"G":11,"p":77,"d":17,"listeC":[
                        {"G":0,"C":{"L":"Projets II"}},
                        {"G":1,"C":[{"L":"IDRISSI"},{"L":"GUYOT"}]},
                        {"G":3,"C":[{"L":"2106"}]},
                        {"G":5,"C":{"str":"Temerbek selon les besoins"}}
                    ]}
                ],
                "ListeAnnulationsCours":[
                    {"G":8,"p":254,"d":8,"commentaire":"Panne de chauffage","listeContenus":[
                        {"G":0,"C":{"L":"Programmation Réseaux"}},
                        {"G":1,"C":[{"L":"PAQUET"}]},
                        {"G":3,"C":[{"L":"2106"}]},
                        {"G":5,"C":{}}
                    ]}
                ]
            }}}
            """;


        [Fact]
        public void Parse_NormalCourse_ReadsAllFields()
        {
            var session = _parser.Parse(OneCourseAndOneCancellation).First();

            Assert.Equal(11, session.Week);
            Assert.Equal(116, session.Position);
            Assert.Equal(4, session.DurationInSlots);
            Assert.Equal("Dév. 4 : Plateforme.net", session.Title);
            Assert.Equal("MEDOL", session.Teacher);
            Assert.Equal("Teams", session.Room);
            Assert.False(session.IsCancelled);
        }

        [Fact]
        public void Parse_TwoTeachers_JoinsThemWithComma()
        {
            var session = _parser.Parse(OneCourseAndOneCancellation).Skip(1).First();

            Assert.Equal("IDRISSI, GUYOT", session.Teacher);
            Assert.Equal("Temerbek selon les besoins", session.Remark);
        }

        [Fact]
        public void Parse_Cancellation_IsMarkedCancelledWithComment()
        {
            var session = _parser.Parse(OneCourseAndOneCancellation).Last();

            Assert.True(session.IsCancelled);
            Assert.Equal(8, session.Week);
            Assert.Equal("Panne de chauffage", session.Remark);
            Assert.Equal("Programmation Réseaux", session.Title);
        }

        [Fact]
        public void Parse_NoCancellationList_ReturnsOnlyCourses()
        {
            var json = """{"donneesSec":{"donnees":{"ListeCours":[]}}}""";

            Assert.Empty(_parser.Parse(json));
        }

        [Fact]
        public void Parse_MissingListeCours_ThrowsFormatException()
        {
            var json = "{'donneesSec':{'donnees':{}}}".Replace('\'', '"');

            Assert.Throws<HyperplanningFormatException>(() => _parser.Parse(json).ToList());
        }

        [Fact]
        public void Parse_CourseWithoutPosition_ThrowsFormatException()
        {
            var json = "{'donneesSec':{'donnees':{'ListeCours':[{'G':8,'d':4,'listeC':[]}]}}}".Replace('\'', '"');

            Assert.Throws<HyperplanningFormatException>(() => _parser.Parse(json).ToList());
        }
    }
}
