using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risebird.Entities.Helpers
{
    /// <summary>
    /// Resolves a job description's title to the nearest existing Assessment Group by name.
    ///
    /// A job title taken off an arbitrary JD rarely equals an AG name character for character —
    /// "Sr. Backend Engineer (Kafka)" should still land on "Backend Developer". So seniority and
    /// punctuation are stripped, a few role-word synonyms are folded together, and what is left is
    /// compared both as a set of words and character by character.
    ///
    /// Nothing is ever created here: a score below the threshold returns no match, and the caller
    /// falls back to the catch-all AG. That matters — the AG is read by human-panel interviewer
    /// routing, so a confident wrong answer is worse than no answer.
    /// </summary>
    public static class AssessmentGroupMatcher
    {
        /// <summary>
        /// Default similarity a candidate must reach to be accepted, between 0 and 1. Deliberately
        /// cautious: below this the catch-all is the safer answer.
        /// </summary>
        public const double DefaultMinimumScore = 0.6d;

        /// <summary>Seniority and level words that say nothing about which AG a role belongs to.</summary>
        private static readonly HashSet<string> NoiseWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "senior", "sr", "junior", "jr", "lead", "principal", "staff", "associate",
            "trainee", "intern", "entry", "level", "mid", "chief", "head", "expert",
            "i", "ii", "iii", "iv", "v", "1", "2", "3", "4", "5"
        };

        /// <summary>
        /// Role words that mean the same thing across a job board. Each maps to a canonical form so
        /// "Java Engineer" and "Java Developer" compare as equal.
        /// </summary>
        private static readonly Dictionary<string, string> Synonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["engineer"] = "developer",
            ["engineering"] = "developer",
            ["developer"] = "developer",
            ["dev"] = "developer",
            ["programmer"] = "developer",
            ["coder"] = "developer",
            ["sde"] = "developer",
            ["tester"] = "qa",
            ["testing"] = "qa",
            ["quality"] = "qa",
            ["qe"] = "qa",
            ["sdet"] = "qa",
            ["frontend"] = "frontend",
            ["front"] = "frontend",
            ["ui"] = "frontend",
            ["backend"] = "backend",
            ["back"] = "backend",
            ["fullstack"] = "fullstack",
            ["full"] = "fullstack",
            ["analyst"] = "analyst",
            ["architect"] = "architect",
            ["administrator"] = "admin",
            ["admin"] = "admin",
            ["ops"] = "devops",
            ["devops"] = "devops"
        };

        /// <summary>One Assessment Group row, as little of it as matching needs.</summary>
        public sealed class Candidate
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>A scored candidate.</summary>
        public sealed class Match
        {
            public int Id { get; set; }
            public string Name { get; set; }

            /// <summary>Similarity between 0 and 1.</summary>
            public double Score { get; set; }
        }

        /// <summary>
        /// Best-matching Assessment Group id for <paramref name="jobTitle"/>, or 0 when nothing
        /// scores well enough and the caller should use the catch-all.
        /// </summary>
        public static int ResolveId(
            string jobTitle,
            IEnumerable<Candidate> candidates,
            double minimumScore = DefaultMinimumScore)
        {
            return FindBest(jobTitle, candidates, minimumScore)?.Id ?? 0;
        }

        /// <summary>
        /// Best match above <paramref name="minimumScore"/>, or null.
        ///
        /// Ties are broken by the shorter name and then the lower id, so the same job title always
        /// resolves to the same AG rather than drifting with row order.
        /// </summary>
        public static Match FindBest(
            string jobTitle,
            IEnumerable<Candidate> candidates,
            double minimumScore = DefaultMinimumScore)
        {
            if (string.IsNullOrWhiteSpace(jobTitle) || candidates == null) return null;

            var target = Prepare(jobTitle);
            if (target.IsEmpty) return null;

            Match best = null;

            foreach (var candidate in candidates)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Name)) continue;

                var score = Score(target, Prepare(candidate.Name));
                if (score < minimumScore) continue;

                if (best == null
                    || score > best.Score
                    || (score == best.Score && IsPreferredTieBreak(candidate, best)))
                {
                    best = new Match { Id = candidate.Id, Name = candidate.Name, Score = score };
                }
            }

            return best;
        }

        /// <summary>Similarity between two names, between 0 and 1. Exposed for diagnostics and tests.</summary>
        public static double Score(string left, string right) => Score(Prepare(left), Prepare(right));

        private static bool IsPreferredTieBreak(Candidate candidate, Match current)
        {
            var candidateLength = candidate.Name.Trim().Length;
            var currentLength = current.Name.Trim().Length;

            if (candidateLength != currentLength) return candidateLength < currentLength;
            return candidate.Id < current.Id;
        }

        #region Scoring

        private static double Score(Prepared left, Prepared right)
        {
            if (left.IsEmpty || right.IsEmpty) return 0d;

            // Same words after normalization — "Sr. Java Developer" vs "Java Engineer".
            if (string.Equals(left.Canonical, right.Canonical, StringComparison.Ordinal)) return 1d;

            var intersection = left.Tokens.Intersect(right.Tokens, StringComparer.Ordinal).Count();

            double tokenScore = 0d;
            if (intersection > 0)
            {
                var dice = 2d * intersection / (left.Tokens.Count + right.Tokens.Count);

                // Containment carries the "extra words on one side" case: a JD title like
                // "backend developer kafka microservices" still belongs to "Backend Developer".
                // Scored just under an outright word-for-word match.
                var containment = (double)intersection / Math.Min(left.Tokens.Count, right.Tokens.Count);

                tokenScore = Math.Max(dice, containment * 0.95d);
            }

            // Character similarity catches typos and joined words that tokenizing splits apart.
            var charScore = CharacterSimilarity(left.Canonical, right.Canonical) * 0.85d;

            return Math.Min(1d, Math.Max(tokenScore, charScore));
        }

        private static double CharacterSimilarity(string left, string right)
        {
            if (left.Length == 0 || right.Length == 0) return 0d;
            if (string.Equals(left, right, StringComparison.Ordinal)) return 1d;

            var distance = LevenshteinDistance(left, right);
            var longest = Math.Max(left.Length, right.Length);
            return 1d - ((double)distance / longest);
        }

        /// <summary>Two-row Levenshtein — the names compared here are short.</summary>
        private static int LevenshteinDistance(string left, string right)
        {
            var previous = new int[right.Length + 1];
            var current = new int[right.Length + 1];

            for (var j = 0; j <= right.Length; j++) previous[j] = j;

            for (var i = 1; i <= left.Length; i++)
            {
                current[0] = i;

                for (var j = 1; j <= right.Length; j++)
                {
                    var substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
                }

                Array.Copy(current, previous, current.Length);
            }

            return previous[right.Length];
        }

        #endregion

        #region Normalization

        private sealed class Prepared
        {
            public List<string> Tokens { get; set; } = new List<string>();

            /// <summary>Tokens joined back together, for the character-level comparison.</summary>
            public string Canonical { get; set; } = string.Empty;

            public bool IsEmpty => Tokens.Count == 0;
        }

        private static Prepared Prepare(string value)
        {
            var prepared = new Prepared();
            if (string.IsNullOrWhiteSpace(value)) return prepared;

            var tokens = Tokenize(value);
            if (tokens.Count == 0) return prepared;

            var meaningful = tokens.Where(t => !NoiseWords.Contains(t)).ToList();

            // A title made only of seniority words ("Senior II") has nothing else to go on, so keep
            // what was there rather than comparing an empty set against everything.
            var kept = meaningful.Count > 0 ? meaningful : tokens;

            prepared.Tokens = kept
                .Select(Canonicalize)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            prepared.Canonical = string.Concat(prepared.Tokens);
            return prepared;
        }

        private static List<string> Tokenize(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
                else builder.Append(' ');
            }

            return builder.ToString()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1 || char.IsDigit(t[0]))
                .ToList();
        }

        private static string Canonicalize(string token)
        {
            if (Synonyms.TryGetValue(token, out var canonical)) return canonical;

            // Fold simple plurals so "systems" and "system" agree.
            if (token.Length > 3 && token.EndsWith("s", StringComparison.Ordinal)
                && !token.EndsWith("ss", StringComparison.Ordinal))
            {
                var singular = token.Substring(0, token.Length - 1);
                return Synonyms.TryGetValue(singular, out var singularCanonical) ? singularCanonical : singular;
            }

            return token;
        }

        #endregion
    }
}
