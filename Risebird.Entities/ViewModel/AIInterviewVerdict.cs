using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Canonical expert-report verdicts. Replaces the older Recommended / Not Recommended labels
    /// in UI, discrepancy comparison, and client-facing display without changing stored integers.
    /// </summary>
    public static class AIInterviewVerdict
    {
        public const string StronglySelected = "Strongly Selected";
        public const string MarginallySelected = "Marginally Selected";
        public const string MarginallyRejected = "Marginally Rejected";
        public const string StronglyRejected = "Strongly Rejected";

        public static readonly string[] Options =
        {
            StronglySelected,
            MarginallySelected,
            MarginallyRejected,
            StronglyRejected
        };

        /// <summary>
        /// Instruction appended to evaluation prompts so new AI output uses the four-value enum.
        /// Parsers still accept Recommended / Not Recommended / select / reject / borderline.
        /// </summary>
        public const string OutputSchemaInstruction =
            "final_verdict MUST be exactly one of: \"Strongly Selected\", \"Marginally Selected\", \"Marginally Rejected\", \"Strongly Rejected\". "
            + "Do not use Recommended, Not Recommended, select, reject, or borderline. "
            + "Include llm_detection as an object with llm_response_suspected (boolean), evidence_comment (string), "
            + "and suspicious_instances (array of per-question objects {\"question\":\"...\",\"evidence\":\"...\"}).";

        /// <summary>
        /// Maps legacy and informal labels onto the four canonical verdicts. Unknown values are
        /// returned trimmed so stored custom text is not discarded.
        /// </summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var v = value.Trim();
            var lower = v.ToLowerInvariant();

            if (IsCanonical(v)) return CanonicalForm(v);

            if (lower == "recommended" || lower == "select" || lower == "selected"
                || lower == "strongly selected" || lower == "strong hire" || lower == "hire")
                return StronglySelected;

            if (lower == "marginally selected" || lower == "marginal selected" || lower == "borderline select"
                || lower == "borderline selected")
                return MarginallySelected;

            if (lower == "marginally rejected" || lower == "marginal rejected" || lower == "borderline reject"
                || lower == "borderline rejected" || lower == "borderline")
                return MarginallyRejected;

            if (lower == "not recommended" || lower == "reject" || lower == "rejected"
                || lower == "strongly rejected" || lower == "no hire" || lower == "fail")
                return StronglyRejected;

            return v;
        }

        public static bool IsSelectFamily(string value)
        {
            var normalized = Normalize(value);
            return string.Equals(normalized, StronglySelected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, MarginallySelected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRejectFamily(string value)
        {
            var normalized = Normalize(value);
            return string.Equals(normalized, StronglyRejected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, MarginallyRejected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Maps a parsed evaluation's verdict onto the canonical four-value enum without
        /// changing any other fields.
        /// </summary>
        public static void ApplyTo(AIInterviewEvaluation evaluation)
        {
            if (evaluation == null) return;
            evaluation.FinalVerdict = Normalize(evaluation.FinalVerdict);
        }

        public static bool IsCanonical(string value)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string CanonicalForm(string value)
        {
            foreach (var option in Options)
            {
                if (string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
                    return option;
            }
            return value;
        }
    }
}
