using System;

namespace Risebird.Entities.Data
{
    /// <summary>Row in ProctoringSessions — lifecycle + cumulative risk score for one DesktopProctor session token.</summary>
    public class ProctoringApiSession
    {
        public string SessionToken { get; set; }
        public string InterviewId { get; set; }
        public string CandidateEmail { get; set; }
        public string BrowserSessionId { get; set; }
        /// <summary>Raw C# member name: "Unverified" or "Verified".</summary>
        public string Trust { get; set; }
        /// <summary>Server-tracked lifecycle: idle | starting | running | stopped (set by join/start/end, not by client heartbeats).</summary>
        public string State { get; set; }
        /// <summary>Last clientState reported by a heartbeat call.</summary>
        public string ClientState { get; set; }
        public int RiskScore { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? JoinedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? LastHeartbeatAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
