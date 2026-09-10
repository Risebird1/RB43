using System;
using System.Text;
using System.Text.RegularExpressions;
using Risebird.CommonHelper.Interface;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Helper class to optimize interview report text (PDF-extracted) before sending to OpenAI for QC analysis.
    /// Reduces token usage by removing PDF artifacts, repeated page headers/footers, and excessive whitespace.
    /// </summary>
    public class ReportQCOptimizationHelper : IReportQCOptimizationHelper
    {
        /// <summary>
        /// Optimizes PDF-extracted report text to reduce token count while preserving evaluation content
        /// </summary>
        public string OptimizeReportText(string reportText)
        {
            if (string.IsNullOrWhiteSpace(reportText))
                return reportText;

            // Step 1: Remove PDF extraction artifacts (non-printable characters, replacement chars)
            reportText = RemovePdfArtifacts(reportText);

            // Step 2: Remove standalone page numbers and "Page X of Y" patterns
            reportText = RemovePageNumbersAndHeaders(reportText);

            // Step 3: Remove excessive whitespace
            reportText = RemoveExcessiveWhitespace(reportText);

            // Step 4: Remove repeated lines (page headers/footers repeated across pages)
            reportText = RemoveRepeatedContent(reportText);

            // Step 5: Normalize line breaks
            reportText = NormalizeLineBreaks(reportText);

            // RemoveRepeatedContent rebuilds the text with AppendLine, which leaves a break after
            // the last line — trim it so the result never grows a newline the input did not have.
            return reportText.Trim();
        }

        /// <summary>
        /// Validates if PDF-extracted report text is worth sending to OpenAI.
        /// Catches corrupted PDFs and empty extractions before they incur an API call.
        /// </summary>
        public (bool isValid, string reason) ValidateReportForQC(string reportText)
        {
            if (string.IsNullOrWhiteSpace(reportText))
                return (false, "Report text is empty or null");

            if (reportText.Length < 200)
                return (false, "Report text too short (less than 200 characters) - PDF extraction likely failed");

            if (reportText.Length > 150000)
                return (false, "Report text too long (more than 150,000 characters) - likely corrupted");

            int alphabeticChars = 0;
            foreach (char c in reportText)
            {
                if (char.IsLetter(c))
                    alphabeticChars++;
            }

            double alphabeticRatio = (double)alphabeticChars / reportText.Length;
            if (alphabeticRatio < 0.1)
                return (false, "Report text appears to be corrupted (less than 10% alphabetic characters)");

            string lowerText = reportText.ToLower();
            bool hasReportKeywords = lowerText.Contains("interview") ||
                                     lowerText.Contains("candidate") ||
                                     lowerText.Contains("rating") ||
                                     lowerText.Contains("skill") ||
                                     lowerText.Contains("feedback") ||
                                     lowerText.Contains("recommendation") ||
                                     lowerText.Contains("score") ||
                                     lowerText.Contains("assessment");

            if (!hasReportKeywords)
                return (false, "Report text doesn't contain expected interview report keywords - PDF may be corrupted or wrong document");

            return (true, string.Empty);
        }

        /// <summary>
        /// Estimates token count (rough approximation: 1 token ≈ 4 characters)
        /// </summary>
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            return (int)Math.Ceiling(text.Length / 4.0);
        }

        #region Private Helper Methods

        private static string RemovePdfArtifacts(string text)
        {
            // Remove non-printable characters (keep newlines, carriage returns, tabs, and printable ASCII + Latin-1)
            text = Regex.Replace(text, @"[^\x09\x0A\x0D\x20-\x7E\xA0-\xFF]", "");

            // Remove Unicode replacement character
            text = text.Replace("\uFFFD", "");

            return text;
        }

        private static string RemovePageNumbersAndHeaders(string text)
        {
            // Remove lines that contain only a number (standalone page numbers)
            text = Regex.Replace(text, @"^\s*\d+\s*$", "", RegexOptions.Multiline);

            // Remove "Page X of Y" and "Page X" patterns
            text = Regex.Replace(text, @"Page\s+\d+\s+of\s+\d+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bPage\s+\d+\b", "", RegexOptions.IgnoreCase);

            return text;
        }

        private static string RemoveExcessiveWhitespace(string text)
        {
            // Replace multiple spaces with single space
            text = Regex.Replace(text, @" {2,}", " ");

            // Replace 3+ consecutive newlines with double newline (preserve section breaks)
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Replace tabs with single space
            text = text.Replace("\t", " ");

            // Remove trailing spaces from each line
            text = Regex.Replace(text, @" +$", "", RegexOptions.Multiline);

            return text.Trim();
        }

        private static string RemoveRepeatedContent(string text)
        {
            // Skip lines that appear multiple times and are short (likely page headers/footers)
            string[] lines = text.Split('\n');
            StringBuilder sb = new StringBuilder();
            string previousLine = string.Empty;
            int repeatCount = 0;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine == previousLine && trimmedLine.Length < 80)
                {
                    repeatCount++;
                    if (repeatCount < 2)
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    repeatCount = 0;
                    previousLine = trimmedLine;
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }

        private static string NormalizeLineBreaks(string text)
        {
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");

            return text;
        }

        #endregion
    }
}
