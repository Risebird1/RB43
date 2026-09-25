using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Keeps report JSON out of the interview turns a candidate hears.
    ///
    /// An adaptive interview and its report run on ONE chained conversation, and a channel with no
    /// Interview Evaluator Prompt Id uses the same prompt for both jobs. If that prompt emits its
    /// evaluation JSON part-way through the interview, the raw blob is read aloud by text-to-speech
    /// and stored as the question text on the report.
    ///
    /// In a healthy interview no spoken turn contains these keys, so this does nothing. It is a
    /// guard against a misbehaving prompt, not a substitute for fixing one — callers log when it
    /// fires.
    /// </summary>
    public static class AIInterviewReplySanitizer
    {
        /// <summary>What the assistant says when its entire turn was JSON.</summary>
        public const string FallbackReply = "Let's move on to the next question.";

        /// <summary>
        /// Keys that only ever belong to the evaluation/report schema. A conversational turn
        /// carrying one of these is a prompt leaking its report into the interview.
        /// </summary>
        private static readonly string[] EvaluationOnlyKeys =
        {
            "\"question_ratings\"",
            "\"rubric_score_summary\"",
            "\"overall_technical_score\"",
            "\"final_verdict\"",
            "\"llm_detection\"",
            "\"overall_communication_score\""
        };

        /// <summary>True when the text carries evaluation-only keys.</summary>
        public static bool LooksLikeEvaluationPayload(string text) =>
            !string.IsNullOrWhiteSpace(text)
            && EvaluationOnlyKeys.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Removes the outermost JSON object from a spoken turn when it carries evaluation keys and
        /// returns the prose around it. Brace matching is string- and escape-aware, so a brace
        /// inside a quoted comment cannot cut the object short.
        /// </summary>
        /// <param name="stripped">True when something was removed.</param>
        public static string Strip(string reply, out bool stripped)
        {
            stripped = false;
            if (string.IsNullOrWhiteSpace(reply)) return reply;
            if (!LooksLikeEvaluationPayload(reply)) return reply;

            var start = reply.IndexOf('{');
            if (start < 0) return reply;

            var end = FindMatchingBrace(reply, start);

            // Unbalanced (truncated) JSON: drop from the first brace onwards rather than keep it.
            var remainder = end < 0
                ? reply.Substring(0, start)
                : reply.Remove(start, end - start + 1);

            stripped = true;
            return Regex.Replace(remainder, @"\s+", " ").Trim();
        }

        /// <summary>
        /// The turn to speak: the prose around the JSON, or a neutral line when the turn was
        /// nothing but JSON.
        /// </summary>
        public static string StripOrFallback(string reply, out bool stripped)
        {
            var cleaned = Strip(reply, out stripped);
            return string.IsNullOrWhiteSpace(cleaned) && stripped ? FallbackReply : cleaned;
        }

        private static int FindMatchingBrace(string text, int start)
        {
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];

                if (escaped) { escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;

                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }
    }
}
