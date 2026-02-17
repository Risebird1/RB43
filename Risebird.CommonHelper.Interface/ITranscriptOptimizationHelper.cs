namespace Risebird.CommonHelper.Interface
{
    public interface ITranscriptOptimizationHelper
    {
        /// <summary>
        /// Optimizes transcript text to reduce token count while preserving important conversation content
        /// </summary>
        string OptimizeTranscriptText(string transcriptText);

        /// <summary>
        /// Validates if transcript is worth processing (basic quality checks)
        /// </summary>
        (bool isValid, string reason) ValidateTranscriptForAnalysis(string transcriptText);

        /// <summary>
        /// Estimates token count (rough approximation: 1 token ≈ 4 characters)
        /// </summary>
        int EstimateTokenCount(string text);

        /// <summary>
        /// Determines if transcript is simple enough for cheaper model
        /// </summary>
        bool IsSimpleTranscript(string transcriptText);
    }
}
