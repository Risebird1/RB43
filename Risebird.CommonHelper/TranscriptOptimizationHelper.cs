using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Risebird.CommonHelper.Interface;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Helper class to optimize transcript text before sending to OpenAI
    /// Reduces token usage by removing filler words, timestamps, and unnecessary content
    /// </summary>
    public class TranscriptOptimizationHelper : ITranscriptOptimizationHelper
    {
        /// <summary>
        /// Optimizes transcript text to reduce token count while preserving important conversation content
        /// </summary>
        public string OptimizeTranscriptText(string transcriptText)
        {
            if (string.IsNullOrWhiteSpace(transcriptText))
                return transcriptText;

            // Step 1: Remove timestamps (common formats: [00:00:00], (00:00), 00:00:00, etc.)
            transcriptText = RemoveTimestamps(transcriptText);

            // Step 2: Remove filler words and disfluencies
            transcriptText = RemoveFillerWords(transcriptText);

            // Step 3: Remove excessive speaker labels if repeated
            transcriptText = OptimizeSpeakerLabels(transcriptText);

            // Step 4: Remove excessive whitespace
            transcriptText = RemoveExcessiveWhitespace(transcriptText);

            // Step 5: Remove repeated phrases (stuttering, false starts)
            transcriptText = RemoveRepeatedPhrases(transcriptText);

            // Step 6: Normalize line breaks
            transcriptText = NormalizeLineBreaks(transcriptText);

            // RemoveRepeatedPhrases rebuilds the text with AppendLine, which leaves a break after
            // the last line — trim it so the result never grows a newline the input did not have.
            return transcriptText.Trim();
        }

        /// <summary>
        /// Validates if transcript is worth processing (basic quality checks)
        /// </summary>
        public (bool isValid, string reason) ValidateTranscriptForAnalysis(string transcriptText)
        {
            if (string.IsNullOrWhiteSpace(transcriptText))
                return (false, "Transcript text is empty or null");

            // Check minimum length (at least 200 characters for meaningful conversation)
            if (transcriptText.Length < 200)
                return (false, "Transcript text too short (less than 200 characters)");

            // Check maximum length (more than 100,000 characters is suspicious)
            if (transcriptText.Length > 100000)
                return (false, "Transcript text too long (more than 100,000 characters) - likely corrupted");

            // Check if it's mostly gibberish (less than 15% alphabetic characters)
            int alphabeticChars = 0;
            foreach (char c in transcriptText)
            {
                if (char.IsLetter(c))
                    alphabeticChars++;
            }

            double alphabeticRatio = (double)alphabeticChars / transcriptText.Length;
            if (alphabeticRatio < 0.15)
                return (false, "Transcript text appears to be corrupted (less than 15% alphabetic characters)");

            // Check for common transcript indicators (conversational language)
            string lowerText = transcriptText.ToLower();
            bool hasConversationalContent = lowerText.Contains("interview") ||
                                           lowerText.Contains("question") ||
                                           lowerText.Contains("answer") ||
                                           lowerText.Contains("tell me") ||
                                           lowerText.Contains("what") ||
                                           lowerText.Contains("how") ||
                                           lowerText.Contains("why") ||
                                           lowerText.Contains("experience") ||
                                           lowerText.Contains("think") ||
                                           lowerText.Contains("work");

            if (!hasConversationalContent)
                return (false, "Transcript text doesn't contain conversational keywords - might be corrupted");

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
        /// Determines if transcript is simple enough for cheaper model
        /// </summary>
        public bool IsSimpleTranscript(string transcriptText)
        {
            if (string.IsNullOrWhiteSpace(transcriptText))
                return false;

            // Simple transcript criteria:
            // 1. Less than 5000 characters (about 1250 tokens)
            // 2. Short duration (estimated)
            // 3. Limited speakers

            if (transcriptText.Length > 5000)
                return false;

            string lowerText = transcriptText.ToLower();

            // Check for complex topics that might need more sophisticated analysis
            bool hasComplexTopics = lowerText.Contains("architecture") ||
                                   lowerText.Contains("algorithm") ||
                                   lowerText.Contains("design pattern") ||
                                   lowerText.Contains("technical") ||
                                   lowerText.Contains("system design");

            if (hasComplexTopics)
                return false;

            // Check number of speakers (if labeled)
            int speakerCount = CountSpeakers(transcriptText);
            if (speakerCount > 3)
                return false;

            return true;
        }

        #region Private Helper Methods

        private static string RemoveTimestamps(string text)
        {
            // Remove various timestamp formats
            // [00:00:00] or [00:00]
            text = Regex.Replace(text, @"\[\d{1,2}:\d{2}(:\d{2})?\]", "", RegexOptions.IgnoreCase);
            
            // (00:00:00) or (00:00)
            text = Regex.Replace(text, @"\(\d{1,2}:\d{2}(:\d{2})?\)", "", RegexOptions.IgnoreCase);
            
            // 00:00:00 or 00:00 at start of line
            text = Regex.Replace(text, @"^\d{1,2}:\d{2}(:\d{2})?\s*", "", RegexOptions.Multiline);
            
            // Timestamp with milliseconds: 00:00:00.000
            text = Regex.Replace(text, @"\d{1,2}:\d{2}:\d{2}\.\d{3}", "", RegexOptions.IgnoreCase);

            return text;
        }

        private static string RemoveFillerWords(string text)
        {
            // Common filler words and disfluencies in spoken language
            string[] fillerWords = new[]
            {
                @"\bum+\b",
                @"\buh+\b",
                @"\bumm+\b",
                @"\buhh+\b",
                @"\ber+\b",
                @"\berr+\b",
                @"\blike\b",        // "like" as filler (context-dependent, but generally safe to remove)
                @"\byou know\b",
                @"\bI mean\b",
                @"\bkind of\b",
                @"\bsort of\b",
                @"\bbasically\b",
                @"\bactually\b",    // Often used as filler
                @"\bliterally\b",   // Often misused as filler
                @"\bwell\b",        // At start of sentences
                @"\bso\b",          // Often used as filler
                @"\bjust\b",        // Often redundant
                @"\breally\b",      // Often redundant
            };

            foreach (var filler in fillerWords)
            {
                // Remove with surrounding spaces
                text = Regex.Replace(text, $@"\s+{filler}\s+", " ", RegexOptions.IgnoreCase);
                // Remove at start of line
                text = Regex.Replace(text, $@"^{filler}\s+", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                // Remove at end of line
                text = Regex.Replace(text, $@"\s+{filler}$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }

            return text;
        }

        private static string OptimizeSpeakerLabels(string text)
        {
            // If speaker labels are too frequent (every line), condense them
            // Common formats: "Speaker 1:", "Interviewer:", "Candidate:", "John:", etc.
            
            // Count speaker label occurrences
            int speakerLabelCount = Regex.Matches(text, @"^[A-Za-z\s]+\d*:\s*", RegexOptions.Multiline).Count;
            int lineCount = text.Split('\n').Length;

            // If almost every line has a speaker label, it's likely over-labeled
            // Keep labels but remove redundant ones (same speaker consecutive lines)
            if (speakerLabelCount > lineCount * 0.5)
            {
                string[] lines = text.Split('\n');
                StringBuilder sb = new StringBuilder();
                string previousSpeaker = string.Empty;

                foreach (string line in lines)
                {
                    Match match = Regex.Match(line, @"^([A-Za-z\s]+\d*):\s*(.*)$");
                    if (match.Success)
                    {
                        string speaker = match.Groups[1].Value;
                        string content = match.Groups[2].Value;

                        // If same speaker as previous line, just append content without label
                        if (speaker == previousSpeaker)
                        {
                            sb.AppendLine(content);
                        }
                        else
                        {
                            sb.AppendLine(line);
                            previousSpeaker = speaker;
                        }
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }

                return sb.ToString();
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

            // Remove trailing spaces from lines
            text = Regex.Replace(text, @" +$", "", RegexOptions.Multiline);

            return text.Trim();
        }

        private static string RemoveRepeatedPhrases(string text)
        {
            // Remove stuttering and false starts (e.g., "I I I think" -> "I think")
            // Match word repeated 2+ times with optional punctuation
            text = Regex.Replace(text, @"\b(\w+)(\s+\1){2,}\b", "$1", RegexOptions.IgnoreCase);

            // Remove partial word repetitions (e.g., "th-th-that" -> "that")
            text = Regex.Replace(text, @"\b(\w{1,3})-\1-(\w+)", "$1$2", RegexOptions.IgnoreCase);

            return text;
        }

        private static string NormalizeLineBreaks(string text)
        {
            // Convert all line breaks to \n
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");

            return text;
        }

        private static int CountSpeakers(string text)
        {
            // Count unique speaker labels (e.g., "Speaker 1:", "Interviewer:", etc.)
            HashSet<string> speakers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            MatchCollection matches = Regex.Matches(text, @"^([A-Za-z\s]+\d*):", RegexOptions.Multiline);
            
            foreach (Match match in matches)
            {
                speakers.Add(match.Groups[1].Value.Trim());
            }

            return speakers.Count;
        }

        #endregion
    }
}
