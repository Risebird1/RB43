using System.ComponentModel.DataAnnotations;

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
    }
}
