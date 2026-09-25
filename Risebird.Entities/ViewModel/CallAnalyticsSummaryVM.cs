using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Per-caller aggregate of CallRecordingData rows, used by the Call Analytics dashboard.
    /// </summary>
    public class CallAnalyticsSummaryVM
    {
        public string Caller { get; set; }
        public int TotalCalls { get; set; }
        public int CandidateCalls { get; set; }
        public int InterviewerCalls { get; set; }
        public int OtherCalls { get; set; }
        public int CallsWithRecording { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime? LastCallOn { get; set; }
    }
}
