using System.Text.Json;
using System.Text.Json.Serialization;

namespace Risebird.Entities.Enum
{
    /// <summary>Serializes an enum as its camelCase name (e.g. "high", not "High" or 3) for the DesktopProctor API contract.</summary>
    public sealed class ProctoringCamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
        where TEnum : struct, System.Enum
    {
        public ProctoringCamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase)
        {
        }
    }
}
