using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Decides whether an adaptive interviewer turn ends the interview.
    ///
    /// The start-of-interview prompt asks the assistant to append <see cref="Marker"/> to its
    /// closing message, but a stored prompt run as json_object does not always do so: it may flag
    /// the end with a JSON field instead, or leave the flag out and simply say goodbye again every
    /// time the candidate replies. Without a fallback the candidate is stuck in that goodbye loop and
    /// the interview never completes.
    /// </summary>
    public static class AIInterviewCompletionDetector
    {
        /// <summary>Internal token the adaptive assistant appends when it ends the interview.</summary>
        public const string Marker = "[[ADAPTIVE_INTERVIEW_COMPLETE]]";

        private static readonly string[] CompletionFlagKeys =
        {
            "interviewcomplete", "interviewcompleted", "iscomplete", "iscompleted", "complete", "completed",
            "endinterview", "interviewended", "isinterviewover", "interviewover", "isfinal", "final", "done"
        };

        private static readonly string[] CompletionStatusValues =
        {
            "complete", "completed", "ended", "end", "closed", "finished", "done"
        };

        /// <summary>
        /// True when the turn carries the marker, or is a JSON object with a top-level completion
        /// flag (e.g. <c>"interview_complete": true</c>, <c>"status": "completed"</c>).
        /// </summary>
        public static bool SignalsCompletion(string reply)
        {
            if (string.IsNullOrWhiteSpace(reply)) return false;
            if (reply.IndexOf(Marker, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var trimmed = reply.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;

            JObject obj;
            try { obj = JObject.Parse(trimmed); }
            catch (JsonReaderException) { return false; }

            foreach (var prop in obj.Properties())
            {
                var key = Regex.Replace(prop.Name, "[^a-zA-Z]", "").ToLowerInvariant();
                var value = prop.Value;

                if (CompletionFlagKeys.Contains(key) && IsTrue(value)) return true;

                if ((key == "status" || key == "state" || key == "stage" || key == "phase")
                    && value.Type == JTokenType.String
                    && CompletionStatusValues.Contains(((string)value).Trim().ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when the assistant has answered the candidate with the same closing statement it
        /// said last turn - the goodbye loop. A repeated <em>question</em> is not counted: the
        /// assistant legitimately repeats a question when the candidate asks to hear it again.
        /// </summary>
        public static bool RepeatsClosingTurn(string reply, string previousAssistantTurn)
        {
            var current = Normalize(reply);
            var previous = Normalize(previousAssistantTurn);
            if (current.Length == 0 || previous.Length == 0) return false;
            if (reply.Contains("?") || previousAssistantTurn.Contains("?")) return false;

            if (current == previous) return true;

            // Near-identical wording ("Thanks for your time" vs "Thanks again for your time") still
            // counts; a short acknowledgement that merely starts the next prompt does not.
            var currentWords = current.Split(' ').ToHashSet();
            var previousWords = previous.Split(' ').ToHashSet();
            var shared = currentWords.Intersect(previousWords).Count();
            var all = currentWords.Union(previousWords).Count();
            return all > 0 && shared >= 0.8 * all;
        }

        private static bool IsTrue(JToken value) =>
            value.Type == JTokenType.Boolean ? (bool)value
            : value.Type == JTokenType.String && string.Equals(((string)value).Trim(), "true", StringComparison.OrdinalIgnoreCase);

        private static string Normalize(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : Regex.Replace(text.Replace(Marker, ""), @"[^\p{L}\p{N}]+", " ").Trim().ToLowerInvariant();
    }
}
