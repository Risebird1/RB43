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
        public string ExpectedAnswer { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }
        public decimal TimeTakenInSeconds { get; set; }
        public int AudioPlayCount { get; set; }
        public bool IsTimeExpired { get; set; }
        public decimal? Score { get; set; }
        public decimal? MaxScore { get; set; }
        public bool? IsPassed { get; set; }
        public string OpenAIAnalysisJson { get; set; }
        public string OpenAIResponseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
