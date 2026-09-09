using System;

namespace Risebird.Entities.Data
{
    /// <summary>Row in ProctoringLogs — one persisted DesktopProctor violation/telemetry event.</summary>
    public class ProctoringApiLog
    {
        public long Id { get; set; }
        public string SessionToken { get; set; }
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
}
