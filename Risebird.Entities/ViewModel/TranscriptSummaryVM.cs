using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class TranscriptSummaryVM
    {
        [JsonProperty("job_title")]
        public string JobTitle { get; set; }

        [JsonProperty("interviewee_analysis")]
        public IntervieweeAnalysis IntervieweeAnalysis { get; set; }

        [JsonProperty("flags")]
        public Flags Flags { get; set; }
    }

    public class IntervieweeAnalysis
    {
        [JsonProperty("qa_pairs")]
        public List<QaPair> QaPairs { get; set; }

        [JsonProperty("summary")]
        public InterviewSummary Summary { get; set; }

        [JsonProperty("verdict")]
        public Verdict Verdict { get; set; }
    }

    public class QaPair
    {
        /// <summary>
        /// Timestamp in format "hh:mm:ss"
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Exact interviewer question
        /// </summary>
        [JsonProperty("question")]
        public string Question { get; set; }

        /// <summary>
        /// Exact interviewee response
        /// </summary>
        [JsonProperty("answer")]
        public string Answer { get; set; }

        /// <summary>
        /// Rating 1–5
        /// </summary>
        [JsonProperty("answer_rating")]
        public int AnswerRating { get; set; }

        /// <summary>
        /// Brief reason for the rating
        /// </summary>
        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

    public class InterviewSummary
    {
        /// <summary>
        /// Total number of questions
        /// </summary>
        [JsonProperty("total_questions")]
        public int TotalQuestions { get; set; }

        /// <summary>
        /// Average rating across all answers
        /// </summary>
        [JsonProperty("average_answer_rating")]
        public double AverageAnswerRating { get; set; }

        /// <summary>
        /// Strength descriptions (optionally with timestamps)
        /// </summary>
        [JsonProperty("strengths")]
        public List<string> Strengths { get; set; }

        /// <summary>
        /// Areas of improvement (optionally with timestamps)
        /// </summary>
        [JsonProperty("areas_of_improvement")]
        public List<string> AreasOfImprovement { get; set; }
    }

    public class Verdict
    {
        /// <summary>
        /// "Recommended" / "Borderline" / "Not Recommended"
        /// </summary>
        [JsonProperty("final_recommendation")]
        public string FinalRecommendation { get; set; }

        /// <summary>
        /// Explanation based only on interviewee answer quality
        /// </summary>
        [JsonProperty("justification")]
        public string Justification { get; set; }
    }

    public class Flags
    {
        [JsonProperty("personal_info_shared")]
        public List<FlagInstance> PersonalInfoShared { get; set; }

        [JsonProperty("unethical_behavior")]
        public List<FlagInstance> UnethicalBehavior { get; set; }
    }

    public class FlagInstance
    {
        /// <summary>
        /// Timestamp in format "hh:mm:ss"
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Exact statement
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}




