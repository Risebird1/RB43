using Newtonsoft.Json;
using Risebird.Entities.ViewModel.AdvancedScreening;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// Everything about a screening result that is computed rather than asked of the model: the
    /// section and total scores, the percentage, the verdict, the dealbreaker override and the
    /// authenticity rating - and the validation that decides whether a response is usable at all.
    /// Pure and static, so every rule here is unit tested.
    /// </summary>
    public static class AdvancedScreeningScorer
    {
        public static string StripJsonFence(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                cleaned = firstNewline >= 0 ? cleaned.Substring(firstNewline + 1) : cleaned.TrimStart('`');
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            return cleaned.Trim();
        }

        /// <summary>Parses the scorer's JSON; on failure returns null and says why.</summary>
        public static AdvancedScreeningResult Parse(string raw, out string error)
        {
            error = null;
            var cleaned = StripJsonFence(raw);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                error = "The scorer returned an empty response.";
                return null;
            }
            try
            {
                var result = JsonConvert.DeserializeObject<AdvancedScreeningResult>(cleaned);
                if (result == null)
                {
                    error = "The scorer returned something that is not a JSON object.";
                }
                return result;
            }
            catch (JsonException ex)
            {
                error = "The scorer returned invalid JSON: " + ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Rejects a response that cannot be scored: a rubric criterion with weight &gt; 0 missing
        /// from it, or a score outside 0..weight. Returns null when it is usable.
        /// </summary>
        public static string Validate(AdvancedScreeningResult result, ScreeningRubric rubric)
        {
            if (result?.Criteria == null)
            {
                return "The scorer returned no criteria.";
            }

            var scored = result.Criteria.Where(c => c?.Key != null)
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var missing = rubric.Criteria.Where(c => c.Weight > 0 && !scored.ContainsKey(c.Key)).Select(c => c.Key).ToList();
            if (missing.Count > 0)
            {
                return "The scorer left out: " + string.Join(", ", missing) + ".";
            }

            foreach (var c in rubric.Criteria.Where(c => c.Weight > 0))
            {
                var s = scored[c.Key];
                if (s.Score < 0 || s.Score > c.Weight)
                {
                    return $"Score {s.Score} for {c.Key} is outside 0..{c.Weight}.";
                }
            }
            return null;
        }

        /// <summary>80%+ Strong Fit, 60-79 Good Fit, 40-59 Partial Fit, under 40 Not a Fit.</summary>
        public static string VerdictFor(decimal percentage)
        {
            if (percentage >= 80m) return ScreeningVerdicts.StrongFit;
            if (percentage >= 60m) return ScreeningVerdicts.GoodFit;
            if (percentage >= 40m) return ScreeningVerdicts.PartialFit;
            return ScreeningVerdicts.NotAFit;
        }

        /// <summary>Any High flag is High risk; two or more Medium flags is Review; otherwise Clean.</summary>
        public static string AuthenticityRatingFor(IEnumerable<AuthenticityFlag> flags)
        {
            var list = (flags ?? Enumerable.Empty<AuthenticityFlag>()).Where(f => f != null).ToList();
            if (list.Any(f => string.Equals(f.Severity?.Trim(), "High", StringComparison.OrdinalIgnoreCase)))
            {
                return AuthenticityRatings.HighRisk;
            }
            if (list.Count(f => string.Equals(f.Severity?.Trim(), "Medium", StringComparison.OrdinalIgnoreCase)) >= 2)
            {
                return AuthenticityRatings.Review;
            }
            return AuthenticityRatings.Clean;
        }

        /// <summary>
        /// Computes the totals onto <paramref name="result"/> and returns them. Each score is
        /// capped at its rubric weight; maxScore and matched are set from the rubric, not taken
        /// from the model; weight-0 criteria are dropped. The percentage is over 300.
        /// </summary>
        public static ComputedTotals Compute(AdvancedScreeningResult result, ScreeningRubric rubric,
            int rubricVersion, int instructionsVersion, DateTime screenedOnUtc)
        {
            var byKey = (result.Criteria ?? new List<CriterionScore>()).Where(c => c?.Key != null)
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var normalized = new List<CriterionScore>();
            foreach (var c in rubric.Criteria.Where(c => c.Weight > 0))
            {
                byKey.TryGetValue(c.Key, out var s);
                var score = Math.Min(Math.Max(s?.Score ?? 0m, 0m), c.Weight);
                normalized.Add(new CriterionScore
                {
                    Key = c.Key,
                    Score = score,
                    MaxScore = c.Weight,
                    Matched = score >= 0.6m * c.Weight,
                    Observation = s?.Observation
                });
            }
            result.Criteria = normalized;

            var sectionOf = rubric.Criteria.ToDictionary(c => c.Key, c => c.Section, StringComparer.OrdinalIgnoreCase);
            var sections = RubricCriteria.Sections.Select(section => new SectionTotal
            {
                Section = section,
                Score = normalized.Where(n => sectionOf[n.Key] == section).Sum(n => n.Score),
                MaxScore = rubric.Criteria.Where(c => c.Section == section).Sum(c => Math.Max(0, c.Weight))
            }).ToList();

            var total = normalized.Sum(n => n.Score);
            var percentage = Math.Round(total / RubricCriteria.TotalWeight * 100m, 2, MidpointRounding.AwayFromZero);
            var dealbreakerFailed = (result.DealbreakersFailed ?? new List<DealbreakerFailure>())
                .Any(d => !string.IsNullOrWhiteSpace(d?.Dealbreaker));

            result.Strengths = (result.Strengths ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Take(5).ToList();
            result.Concerns = (result.Concerns ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Take(5).ToList();
            result.Authenticity ??= new AuthenticityResult();
            result.Authenticity.Flags = (result.Authenticity.Flags ?? new List<AuthenticityFlag>()).Where(f => f != null).ToList();
            result.ProbeQuestions = (result.ProbeQuestions ?? new List<string>()).Where(q => !string.IsNullOrWhiteSpace(q)).ToList();

            result.Computed = new ComputedTotals
            {
                TotalScore = total,
                MaxScore = RubricCriteria.TotalWeight,
                OverallPercentage = percentage,
                Verdict = dealbreakerFailed ? ScreeningVerdicts.NotAFit : VerdictFor(percentage),
                DealbreakerOverride = dealbreakerFailed && VerdictFor(percentage) != ScreeningVerdicts.NotAFit,
                Sections = sections,
                AuthenticityRating = AuthenticityRatingFor(result.Authenticity.Flags),
                AuthenticityFlagCount = result.Authenticity.Flags.Count,
                RubricVersion = rubricVersion,
                InstructionsVersion = instructionsVersion,
                ScreenedOn = screenedOnUtc
            };
            return result.Computed;
        }

        /// <summary>
        /// Adds flags that rest on computed facts rather than on the model's reading, so a known
        /// injection attempt or blocklisted employer is never lost because the model missed it.
        /// The model's own flags are kept; a computed flag is only added when the model raised no
        /// flag of the same check.
        /// </summary>
        public static void MergeFactFlags(AdvancedScreeningResult result, VerifiedFacts facts)
        {
            if (result == null || facts == null)
            {
                return;
            }
            result.Authenticity ??= new AuthenticityResult();
            result.Authenticity.Flags ??= new List<AuthenticityFlag>();
            var flags = result.Authenticity.Flags;
            foreach (var f in flags.Where(f => f != null && string.IsNullOrWhiteSpace(f.Source)))
            {
                f.Source = "Resume";
            }

            void AddIfAbsent(string check, string severity, string evidence)
            {
                if (!flags.Any(f => string.Equals(f?.Check, check, StringComparison.OrdinalIgnoreCase)))
                {
                    flags.Add(new AuthenticityFlag { Check = check, Severity = severity, Evidence = evidence, Source = "Verified facts" });
                }
            }

            if (facts.SuspectedInjection?.Count > 0)
            {
                AddIfAbsent("PromptInjection", "High", "Instruction-like text in the resume: \"" + facts.SuspectedInjection[0] + "\"");
            }
            var blocklisted = facts.Companies?.FirstOrDefault(c => c.Blocklisted);
            if (blocklisted != null)
            {
                AddIfAbsent("CompanyGenuineness", "High", $"Employer '{blocklisted.Name}' is on the internal blocklist"
                    + (string.IsNullOrWhiteSpace(blocklisted.BlocklistReason) ? "." : ": " + blocklisted.BlocklistReason));
            }
            if (facts.Duplicates != null && facts.Duplicates.MatchingResumes >= 3)
            {
                AddIfAbsent("ProjectDuplicate", "Medium",
                    $"A project or summary paragraph is {facts.Duplicates.MaxProjectSimilarity:P0} similar to text in {facts.Duplicates.MatchingResumes} other candidates' resumes.");
            }
        }
    }
}
