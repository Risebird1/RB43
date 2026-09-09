using System.Text.Json.Serialization;

namespace Risebird.Entities.Enum
{
    /// <summary>Serialized as the raw C# member name ("Unverified"/"Verified"), not camelCase — per the DesktopProctor API contract.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ProctoringTrust>))]
    public enum ProctoringTrust
    {
        Unverified,
        Verified
    }
}
