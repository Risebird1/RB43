using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Shared evidence filter for Trial Flag Review badges.
    /// NULL / 0 / empty / noise values never render — one place so payment and merge-JSON flags cannot regress separately.
    /// </summary>
    public static class TrialFlagEvidenceBadgeHelper
    {
        public static bool IsPositiveEvidence(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            switch (value.Trim().ToLowerInvariant())
            {
                case "0":
                case "no":
                case "n":
                case "false":
                case "na":
                case "n/a":
                case "none":
                case "null":
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// PaymentSLA reason columns: emit the label only when the stored value is positive evidence.
        /// Regression: null/empty WeakInfrastructure → no "WeakInfrastructure" badge.
        /// </summary>
        public static IReadOnlyList<string> CollectPaymentReasonBadges(
            string limitedHandoncoding,
            string wrongStatusUpdates,
            string weakInfrastructure,
            string proctoringIgnorance,
            string unethicalPractices,
            string poorCommunicationSkills,
            string attendanceJoinCompliance)
        {
            var tags = new List<string>();
            TryAdd(tags, "LimitedHandoncoding", limitedHandoncoding);
            TryAdd(tags, "WrongStatusUpdates", wrongStatusUpdates);
            TryAdd(tags, "WeakInfrastructure", weakInfrastructure);
            TryAdd(tags, "ProctoringIgnorance", proctoringIgnorance);
            TryAdd(tags, "UnethicalPractices", unethicalPractices);
            TryAdd(tags, "PoorCommunicationSkills", poorCommunicationSkills);
            TryAdd(tags, "AttendanceJoinCompliance", attendanceJoinCompliance);
            return tags;
        }

        /// <summary>
        /// Parses PaymentReasonValuesJson from usp_GetTrialFlagsForReview (object of column→value).
        /// </summary>
        public static IReadOnlyList<string> CollectPaymentReasonBadgesFromJson(string paymentReasonValuesJson)
        {
            if (string.IsNullOrWhiteSpace(paymentReasonValuesJson))
                return Array.Empty<string>();

            try
            {
                var obj = JObject.Parse(paymentReasonValuesJson);
                return CollectPaymentReasonBadges(
                    obj.Value<string>("LimitedHandoncoding"),
                    obj.Value<string>("WrongStatusUpdates"),
                    obj.Value<string>("WeakInfrastructure"),
                    obj.Value<string>("ProctoringIgnorance"),
                    obj.Value<string>("UnethicalPractices"),
                    obj.Value<string>("PoorCommunicationSkills"),
                    obj.Value<string>("AttendanceJoinCompliance"));
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// MergedAIQCDetails flags[]: badge only when <c>flag</c> is non-null/non-empty evidence.
        /// Regression: category present with null/empty flag → no badge.
        /// </summary>
        public static IReadOnlyList<string> CollectMergeFlagBadges(string flagsJson)
        {
            if (string.IsNullOrWhiteSpace(flagsJson))
                return Array.Empty<string>();

            try
            {
                var token = JToken.Parse(flagsJson);
                var arr = token as JArray ?? (token["flags"] as JArray);
                if (arr == null || arr.Count == 0)
                    return Array.Empty<string>();

                var tags = new List<string>();
                foreach (var item in arr)
                {
                    if (item == null || item.Type == JTokenType.Null)
                        continue;

                    var flag = (item.Value<string>("flag") ?? item.Value<string>("Flag") ?? "").Trim();
                    if (!IsPositiveEvidence(flag))
                        continue;

                    var category = (item.Value<string>("category") ?? item.Value<string>("Category") ?? "").Trim();
                    if (IsPositiveEvidence(category))
                        tags.Add(category + ": " + flag);
                    else
                        tags.Add(flag);
                }

                return tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>Legacy CSV of reason labels — drop empty/noise tokens (defense in depth).</summary>
        public static IReadOnlyList<string> FilterLabelCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return Array.Empty<string>();

            return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(IsPositiveEvidence)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void TryAdd(List<string> tags, string label, string value)
        {
            if (IsPositiveEvidence(value) && !string.IsNullOrWhiteSpace(label))
                tags.Add(label);
        }
    }
}
