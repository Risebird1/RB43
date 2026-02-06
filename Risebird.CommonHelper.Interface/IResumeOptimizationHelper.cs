namespace Risebird.CommonHelper.Interface
{
    public interface IResumeOptimizationHelper
    {
        /// <summary>
        /// Optimizes resume text to reduce token count while preserving important information
        /// </summary>
        string OptimizeResumeText(string resumeText);

        /// <summary>
        /// Validates if resume is worth processing (basic quality checks)
        /// </summary>
        (bool isValid, string reason) ValidateResumeForParsing(string resumeText);

        /// <summary>
        /// Estimates token count (rough approximation: 1 token ≈ 4 characters)
        /// </summary>
        int EstimateTokenCount(string text);

        /// <summary>
        /// Determines if resume is simple enough for cheaper model
        /// </summary>
        bool IsSimpleResume(string resumeText);
    }
}
