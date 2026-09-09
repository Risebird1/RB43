using System.Text.Json.Serialization;

namespace Risebird.Entities.Enum
{
    [JsonConverter(typeof(ProctoringCamelCaseEnumConverter<ProctoringSeverity>))]
    public enum ProctoringSeverity
    {
        Info,
        Low,
        Medium,
        High
    }
}
