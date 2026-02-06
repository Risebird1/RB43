using System;

namespace Risebird.Entities.ViewModel
{
    public class TypingSpeedAssessment
    {
        public int id { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public string SampleText { get; set; }
        public string TypedText { get; set; }
        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }
        public decimal TimeTakenInSeconds { get; set; }
        public int TotalCharacters { get; set; }
        public int CorrectCharacters { get; set; }
        public decimal AccuracyPercentage { get; set; }
        public decimal WordsPerMinute { get; set; }
        public decimal PassingWPM { get; set; } = 30.00M;
        public decimal PassingAccuracyPercentage { get; set; } = 97.00M;
        public bool? IsPassed { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
