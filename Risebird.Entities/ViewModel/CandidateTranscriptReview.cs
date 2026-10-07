using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// What the candidate confirmed against what speech-to-text actually heard.
    ///
    /// The candidate reviews the transcript before submitting, so the stored answer is theirs, not
    /// the machine's. The raw transcript is kept alongside it and never overwritten: it is the
    /// tiebreaker on any dispute, and the divergence is what tells QC whether an edit fixed a
    /// mis-heard name or rewrote the answer.
    /// </summary>
    public class CandidateTranscriptReview
    {
        /// <summary>What speech-to-text returned.</summary>
        public string RawTranscript { get; set; }

        /// <summary>What the candidate submitted. This is the answer that gets scored.</summary>
        public string ConfirmedTranscript { get; set; }

        /// <summary>True when the two differ beyond whitespace and casing.</summary>
        public bool EditedByCandidate { get; set; }

        /// <summary>How much they differ, 0-100.</summary>
        public int DivergencePct { get; set; }

        /// <summary>
        /// Builds the review record. A confirmed transcript that is blank, or identical to the raw
        /// one, records no edit.
        /// </summary>
        public static CandidateTranscriptReview Build(string rawTranscript, string confirmedTranscript)
        {
            var raw = rawTranscript ?? string.Empty;
            var confirmed = string.IsNullOrWhiteSpace(confirmedTranscript) ? raw : confirmedTranscript;

            var divergence = ComputeDivergencePct(raw, confirmed);

            return new CandidateTranscriptReview
            {
                RawTranscript = raw,
                ConfirmedTranscript = confirmed,
                EditedByCandidate = divergence > 0,
                DivergencePct = divergence
            };
        }

        /// <summary>
        /// Word-level difference between the two, 0-100, rounded up so any real edit reads as at
        /// least 1%.
        ///
        /// Compared on words rather than characters because that is how a transcript goes wrong:
        /// "four years" heard as "fourteen years" is one word, not six characters. Case and
        /// punctuation are ignored — a candidate fixing a comma has not changed their answer.
        /// </summary>
        public static int ComputeDivergencePct(string rawTranscript, string confirmedTranscript)
        {
            var raw = Tokenize(rawTranscript);
            var confirmed = Tokenize(confirmedTranscript);

            if (raw.Count == 0 && confirmed.Count == 0) return 0;
            if (raw.Count == 0 || confirmed.Count == 0) return 100;

            var distance = WordLevenshtein(raw, confirmed);
            var longest = Math.Max(raw.Count, confirmed.Count);

            var pct = (int)Math.Ceiling(distance * 100.0 / longest);
            return Math.Min(100, Math.Max(0, pct));
        }

        /// <summary>Lowercased words, punctuation dropped, so only real wording differences count.</summary>
        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            var cleaned = new string(text.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ').ToArray());
            return cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>Levenshtein over words, two rows at a time.</summary>
        private static int WordLevenshtein(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            var previous = new int[right.Count + 1];
            var current = new int[right.Count + 1];

            for (var j = 0; j <= right.Count; j++) previous[j] = j;

            for (var i = 1; i <= left.Count; i++)
            {
                current[0] = i;

                for (var j = 1; j <= right.Count; j++)
                {
                    var substitution = previous[j - 1]
                        + (string.Equals(left[i - 1], right[j - 1], StringComparison.Ordinal) ? 0 : 1);
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
                }

                Array.Copy(current, previous, current.Length);
            }

            return previous[right.Count];
        }
    }
}
