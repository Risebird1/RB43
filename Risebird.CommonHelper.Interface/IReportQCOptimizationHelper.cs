namespace Risebird.CommonHelper.Interface
{
    public interface IReportQCOptimizationHelper
    {
        /// <summary>
        /// Optimizes interview report text (PDF-extracted) to reduce token count before sending to OpenAI
        /// </summary>
        string OptimizeReportText(string reportText);

        /// <summary>
        /// Validates if report text is worth sending to OpenAI (basic quality checks)
        /// </summary>
        (bool isValid, string reason) ValidateReportForQC(string reportText);

        /// <summary>
        /// Estimates token count (rough approximation: 1 token ≈ 4 characters)
        /// </summary>
        int EstimateTokenCount(string text);
    }
}
