using System;
using System.Text;
using System.Text.RegularExpressions;
using Risebird.CommonHelper.Interface;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Helper class to optimize resume text before sending to OpenAI
    /// Reduces token usage by removing unnecessary content
    /// </summary>
    public class ResumeOptimizationHelper : IResumeOptimizationHelper
    {
        /// <summary>
        /// Optimizes resume text to reduce token count while preserving important information
        /// </summary>
        public string OptimizeResumeText(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                return resumeText;

            // Step 1: Remove common boilerplate phrases
            resumeText = RemoveBoilerplate(resumeText);

            // Step 2: Remove excessive whitespace
            resumeText = RemoveExcessiveWhitespace(resumeText);

            // Step 3: Remove repeated headers/footers
            resumeText = RemoveRepeatedContent(resumeText);

            // Step 4: Normalize line breaks
            resumeText = NormalizeLineBreaks(resumeText);

            // RemoveRepeatedContent rebuilds the text with AppendLine, which leaves a break after
            // the last line — trim it so the result never grows a newline the input did not have.
            return resumeText.Trim();
        }

        /// <summary>
        /// Validates if resume is worth processing (basic quality checks)
        /// </summary>
        public (bool isValid, string reason) ValidateResumeForParsing(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                return (false, "Resume text is empty or null");

            // Check minimum length (at least 100 characters)
            if (resumeText.Length < 100)
                return (false, "Resume text too short (less than 100 characters)");

            // Check maximum length (more than 50,000 characters is suspicious)
            if (resumeText.Length > 50000)
                return (false, "Resume text too long (more than 50,000 characters) - likely corrupted");

            // Check if it's mostly gibberish (less than 10% alphabetic characters)
            int alphabeticChars = 0;
            foreach (char c in resumeText)
            {
                if (char.IsLetter(c))
                    alphabeticChars++;
            }

            double alphabeticRatio = (double)alphabeticChars / resumeText.Length;
            if (alphabeticRatio < 0.1)
                return (false, "Resume text appears to be corrupted (less than 10% alphabetic characters)");

            // Check for common resume keywords (at least one should be present)
            string lowerText = resumeText.ToLower();
            bool hasResumeKeywords = lowerText.Contains("experience") ||
                                    lowerText.Contains("education") ||
                                    lowerText.Contains("skills") ||
                                    lowerText.Contains("work") ||
                                    lowerText.Contains("university") ||
                                    lowerText.Contains("college") ||
                                    lowerText.Contains("email") ||
                                    lowerText.Contains("phone");

            if (!hasResumeKeywords)
                return (false, "Resume text doesn't contain common resume keywords - might be corrupted");

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

        /// <summary>
        /// Determines if resume is simple enough for cheaper model
        /// </summary>
        public bool IsSimpleResume(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                return false;

            // Simple resume criteria:
            // 1. Less than 3000 characters (about 750 tokens)
            // 2. No PhD
            // 3. Estimated experience <= 3 years

            if (resumeText.Length > 3000)
                return false;

            string lowerText = resumeText.ToLower();

            // Check for PhD
            if (lowerText.Contains("phd") || lowerText.Contains("ph.d") || 
                lowerText.Contains("doctorate") || lowerText.Contains("doctoral"))
                return false;

            // Estimate years of experience (very rough)
            int estimatedYears = EstimateYearsOfExperience(resumeText);
            if (estimatedYears > 3)
                return false;

            return true;
        }

        #region Private Helper Methods

        private static string RemoveBoilerplate(string text)
        {
            // Common boilerplate phrases to remove
            string[] boilerplate = new[]
            {
                "References available upon request",
                "References available on request",
                "References upon request",
                "Available upon request",
                "Confidential Resume",
                "Curriculum Vitae",
                "Page \\d+ of \\d+",  // Page numbers
                "\\d+/\\d+/\\d+",      // Repeated dates in headers
            };

            foreach (var phrase in boilerplate)
            {
                text = Regex.Replace(text, phrase, "", RegexOptions.IgnoreCase);
            }

            return text;
        }

        private static string RemoveExcessiveWhitespace(string text)
        {
            // Replace multiple spaces with single space
            text = Regex.Replace(text, @" {2,}", " ");

            // Replace multiple newlines with double newline (preserve paragraph breaks)
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Replace tabs with single space
            text = text.Replace("\t", " ");

            return text.Trim();
        }

        private static string RemoveRepeatedContent(string text)
        {
            // Remove lines that appear to be repeated headers/footers
            // Split into lines
            string[] lines = text.Split('\n');
            StringBuilder sb = new StringBuilder();
            string previousLine = string.Empty;
            int repeatCount = 0;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // If same as previous line and it's short (likely header/footer)
                if (trimmedLine == previousLine && trimmedLine.Length < 50)
                {
                    repeatCount++;
                    if (repeatCount < 2)  // Allow first repeat, skip subsequent
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
            // Convert all line breaks to \n
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");

            return text;
        }

        private static int EstimateYearsOfExperience(string resumeText)
        {
            // Look for year patterns (e.g., "2018-2023", "2018 to 2023")
            MatchCollection matches = Regex.Matches(resumeText, @"\b(19|20)\d{2}\b");

            if (matches.Count < 2)
                return 0;

            // Get all years mentioned
            int minYear = int.MaxValue;
            int maxYear = int.MinValue;

            foreach (Match match in matches)
            {
                if (int.TryParse(match.Value, out int year))
                {
                    if (year > 1990 && year <= DateTime.Now.Year)  // Valid range
                    {
                        minYear = Math.Min(minYear, year);
                        maxYear = Math.Max(maxYear, year);
                    }
                }
            }

            if (minYear == int.MaxValue)
                return 0;

            return maxYear - minYear;
        }

        #endregion
    }
}
