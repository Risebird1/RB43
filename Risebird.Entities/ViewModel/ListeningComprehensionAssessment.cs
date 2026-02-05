using System;

namespace Risebird.Entities.ViewModel
{
    public class ListeningComprehensionAssessment
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string AudioUrl { get; set; }
        public string AudioTranscription { get; set; }
        public string CandidateSummary { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }
        public decimal TimeTakenInSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
