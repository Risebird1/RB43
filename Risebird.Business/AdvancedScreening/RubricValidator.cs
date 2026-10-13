using Newtonsoft.Json;
using Risebird.Entities.ViewModel.AdvancedScreening;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// Pure rules for a rubric. It enforces exactly two things - all 27 catalog keys are present
    /// (once each), and the weights total 300 - plus the shape rules that keep it renderable:
    /// sections and labels always come from the catalog, never the model, and a weight-0
    /// criterion always reads "Not used for this role".
    /// </summary>
    public static class RubricValidator
    {
        /// <summary>Strips ```json fences and parses a rubric; null when the text is not one.</summary>
        public static ScreeningRubric Parse(string raw)
        {
            var cleaned = AdvancedScreeningScorer.StripJsonFence(raw);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<ScreeningRubric>(cleaned);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Puts a rubric into canonical form: catalog order, catalog sections and labels, trimmed
        /// text, no negative weights, "Not used for this role" on every weight-0 criterion, unknown
        /// keys dropped, and TotalWeight recomputed. Missing keys are not invented - Validate
        /// reports them.
        /// </summary>
        public static ScreeningRubric Normalize(ScreeningRubric rubric)
        {
            if (rubric == null)
            {
                return null;
            }

            var byKey = new Dictionary<string, RubricCriterion>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in rubric.Criteria ?? new List<RubricCriterion>())
            {
                if (c?.Key != null && RubricCriteria.Find(c.Key) != null && !byKey.ContainsKey(c.Key.Trim()))
                {
                    byKey[c.Key.Trim()] = c;
                }
            }

            var criteria = new List<RubricCriterion>();
            foreach (var def in RubricCriteria.All)
            {
                if (!byKey.TryGetValue(def.Key, out var c))
                {
                    continue;
                }
                var weight = Math.Max(0, c.Weight);
                var expected = (c.Expected ?? string.Empty).Trim();
                if (weight == 0 || string.IsNullOrWhiteSpace(expected) || IsPlaceholder(expected))
                {
                    expected = weight == 0 ? RubricCriteria.NotUsedText : expected;
                }
                criteria.Add(new RubricCriterion
                {
                    Key = def.Key,
                    Section = def.Section,
                    Label = def.Label,
                    Expected = expected,
                    Weight = weight
                });
            }

            return new ScreeningRubric
            {
                JdTitle = string.IsNullOrWhiteSpace(rubric.JdTitle) ? null : rubric.JdTitle.Trim(),
                Criteria = criteria,
                Dealbreakers = (rubric.Dealbreakers ?? new List<string>())
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                TotalWeight = criteria.Sum(c => c.Weight)
            };
        }

        private static bool IsPlaceholder(string expected)
        {
            var e = expected.Trim().ToLowerInvariant();
            return e == "unknown" || e == "n/a" || e == "na" || e == "none" || e == "-";
        }

        public static int Total(ScreeningRubric rubric) => rubric?.Criteria?.Sum(c => Math.Max(0, c.Weight)) ?? 0;

        public static List<string> MissingKeys(ScreeningRubric rubric)
        {
            var present = new HashSet<string>((rubric?.Criteria ?? new List<RubricCriterion>())
                .Where(c => c?.Key != null).Select(c => c.Key), StringComparer.OrdinalIgnoreCase);
            return RubricCriteria.All.Where(d => !present.Contains(d.Key)).Select(d => d.Key).ToList();
        }

        /// <summary>Every rule broken, as messages for a recruiter. Empty means the rubric can be approved.</summary>
        public static List<string> Validate(ScreeningRubric rubric)
        {
            var errors = new List<string>();
            if (rubric?.Criteria == null)
            {
                errors.Add("The rubric is empty.");
                return errors;
            }

            var missing = MissingKeys(rubric);
            if (missing.Count > 0)
            {
                errors.Add("Missing criteria: " + string.Join(", ", missing.Select(k => RubricCriteria.Find(k)?.Label ?? k)) + ".");
            }

            var duplicates = rubric.Criteria.Where(c => c?.Key != null)
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                errors.Add("Criteria listed more than once: " + string.Join(", ", duplicates) + ".");
            }

            var unknown = rubric.Criteria.Where(c => c?.Key == null || RubricCriteria.Find(c.Key) == null)
                .Select(c => c?.Key ?? "(blank)").ToList();
            if (unknown.Count > 0)
            {
                errors.Add("Unknown criteria: " + string.Join(", ", unknown) + ".");
            }

            if (rubric.Criteria.Any(c => c != null && c.Weight < 0))
            {
                errors.Add("Weights cannot be negative. Use 0 to switch a criterion off.");
            }

            var total = Total(rubric);
            if (total != RubricCriteria.TotalWeight)
            {
                errors.Add($"Weights total {total}; they must total exactly {RubricCriteria.TotalWeight}.");
            }

            return errors;
        }

        /// <summary>
        /// Scales the weights so they total exactly 300, keeping their proportions. Largest-
        /// remainder rounding, so the rounded weights still add up to 300 and no weight that was 0
        /// becomes non-zero. When every weight is 0 the catalog defaults are used.
        /// </summary>
        public static ScreeningRubric RepairWeights(ScreeningRubric rubric)
        {
            if (rubric?.Criteria == null || rubric.Criteria.Count == 0)
            {
                return rubric;
            }

            var weights = rubric.Criteria.Select(c => Math.Max(0, c.Weight)).ToList();
            var total = weights.Sum();
            if (total == RubricCriteria.TotalWeight)
            {
                rubric.TotalWeight = total;
                return rubric;
            }

            if (total == 0)
            {
                weights = rubric.Criteria.Select(c => RubricCriteria.Find(c.Key)?.DefaultWeight ?? 0).ToList();
                total = weights.Sum();
                if (total == 0)
                {
                    return rubric;
                }
            }

            var exact = weights.Select(w => (decimal)w * RubricCriteria.TotalWeight / total).ToList();
            var floored = exact.Select(x => (int)Math.Floor(x)).ToList();
            var shortfall = RubricCriteria.TotalWeight - floored.Sum();
            var order = exact
                .Select((x, i) => new { i, remainder = x - Math.Floor(x) })
                .Where(x => weights[x.i] > 0)
                .OrderByDescending(x => x.remainder)
                .ThenBy(x => x.i)
                .ToList();
            for (var k = 0; k < shortfall && order.Count > 0; k++)
            {
                floored[order[k % order.Count].i]++;
            }

            for (var i = 0; i < rubric.Criteria.Count; i++)
            {
                rubric.Criteria[i].Weight = floored[i];
                if (floored[i] == 0)
                {
                    rubric.Criteria[i].Expected = RubricCriteria.NotUsedText;
                }
            }
            rubric.TotalWeight = rubric.Criteria.Sum(c => c.Weight);
            return rubric;
        }

        /// <summary>The sum of each section's weights.</summary>
        public static Dictionary<string, int> SectionTotals(ScreeningRubric rubric)
        {
            return RubricCriteria.Sections.ToDictionary(
                s => s,
                s => rubric?.Criteria?.Where(c => c.Section == s).Sum(c => Math.Max(0, c.Weight)) ?? 0);
        }
    }
}
