using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Maps an AI screening evaluation onto <see cref="InterviewReport"/> — the model
    /// ClientReportV3 already renders — so the two-section screening report needs no second
    /// report view and no parallel schema.
    ///
    /// Section one lands in <see cref="InterviewReport.TechSkillReportCollection"/> and section two
    /// in <see cref="InterviewReport.TechSkillTwoReportCollection"/>, which is the split V3 already
    /// draws when SkillSetsevaluate is greater than 1.
    ///
    /// Section scores, the overall figure and each parameter score are computed here from the
    /// model's 1-5 ratings rather than taken from the evaluation JSON. Display the computed values;
    /// <see cref="AIScreeningScores.OverallDisagreesWithModel"/> says when the model's own overall
    /// figure disagrees, which is worth a warning in the log.
    /// </summary>
    public static class AIScreeningReportMapper
    {
        private const int SelectedStatus = 2;
        private const int RejectedStatus = 3;

        /// <summary>
        /// Fills <paramref name="report"/> from the channel's two-section rubric and the evaluator's
        /// ratings, and returns the computed scores.
        ///
        /// The rubric normally comes from the report itself, which already carries the channel's
        /// parameters and their weights; pass <paramref name="rubric"/> to override that.
        /// Returns null and leaves the report untouched when there is nothing to map.
        /// </summary>
        public static AIScreeningScores Apply(
            InterviewReport report,
            AIInterviewEvaluation evaluation,
            IEnumerable<TransSkill> rubric = null)
        {
            if (report == null || evaluation == null) return null;

            var rubricRows = ResolveRubric(report, rubric);
            if (rubricRows.Count == 0) return null;

            var ratings = BuildRatingLookup(evaluation);
            var scores = new AIScreeningScores();

            var roleRows = BuildSectionRows(rubricRows, AIScreeningSectionEnum.RoleFitment, ratings, scores);
            var motivationRows = BuildSectionRows(rubricRows, AIScreeningSectionEnum.MotivationToJoinFitment, ratings, scores);

            if (roleRows.Count == 0 && motivationRows.Count == 0) return null;

            report.TechSkillReportCollection = roleRows;
            report.TechSkillTwoReportCollection = motivationRows;
            report.SkillSetsevaluate = 2;

            // The two sections carry equal weight, so the headline figure is the mean of both. It is
            // deliberately not section one's score: a candidate who fits the role perfectly but will
            // not join must not read as a pass.
            scores.OverallScore = AIScreeningIntakeHelper.ComputeOverallScore(
                scores.RoleFitmentScore, scores.MotivationScore);

            report.OverallScore = (float)scores.OverallScore;

            if (evaluation.OverallTechnicalScore > 0)
            {
                scores.ModelReportedOverallScore = (decimal)evaluation.OverallTechnicalScore;
                scores.OverallDisagreesWithModel =
                    Math.Abs(scores.ModelReportedOverallScore - scores.OverallScore) > 0.5m;
            }

            scores.FinalVerdict = evaluation.FinalVerdict;
            scores.FinalComment = evaluation.FinalComment;

            if (!string.IsNullOrWhiteSpace(evaluation.FinalComment))
                report.FinalComments = evaluation.FinalComment;

            // One joint verdict for the whole screen, written to both sections so nothing on the
            // page can read as section one's outcome alone.
            var status = ResolveStatus(evaluation.FinalVerdict);
            if (status.HasValue)
            {
                report.InterviewStatus = status;
                report.InterviewStatusTwo = status;
                scores.InterviewStatus = status;
            }

            ApplyQuestions(report, evaluation, rubricRows);
            return scores;
        }

        /// <summary>Questions asked, split across the two sections by the parameter each one targeted.</summary>
        private static void ApplyQuestions(
            InterviewReport report, AIInterviewEvaluation evaluation, List<TransSkill> rubricRows)
        {
            if (evaluation.QuestionRatings == null || evaluation.QuestionRatings.Count == 0) return;

            var sectionByParameter = rubricRows
                .GroupBy(s => s.SkillName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().SkillSet, StringComparer.OrdinalIgnoreCase);

            var role = new List<QuestionsDiscussed>();
            var motivation = new List<QuestionsDiscussed>();

            foreach (var rating in evaluation.QuestionRatings)
            {
                if (rating == null || string.IsNullOrWhiteSpace(rating.Question)) continue;

                var parameter = MatchParameter(sectionByParameter.Keys, rating.Question);
                var section = parameter != null && sectionByParameter.TryGetValue(parameter, out var s)
                    ? s
                    : (int)AIScreeningSectionEnum.RoleFitment;

                var entry = new QuestionsDiscussed
                {
                    InterviewId = report.InterviewId,
                    Comment = rating.Question,
                    SkillName = parameter,
                    RatingId = (int)Math.Round(rating.Score, MidpointRounding.AwayFromZero),
                    QuestionStatus = rating.Verdict,
                    SkillSet = section
                };

                if (section == (int)AIScreeningSectionEnum.MotivationToJoinFitment) motivation.Add(entry);
                else role.Add(entry);
            }

            if (role.Count > 0) report.QuestionsCollection = role;
            if (motivation.Count > 0) report.QuestionsTwoCollection = motivation;
        }

        /// <summary>
        /// Rubric rows for one section, scored. An unrated parameter scores zero rather than being
        /// dropped, so the section's weights still total 100.
        /// </summary>
        private static List<TechSkillReport> BuildSectionRows(
            List<TransSkill> rubricRows,
            AIScreeningSectionEnum section,
            Dictionary<string, decimal> ratings,
            AIScreeningScores scores)
        {
            var rows = new List<TechSkillReport>();
            var sectionRubric = rubricRows.Where(s => s.SkillSet == (int)section).ToList();
            if (sectionRubric.Count == 0) return rows;

            var parameterRatings = new List<AIScreeningParameterRating>();

            foreach (var source in sectionRubric)
            {
                var name = source.SkillName.Trim();
                var score = LookupScore(ratings, name);

                var rating = new AIScreeningParameterRating
                {
                    ParameterName = name,
                    Section = section,
                    Weight = Math.Max(0, source.Weightage),
                    Score = score
                };
                parameterRatings.Add(rating);

                rows.Add(new TechSkillReport
                {
                    SkillName = name,
                    SkillSet = (int)section,
                    WeightageRequired = rating.Weight,
                    WeightageActual = (float)rating.ParameterScore,
                    RequiredLevel = source.LevelName,
                    RequiredLevelId = source.LevelId,
                    Concepts = source.Concepts,
                    Natureofquestion = source.Natureofquestion,
                    Timelimit = source.Timelimit > 0 ? source.Timelimit : 3,
                    isEliminationskill = source.isEliminationskill
                });
            }

            var sectionScore = AIScreeningIntakeHelper.ComputeSectionScore(parameterRatings);

            if (section == AIScreeningSectionEnum.RoleFitment)
            {
                scores.RoleFitmentScore = sectionScore;
                scores.RoleFitmentParameters = parameterRatings;
            }
            else
            {
                scores.MotivationScore = sectionScore;
                scores.MotivationParameters = parameterRatings;
            }

            return rows;
        }

        /// <summary>
        /// The channel's rubric. Preferred from the report's own collections, which
        /// MeetingService.GetInteviewReport has already filled from the channel and which carry
        /// SkillSet per row.
        /// </summary>
        private static List<TransSkill> ResolveRubric(InterviewReport report, IEnumerable<TransSkill> rubric)
        {
            if (rubric != null)
            {
                var supplied = rubric
                    .Where(s => s != null && !string.IsNullOrWhiteSpace(s.SkillName))
                    .ToList();
                if (supplied.Count > 0) return supplied;
            }

            var rows = new List<TransSkill>();
            rows.AddRange(ToTransSkills(report.TechSkillReportCollection, AIScreeningSectionEnum.RoleFitment));
            rows.AddRange(ToTransSkills(report.TechSkillTwoReportCollection, AIScreeningSectionEnum.MotivationToJoinFitment));
            return rows;
        }

        private static IEnumerable<TransSkill> ToTransSkills(
            IEnumerable<TechSkillReport> source, AIScreeningSectionEnum fallbackSection)
        {
            return (source ?? Enumerable.Empty<TechSkillReport>())
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.SkillName))
                .Select(r => new TransSkill
                {
                    SkillName = r.SkillName,
                    Weightage = (int)Math.Round(r.WeightageRequired, MidpointRounding.AwayFromZero),
                    LevelId = r.RequiredLevelId,
                    LevelName = r.RequiredLevel,
                    Concepts = r.Concepts,
                    Natureofquestion = r.Natureofquestion,
                    Timelimit = r.Timelimit,
                    isEliminationskill = r.isEliminationskill,
                    // Rows written before SkillSet was carried through fall back to their collection.
                    SkillSet = r.SkillSet > 0 ? r.SkillSet : (int)fallbackSection
                });
        }

        private static int? ResolveStatus(string verdict)
        {
            if (string.IsNullOrWhiteSpace(verdict)) return null;
            if (AIInterviewVerdict.IsSelectFamily(verdict)) return SelectedStatus;
            if (AIInterviewVerdict.IsRejectFamily(verdict)) return RejectedStatus;
            return null;
        }

        private static Dictionary<string, decimal> BuildRatingLookup(AIInterviewEvaluation evaluation)
        {
            var lookup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (evaluation?.RubricScoreSummary == null) return lookup;

            foreach (var item in evaluation.RubricScoreSummary)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.RubricItem)) continue;
                lookup[item.RubricItem.Trim()] = (decimal)item.Score;
            }

            return lookup;
        }

        /// <summary>
        /// Exact name match first, then a contains match either way — the evaluator commonly returns
        /// a rubric item worded slightly differently from the parameter it scored.
        /// </summary>
        private static decimal LookupScore(Dictionary<string, decimal> ratings, string parameterName)
        {
            if (ratings.Count == 0 || string.IsNullOrWhiteSpace(parameterName)) return 0m;

            var name = parameterName.Trim();
            if (ratings.TryGetValue(name, out var exact)) return exact;

            foreach (var pair in ratings)
            {
                if (name.Contains(pair.Key, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return 0m;
        }

        private static string MatchParameter(IEnumerable<string> parameters, string question)
        {
            return parameters.FirstOrDefault(p =>
                question.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    /// <summary>
    /// The computed figures behind a screening report. The report itself carries what V3 renders;
    /// this carries what the caller needs to log or to render in the screening-only overall block.
    /// </summary>
    public class AIScreeningScores
    {
        /// <summary>Out of 100, computed from the parameters' weights and 1-5 ratings.</summary>
        public decimal RoleFitmentScore { get; set; }

        /// <summary>Out of 100, computed independently of section one.</summary>
        public decimal MotivationScore { get; set; }

        /// <summary>Mean of both section scores. Not section one's score.</summary>
        public decimal OverallScore { get; set; }

        /// <summary>What the evaluation JSON claimed, kept only to detect disagreement.</summary>
        public decimal ModelReportedOverallScore { get; set; }

        /// <summary>True when the model's own overall figure differs from the computed one.</summary>
        public bool OverallDisagreesWithModel { get; set; }

        public string FinalVerdict { get; set; }
        public string FinalComment { get; set; }

        /// <summary>2 = selected, 3 = rejected, null = neither could be read from the verdict.</summary>
        public int? InterviewStatus { get; set; }

        public List<AIScreeningParameterRating> RoleFitmentParameters { get; set; } = new List<AIScreeningParameterRating>();
        public List<AIScreeningParameterRating> MotivationParameters { get; set; } = new List<AIScreeningParameterRating>();
    }
}
