using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Maps <see cref="QcInterviewAIMergeAssistantResponse"/> onto <see cref="PaymentSLAVM"/> (same shape as Interview/GetPaymentSlaDetails).
    /// Used by <c>QcInterviewAIMergeJob</c> (overwrite) and the GET action (fill empty only).
    /// </summary>
    public static class PaymentSlaVmQcMergeMapper
    {
        private static readonly HashSet<string> QcStringPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(PaymentSLAVM.Inaccuratedecisionmaking),
            nameof(PaymentSLAVM.TimeManagement),
            nameof(PaymentSLAVM.QuestionsRelevance),
            nameof(PaymentSLAVM.ScoreRubricIgnorance),
            nameof(PaymentSLAVM.PoorFeedbackComments),
            nameof(PaymentSLAVM.CandidateInteractionQuality),
            nameof(PaymentSLAVM.LimitedHandoncoding),
            nameof(PaymentSLAVM.WrongStatusUpdates),
            nameof(PaymentSLAVM.WeakInfrastructure),
            nameof(PaymentSLAVM.ProctoringIgnorance),
            nameof(PaymentSLAVM.UnethicalPractices),
            nameof(PaymentSLAVM.PoorCommunicationSkills),
        };

        /// <param name="applyOnlyIfEmpty">True: pre-fill GET form (skip non-empty). False: timer job overwrites mapped fields from AI.</param>
        public static void ApplyFromMergeResponse(PaymentSLAVM model, QcInterviewAIMergeAssistantResponse qc, bool applyOnlyIfEmpty = true)
        {
            if (model == null || qc == null)
                return;

            EnsureQcStringDefaults(model);

            var type = typeof(PaymentSLAVM);

            foreach (var item in qc.flags ?? Enumerable.Empty<QcInterviewAIMergeFlagItem>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.category))
                    continue;

                var propName = ResolvePropertyName(item.category.Trim());
                if (propName == null || !QcStringPropertyNames.Contains(propName))
                    continue;

                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null || prop.PropertyType != typeof(string))
                    continue;

                var current = prop.GetValue(model) as string;
                if (applyOnlyIfEmpty && !string.IsNullOrWhiteSpace(current))
                    continue;

                var flagPart = (item.flag ?? "").Trim();
                //if (item.penalty.HasValue && item.penalty.Value != 0)
                //    flagPart = string.IsNullOrEmpty(flagPart)
                //        ? $"penalty:{item.penalty}"
                //        : $"{flagPart} (penalty:{item.penalty})";

                prop.SetValue(model, Truncate(flagPart, 250));
            }

            if (qc.total_penalty.HasValue)
            {
                if (!applyOnlyIfEmpty || model.PaymentDeduction == 0)
                    model.PaymentDeduction = qc.total_penalty.Value;
                //model.PaymentDeduction = RoundToPaymentSlab(qc.total_penalty.Value);
            }

            var aiNoteParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(qc.interviewer_suspicious))
                aiNoteParts.Add($"interviewer suspicious: {qc.interviewer_suspicious}");
            if (qc.RedoInterview.HasValue)
                aiNoteParts.Add($"redo interview: {qc.RedoInterview.Value}");

            if (aiNoteParts.Count == 0)
                return;

            var line = "AI QC — " + string.Join("; ", aiNoteParts);
            if (model.RecommendationComments != null && model.RecommendationComments.Contains("AI QC —", StringComparison.OrdinalIgnoreCase))
                return;

            var mergedComments = string.IsNullOrWhiteSpace(model.RecommendationComments)
                ? line
                : model.RecommendationComments.TrimEnd() + Environment.NewLine + line;
            model.RecommendationComments = Truncate(mergedComments, 500);
        }

        public static void EnsureQcStringDefaults(PaymentSLAVM model)
        {
            if (model == null)
                return;
            foreach (var name in QcStringPropertyNames)
            {
                var p = typeof(PaymentSLAVM).GetProperty(name);
                if (p?.GetValue(model) == null)
                    p.SetValue(model, string.Empty);
            }
            if (model.InterviewerVideo == null) model.InterviewerVideo = string.Empty;
            if (model.Recommendation == null) model.Recommendation = string.Empty;
            if (model.RecommendationComments == null) model.RecommendationComments = string.Empty;
            if (model.CorrectInterview == null) model.CorrectInterview = string.Empty;
            if (model.FeedBackComment == null) model.FeedBackComment = string.Empty;
        }

        private static string ResolvePropertyName(string category)
        {
            var compact = category.Replace(" ", "").Replace("_", "");
            foreach (var name in QcStringPropertyNames)
            {
                if (string.Equals(name, category, StringComparison.OrdinalIgnoreCase))
                    return name;
                if (string.Equals(name, compact, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            return null;
        }

        private static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLen)
                return value;
            return value.Substring(0, maxLen);
        }

        private static int RoundToPaymentSlab(int totalPenalty)
        {
            var p = Math.Clamp(totalPenalty, 0, 100);
            return (int)Math.Round(p / 5.0, MidpointRounding.AwayFromZero) * 5;
        }
    }
}
