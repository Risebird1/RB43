using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class AIInterviewEvaluation
    {
        [JsonProperty("question_ratings")]
        public List<QuestionRating> QuestionRatings { get; set; }

        [JsonProperty("rubric_score_summary")]
        public List<RubricScoreSummary> RubricScoreSummary { get; set; }

        [JsonProperty("overall_technical_score")]
        public int OverallTechnicalScore { get; set; }

        [JsonProperty("final_verdict")]
        public string FinalVerdict { get; set; }

        [JsonProperty("final_comment")]
        public string FinalComment { get; set; }

        [JsonProperty("overall_communication_score")]
        public int OverallCommunicationScore { get; set; }

        // Note: property name has a space in JSON ("overall_communication_score reasoning")
        [JsonProperty("overall_communication_score reasoning")]
        public string OverallCommunicationScoreReasoning { get; set; }

        [JsonProperty("llm_detection")]
        public LlmDetection LlmDetection { get; set; }
    }

    public class QuestionRating
    {
        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("verdict")]
        public string Verdict { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("communication_score")]
        public int CommunicationScore { get; set; }

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
        public int Score { get; set; }
    }

    public class LlmDetection
    {
        [JsonProperty("llm_response_suspected")]
        public bool LlmResponseSuspected { get; set; }

        [JsonProperty("evidence_comment")]
        public string EvidenceComment { get; set; }

        [JsonProperty("suspicious_instances")]
        public List<string> SuspiciousInstances { get; set; }
    }
}




