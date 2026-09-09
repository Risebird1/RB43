using System;
using System.Collections.Generic;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    public class ProctoringJoinRequest
    {
        public string SessionToken { get; set; }
        public string InterviewId { get; set; }
        public string CandidateEmail { get; set; }
        public string BrowserSessionId { get; set; }
        public ProctoringTrust Trust { get; set; }
    }

    public class ProctoringSessionActionRequest
    {
        public string SessionToken { get; set; }
    }

    public class ProctoringSessionResponse
    {
        public string SessionId { get; set; }
        public int RiskScore { get; set; }
    }

    public class ProctoringHeartbeatRequest
    {
        public string SessionToken { get; set; }
        public ProctoringSessionState ClientState { get; set; }
    }

    public class ProctoringHeartbeatResponse
    {
        public int RiskScore { get; set; }
        public ProctoringSessionState ClientState { get; set; }
    }

    public class ProctoringBlockedProcess
    {
        public string ProcessName { get; set; }
        public string Category { get; set; }
    }

    public class ProctoringPolicyResponse
    {
        public int Version { get; set; }
        public List<ProctoringBlockedProcess> BlockedProcesses { get; set; } = new List<ProctoringBlockedProcess>();
        public List<string> WatchedTitlePhrases { get; set; } = new List<string>();
        public double ForegroundFlickerFloorSeconds { get; set; }
        public int MaxQueuedEvents { get; set; }
    }

    public class ProctoringEventItem
    {
        public int Sequence { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public ProctoringSeverity Level { get; set; }
        public ProctoringConfidence Confidence { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string RedactedDetail { get; set; }
        public string RawDetail { get; set; }
    }

    public class ProctoringEventsRequest
    {
        public string SessionToken { get; set; }
        public List<ProctoringEventItem> Events { get; set; } = new List<ProctoringEventItem>();
    }

    public class ProctoringEventsResponse
    {
        public int RiskScore { get; set; }
    }

    public class ProctoringMintSessionRequest
    {
        public string InterviewId { get; set; }
        public string CandidateEmail { get; set; }
        public string BrowserSessionId { get; set; }
        public ProctoringTrust Trust { get; set; } = ProctoringTrust.Unverified;
        public int? TtlMinutes { get; set; }
    }

    public class ProctoringMintSessionResponse
    {
        public string LaunchToken { get; set; }
        public string SessionToken { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    public class ProctoringLogEntry
    {
        public string SessionToken { get; set; }
        public int Sequence { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Level { get; set; }
        public string Confidence { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string RedactedDetail { get; set; }
        public string RawDetail { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
