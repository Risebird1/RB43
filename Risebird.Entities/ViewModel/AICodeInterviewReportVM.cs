using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// One copy of an AI code interview report. The same shape stores both the assistant's report
    /// and the expert's edited copy of it, told apart by <see cref="ReportSource"/>.
    /// </summary>
    public class AICodeInterviewReportVM
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int InterviewId { get; set; }

        /// <summary>0 = AI report, 1 = expert copy. Forced server side on submit.</summary>
        public int ReportSource { get; set; }

        public string OverallVerdict { get; set; }
        public string OverallComment { get; set; }
        public decimal? OverallCommunicationRating { get; set; }
        public string OverallCommunicationComment { get; set; }

        /// <summary>
        /// Report level rubric dimensions. Tech skill scores live here too, as dimensions keyed by
        /// skill name, so no separate column is needed for them.
        /// </summary>
        public string RubricRatingsJson { get; set; }

        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string ModifiedBy { get; set; }

        public List<AICodeInterviewReportQuestionRatingVM> QuestionRatings { get; set; }

        /// <summary>
        /// Deserialized view of <see cref="RubricRatingsJson"/>. Not persisted directly; the JSON
        /// column is the storage and this is what the form binds to and the diff engine reads.
        /// </summary>
        public List<AICodeInterviewRubricRatingVM> RubricRatings { get; set; }
    }

    /// <summary>
    /// Per question row of a report copy: the rating the reviewer gave that answer, their comment,
    /// and the transcript text shown beside the recorded answer.
    /// </summary>
    public class AICodeInterviewReportQuestionRatingVM
    {
        public int id { get; set; }
        public int ReportId { get; set; }

        /// <summary>SkillChannelSampleQuestions.id, as carried by BotInterviewQuestionVideos.DeliveryHeadQuestionId.</summary>
        public int? QuestionId { get; set; }

        /// <summary>BotInterviewQuestionVideos.id, so the row can be tied back to the recorded video.</summary>
        public int? BotInterviewQuestionVideoId { get; set; }

        /// <summary>Position of the question in the interview. Used to pair AI and expert rows when diffing.</summary>
        public int DisplayOrder { get; set; }

        public string QuestionText { get; set; }
        public decimal? Rating { get; set; }
        public string Verdict { get; set; }
        public string Comment { get; set; }
        public decimal? CommunicationRating { get; set; }
        public string CommunicationComment { get; set; }

        /// <summary>Per question rubric dimensions, same shape as the report level column.</summary>
        public string RubricRatingsJson { get; set; }

        /// <summary>
        /// The text shown beside the recorded answer. The AI copy holds the speech to text
        /// transcript, the expert copy holds their corrected version of it.
        /// </summary>
        public string TranscriptText { get; set; }

        public List<AICodeInterviewRubricRatingVM> RubricRatings { get; set; }

        /// <summary>Answer video URL, populated for display only. Never persisted on this row.</summary>
        public string VideoLink { get; set; }

        /// <summary>Question video URL, populated for display only. Never persisted on this row.</summary>
        public string QuestionVideoLink { get; set; }

        /// <summary>
        /// Backend max for this question from the HM / Delivery Manager score rubric
        /// (the mapped skill's weightage). Display only; never persisted on this row.
        /// </summary>
        public decimal? MaxScore { get; set; }
    }

    /// <summary>
    /// A single rubric dimension and its rating. Tech skill scores use the same shape with
    /// <see cref="IsTechSkill"/> set, so the UI can label those rows "Tech skill score".
    /// </summary>
    public class AICodeInterviewRubricRatingVM
    {
        public string Dimension { get; set; }
        public decimal? Rating { get; set; }
        public string ExpectedLevel { get; set; }
        public string ObservedLevel { get; set; }
        public bool IsTechSkill { get; set; }

        /// <summary>
        /// Per-item max from the HM / Delivery Manager Instructions score rubric (skill weightage).
        /// Display only on the rubric review card; missing values render as an em dash.
        /// </summary>
        public decimal? MaxScore { get; set; }
    }
}
