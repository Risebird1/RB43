namespace Risebird.Entities.ViewModel
{
    /// <summary>AI interview question sets — the channel's explicit opt-in.</summary>
    public partial class IRFVm
    {
        /// <summary>
        /// Whether this channel deals its questions into balanced sets. False means the pre-feature
        /// question picker applies, which is what keeps channels that have not adopted the feature
        /// behaving exactly as they did before it existed.
        /// </summary>
        public bool UseAIQuestionSets { get; set; }
    }
}
