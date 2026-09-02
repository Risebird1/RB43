using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class TechSkillComments
    {
        public TechSkillComments()
        {
            SkillCollection = (IList<SkillComment>)(new List<SkillComment>());
            IrfName = string.Empty;
        }
        public int id { get; set; }
        public IList<SkillComment> SkillCollection { get; set; }
        public int IRFId { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Profile { get; set; }
        public SelectList Skills { get; set; }
        public string SkillsOptionsData { get; set; }
        public string IrfName { get; set; }
        public string CreatedBy { get; set; }
        public int interviewid { get; set; }
        public bool IsAIInterview { get; set; }
        public int AIInterviewType { get; set; }
        public string AIQuestionVideoLink { get; set; }
        public bool IsTypingSpeedAssessment { get; set; }
        public bool IsListeningComprehension { get; set; }
        public string ListeningAudioUrl { get; set; }
        public string ListeningAudioTranscription { get; set; }
        public string ExpectedAnswer { get; set; }
        public string QuestionGist { get; set; }
        public bool IsCodingQuestion { get; set; }
        public int? CodingTimeLimitInMinutes { get; set; }

        /// <summary>
        /// This "question" is the MCQ assessment section, served in an iframe from
        /// screen.risebird.io rather than asked by the avatar. Comment carries the spoken
        /// introduction; the questions themselves live on the MCQ side.
        /// </summary>
        public bool IsMcqAssessmentSection { get; set; }
    }
}
