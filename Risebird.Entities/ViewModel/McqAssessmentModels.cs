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

    /// <summary>Why a report is being served from the portal cache instead of a live read.</summary>
    public enum McqCacheReason
    {
        /// <summary>Not a cached copy.</summary>
        None = 0,

        /// <summary>The live call failed - timeout, transport error, or an error status.</summary>
        Unreachable = 1,

        /// <summary>The call succeeded but the MCQ system holds no result for this session.</summary>
        NoLiveResult = 2,

        /// <summary>No MCQ base address is configured for this environment, so no call was made.</summary>
        NotConfigured = 3
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

        /// <summary>
        /// Why the cached copy is being shown rather than a live read. The banner is worded from
        /// this: a live lookup returning "no result for this session" is a different fact from the
        /// assessment system being unreachable, and reporting the second when the first happened
        /// sends someone chasing an outage that is not there.
        /// </summary>
        [JsonIgnore] public McqCacheReason CacheReason { get; set; }


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

    /// <summary>
    /// Answer from the MCQ channel-link endpoints - whether an MCQ skill channel is linked to a
    /// portal channel and marked as an AI Interview channel, and whether this call created it.
    /// </summary>
    public class McqChannelLinkResult
    {
        [JsonProperty("skillChannelId")] public string SkillChannelId { get; set; }
        [JsonProperty("linked")] public bool Linked { get; set; }

        /// <summary>True only when the call that returned this created the channel.</summary>
        [JsonProperty("created")] public bool Created { get; set; }

        [JsonProperty("irfId")] public int? IrfId { get; set; }
        [JsonProperty("mcqIrfId")] public string McqIrfId { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
    }

    /// <summary>
    /// What the portal sends to POST {mcq}/api/v1/ai-sessions/channel-link to have the MCQ skill
    /// channel for a portal channel created if it does not exist yet.
    /// </summary>
    public class McqEnsureChannelLinkRequest
    {
        [JsonProperty("skillChannelId")] public string SkillChannelId { get; set; }
        [JsonProperty("requestedBy")] public string RequestedBy { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("primarySkills")] public string PrimarySkills { get; set; }
        [JsonProperty("secondarySkill")] public string SecondarySkill { get; set; }
        [JsonProperty("profileType")] public string ProfileType { get; set; }
        [JsonProperty("seniorityLevel")] public string SeniorityLevel { get; set; }
        [JsonProperty("employmentType")] public string EmploymentType { get; set; }
        [JsonProperty("countryType")] public string CountryType { get; set; }
        [JsonProperty("jobFunction")] public string JobFunction { get; set; }
        [JsonProperty("portalIRFTitle")] public string PortalIRFTitle { get; set; }
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
