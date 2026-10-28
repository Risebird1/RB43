using Risebird.Business;
using Risebird.Entities.ViewModel;
using Xunit;

namespace Risebird.Business.Tests
{
    public class AIQuestionSetDealerTests
    {
        private static readonly IList<AIQuestionSetRubricParameterVM> Params = new List<AIQuestionSetRubricParameterVM>
        {
            new() { SkillName = "Java", ParameterOrder = 1 },
            new() { SkillName = "React", ParameterOrder = 2 },
            new() { SkillName = "SQL", ParameterOrder = 3 }
        };

        private static AIQuestionPoolItemVM Q(int id, string skill, bool coding = false) =>
            new AIQuestionPoolItemVM { id = id, SkillName = skill, IsCodingQuestion = coding };

        [Fact]
        public void Deals_only_unused_questions_one_per_parameter_per_set()
        {
            var pool = new[]
            {
                Q(1, "Java"), Q(2, "Java"), Q(3, "Java"), Q(4, "Java"),
                Q(11, "React"), Q(12, "React"), Q(13, "React"),
                Q(21, "SQL"), Q(22, "SQL"), Q(23, "SQL")
            };
            var used = new HashSet<int> { 1, 11, 21 };   // set A already holds these

            var deal = AIQuestionSetDealer.DealNewSets(Params, pool, used, new HashSet<int>());

            Assert.Equal(2, deal.Sets.Count);   // React and SQL have 2 unused each
            Assert.All(deal.Sets, s => Assert.Equal(new[] { "Java", "React", "SQL" }, s.Select(q => q.SkillName).OrderBy(x => x)));
            var ids = deal.Sets.SelectMany(s => s).Select(q => q.id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.DoesNotContain(ids, used.Contains);
        }

        [Fact]
        public void Code_project_questions_are_always_dealt_and_spread_across_sets()
        {
            // React has 3 unused, so 3 sets; Java has 5 unused including two code projects (ids 9, 10).
            var pool = new[]
            {
                Q(5, "Java"), Q(6, "Java"), Q(7, "Java", coding: true), Q(9, "Java"), Q(10, "Java"),
                Q(11, "React"), Q(12, "React"), Q(13, "React"),
                Q(21, "SQL"), Q(22, "SQL"), Q(23, "SQL"), Q(24, "SQL", coding: true)
            };
            var projects = new HashSet<int> { 9, 10, 24 };

            var deal = AIQuestionSetDealer.DealNewSets(Params, pool, new HashSet<int>(), projects);

            Assert.Equal(3, deal.Sets.Count);
            var dealt = deal.Sets.SelectMany(s => s).Select(q => q.id).ToHashSet();
            Assert.Superset(projects, dealt);                     // every code project question made it in
            Assert.Contains(7, dealt);                            // then the coding question
            // Three code project questions over three sets: one each.
            Assert.All(deal.Sets, s => Assert.Single(s, q => projects.Contains(q.id)));
        }

        [Fact]
        public void Names_the_parameter_with_no_unused_question()
        {
            var pool = new[] { Q(1, "Java"), Q(2, "Java"), Q(11, "React"), Q(21, "SQL") };
            var deal = AIQuestionSetDealer.DealNewSets(Params, pool, new HashSet<int> { 11 }, new HashSet<int>());
            Assert.Empty(deal.Sets);
            Assert.Equal("React", deal.EmptyParameter);
        }

        [Theory]
        [InlineData("A", 0)]
        [InlineData("C", 2)]
        [InlineData("Z", 25)]
        [InlineData("AA", 26)]
        [InlineData("AB", 27)]
        [InlineData("x1", -1)]
        public void Set_names_map_back_to_their_position(string name, int index) =>
            Assert.Equal(index, AIQuestionSetDealer.SetNameIndex(name));
    }
}
