using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Maps an AI screening evaluation onto the structures ClientReportV3 already renders, so the
    /// two-section screening report needs no new report view.
    ///
    /// Section scores, the overall figure and each parameter score are computed here from the
    /// model's 1-5 ratings rather than taken from the evaluation JSON. Display the computed values;
    /// <see cref="AIScreeningReport.OverallDisagreesWithModel"/> says when the model's own overall
    /// figure disagrees, which is worth a warning in the log.
    /// </summary>
    public static class AIScreeningReportMapper
    {
        /// <summary>
        /// Builds the report from the channel's two-section rubric and the evaluator's ratings.
        /// Rubric rows are matched to evaluation items by name; an unrated parameter scores zero
        /// rather than being dropped, so a section's weights still total 100.
        /// </summary>
        public static AIScreeningReport Build(
            IEnumerable<TransSkill> rubric,
            AIInterviewEvaluation evaluation)
        {
            var report = new AIScreeningReport();

            var rubricRows = (rubric ?? Enumerable.Empty<TransSkill>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.SkillName))
                .ToList();

            if (rubricRows.Count == 0) return report;

            var ratings = BuildRatingLookup(evaluation);

            foreach (AIScreeningSectionEnum section in System.Enum.GetValues(typeof(AIScreeningSectionEnum)))
            {
                var rows = rubricRows.Where(s => s.SkillSet == (int)section).ToList();
                if (rows.Count == 0) continue;

                var sectionResult = new AIScreeningReportSection
                {
                    Section = section,
                    Title = AIScreeningIntakeHelper.GetSectionTitle(section)
                };

                foreach (var row in rows)
                {
                    var score = LookupScore(ratings, row.SkillName);

                    sectionResult.Parameters.Add(new AIScreeningParameterRating
                    {
                        ParameterName = row.SkillName.Trim(),
                        Section = section,
                        Weight = Math.Max(0, row.Weightage),
                        Score = score
                    });
                }

                sectionResult.Score = AIScreeningIntakeHelper.ComputeSectionScore(sectionResult.Parameters);
                report.Sections.Add(sectionResult);
            }

            if (report.Sections.Count == 0) return report;

            var roleFitment = report.GetSection(AIScreeningSectionEnum.RoleFitment)?.Score ?? 0m;
            var motivation = report.GetSection(AIScreeningSectionEnum.MotivationToJoinFitment)?.Score ?? 0m;

            // The two sections carry equal weight, so the headline figure is the mean of both.
            // It is deliberately not section one's score: a candidate who fits the role perfectly
            // but will not join must not read as a pass.
            report.OverallScore = AIScreeningIntakeHelper.ComputeOverallScore(roleFitment, motivation);

            if (evaluation != null && evaluation.OverallTechnicalScore > 0)
            {
                report.ModelReportedOverallScore = (decimal)evaluation.OverallTechnicalScore;
                report.OverallDisagreesWithModel =
                    Math.Abs(report.ModelReportedOverallScore - report.OverallScore) > 0.5m;
            }

            report.FinalVerdict = evaluation?.FinalVerdict;
            report.FinalComment = evaluation?.FinalComment;
            return report;
        }

        /// <summary>
        /// Rubric rows as TechSkillReport entries carrying their section, which is what
        /// ClientReportV3 groups on when SkillSetsevaluate is greater than 1.
        /// </summary>
        public static List<TechSkillReport> ToTechSkillReports(
            AIScreeningReport report,
            IEnumerable<TransSkill> rubric)
        {
            var rows = new List<TechSkillReport>();
            if (report?.Sections == null) return rows;

            var rubricByName = (rubric ?? Enumerable.Empty<TransSkill>())
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.SkillName))
                .GroupBy(s => s.SkillName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var section in report.Sections)
            {
                foreach (var parameter in section.Parameters ?? new List<AIScreeningParameterRating>())
                {
                    rubricByName.TryGetValue(parameter.ParameterName ?? string.Empty, out var source);

                    rows.Add(new TechSkillReport
                    {
                        SkillName = parameter.ParameterName,
                        SkillSet = (int)section.Section,
                        WeightageRequired = parameter.Weight,
                        WeightageActual = (float)parameter.ParameterScore,
                        RequiredLevel = source?.LevelName,
                        RequiredLevelId = source?.LevelId ?? 0,
                        Concepts = source?.Concepts,
                        Natureofquestion = source?.Natureofquestion,
                        Timelimit = source != null && source.Timelimit > 0 ? source.Timelimit : 3,
                        isEliminationskill = source?.isEliminationskill ?? false
                    });
                }
            }

            return rows;
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
    }

    /// <summary>The computed two-section screening result.</summary>
    public class AIScreeningReport
    {
        public List<AIScreeningReportSection> Sections { get; set; } = new List<AIScreeningReportSection>();

        /// <summary>Mean of both section scores. Not section one's score.</summary>
        public decimal OverallScore { get; set; }

        /// <summary>What the evaluation JSON claimed, kept only to detect disagreement.</summary>
        public decimal ModelReportedOverallScore { get; set; }

        /// <summary>True when the model's own overall figure differs from the computed one.</summary>
        public bool OverallDisagreesWithModel { get; set; }

        public string FinalVerdict { get; set; }
        public string FinalComment { get; set; }

        public AIScreeningReportSection GetSection(AIScreeningSectionEnum section) =>
            Sections?.FirstOrDefault(s => s.Section == section);
    }

    /// <summary>One scored section of the screening report.</summary>
    public class AIScreeningReportSection
    {
        public AIScreeningSectionEnum Section { get; set; }
        public string Title { get; set; }

        public List<AIScreeningParameterRating> Parameters { get; set; } = new List<AIScreeningParameterRating>();

        /// <summary>Out of 100, computed from the parameters' weights and 1-5 ratings.</summary>
        public decimal Score { get; set; }
    }
}
