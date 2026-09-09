using System;

namespace Risebird.Entities.Enum
{
    /// <summary>Converts the DesktopProctor enums to/from the same camelCase wire strings used in the JSON contract, for DB storage.</summary>
    public static class ProctoringEnumExtensions
    {
        public static string ToWireString(this ProctoringSeverity value) => value switch
        {
            ProctoringSeverity.Info => "info",
            ProctoringSeverity.Low => "low",
            ProctoringSeverity.Medium => "medium",
            ProctoringSeverity.High => "high",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToWireString(this ProctoringConfidence value) => value switch
        {
            ProctoringConfidence.Low => "low",
            ProctoringConfidence.Medium => "medium",
            ProctoringConfidence.High => "high",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        public static string ToWireString(this ProctoringSessionState value) => value switch
        {
            ProctoringSessionState.Idle => "idle",
            ProctoringSessionState.Starting => "starting",
            ProctoringSessionState.Running => "running",
            ProctoringSessionState.Degraded => "degraded",
            ProctoringSessionState.Faulted => "faulted",
            ProctoringSessionState.Stopped => "stopped",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        /// <summary>Trust is sent over the wire as the raw C# member name ("Unverified"/"Verified"), so this is the same string used for DB storage.</summary>
        public static string ToWireString(this ProctoringTrust value) => value.ToString();

        public static ProctoringSessionState ParseSessionState(string value) => value?.Trim().ToLowerInvariant() switch
        {
            "idle" => ProctoringSessionState.Idle,
            "starting" => ProctoringSessionState.Starting,
            "running" => ProctoringSessionState.Running,
            "degraded" => ProctoringSessionState.Degraded,
            "faulted" => ProctoringSessionState.Faulted,
            "stopped" => ProctoringSessionState.Stopped,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unrecognized SessionState.")
        };

        public static ProctoringTrust ParseTrust(string value) => value?.Trim() switch
        {
            "Verified" => ProctoringTrust.Verified,
            "Unverified" => ProctoringTrust.Unverified,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unrecognized Trust.")
        };
    }
}
