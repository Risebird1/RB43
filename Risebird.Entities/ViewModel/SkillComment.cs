using System.ComponentModel.DataAnnotations;
using System;

namespace Risebird.Entities.ViewModel
{
    public class SkillComment
    {
        public int id { get; set; }
        public int IRFId { get; set; }

        //[Required]
        public string Skill { get; set; }
        public string SkillName { get; set; }

        [Required]
        public string Comment { get; set; }
        public int? UserId { get; set; }
        public string CreatedBy { get; set; }
        public string AIQuestionVideoLink { get; set; }
        public string ExpectedAnswer { get; set; }
        public bool IsFlaggedForSentimentAnalysis { get; set; }
        public string GeneratedPayload { get; set; }
        public bool IsGeneratedByAi { get; set; }
        public bool IsCodingQuestion { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }

        /// <summary>
        /// Plain-language summary of a coding question, narrated by the Synthesia avatar in
        /// place of the question text. Code read aloud verbatim is unintelligible.
        /// Mandatory for coding questions, unused for every other type.
        /// </summary>
        public string QuestionGist { get; set; }

        /// <summary>
        /// AI-written model answer for HM-saved bank suggestions. Distinct from ExpectedAnswer
        /// used by preset AI interviews.
        /// </summary>
        public string IdealAnswer { get; set; }

        /// <summary>
        /// True when the row was saved from the HM AI question picker (not Generate with AI).
        /// </summary>
        public bool IsAIGenerated { get; set; }

        public int MaxScore { get; set; }
        public string ExpectedOutput { get; set; }
        public string AnswerExplanation { get; set; }
        public string SafeImplementation { get; set; }
        public string SemanticMatchCriteria { get; set; }
        public string MisconceptionRedFlags { get; set; }

        /// <summary>0 None · 1 Generating · 2 Ready · 3 Stale · 4 Failed</summary>
        public byte AIVideoStatus { get; set; }
        public DateTime? AIVideoGeneratedOn { get; set; }

        /// <summary>Immutable AI original, stamped once at generation.</summary>
        public string AIDraftPayload { get; set; }

        /// <summary>NULL = legacy (served). 0 Draft · 1 Confirmed · 2 Rejected</summary>
        public byte? VettingStatus { get; set; }
        public string VettedBy { get; set; }
        public DateTime? VettedOn { get; set; }
        public string RejectionReason { get; set; }
        public string QuestionSlot { get; set; }
        public string QuestionVariant { get; set; }
        public int? RubricItemId { get; set; }
    }
}
