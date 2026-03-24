namespace Risebird.Entities.Data
{
    /// <summary>Persisted merged finding: only category and flag, combined category-wise in MergedAIQCDetails JSON.</summary>
    public class QcInterviewAIMergedFindingItem
    {
        public string category { get; set; }
        /// <summary>Distinct flags for this category, joined when multiple (e.g. comma-separated).</summary>
        public string flag { get; set; }
    }
}
