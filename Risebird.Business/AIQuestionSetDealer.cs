using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    /// <summary>
    /// Deals additional question sets for a channel whose existing sets are already in use, from the
    /// questions no set holds yet. Deterministic, so the result is predictable and testable:
    ///   • the number of new sets is the smallest per-parameter count of unused questions;
    ///   • per parameter, code project questions are taken first, then coding questions, then the rest,
    ///     so a newly published code project question is never left out while there is room for it;
    ///   • code project and coding questions are spread across the new sets as evenly as possible.
    /// Every new set holds exactly one question per rubric parameter, and no question is used twice.
    /// </summary>
    public static class AIQuestionSetDealer
    {
        public sealed class Deal
        {
            /// <summary>One list per new set, each holding one question per parameter, in parameter order.</summary>
            public List<List<AIQuestionPoolItemVM>> Sets { get; } = new List<List<AIQuestionPoolItemVM>>();
            /// <summary>Set when no set can be dealt: the parameter that has no unused question.</summary>
            public string EmptyParameter { get; set; }
        }

        public static Deal DealNewSets(IList<AIQuestionSetRubricParameterVM> parameters, IEnumerable<AIQuestionPoolItemVM> pool,
            ISet<int> usedQuestionIds, ISet<int> codeProjectQuestionIds)
        {
            var deal = new Deal();
            var ordered = parameters.Where(p => !string.IsNullOrWhiteSpace(p.SkillName)).OrderBy(p => p.ParameterOrder).ToList();
            var unused = pool.Where(q => !usedQuestionIds.Contains(q.id)).ToList();

            bool IsProject(AIQuestionPoolItemVM q) => codeProjectQuestionIds.Contains(q.id);
            bool IsCoding(AIQuestionPoolItemVM q) => q.IsCodingQuestion && !IsProject(q);

            var perParameter = ordered.Select(p => unused
                    .Where(q => string.Equals((q.SkillName ?? "").Trim(), p.SkillName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(IsProject).ThenByDescending(IsCoding).ThenBy(q => q.id)
                    .ToList())
                .ToList();
            if (ordered.Count == 0) return deal;

            var setCount = perParameter.Min(l => l.Count);
            if (setCount == 0)
            {
                deal.EmptyParameter = ordered[perParameter.FindIndex(l => l.Count == 0)].SkillName.Trim();
                return deal;
            }

            for (var i = 0; i < setCount; i++) deal.Sets.Add(new List<AIQuestionPoolItemVM>());
            var projects = new int[setCount];
            var heavy = new int[setCount];

            foreach (var candidates in perParameter)
            {
                var open = Enumerable.Range(0, setCount).ToList();
                foreach (var q in candidates.Take(setCount))   // heavy first, by the ordering above
                {
                    int target;
                    if (IsProject(q))
                        target = open.OrderBy(s => projects[s]).ThenBy(s => heavy[s]).ThenBy(s => s).First();
                    else if (IsCoding(q))
                        target = open.OrderBy(s => heavy[s]).ThenBy(s => s).First();
                    else
                        target = open.First();
                    deal.Sets[target].Add(q);
                    if (IsProject(q)) projects[target]++;
                    if (IsProject(q) || IsCoding(q)) heavy[target]++;
                    open.Remove(target);
                }
            }
            return deal;
        }

        /// <summary>Inverse of the positional set names (A..Z, AA..ZZ): index of a name, or -1.</summary>
        public static int SetNameIndex(string name)
        {
            var n = (name ?? "").Trim().ToUpperInvariant();
            if (n.Length == 1 && n[0] >= 'A' && n[0] <= 'Z') return n[0] - 'A';
            if (n.Length == 2 && n.All(c => c >= 'A' && c <= 'Z')) return (n[0] - 'A' + 1) * 26 + (n[1] - 'A');
            return -1;
        }
    }
}
