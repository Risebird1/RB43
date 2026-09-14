using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Row in the dashboard's Proctoring Report master list — one ProctoringSessions row plus its log count.</summary>
    public class ProctoringSessionReportVM
    {
        public int Id { get; set; }
        public string SessionToken { get; set; }
        public string InterviewId { get; set; }
        public string CandidateEmail { get; set; }
        public string BrowserSessionId { get; set; }
        public string Trust { get; set; }
        public string State { get; set; }
        public string ClientState { get; set; }
        public int RiskScore { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? JoinedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? LastHeartbeatAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public int LogCount { get; set; }
    }

    /// <summary>Row in the dashboard's Proctoring Session Report — one ProctoringLogs row.</summary>
    public class ProctoringLogReportVM
    {
        public int Id { get; set; }
        public int ProctoringSessionId { get; set; }
        public int Sequence { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Confidence { get; set; }
        public DateTime OpenedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public string RedactedDetail { get; set; }
        public string RawDetail { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Row in the dashboard's Proctoring Session Report — one Screenshots row (blob reference, not the image).</summary>
    public class ProctoringScreenshotReportVM
    {
        public long Id { get; set; }
        public int ProctoringSessionId { get; set; }
        public int Sequence { get; set; }
        public string BlobUrl { get; set; }
        public string ContentType { get; set; }
        public DateTime CapturedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full-page Proctoring Session Report: session header + its logs + its screenshots.</summary>
    public class ProctoringSessionReportDetailVM
    {
        public ProctoringSessionReportVM Session { get; set; }
        public IEnumerable<ProctoringLogReportVM> Logs { get; set; }
        public IEnumerable<ProctoringScreenshotReportVM> Screenshots { get; set; }
    }
}
