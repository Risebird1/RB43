using System;

namespace Risebird.Entities.ViewModel
{
    public class CodingAssessment
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string CodeLanguage { get; set; }
        public string SubmittedCode { get; set; }
        public int? TimeLimitInMinutes { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }
        public decimal TimeTakenInSeconds { get; set; }
        public bool IsTimeExpired { get; set; }
        public string StdInput { get; set; }
        public string LastRunOutput { get; set; }
        public string LastRunStatus { get; set; }
        public int RunCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
