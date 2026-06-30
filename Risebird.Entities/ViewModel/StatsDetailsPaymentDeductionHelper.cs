using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class StatsDetailsPaymentDeductionDisplay
    {
        public string StatusLabel { get; set; }
        public int? DeductionPercent { get; set; }
        public IReadOnlyList<string> RedFlagTags { get; set; } = Array.Empty<string>();
        public bool IsClickable { get; set; }
    }

    public static class StatsDetailsPaymentDeductionHelper
    {
        public const string StatusDeducted = "Deducted";
        public const string StatusFlags = "Flags";

        public static StatsDetailsPaymentDeductionDisplay ResolveDisplay(StatsDetailsPaymentDeductionSummary summary)
        {
            if (summary == null)
                return EmptyDisplay();

            var tags = CollectRedFlagTags(summary);
            var hasDeduction = summary.PaymentDeduction.HasValue && summary.PaymentDeduction.Value > 0;
            var hasFlags = tags.Count > 0
                || (summary.PaymentComplianceCount.HasValue && summary.PaymentComplianceCount.Value > 0);

            if (!hasDeduction && !hasFlags)
                return EmptyDisplay();

            return new StatsDetailsPaymentDeductionDisplay
            {
                StatusLabel = hasDeduction ? StatusDeducted : StatusFlags,
                DeductionPercent = hasDeduction ? summary.PaymentDeduction : null,
                RedFlagTags = tags,
                IsClickable = true
            };
        }

        private static StatsDetailsPaymentDeductionDisplay EmptyDisplay() =>
            new StatsDetailsPaymentDeductionDisplay { IsClickable = false };

        private static List<string> CollectRedFlagTags(StatsDetailsPaymentDeductionSummary s)
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AppendCsv(tags, s.NonAIreasoning);
            AppendCsv(tags, s.Riskcategory);

            AppendCsv(tags, s.Inaccuratedecisionmaking);
            AppendCsv(tags, s.TimeManagement);
            AppendCsv(tags, s.QuestionsRelevance);
            AppendCsv(tags, s.ScoreRubricIgnorance);
            AppendCsv(tags, s.PoorFeedbackComments);
            AppendCsv(tags, s.CandidateInteractionQuality);
            AppendCsv(tags, s.LimitedHandoncoding);
            AppendCsv(tags, s.WrongStatusUpdates);
            AppendCsv(tags, s.WeakInfrastructure);
            AppendCsv(tags, s.ProctoringIgnorance);
            AppendCsv(tags, s.UnethicalPractices);
            AppendCsv(tags, s.PoorCommunicationSkills);
            AppendCsv(tags, s.InterviewerVideo);
            AppendCsv(tags, s.AttendanceJoinCompliance);
            AppendCsv(tags, s.AIContradictionScore);

            return tags.Where(t => !string.IsNullOrWhiteSpace(t) && !IsNoiseTag(t)).OrderBy(t => t).ToList();
        }

        private static bool IsNoiseTag(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            switch (value.Trim().ToLowerInvariant())
            {
                case "0":
                case "no":
                case "n":
                case "false":
                case "na":
                case "n/a":
                case "none":
                    return true;
                default:
                    return false;
            }
        }

        private static void AppendCsv(HashSet<string> tags, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return;

            foreach (var part in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    tags.Add(trimmed);
            }
        }
    }
}
