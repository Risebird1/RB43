namespace Risebird.Entities.ViewModel
{
    /// <summary>Display helpers for per-interviewer QC verdict aggregates on DM Bad Quality sub-tab.</summary>
    public static class DmIvQcVerdictAggregateHelper
    {
        public static string FormatSuspiciousRatio(int suspicious, int completed) =>
            completed > 0 ? $"{suspicious}/{completed} suspicious" : $"{suspicious} suspicious";

        public static string FormatRedoCount(int redo) =>
            redo == 1 ? "1 redo" : $"{redo} redo";
    }
}
