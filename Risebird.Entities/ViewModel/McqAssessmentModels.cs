using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// What the portal sends to POST {mcq}/api/v1/ai-sessions/{aiSessionId}/mcq to have the MCQ
    /// section of an interview prepared.
    /// </summary>
    public class McqSectionProvisionRequest
    {
        [JsonProperty("skillChannelId")] public string SkillChannelId { get; set; }
        [JsonProperty("candidateEmail")] public string CandidateEmail { get; set; }
        [JsonProperty("candidateName")] public string CandidateName { get; set; }
        [JsonProperty("primarySkill")] public string PrimarySkill { get; set; }
        [JsonProperty("jobDescription")] public string JobDescription { get; set; }
        [JsonProperty("sections")] public List<string> Sections { get; set; }
        [JsonProperty("totalMinutes")] public int? TotalMinutes { get; set; }
        [JsonProperty("difficultyLevel")] public string DifficultyLevel { get; set; }
    }

    public class McqSectionProvisionResult
    {
        [JsonProperty("aiSessionId")] public string AiSessionId { get; set; }
        [JsonProperty("mcqId")] public string McqId { get; set; }
        [JsonProperty("launchToken")] public string LaunchToken { get; set; }

        /// <summary>Absolute URL for the iframe. Built by the MCQ side, used verbatim.</summary>
        [JsonProperty("embedUrl")] public string EmbedUrl { get; set; }

        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("totalMinutes")] public int TotalMinutes { get; set; }
        [JsonProperty("questionCount")] public int QuestionCount { get; set; }
        [JsonProperty("sections")] public List<McqSectionInfo> Sections { get; set; } = new List<McqSectionInfo>();
    }

    public class McqSectionInfo
    {
        [JsonProperty("sectionKey")] public string SectionKey { get; set; }
        [JsonProperty("sectionTitle")] public string SectionTitle { get; set; }
        [JsonProperty("sectionDescription")] public string SectionDescription { get; set; }
        [JsonProperty("sortOrder")] public int SortOrder { get; set; }
        [JsonProperty("questionCount")] public int? QuestionCount { get; set; }
        [JsonProperty("timerMinutes")] public int? TimerMinutes { get; set; }
    }

    /// <summary>
    /// The MCQ section result, as returned by GET {mcq}/api/v1/ai-sessions/{id}/mcq/report and as
    /// cached verbatim in AIInterviewMcqAssessment.ReportJson.
    /// </summary>
    public class McqAssessmentReport
    {
        [JsonProperty("aiSessionId")] public string AiSessionId { get; set; }
        [JsonProperty("mcqId")] public string McqId { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("candidateEmail")] public string CandidateEmail { get; set; }
        [JsonProperty("candidateName")] public string CandidateName { get; set; }
        [JsonProperty("setTitle")] public string SetTitle { get; set; }
        [JsonProperty("primarySkill")] public string PrimarySkill { get; set; }
        [JsonProperty("startedOn")] public DateTime? StartedOn { get; set; }
        [JsonProperty("completedOn")] public DateTime? CompletedOn { get; set; }
        [JsonProperty("allottedMinutes")] public int? AllottedMinutes { get; set; }
        [JsonProperty("elapsedSeconds")] public int? ElapsedSeconds { get; set; }
        [JsonProperty("tabSwitchCount")] public int? TabSwitchCount { get; set; }
        [JsonProperty("totalQuestions")] public int TotalQuestions { get; set; }
        [JsonProperty("correctAnswers")] public int CorrectAnswers { get; set; }
        [JsonProperty("redFlags")] public int RedFlags { get; set; }
        [JsonProperty("scorePercent")] public decimal ScorePercent { get; set; }
        [JsonProperty("sections")] public List<McqSectionResult> Sections { get; set; } = new List<McqSectionResult>();
        [JsonProperty("questions")] public List<McqQuestionResult> Questions { get; set; } = new List<McqQuestionResult>();

        /// <summary>
        /// True when this came from the portal's cache rather than a live call. Surfaced on the
        /// report so a reader can tell a stale figure from a current one.
        /// </summary>
        [JsonIgnore] public bool IsFromCache { get; set; }

        [JsonIgnore] public DateTime? CachedOn { get; set; }

        [JsonIgnore]
        public bool IsCompleted =>
            string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);

        /// <summary>Wall-clock time the candidate spent, for the report's time-analysis block.</summary>
        [JsonIgnore]
        public TimeSpan? Elapsed => ElapsedSeconds.HasValue ? TimeSpan.FromSeconds(ElapsedSeconds.Value) : (TimeSpan?)null;

        /// <summary>
        /// Sections that carry questions, in ask order. A section the candidate never reached
        /// still appears - reporting 0 of N is information; omitting it is not.
        /// </summary>
        [JsonIgnore]
        public List<McqSectionResult> OrderedSections =>
            (Sections ?? new List<McqSectionResult>()).OrderBy(s => s.SortOrder).ToList();
    }

    public class McqSectionResult
    {
        [JsonProperty("sectionKey")] public string SectionKey { get; set; }
        [JsonProperty("sectionTitle")] public string SectionTitle { get; set; }
        [JsonProperty("sectionDescription")] public string SectionDescription { get; set; }
        [JsonProperty("sortOrder")] public int SortOrder { get; set; }
        [JsonProperty("timerMinutes")] public int? TimerMinutes { get; set; }
        [JsonProperty("questionCount")] public int QuestionCount { get; set; }
        [JsonProperty("attempted")] public int Attempted { get; set; }
        [JsonProperty("correct")] public int Correct { get; set; }
        [JsonProperty("redFlags")] public int RedFlags { get; set; }
        [JsonProperty("timeSpentSeconds")] public int TimeSpentSeconds { get; set; }
        [JsonProperty("scorePercent")] public decimal ScorePercent { get; set; }
    }

    public class McqQuestionResult
    {
        [JsonProperty("questionNumber")] public int QuestionNumber { get; set; }
        [JsonProperty("sectionKey")] public string SectionKey { get; set; }
        [JsonProperty("sectionTitle")] public string SectionTitle { get; set; }
        [JsonProperty("questionType")] public string QuestionType { get; set; }
        [JsonProperty("question")] public string Question { get; set; }
        [JsonProperty("option1")] public string Option1 { get; set; }
        [JsonProperty("option2")] public string Option2 { get; set; }
        [JsonProperty("option3")] public string Option3 { get; set; }
        [JsonProperty("option4")] public string Option4 { get; set; }
        [JsonProperty("correctAnswer")] public string CorrectAnswer { get; set; }
        [JsonProperty("selectedAnswer")] public string SelectedAnswer { get; set; }
        [JsonProperty("explanation")] public string Explanation { get; set; }
        [JsonProperty("rating")] public int? Rating { get; set; }
        [JsonProperty("redFlag")] public bool? RedFlag { get; set; }
        [JsonProperty("timeSpentSeconds")] public int? TimeSpentSeconds { get; set; }

        [JsonIgnore] public bool IsCorrect => (Rating ?? 0) > 0;
        [JsonIgnore] public bool WasAttempted => !string.IsNullOrWhiteSpace(SelectedAnswer);

        /// <summary>
        /// The four options as a list, so the report can render them positionally without four
        /// near-identical blocks of markup.
        /// </summary>
        [JsonIgnore]
        public List<string> Options => new List<string> { Option1, Option2, Option3, Option4 };

        /// <summary>
        /// Option numbers the candidate chose. Multi-select answers arrive comma separated.
        /// </summary>
        [JsonIgnore]
        public HashSet<int> SelectedOptionNumbers => ParseOptionNumbers(SelectedAnswer);

        [JsonIgnore]
        public HashSet<int> CorrectOptionNumbers => ParseOptionNumbers(CorrectAnswer);

        private static HashSet<int> ParseOptionNumbers(string raw)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(raw)) return set;

            foreach (var token in raw.Split(','))
            {
                if (int.TryParse(token.Trim(), out var n) && n >= 1 && n <= 4)
                    set.Add(n);
            }
            return set;
        }
    }

    /// <summary>The cached row, before the JSON payload is unpacked.</summary>
    public class McqAssessmentCacheRow
    {
        public string SessionId { get; set; }
        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public string McqId { get; set; }
        public string Status { get; set; }
        public int? TotalQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public decimal? ScorePercent { get; set; }
        public int? RedFlags { get; set; }
        public int? AllottedMinutes { get; set; }
        public int? ElapsedSeconds { get; set; }
        public int? TabSwitchCount { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string ReportJson { get; set; }
        public DateTime? FetchedOn { get; set; }
    }
}
