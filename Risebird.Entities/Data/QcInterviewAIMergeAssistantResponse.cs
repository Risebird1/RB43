using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    /// <summary>Root JSON from the QC merge assistant (stored in <see cref="QcInterviewAIevaluation.MergedAIQCDetails"/>).</summary>
    public class QcInterviewAIMergeAssistantResponse
    {
        [JsonProperty("flags")]
        public List<QcInterviewAIMergeFlagItem> flags { get; set; }

        [JsonProperty("total_penalty")]
        public int? total_penalty { get; set; }

        [JsonProperty("interviewer_suspicious")]
        public string interviewer_suspicious { get; set; }

        [JsonProperty("RedoInterview")]
        public bool? RedoInterview { get; set; }

        [JsonProperty("total_qcmerge_score")]
        public int? total_qcmerge_score { get; set; }

        [JsonProperty("qcmerge_verdict")]
        public string qcmerge_verdict { get; set; }

        /// <summary>Strips markdown fences and parses assistant JSON (object with flags, or legacy array of flags).</summary>
        public static bool TryParseFromAssistantText(string rawText, out QcInterviewAIMergeAssistantResponse result)
        {
            result = null;
            var normalized = NormalizeAssistantJson(rawText);
            if (string.IsNullOrEmpty(normalized))
                return false;

            try
            {
                var token = JToken.Parse(normalized);
                if (token.Type == JTokenType.Object)
                {
                    result = token.ToObject<QcInterviewAIMergeAssistantResponse>();
                    return result != null && (result.flags != null || result.total_penalty.HasValue || result.RedoInterview.HasValue || !string.IsNullOrEmpty(result.interviewer_suspicious) || result.total_qcmerge_score.HasValue || !string.IsNullOrEmpty(result.qcmerge_verdict));
                }

                if (token.Type == JTokenType.Array)
                {
                    var flags = token.ToObject<List<QcInterviewAIMergeFlagItem>>();
                    result = new QcInterviewAIMergeAssistantResponse { flags = flags };
                    return flags != null && flags.Count > 0;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                result = JsonConvert.DeserializeObject<QcInterviewAIMergeAssistantResponse>(normalized);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Deserialize from <see cref="QcInterviewAIevaluation.MergedAIQCDetails"/> (canonical JSON saved by the merge job).</summary>
        public static QcInterviewAIMergeAssistantResponse FromMergedDetailsJson(string mergedDetailsJson)
        {
            if (string.IsNullOrWhiteSpace(mergedDetailsJson))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<QcInterviewAIMergeAssistantResponse>(mergedDetailsJson);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeAssistantJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var t = raw.Trim();
            if (t.StartsWith("```", System.StringComparison.Ordinal))
            {
                var firstNl = t.IndexOf('\n');
                if (firstNl >= 0)
                    t = t.Substring(firstNl + 1);
                var end = t.LastIndexOf("```", System.StringComparison.Ordinal);
                if (end > 0)
                    t = t.Substring(0, end);
            }
            return t.Trim();
        }
    }
}
