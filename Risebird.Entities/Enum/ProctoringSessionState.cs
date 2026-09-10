using System.Text.Json.Serialization;

namespace Risebird.Entities.Enum
{
    [JsonConverter(typeof(ProctoringCamelCaseEnumConverter<ProctoringSessionState>))]
    public enum ProctoringSessionState
    {
        Idle,
        Starting,
        Running,
        Degraded,
        Faulted,
        Stopped
    }
}
