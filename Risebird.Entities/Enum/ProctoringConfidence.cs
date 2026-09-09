using System.Text.Json.Serialization;

namespace Risebird.Entities.Enum
{
    [JsonConverter(typeof(ProctoringCamelCaseEnumConverter<ProctoringConfidence>))]
    public enum ProctoringConfidence
    {
        Low,
        Medium,
        High
    }
}
