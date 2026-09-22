using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Helpers;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// Job titles come off arbitrary job descriptions, so they rarely equal an Assessment Group
    /// name exactly. These pin down what counts as near enough — and, just as importantly, what
    /// does not, because a confident wrong AG is worse than falling through to the catch-all.
    /// </summary>
    public class AssessmentGroupMatcherTests
    {
        private static List<AssessmentGroupMatcher.Candidate> Groups() => new()
        {
            new AssessmentGroupMatcher.Candidate { Id = 1, Name = "Java Developer" },
            new AssessmentGroupMatcher.Candidate { Id = 2, Name = "Backend Developer" },
            new AssessmentGroupMatcher.Candidate { Id = 3, Name = "Frontend Developer" },
            new AssessmentGroupMatcher.Candidate { Id = 4, Name = "QA Engineer" },
            new AssessmentGroupMatcher.Candidate { Id = 5, Name = "Data Engineer" },
            new AssessmentGroupMatcher.Candidate { Id = 6, Name = "DevOps Engineer" },
            new AssessmentGroupMatcher.Candidate { Id = 7, Name = "Business Analyst" }
        };

        private static int Resolve(string jobTitle) =>
            AssessmentGroupMatcher.ResolveId(jobTitle, Groups());

        #region Matching on the Assessment Group's name

        [Fact]
        public void An_exact_name_resolves_to_that_group()
        {
            Assert.Equal(1, Resolve("Java Developer"));
            Assert.Equal(7, Resolve("Business Analyst"));
        }

        [Fact]
        public void Case_and_punctuation_do_not_matter()
        {
            Assert.Equal(1, Resolve("java developer"));
            Assert.Equal(1, Resolve("JAVA  DEVELOPER"));
            Assert.Equal(7, Resolve("Business-Analyst"));
        }

        [Theory]
        [InlineData("Senior Java Developer")]
        [InlineData("Sr. Java Developer")]
        [InlineData("Junior Java Developer")]
        [InlineData("Lead Java Developer")]
        [InlineData("Java Developer II")]
        [InlineData("Principal Java Developer")]
        public void Seniority_words_are_ignored(string jobTitle)
        {
            Assert.Equal(1, Resolve(jobTitle));
        }

        [Theory]
        [InlineData("Java Engineer")]
        [InlineData("Java Programmer")]
        [InlineData("Java Dev")]
        [InlineData("Java SDE")]
        public void Engineer_developer_and_programmer_are_treated_as_the_same_role_word(string jobTitle)
        {
            Assert.Equal(1, Resolve(jobTitle));
        }

        [Fact]
        public void Extra_words_on_the_job_title_still_resolve_to_the_group_they_extend()
        {
            // The case the old exact match could never handle.
            Assert.Equal(2, Resolve("Sr. Backend Engineer (Kafka)"));
            Assert.Equal(2, Resolve("Backend Developer - Microservices, High Throughput"));
            Assert.Equal(3, Resolve("Frontend Developer (React)"));
        }

        [Fact]
        public void Word_order_does_not_matter()
        {
            Assert.Equal(7, Resolve("Analyst, Business"));
        }

        [Fact]
        public void A_small_typo_still_resolves()
        {
            Assert.Equal(1, Resolve("Java Devloper"));
        }

        [Fact]
        public void Plurals_fold_onto_the_singular()
        {
            var groups = new List<AssessmentGroupMatcher.Candidate>
            {
                new AssessmentGroupMatcher.Candidate { Id = 11, Name = "Embedded Systems Developer" }
            };

            Assert.Equal(11, AssessmentGroupMatcher.ResolveId("Embedded System Engineer", groups));
        }

        #endregion

        #region Not guessing

        [Fact]
        public void An_unrelated_role_falls_through_to_the_catch_all()
        {
            // 0 means "use the catch-all", which is the safe answer.
            Assert.Equal(0, Resolve("Registered Nurse"));
            Assert.Equal(0, Resolve("Commercial Real Estate Paralegal"));
        }

        [Fact]
        public void An_empty_or_missing_job_title_resolves_to_nothing()
        {
            Assert.Equal(0, Resolve(null));
            Assert.Equal(0, Resolve(""));
            Assert.Equal(0, Resolve("   "));
        }

        [Fact]
        public void No_assessment_groups_resolves_to_nothing()
        {
            Assert.Equal(0, AssessmentGroupMatcher.ResolveId(
                "Java Developer", new List<AssessmentGroupMatcher.Candidate>()));
            Assert.Equal(0, AssessmentGroupMatcher.ResolveId("Java Developer", null));
        }

        [Fact]
        public void Rows_with_no_name_are_skipped_rather_than_matched()
        {
            var groups = new List<AssessmentGroupMatcher.Candidate>
            {
                new AssessmentGroupMatcher.Candidate { Id = 20, Name = null },
                new AssessmentGroupMatcher.Candidate { Id = 21, Name = "   " },
                new AssessmentGroupMatcher.Candidate { Id = 22, Name = "Java Developer" }
            };

            Assert.Equal(22, AssessmentGroupMatcher.ResolveId("Senior Java Developer", groups));
        }

        [Fact]
        public void A_shared_role_word_alone_is_not_enough_to_match()
        {
            // "Developer" is common to several groups; on its own it should not pick one of them.
            var groups = new List<AssessmentGroupMatcher.Candidate>
            {
                new AssessmentGroupMatcher.Candidate { Id = 1, Name = "Java Developer" },
                new AssessmentGroupMatcher.Candidate { Id = 2, Name = "Backend Developer" }
            };

            Assert.Equal(0, AssessmentGroupMatcher.ResolveId("Salesforce CRM Developer", groups));
        }

        [Fact]
        public void Raising_the_threshold_makes_matching_stricter()
        {
            var groups = Groups();

            Assert.Equal(2, AssessmentGroupMatcher.ResolveId("Sr. Backend Engineer (Kafka)", groups));
            Assert.Equal(0, AssessmentGroupMatcher.ResolveId("Sr. Backend Engineer (Kafka)", groups, minimumScore: 0.99d));
        }

        #endregion

        #region Determinism

        [Fact]
        public void The_same_title_resolves_the_same_way_whatever_order_the_rows_arrive_in()
        {
            var forward = Groups();
            var reversed = Groups();
            reversed.Reverse();

            Assert.Equal(
                AssessmentGroupMatcher.ResolveId("Senior Backend Engineer", forward),
                AssessmentGroupMatcher.ResolveId("Senior Backend Engineer", reversed));
        }

        [Fact]
        public void An_equally_good_match_is_broken_by_the_shorter_name_then_the_lower_id()
        {
            var groups = new List<AssessmentGroupMatcher.Candidate>
            {
                new AssessmentGroupMatcher.Candidate { Id = 9, Name = "Java Developer" },
                new AssessmentGroupMatcher.Candidate { Id = 4, Name = "Java Engineer" }
            };

            // Both normalize to the same thing and score 1.0; the names are the same length, so the
            // lower id wins and the answer does not drift with row order.
            Assert.Equal(4, AssessmentGroupMatcher.ResolveId("Sr. Java Developer", groups));
            groups.Reverse();
            Assert.Equal(4, AssessmentGroupMatcher.ResolveId("Sr. Java Developer", groups));
        }

        [Fact]
        public void FindBest_reports_the_name_and_score_it_chose()
        {
            var match = AssessmentGroupMatcher.FindBest("Sr. Backend Engineer (Kafka)", Groups());

            Assert.NotNull(match);
            Assert.Equal("Backend Developer", match.Name);
            Assert.InRange(match.Score, 0.6d, 1.0d);
        }

        [Fact]
        public void Score_is_one_for_names_that_differ_only_by_seniority_and_role_wording()
        {
            Assert.Equal(1d, AssessmentGroupMatcher.Score("Sr. Java Engineer", "Java Developer"));
        }

        #endregion
    }
}
