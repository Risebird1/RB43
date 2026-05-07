using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Row in ProctoringSessionEvents — extension telemetry and session lifecycle (join/start/stop/screenshot meta).
    /// </summary>
    public class ProctoringSessionEvent
    {
        public long Id { get; set; }
        /// <summary>UTC timestamp (audit).</summary>
        public DateTime CreatedAtUtc { get; set; }
        /// <summary>India Standard Time wall clock for reporting (same instant as CreatedAtUtc).</summary>
        public DateTime? CreatedAtIst { get; set; }
        public int InterviewId { get; set; }
        public string CandidateEmail { get; set; }
        /// <summary>telemetry | join | start | stop | screenshot | heartbeat | screen_chunk</summary>
        public string EventKind { get; set; }
        public string Level { get; set; }
        public string Reason { get; set; }
        public string MetadataJson { get; set; }
        public int? ScreenshotPayloadLength { get; set; }
    }
}
