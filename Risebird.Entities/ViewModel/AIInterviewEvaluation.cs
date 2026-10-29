using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class AIInterviewEvaluation
    {
        [JsonProperty("question_ratings")]
        public List<QuestionRating> QuestionRatings { get; set; }

        [JsonProperty("rubric_score_summary")]
        public List<RubricScoreSummary> RubricScoreSummary { get; set; }

        [JsonProperty("overall_technical_score")]
        public float OverallTechnicalScore { get; set; }
        public int AadharVerificationId { get; set; }

        /// <summary>
        /// Canonical values: Strongly Selected, Marginally Selected, Marginally Rejected, Strongly Rejected.
        /// Legacy Recommended / Not Recommended / select / reject / borderline are normalized on parse.
        /// </summary>
        [JsonProperty("final_verdict")]
        public string FinalVerdict { get; set; }

        [JsonProperty("final_comment")]
        public string FinalComment { get; set; }

        [JsonProperty("overall_communication_score")]
        public float OverallCommunicationScore { get; set; }

        // Note: property name has a space in JSON ("overall_communication_score reasoning")
        [JsonProperty("overall_communication_score reasoning")]
        public string OverallCommunicationScoreReasoning { get; set; }

        [JsonProperty("llm_detection")]
        public LlmDetection LlmDetection { get; set; }

        /// <summary>
        /// Technical score as a 0-100 percentage for display. The evaluator rates each question 1-5
        /// and its overall_technical_score has no fixed scale (often 0 or missing), so the percentage
        /// is computed from the per-question ratings, then the rubric, and only then the overall field.
        /// </summary>
        [JsonIgnore]
        public float? TechnicalScorePercent =>
            RatingsPercent(QuestionRatings?.Select(q => q.Score))
            ?? RubricPercent(RubricScoreSummary)
            ?? OverallPercent(OverallTechnicalScore);

        /// <summary>Communication score as a 0-100 percentage; see <see cref="TechnicalScorePercent"/>.</summary>
        [JsonIgnore]
        public float? CommunicationScorePercent =>
            RatingsPercent(QuestionRatings?.Select(q => q.CommunicationScore))
            ?? OverallPercent(OverallCommunicationScore);

        private static float? RatingsPercent(IEnumerable<float> ratings)
        {
            // 0 means the question was not rated (the mapper defaults a missing score to 0).
            var rated = ratings?.Where(r => r > 0).ToList();
            if (rated == null || rated.Count == 0) return null;
            var scale = ScaleOf(rated.Max());
            return Clamp(rated.Average() / scale * 100);
        }

        private static float? RubricPercent(List<RubricScoreSummary> rubric)
        {
            var rated = rubric?.Where(r => r.Score > 0).ToList();
            if (rated == null || rated.Count == 0) return null;
            var max = rated.Sum(r => r.MaxScore > 0 ? r.MaxScore.Value : ScaleOf(r.Score));
            return Clamp(rated.Sum(r => r.Score) / max * 100);
        }

        private static float? OverallPercent(float overall) =>
            overall > 0 ? Clamp(overall / ScaleOf(overall) * 100) : (float?)null;

        private static float ScaleOf(float value) => value <= 5 ? 5 : value <= 10 ? 10 : 100;

        private static float Clamp(float percent) => Math.Min(100, Math.Max(0, percent));

        /// <summary>
        /// Parses an assistant evaluation payload. Accepts fenced JSON, the four canonical
        /// verdicts, and legacy Recommended / select / reject labels. Hydrates llm_detection
        /// from either snake_case or camelCase, including per-question instance objects.
        /// </summary>
        public static AIInterviewEvaluation TryParse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();
            else if (cleaned.StartsWith("```", StringComparison.Ordinal))
                cleaned = cleaned.Substring(3).TrimStart();
            if (cleaned.EndsWith("```", StringComparison.Ordinal))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();
            cleaned = cleaned.TrimStart('n');

            try
            {
                var strict = JsonConvert.DeserializeObject<AIInterviewEvaluation>(cleaned);
                if (HasContent(strict))
                {
                    var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                    RepairFromRoot(strict, root);
                    AIInterviewVerdict.ApplyTo(strict);
                    return strict;
                }
            }
            catch
            {
                // Fall through to the alias-tolerant mapper.
            }

            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                if (root == null) return null;

                var eval = new AIInterviewEvaluation
                {
                    OverallTechnicalScore = root.Value<float?>("overall_technical_score") ?? root.Value<float?>("overall_score") ?? 0,
                    FinalVerdict = root.Value<string>("final_verdict") ?? root.Value<string>("verdict") ?? root.Value<string>("overall_recommendation"),
                    FinalComment = root.Value<string>("final_comment") ?? root.Value<string>("comment") ?? root.Value<string>("overall_summary"),
                    OverallCommunicationScore = root.Value<float?>("overall_communication_score") ?? root.Value<float?>("communication_score") ?? 0,
                    OverallCommunicationScoreReasoning = root.Value<string>("overall_communication_score reasoning")
                        ?? root.Value<string>("overall_communication_score_reasoning"),
                    QuestionRatings = MapQuestions((JArray)(root["question_ratings"] ?? root["questions"] ?? root["questionRatings"] ?? root["question_evaluations"])),
                    RubricScoreSummary = MapRubric((JArray)(root["rubric_score_summary"] ?? root["rubric_summary"] ?? root["rubric"] ?? root["category_scores"])),
                    LlmDetection = LlmDetection.FromToken(root["llm_detection"] ?? root["llmDetection"])
                };

                if (!HasContent(eval)) return null;
                AIInterviewVerdict.ApplyTo(eval);
                return eval;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasContent(AIInterviewEvaluation eval)
        {
            return eval != null &&
                (eval.QuestionRatings != null || eval.RubricScoreSummary != null || eval.LlmDetection != null ||
                 !string.IsNullOrEmpty(eval.FinalVerdict) || !string.IsNullOrEmpty(eval.FinalComment) ||
                 eval.OverallTechnicalScore != 0 || eval.OverallCommunicationScore != 0);
        }

        private static void RepairFromRoot(AIInterviewEvaluation strict, JObject root)
        {
            if (strict == null || root == null) return;

            if (strict.RubricScoreSummary != null && strict.RubricScoreSummary.Exists(r => string.IsNullOrEmpty(r.RubricItem)))
            {
                var mapped = MapRubric(root["rubric_score_summary"] as JArray);
                if (mapped != null) strict.RubricScoreSummary = mapped;
            }

            if (strict.QuestionRatings == null)
                strict.QuestionRatings = MapQuestions((JArray)(root["question_ratings"] ?? root["questions"] ?? root["questionRatings"] ?? root["question_evaluations"]));

            if (strict.RubricScoreSummary == null)
                strict.RubricScoreSummary = MapRubric((JArray)(root["rubric_score_summary"] ?? root["category_scores"] ?? root["rubric_summary"] ?? root["rubric"]));

            if (string.IsNullOrEmpty(strict.FinalVerdict))
                strict.FinalVerdict = root.Value<string>("final_verdict") ?? root.Value<string>("verdict") ?? root.Value<string>("overall_recommendation");

            if (string.IsNullOrEmpty(strict.FinalComment))
                strict.FinalComment = root.Value<string>("final_comment") ?? root.Value<string>("comment") ?? root.Value<string>("overall_summary");

            if (strict.LlmDetection == null)
                strict.LlmDetection = LlmDetection.FromToken(root["llm_detection"] ?? root["llmDetection"]);
            else if (strict.LlmDetection.SuspiciousInstances == null)
            {
                var hydrated = LlmDetection.FromToken(root["llm_detection"] ?? root["llmDetection"]);
                if (hydrated?.SuspiciousInstances != null)
                    strict.LlmDetection.SuspiciousInstances = hydrated.SuspiciousInstances;
                if (string.IsNullOrWhiteSpace(strict.LlmDetection.EvidenceComment) && hydrated != null)
                    strict.LlmDetection.EvidenceComment = hydrated.EvidenceComment;
            }
        }

        private static List<QuestionRating> MapQuestions(JArray qNode)
        {
            if (qNode == null) return null;
            return qNode.Select(q => new QuestionRating
            {
                Question = q.Value<string>("question") ?? q.Value<string>("text") ?? q.Value<string>("question_id"),
                Verdict = q.Value<string>("verdict") ?? q.Value<string>("evaluation_summary"),
                Score = q.Value<float?>("score") ?? 0,
                Comment = q.Value<string>("comment") ?? q.Value<string>("evaluation_summary"),
                CommunicationScore = q.Value<float?>("communication_score") ?? 0,
                CommunicationComment = q.Value<string>("communication_comment")
            }).ToList();
        }

        private static List<RubricScoreSummary> MapRubric(JArray rNode)
        {
            if (rNode == null) return null;
            return rNode.Select(r => new RubricScoreSummary
            {
                RubricItem = r.Value<string>("rubric_item") ?? r.Value<string>("score_rubric_parameter") ?? r.Value<string>("parameter") ?? r.Value<string>("item") ?? r.Value<string>("name"),
                ExpectedLevel = r.Value<string>("expected_level") ?? r.Value<string>("expected") ?? r.Value<string>("evaluated_at"),
                ObservedLevel = r.Value<string>("observed_level") ?? r.Value<string>("observed") ?? r.Value<string>("summary"),
                Score = r.Value<float?>("score") ?? r.Value<float?>("average_score") ?? 0,
                MaxScore = r.Value<float?>("max_score") ?? r.Value<float?>("max")
            }).ToList();
        }
    }

    public class QuestionRating
    {
        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("verdict")]
        public string Verdict { get; set; }

        [JsonProperty("score")]
        public float Score { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("communication_score")]
        public float CommunicationScore { get; set; }

        [JsonProperty("communication_comment")]
        public string CommunicationComment { get; set; }
    }

    public class RubricScoreSummary
    {
        [JsonProperty("rubric_item")]
        public string RubricItem { get; set; }

        [JsonProperty("expected_level")]
        public string ExpectedLevel { get; set; }

        [JsonProperty("observed_level")]
        public string ObservedLevel { get; set; }

        [JsonProperty("score")]
        public float Score { get; set; }

        /// <summary>Optional per-item max from the evaluation payload. Display only.</summary>
        [JsonProperty("max_score")]
        public float? MaxScore { get; set; }
    }

    public class LlmDetection
    {
        [JsonProperty("llm_response_suspected")]
        public bool LlmResponseSuspected { get; set; }

        [JsonProperty("evidence_comment")]
        public string EvidenceComment { get; set; }

        [JsonProperty("suspicious_instances")]
        [JsonConverter(typeof(LlmSuspiciousInstanceListConverter))]
        public List<string> SuspiciousInstances { get; set; }

        public static LlmDetection FromToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type != JTokenType.Object)
                return null;

            var obj = (JObject)token;
            var instancesToken = obj["suspicious_instances"] ?? obj["suspiciousInstances"] ?? obj["instances"];
            List<string> instances = null;
            if (instancesToken != null && instancesToken.Type != JTokenType.Null)
            {
                instances = new List<string>();
                if (instancesToken.Type == JTokenType.Array)
                {
                    foreach (var item in instancesToken)
                    {
                        var formatted = LlmSuspiciousInstanceListConverter.Format(item);
                        if (!string.IsNullOrWhiteSpace(formatted)) instances.Add(formatted);
                    }
                }
                else
                {
                    var formatted = LlmSuspiciousInstanceListConverter.Format(instancesToken);
                    if (!string.IsNullOrWhiteSpace(formatted)) instances.Add(formatted);
                }
            }

            return new LlmDetection
            {
                LlmResponseSuspected = obj.Value<bool?>("llm_response_suspected")
                    ?? obj.Value<bool?>("llmResponseSuspected")
                    ?? obj.Value<bool?>("suspected")
                    ?? false,
                EvidenceComment = obj.Value<string>("evidence_comment")
                    ?? obj.Value<string>("evidenceComment")
                    ?? obj.Value<string>("evidence"),
                SuspiciousInstances = instances
            };
        }

        public static (string Question, string Evidence) SplitInstance(string instance)
        {
            if (string.IsNullOrWhiteSpace(instance))
                return ("—", instance);

            var splitAt = instance.IndexOf(": ", StringComparison.Ordinal);
            if (splitAt <= 0) return ("—", instance);
            return (instance.Substring(0, splitAt), instance.Substring(splitAt + 2));
        }
    }

    /// <summary>
    /// Accepts either plain strings or per-question objects from the evaluation JSON.
    /// Public so Web / Business / Services can deserialize llm_detection instance objects.
    /// </summary>
    public sealed class LlmSuspiciousInstanceListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<string>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token == null || token.Type == JTokenType.Null) return null;

            var list = new List<string>();
            if (token.Type == JTokenType.Array)
            {
                foreach (var item in token)
                {
                    var formatted = Format(item);
                    if (!string.IsNullOrWhiteSpace(formatted)) list.Add(formatted);
                }
            }
            else
            {
                var formatted = Format(token);
                if (!string.IsNullOrWhiteSpace(formatted)) list.Add(formatted);
            }
            return list;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public static string Format(JToken item)
        {
            if (item == null || item.Type == JTokenType.Null) return null;
            if (item.Type == JTokenType.String) return item.ToString();
            if (item.Type != JTokenType.Object) return item.ToString();

            var question = item.Value<string>("question")
                ?? item.Value<string>("question_text")
                ?? item.Value<string>("question_id");
            var evidence = item.Value<string>("evidence")
                ?? item.Value<string>("comment")
                ?? item.Value<string>("reason")
                ?? item.Value<string>("summary")
                ?? item.Value<string>("evidence_comment");

            if (!string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(evidence))
                return question.Trim() + ": " + evidence.Trim();
            if (!string.IsNullOrWhiteSpace(evidence)) return evidence.Trim();
            if (!string.IsNullOrWhiteSpace(question)) return question.Trim();
            return item.ToString(Formatting.None);
        }
    }
}




