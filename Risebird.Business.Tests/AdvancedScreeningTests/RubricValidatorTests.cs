using System.Linq;
using Risebird.Business.AdvancedScreening;
using Risebird.Entities.ViewModel.AdvancedScreening;

namespace Risebird.Business.Tests.AdvancedScreeningTests
{
    /// <summary>
    /// The validator enforces two rules - all 27 keys, total exactly 300 - and the repair must
    /// keep that total exact after rounding, or an approvable rubric could never be generated.
    /// </summary>
    public class RubricValidatorTests
    {
        [Fact]
        public void The_catalog_has_27_unique_keys_in_three_sections_whose_defaults_total_300()
        {
            Assert.Equal(27, RubricCriteria.All.Count);
            Assert.Equal(27, RubricCriteria.All.Select(c => c.Key).Distinct().Count());
            Assert.Equal(5, RubricCriteria.All.Count(c => c.Section == "Skills"));
            Assert.Equal(16, RubricCriteria.All.Count(c => c.Section == "Work"));
            Assert.Equal(6, RubricCriteria.All.Count(c => c.Section == "Education"));
            Assert.Equal(110, RubricCriteria.All.Where(c => c.Section == "Skills").Sum(c => c.DefaultWeight));
            Assert.Equal(145, RubricCriteria.All.Where(c => c.Section == "Work").Sum(c => c.DefaultWeight));
            Assert.Equal(45, RubricCriteria.All.Where(c => c.Section == "Education").Sum(c => c.DefaultWeight));
            Assert.Equal(300, RubricCriteria.All.Sum(c => c.DefaultWeight));
        }

        [Fact]
        public void The_default_rubric_is_valid()
        {
            Assert.Empty(RubricValidator.Validate(RubricCriteria.DefaultRubric()));
        }

        [Fact]
        public void A_missing_key_is_reported()
        {
            var rubric = RubricCriteria.DefaultRubric();
            rubric.Criteria.RemoveAll(c => c.Key == "kras");
            rubric.Criteria.Single(c => c.Key == "kpis").Weight += 5;

            var errors = RubricValidator.Validate(rubric);

            Assert.Contains(errors, e => e.Contains("KRAs"));
            Assert.Equal(new[] { "kras" }, RubricValidator.MissingKeys(rubric));
        }

        [Fact]
        public void A_total_other_than_300_is_reported()
        {
            var rubric = RubricCriteria.DefaultRubric();
            rubric.Criteria.Single(c => c.Key == "jdTitle").Weight = 7;   // 297

            Assert.Contains(RubricValidator.Validate(rubric), e => e.Contains("297"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(7)]
        public void Weight_repair_totals_exactly_300_after_rounding(int multiplier)
        {
            var rubric = RubricCriteria.DefaultRubric();
            // Awkward proportions that do not divide evenly into 300.
            foreach (var (c, i) in rubric.Criteria.Select((c, i) => (c, i)))
            {
                c.Weight = (i % 4 == 0) ? 0 : (i * multiplier % 13) + 1;
            }

            RubricValidator.RepairWeights(rubric);

            Assert.Equal(300, rubric.Criteria.Sum(c => c.Weight));
            Assert.Equal(300, rubric.TotalWeight);
            Assert.Empty(RubricValidator.Validate(rubric));
        }

        [Fact]
        public void Weight_repair_keeps_zero_weights_at_zero()
        {
            var rubric = RubricCriteria.DefaultRubric();
            rubric.Criteria.Single(c => c.Key == "certifications").Weight = 0;
            rubric.Criteria.Single(c => c.Key == "primarySkills").Weight = 90;   // 340

            RubricValidator.RepairWeights(rubric);

            Assert.Equal(0, rubric.Criteria.Single(c => c.Key == "certifications").Weight);
            Assert.Equal(RubricCriteria.NotUsedText, rubric.Criteria.Single(c => c.Key == "certifications").Expected);
            Assert.Equal(300, rubric.Criteria.Sum(c => c.Weight));
        }

        [Fact]
        public void Weight_repair_falls_back_to_the_defaults_when_every_weight_is_zero()
        {
            var rubric = RubricCriteria.DefaultRubric();
            foreach (var c in rubric.Criteria) c.Weight = 0;

            RubricValidator.RepairWeights(rubric);

            Assert.Equal(300, rubric.Criteria.Sum(c => c.Weight));
            Assert.Equal(50, rubric.Criteria.Single(c => c.Key == "primarySkills").Weight);
        }

        [Fact]
        public void Normalize_takes_sections_and_labels_from_the_catalog_and_fills_not_used_text()
        {
            var rubric = new ScreeningRubric
            {
                JdTitle = "  Vue Developer ",
                Criteria = RubricCriteria.All.Reverse().Select(d => new RubricCriterion
                {
                    Key = d.Key.ToUpperInvariant(),
                    Section = "wrong",
                    Label = "wrong",
                    Expected = d.Key == "certifications" ? "unknown" : "  bar  ",
                    Weight = d.Key == "certifications" ? 0 : d.DefaultWeight
                }).Concat(new[] { new RubricCriterion { Key = "madeUp", Weight = 5 } }).ToList()
            };

            var n = RubricValidator.Normalize(rubric);

            Assert.Equal("Vue Developer", n.JdTitle);
            Assert.Equal(RubricCriteria.All.Select(d => d.Key), n.Criteria.Select(c => c.Key));
            Assert.All(n.Criteria, c => Assert.Equal(RubricCriteria.Find(c.Key).Section, c.Section));
            Assert.All(n.Criteria, c => Assert.Equal(RubricCriteria.Find(c.Key).Label, c.Label));
            Assert.Equal(RubricCriteria.NotUsedText, n.Criteria.Single(c => c.Key == "certifications").Expected);
            Assert.Equal("bar", n.Criteria.Single(c => c.Key == "jdTitle").Expected);
            Assert.DoesNotContain(n.Criteria, c => c.Key == "madeUp");
            Assert.Equal(295, n.TotalWeight);
        }

        [Fact]
        public void Parse_reads_a_fenced_rubric()
        {
            var raw = "```json\n{\"jdTitle\":\"X\",\"criteria\":[{\"key\":\"jdTitle\",\"section\":\"Skills\",\"label\":\"JD Title\",\"expected\":\"X dev\",\"weight\":10}],\"dealbreakers\":[\"No X\"],\"totalWeight\":10}\n```";

            var rubric = RubricValidator.Parse(raw);

            Assert.Equal("X", rubric.JdTitle);
            Assert.Single(rubric.Criteria);
            Assert.Equal("No X", rubric.Dealbreakers.Single());
        }
    }
}
