namespace Risebird.Entities.Enum
{
    /// <summary>
    /// Distinguishes the two copies of an AI code interview report that are stored side by side:
    /// the report the assistant produced and the expert's edited copy of it.
    /// </summary>
    public enum AICodeInterviewReportSourceEnum
    {
        AI = 0,
        Expert = 1
    }
}
