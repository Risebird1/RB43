namespace Risebird.Entities.Enum
{
    /// <summary>
    /// Severity of a single field level disagreement between the AI report and the expert copy.
    /// <see cref="None"/> is used for fields that are recorded side by side for context only
    /// (free text comments), where a difference in wording is not itself a quality signal.
    /// </summary>
    public enum AICodeInterviewDiscrepancySeverityEnum
    {
        None = 0,
        Minor = 1,
        Major = 2,
        Critical = 3
    }
}
