using Risebird.Entities.ViewModel.AdvancedScreening;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// The facts the scorer is given as ground truth, computed in C# from the extracted work
    /// history and the texts - never by the model. Pure and static; the data access around it
    /// lives in AuthenticityFactsBuilder.
    /// </summary>
    public static class AuthenticityFacts
    {
        // ------------------------------------------------------------------ dates

        private static readonly Regex YearMonth = new Regex(@"^\s*(\d{4})(?:[-/.](\d{1,2}))?\s*$", RegexOptions.Compiled);

        /// <summary>"YYYY-MM" (or "YYYY") to a month index; "present" to <paramref name="asOf"/>; otherwise null.</summary>
        public static int? ToMonthIndex(string value, DateTime asOf, bool isEnd = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            var v = value.Trim().ToLowerInvariant();
            if (v == "present" || v == "current" || v == "now" || v == "till date" || v == "to date")
            {
                return MonthIndex(asOf.Year, asOf.Month);
            }
            var m = YearMonth.Match(v);
            if (!m.Success)
            {
                return null;
            }
            var year = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : (isEnd ? 12 : 1);
            if (year < 1950 || year > asOf.Year + 1 || month < 1 || month > 12)
            {
                return null;
            }
            return MonthIndex(year, month);
        }

        private static int MonthIndex(int year, int month) => year * 12 + (month - 1);

        private static string FormatMonth(int index) => $"{index / 12:D4}-{index % 12 + 1:D2}";

        private static bool IsPresent(string end) =>
            !string.IsNullOrWhiteSpace(end) && new[] { "present", "current", "now", "till date", "to date" }.Contains(end.Trim().ToLowerInvariant());

        private static bool IsFullTime(WorkHistoryEntry e)
        {
            var t = (e.EmploymentType ?? string.Empty).Trim().ToLowerInvariant();
            return t == string.Empty || t.Contains("full");
        }

        private sealed class Span
        {
            public WorkHistoryEntry Entry;
            public int Start;
            public int End;
            public int Months => Math.Max(1, End - Start + 1);
        }

        private static List<Span> Spans(IEnumerable<WorkHistoryEntry> history, DateTime asOf)
        {
            var spans = new List<Span>();
            foreach (var e in history ?? Enumerable.Empty<WorkHistoryEntry>())
            {
                if (e == null) continue;
                var start = ToMonthIndex(e.Start, asOf);
                var end = ToMonthIndex(e.End, asOf, isEnd: true);
                if (start == null) continue;
                // A role with a start but no end is only "current" if the resume says so; otherwise
                // it is counted as one month rather than guessed to run to today.
                var endValue = end ?? start.Value;
                if (endValue < start.Value) continue;
                spans.Add(new Span { Entry = e, Start = start.Value, End = endValue });
            }
            return spans.OrderBy(s => s.Start).ToList();
        }

        // ------------------------------------------------------------------ stability

        /// <summary>
        /// Tenure, job-hopping, gap and overlap figures from the work history. Internships are
        /// left out. Returns null when no role has a usable start date.
        /// </summary>
        public static StabilityFacts ComputeStability(IEnumerable<WorkHistoryEntry> history, DateTime asOf)
        {
            var spans = Spans(history, asOf)
                .Where(s => !(s.Entry.EmploymentType ?? string.Empty).ToLowerInvariant().Contains("intern"))
                .ToList();
            if (spans.Count == 0)
            {
                return null;
            }

            var now = MonthIndex(asOf.Year, asOf.Month);

            // Gaps are measured between merged employment periods, so an overlap is not a gap.
            var merged = new List<(int Start, int End)>();
            foreach (var s in spans)
            {
                if (merged.Count > 0 && s.Start <= merged[^1].End + 1)
                {
                    merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, s.End));
                }
                else
                {
                    merged.Add((s.Start, s.End));
                }
            }
            var maxGap = 0;
            for (var i = 1; i < merged.Count; i++)
            {
                maxGap = Math.Max(maxGap, merged[i].Start - merged[i - 1].End - 1);
            }

            var fullTime = spans.Where(s => IsFullTime(s.Entry)).ToList();
            var overlaps = 0;
            for (var i = 0; i < fullTime.Count; i++)
            {
                for (var j = i + 1; j < fullTime.Count; j++)
                {
                    var overlapMonths = Math.Min(fullTime[i].End, fullTime[j].End) - Math.Max(fullTime[i].Start, fullTime[j].Start) + 1;
                    // A one-month overlap is a normal notice-period handover, not two jobs.
                    if (overlapMonths > 1)
                    {
                        overlaps++;
                    }
                }
            }

            var current = spans.Where(s => IsPresent(s.Entry.End)).OrderByDescending(s => s.Start).FirstOrDefault();

            return new StabilityFacts
            {
                AvgTenureMonths = Math.Round((decimal)spans.Average(s => s.Months), 1),
                JobsLast5Years = spans.Count(s => s.End >= now - 59),
                JobsLast3Years = spans.Count(s => s.End >= now - 35),
                ShortestStintMonths = spans.Min(s => s.Months),
                MaxGapMonths = Math.Max(0, maxGap),
                OverlappingFullTimeRoles = overlaps,
                CurrentTenureMonths = current?.Months,
                RolesCounted = spans.Count
            };
        }

        // ------------------------------------------------------------------ date checks

        /// <summary>Well-known tools and the year they were first released, for "used before it existed".</summary>
        private static readonly (string Name, int Year)[] ToolReleaseYears =
        {
            ("Kubernetes", 2014), ("Docker", 2013), ("React Native", 2015), ("React", 2013), ("Vue", 2014),
            ("Nuxt", 2016), ("Next.js", 2016), ("Svelte", 2016), ("Angular", 2016), ("TypeScript", 2012),
            ("Kafka", 2011), ("Spring Boot", 2014), ("Flutter", 2017), ("Kotlin", 2016), ("Swift", 2014),
            ("Terraform", 2014), ("GraphQL", 2015), ("TensorFlow", 2015), ("PyTorch", 2016), ("Airflow", 2015),
            ("Snowflake", 2014), ("Databricks", 2013), ("AWS Lambda", 2014), ("Azure Functions", 2016),
            ("ChatGPT", 2022), ("GPT-4", 2023), ("LangChain", 2022), ("GitHub Copilot", 2021), ("Deno", 2018),
            ("FastAPI", 2018), ("Tailwind", 2017), ("Rust", 2015), (".NET Core", 2016), ("Power BI", 2014)
        };

        /// <summary>
        /// Dates that do not add up: projects dated outside the employment they sit under, roles
        /// ending before they start or starting in the future, and tools used before they existed.
        /// </summary>
        public static List<string> CheckDates(IEnumerable<WorkHistoryEntry> history, DateTime asOf)
        {
            var issues = new List<string>();
            var now = MonthIndex(asOf.Year, asOf.Month);
            foreach (var e in history ?? Enumerable.Empty<WorkHistoryEntry>())
            {
                if (e == null) continue;
                var employer = string.IsNullOrWhiteSpace(e.Employer) ? "an unnamed employer" : e.Employer.Trim();
                var start = ToMonthIndex(e.Start, asOf);
                var end = ToMonthIndex(e.End, asOf, isEnd: true);

                if (start != null && end != null && end < start)
                {
                    issues.Add($"Role at {employer} ends ({FormatMonth(end.Value)}) before it starts ({FormatMonth(start.Value)}).");
                }
                if (start != null && start > now)
                {
                    issues.Add($"Role at {employer} starts in the future ({FormatMonth(start.Value)}).");
                }

                foreach (var p in e.Projects ?? new List<WorkHistoryProject>())
                {
                    if (p == null) continue;
                    var name = string.IsNullOrWhiteSpace(p.Name) ? "a project" : $"Project '{p.Name.Trim()}'";
                    var ps = ToMonthIndex(p.Start, asOf);
                    var pe = ToMonthIndex(p.End, asOf, isEnd: true);
                    if (ps != null && start != null && ps < start)
                    {
                        issues.Add($"{name} ({FormatMonth(ps.Value)}) is before employment at {employer} ({FormatMonth(start.Value)}).");
                    }
                    if (pe != null && end != null && pe > end && !IsPresent(e.End))
                    {
                        issues.Add($"{name} runs to {FormatMonth(pe.Value)}, after employment at {employer} ended ({FormatMonth(end.Value)}).");
                    }

                    var projectYear = (ps ?? start) / 12;
                    if (projectYear != null && !string.IsNullOrWhiteSpace(p.Text))
                    {
                        foreach (var (tool, year) in ToolReleaseYears)
                        {
                            if (projectYear < year && Regex.IsMatch(p.Text, @"(?<![\w.])" + Regex.Escape(tool) + @"(?![\w])", RegexOptions.IgnoreCase))
                            {
                                issues.Add($"{name} ({projectYear}) mentions {tool}, first released in {year}.");
                            }
                        }
                    }
                }
            }
            return issues.Distinct().ToList();
        }

        // ------------------------------------------------------------------ versions

        /// <summary>
        /// Differences between this application's history and the same person's history from an
        /// earlier application: employers that appear or vanish for the same period, moved dates,
        /// and changed titles.
        /// </summary>
        public static List<string> CompareVersions(IList<WorkHistoryEntry> current, IList<WorkHistoryEntry> previous, DateTime asOf)
        {
            var diffs = new List<string>();
            var cur = Spans(current, asOf);
            var prev = Spans(previous, asOf);
            if (cur.Count == 0 || prev.Count == 0)
            {
                return diffs;
            }

            foreach (var p in prev)
            {
                var pName = NormalizeCompanyName(p.Entry.Employer);
                var sameEmployer = cur.FirstOrDefault(c => NormalizeCompanyName(c.Entry.Employer) == pName);
                if (sameEmployer == null)
                {
                    // Something else now occupies that period.
                    var replacement = cur.FirstOrDefault(c => Math.Min(c.End, p.End) - Math.Max(c.Start, p.Start) >= 2);
                    if (replacement != null)
                    {
                        diffs.Add($"Employer at {p.Start / 12} changed: '{p.Entry.Employer}' -> '{replacement.Entry.Employer}'");
                    }
                    else
                    {
                        diffs.Add($"Employer '{p.Entry.Employer}' ({FormatMonth(p.Start)}) no longer listed");
                    }
                    continue;
                }
                if (Math.Abs(sameEmployer.Start - p.Start) > 2 || (!IsPresent(p.Entry.End) && Math.Abs(sameEmployer.End - p.End) > 2))
                {
                    diffs.Add($"Dates at '{p.Entry.Employer}' changed: {FormatMonth(p.Start)}–{FormatMonth(p.End)} -> {FormatMonth(sameEmployer.Start)}–{FormatMonth(sameEmployer.End)}");
                }
                if (!string.IsNullOrWhiteSpace(p.Entry.Title) && !string.IsNullOrWhiteSpace(sameEmployer.Entry.Title)
                    && !string.Equals(p.Entry.Title.Trim(), sameEmployer.Entry.Title.Trim(), StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(sameEmployer.Start - p.Start) <= 2)
                {
                    diffs.Add($"Title at '{p.Entry.Employer}' changed: '{p.Entry.Title}' -> '{sameEmployer.Entry.Title}'");
                }
            }
            return diffs.Distinct().ToList();
        }

        // ------------------------------------------------------------------ text checks

        private static readonly Regex Word = new Regex(@"[a-z0-9+#.]+", RegexOptions.Compiled);

        public static List<string> Words(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : Word.Matches(text.ToLowerInvariant()).Select(m => m.Value.Trim('.')).Where(w => w.Length > 0).ToList();
        }

        public static HashSet<string> Shingles(string text, int size)
        {
            var words = Words(text);
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i + size <= words.Count; i++)
            {
                set.Add(string.Join(" ", words.Skip(i).Take(size)));
            }
            return set;
        }

        /// <summary>
        /// The share (0-1) of the resume's 5-word phrases that also appear in the JD. A resume
        /// written by pasting the JD back scores high; a normal tailored resume shares a few phrases.
        /// Null when either text is too short to judge.
        /// </summary>
        public static decimal? JdOverlap(string resumeText, string jdText)
        {
            var resume = Shingles(resumeText, 5);
            var jd = Shingles(jdText, 5);
            if (resume.Count < 20 || jd.Count < 20)
            {
                return null;
            }
            return Math.Round((decimal)resume.Count(jd.Contains) / resume.Count, 2);
        }

        private static readonly Regex[] InjectionPatterns =
        {
            new Regex(@"(ignore|disregard|forget)\s+(all\s+|any\s+|the\s+)?(previous|prior|above|earlier|preceding)\s+(instructions|prompts?|rules|directions)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(rate|score|rank|grade|evaluate)\s+(this|the|me|my)\s+(candidate|resume|cv|profile|application)?\s*(as\s+)?(highly|100|10\s*/\s*10|very\s+high|the\s+best|top|excellent)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(note|instruction|message)\s+(to|for)\s+(the\s+)?(ai|llm|gpt|chatgpt|model|screener|evaluator|recruiter\s+bot)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"\b(you\s+are\s+(an?\s+)?(ai|language\s+model|assistant)|as\s+an\s+ai\b|system\s+prompt)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(this\s+candidate|the\s+candidate)\s+(is|must\s+be)\s+(a\s+)?(perfect|ideal|strong(ly)?\s+recommended)\s+(fit|match|hire)?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        /// <summary>Phrases addressed to an AI or evaluator, quoted with a little context.</summary>
        public static List<string> DetectInjection(string text)
        {
            var found = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return found;
            }
            foreach (var pattern in InjectionPatterns)
            {
                foreach (Match m in pattern.Matches(text))
                {
                    var start = Math.Max(0, m.Index - 30);
                    var length = Math.Min(text.Length - start, m.Length + 60);
                    found.Add(Regex.Replace(text.Substring(start, length), @"\s+", " ").Trim());
                }
            }
            return found.Distinct().Take(5).ToList();
        }

        private static readonly Regex CompanySuffix = new Regex(
            @"\b(private|pvt|limited|ltd|llp|llc|inc|incorporated|corp|corporation|co|company|plc|gmbh|pte|s\.?a|bv)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Employer name reduced for matching and caching: lower case, no legal suffixes or punctuation.</summary>
        public static string NormalizeCompanyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }
            var n = name.ToLowerInvariant().Replace("&", " and ");
            n = Regex.Replace(n, @"\(.*?\)", " ");
            n = Regex.Replace(n, @"[^a-z0-9 ]", " ");
            n = CompanySuffix.Replace(n, " ");
            n = Regex.Replace(n, @"\s+", " ").Trim();
            return n.Length > 300 ? n.Substring(0, 300) : n;
        }
    }
}
