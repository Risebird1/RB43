using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The screening report is computed from the model's 1-5 ratings, not taken from the evaluation
    /// JSON, and its headline figure is a joint judgement across both sections.
    /// </summary>
    public class AIScreeningReportMapperTests
    {
        private static TransSkill Row(string name, int weight, AIScreeningSectionEnum section) =>
            new TransSkill { SkillName = name, Weightage = weight, SkillSet = (int)section, Timelimit = 3 };

        private static List<TransSkill> Rubric() => new List<TransSkill>
        {
            Row("Core Tech Stack", 60, AIScreeningSectionEnum.RoleFitment),
            Row("Relevant Experience", 40, AIScreeningSectionEnum.RoleFitment),
            Row("Compensation Feasibility", 70, AIScreeningSectionEnum.MotivationToJoinFitment),
            Row("Location Fit", 30, AIScreeningSectionEnum.MotivationToJoinFitment)
        };

        private static AIInterviewEvaluation Evaluation(params (string item, float score)[] ratings) =>
            new AIInterviewEvaluation
            {
                RubricScoreSummary = ratings
                    .Select(r => new RubricScoreSummary { RubricItem = r.item, Score = r.score })
                    .ToList()
            };

        [Fact]
        public void Build_scores_each_section_out_of_100_from_the_one_to_five_ratings()
        {
            var report = AIScreeningReportMapper.Build(Rubric(), Evaluation(
                ("Core Tech Stack", 5),          // 60
                ("Relevant Experience", 5),      // 40
                ("Compensation Feasibility", 5), // 70
                ("Location Fit", 5)));           // 30

            Assert.Equal(100m, report.GetSection(AIScreeningSectionEnum.RoleFitment).Score);
            Assert.Equal(100m, report.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).Score);
            Assert.Equal(100m, report.OverallScore);
        }

        [Fact]
        public void Build_overall_is_the_mean_of_both_sections_so_a_perfect_role_fit_who_will_not_join_does_not_pass()
        {
            var report = AIScreeningReportMapper.Build(Rubric(), Evaluation(
                ("Core Tech Stack", 5),
                ("Relevant Experience", 5),
                ("Compensation Feasibility", 1),
                ("Location Fit", 1)));

            Assert.Equal(100m, report.GetSection(AIScreeningSectionEnum.RoleFitment).Score);
            Assert.Equal(20m, report.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment).Score);

            // Not 100 — the candidate can do the job but will not take it.
            Assert.Equal(60m, report.OverallScore);
        }

        [Fact]
        public void Build_scores_an_unrated_parameter_zero_rather_than_dropping_it()
        {
            var report = AIScreeningReportMapper.Build(Rubric(), Evaluation(
                ("Core Tech Stack", 5)));

            var role = report.GetSection(AIScreeningSectionEnum.RoleFitment);

            // Both parameters are still present, so the section's weights still total 100.
            Assert.Equal(2, role.Parameters.Count);
            Assert.Equal(100, role.Parameters.Sum(p => p.Weight));
            Assert.Equal(60m, role.Score);
        }

        [Fact]
        public void Build_flags_when_the_models_own_overall_figure_disagrees_with_the_computed_one()
        {
            var evaluation = Evaluation(
                ("Core Tech Stack", 5),
                ("Relevant Experience", 5),
                ("Compensation Feasibility", 1),
                ("Location Fit", 1));
            evaluation.OverallTechnicalScore = 95f;

            var report = AIScreeningReportMapper.Build(Rubric(), evaluation);

            Assert.Equal(60m, report.OverallScore);
            Assert.True(report.OverallDisagreesWithModel);
        }

        [Fact]
        public void Build_does_not_flag_disagreement_when_the_model_agrees()
        {
            var evaluation = Evaluation(
                ("Core Tech Stack", 5),
                ("Relevant Experience", 5),
                ("Compensation Feasibility", 5),
                ("Location Fit", 5));
            evaluation.OverallTechnicalScore = 100f;

            var report = AIScreeningReportMapper.Build(Rubric(), evaluation);

            Assert.False(report.OverallDisagreesWithModel);
        }

        [Fact]
        public void ToTechSkillReports_carries_the_section_so_ClientReportV3_can_group_on_it()
        {
            var rubric = Rubric();
            var report = AIScreeningReportMapper.Build(rubric, Evaluation(
                ("Core Tech Stack", 4),
                ("Relevant Experience", 3),
                ("Compensation Feasibility", 5),
                ("Location Fit", 2)));

            var rows = AIScreeningReportMapper.ToTechSkillReports(report, rubric);

            Assert.Equal(4, rows.Count);
            Assert.Equal(2, rows.Count(r => r.SkillSet == (int)AIScreeningSectionEnum.RoleFitment));
            Assert.Equal(2, rows.Count(r => r.SkillSet == (int)AIScreeningSectionEnum.MotivationToJoinFitment));

            // weight x (score / 5): 60 x 4/5 = 48
            Assert.Equal(48f, rows.Single(r => r.SkillName == "Core Tech Stack").WeightageActual);
        }

        [Fact]
        public void Build_returns_an_empty_report_when_the_channel_has_no_rubric()
        {
            var report = AIScreeningReportMapper.Build(new List<TransSkill>(), Evaluation(("anything", 5)));

            Assert.Empty(report.Sections);
            Assert.Equal(0m, report.OverallScore);
        }
    }
}
