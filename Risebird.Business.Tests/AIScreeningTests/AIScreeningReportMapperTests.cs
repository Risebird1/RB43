using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The screening report is mapped onto InterviewReport — the model ClientReportV3 already
    /// renders — so there is one renderer and no parallel schema. Scores are computed here from the
    /// model's 1-5 ratings, not taken from the evaluation JSON, and the headline figure is a joint
    /// judgement across both sections.
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

        private static AIInterviewEvaluation FullMarks() => Evaluation(
            ("Core Tech Stack", 5),
            ("Relevant Experience", 5),
            ("Compensation Feasibility", 5),
            ("Location Fit", 5));

        private static InterviewReport Report() => new InterviewReport { InterviewId = 88 };

        #region Mapping onto InterviewReport

        [Fact]
        public void The_two_sections_land_in_the_two_collections_ClientReportV3_renders()
        {
            var report = Report();

            AIScreeningReportMapper.Apply(report, FullMarks(), Rubric());

            Assert.Equal(2, report.TechSkillReportCollection.Count);
            Assert.Equal(2, report.TechSkillTwoReportCollection.Count);

            Assert.All(report.TechSkillReportCollection,
                r => Assert.Equal((int)AIScreeningSectionEnum.RoleFitment, r.SkillSet));
            Assert.All(report.TechSkillTwoReportCollection,
                r => Assert.Equal((int)AIScreeningSectionEnum.MotivationToJoinFitment, r.SkillSet));
        }

        [Fact]
        public void SkillSetsevaluate_is_set_so_the_view_draws_the_second_section()
        {
            var report = Report();

            AIScreeningReportMapper.Apply(report, FullMarks(), Rubric());

            // ClientReportV3 gates the second section on this being greater than 1.
            Assert.Equal(2, report.SkillSetsevaluate);
        }

        [Fact]
        public void The_rubric_is_taken_from_the_report_itself_when_none_is_supplied()
        {
            // MeetingService already fills these from the channel, split by section.
            var report = Report();
            report.TechSkillReportCollection = new List<TechSkillReport>
            {
                new TechSkillReport { SkillName = "Core Tech Stack", WeightageRequired = 60, SkillSet = 1 },
                new TechSkillReport { SkillName = "Relevant Experience", WeightageRequired = 40, SkillSet = 1 }
            };
            report.TechSkillTwoReportCollection = new List<TechSkillReport>
            {
                new TechSkillReport { SkillName = "Compensation Feasibility", WeightageRequired = 70, SkillSet = 2 },
                new TechSkillReport { SkillName = "Location Fit", WeightageRequired = 30, SkillSet = 2 }
            };

            var scores = AIScreeningReportMapper.Apply(report, FullMarks());

            Assert.NotNull(scores);
            Assert.Equal(100m, scores.RoleFitmentScore);
            Assert.Equal(100m, scores.MotivationScore);
        }

        [Fact]
        public void Each_parameter_row_carries_its_earned_score()
        {
            var report = Report();

            AIScreeningReportMapper.Apply(report, Evaluation(
                ("Core Tech Stack", 4),
                ("Relevant Experience", 3),
                ("Compensation Feasibility", 5),
                ("Location Fit", 2)), Rubric());

            // weight x (score / 5): 60 x 4/5 = 48
            Assert.Equal(48f, report.TechSkillReportCollection.Single(r => r.SkillName == "Core Tech Stack").WeightageActual);
            Assert.Equal(60f, report.TechSkillReportCollection.Single(r => r.SkillName == "Core Tech Stack").WeightageRequired);
        }

        #endregion

        #region Scoring

        [Fact]
        public void Each_section_scores_out_of_100_from_the_one_to_five_ratings()
        {
            var report = Report();

            var scores = AIScreeningReportMapper.Apply(report, FullMarks(), Rubric());

            Assert.Equal(100m, scores.RoleFitmentScore);
            Assert.Equal(100m, scores.MotivationScore);
            Assert.Equal(100m, scores.OverallScore);
            Assert.Equal(100f, report.OverallScore);
        }

        [Fact]
        public void A_perfect_role_fit_who_will_not_join_does_not_read_as_a_pass()
        {
            var report = Report();

            var scores = AIScreeningReportMapper.Apply(report, Evaluation(
                ("Core Tech Stack", 5),
                ("Relevant Experience", 5),
                ("Compensation Feasibility", 1),
                ("Location Fit", 1)), Rubric());

            Assert.Equal(100m, scores.RoleFitmentScore);
            Assert.Equal(20m, scores.MotivationScore);

            // The mean of both, not section one's.
            Assert.Equal(60m, scores.OverallScore);
            Assert.Equal(60f, report.OverallScore);
        }

        [Fact]
        public void An_unrated_parameter_scores_zero_rather_than_being_dropped()
        {
            var report = Report();

            var scores = AIScreeningReportMapper.Apply(report, Evaluation(("Core Tech Stack", 5)), Rubric());

            // Both parameters are still present, so the section's weights still total 100.
            Assert.Equal(2, report.TechSkillReportCollection.Count);
            Assert.Equal(100f, report.TechSkillReportCollection.Sum(r => r.WeightageRequired));
            Assert.Equal(60m, scores.RoleFitmentScore);
        }

        [Fact]
        public void A_disagreement_with_the_models_own_overall_figure_is_flagged()
        {
            var evaluation = Evaluation(
                ("Core Tech Stack", 5),
                ("Relevant Experience", 5),
                ("Compensation Feasibility", 1),
                ("Location Fit", 1));
            evaluation.OverallTechnicalScore = 95f;

            var scores = AIScreeningReportMapper.Apply(Report(), evaluation, Rubric());

            Assert.Equal(60m, scores.OverallScore);
            Assert.True(scores.OverallDisagreesWithModel);
        }

        [Fact]
        public void No_disagreement_is_flagged_when_the_model_agrees()
        {
            var evaluation = FullMarks();
            evaluation.OverallTechnicalScore = 100f;

            var scores = AIScreeningReportMapper.Apply(Report(), evaluation, Rubric());

            Assert.False(scores.OverallDisagreesWithModel);
        }

        #endregion

        #region Verdict and comments

        [Fact]
        public void The_joint_verdict_is_written_to_both_sections_so_neither_reads_alone()
        {
            var evaluation = FullMarks();
            evaluation.FinalVerdict = "Strongly Selected";
            evaluation.FinalComment = "Strong on both counts.";

            var report = Report();
            var scores = AIScreeningReportMapper.Apply(report, evaluation, Rubric());

            Assert.Equal(2, report.InterviewStatus);
            Assert.Equal(2, report.InterviewStatusTwo);
            Assert.Equal(2, scores.InterviewStatus);
            Assert.Equal("Strong on both counts.", report.FinalComments);
        }

        [Fact]
        public void A_rejection_verdict_maps_to_the_rejected_status()
        {
            var evaluation = FullMarks();
            evaluation.FinalVerdict = "Strongly Rejected";

            var report = Report();

            AIScreeningReportMapper.Apply(report, evaluation, Rubric());

            Assert.Equal(3, report.InterviewStatus);
            Assert.Equal(3, report.InterviewStatusTwo);
        }

        [Fact]
        public void An_unreadable_verdict_leaves_the_status_alone()
        {
            var evaluation = FullMarks();
            evaluation.FinalVerdict = "Mixed feelings";

            var report = Report();
            report.InterviewStatus = 9;

            AIScreeningReportMapper.Apply(report, evaluation, Rubric());

            Assert.Equal(9, report.InterviewStatus);
        }

        #endregion

        #region Questions

        [Fact]
        public void Questions_are_split_across_the_sections_by_the_parameter_they_targeted()
        {
            var evaluation = FullMarks();
            evaluation.QuestionRatings = new List<QuestionRating>
            {
                new QuestionRating { Question = "Walk me through your Core Tech Stack experience.", Score = 4, Verdict = "Correct" },
                new QuestionRating { Question = "What is your Location Fit for this role?", Score = 2, Verdict = "Partially Correct" }
            };

            var report = Report();

            AIScreeningReportMapper.Apply(report, evaluation, Rubric());

            Assert.Single(report.QuestionsCollection);
            Assert.Single(report.QuestionsTwoCollection);
            Assert.Equal("Core Tech Stack", report.QuestionsCollection.First().SkillName);
            Assert.Equal(88, report.QuestionsCollection.First().InterviewId);
            Assert.Equal("Location Fit", report.QuestionsTwoCollection.First().SkillName);
        }

        #endregion

        #region Nothing to map

        [Fact]
        public void A_report_with_no_rubric_is_left_untouched()
        {
            var report = Report();

            var scores = AIScreeningReportMapper.Apply(report, FullMarks());

            Assert.Null(scores);
            Assert.Equal(0, report.SkillSetsevaluate);
        }

        [Fact]
        public void A_null_evaluation_or_report_is_handled()
        {
            Assert.Null(AIScreeningReportMapper.Apply(Report(), null, Rubric()));
            Assert.Null(AIScreeningReportMapper.Apply(null, FullMarks(), Rubric()));
        }

        #endregion
    }
}
