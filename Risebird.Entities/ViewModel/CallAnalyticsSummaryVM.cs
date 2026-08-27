using System;

namespace Risebird.Entities.ViewModel
{
    public class CallAnalyticsSummaryVM
    {
        public string Caller { get; set; }
        public int TotalCalls { get; set; }
        public int CandidateCalls { get; set; }
        public int InterviewerCalls { get; set; }
        public int OtherCalls { get; set; }
        public int CallsWithRecording { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime? LastCallAt { get; set; }
    }
}
