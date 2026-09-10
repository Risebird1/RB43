namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Which way the generated data leans.
    ///
    /// A single set of populated values only ever walks one side of each condition: with every
    /// boolean true and every enum at its first member, <c>if (row.IsActive)</c> and
    /// <c>case Status.Pending:</c> are covered and their alternatives never run. Sweeping the
    /// same services once per profile turns those into covered lines without anyone hand-writing
    /// a case per branch.
    /// </summary>
    internal sealed class PopulationProfile
    {
        public string Name { get; init; } = "affirmative";

        /// <summary>Value handed to every <see cref="bool"/>.</summary>
        public bool BooleanValue { get; init; } = true;

        /// <summary>Which member of an enum to pick, clamped to the values that exist.</summary>
        public int EnumIndex { get; init; }

        /// <summary>Value handed to every numeric type.</summary>
        public int NumericValue { get; init; } = 1;

        /// <summary>When set, strings come back empty rather than plausible.</summary>
        public bool BlankStrings { get; init; }

        /// <summary>
        /// Status the stubbed <see cref="System.Net.Http.HttpClient"/> answers with. The
        /// HTTP-backed services (code execution, resume parsing, payouts, the AI callers) all
        /// branch on <c>IsSuccessStatusCode</c>, and a sweep that only ever sees 200 never runs
        /// the other half of any of them.
        /// </summary>
        public System.Net.HttpStatusCode HttpStatus { get; init; } = System.Net.HttpStatusCode.OK;

        /// <summary>Everything true, first enum member, small positive numbers.</summary>
        public static readonly PopulationProfile Affirmative = new()
        {
            Name = "affirmative"
        };

        /// <summary>The other side of every boolean, and the second enum member.</summary>
        public static readonly PopulationProfile Negative = new()
        {
            Name = "negative",
            BooleanValue = false,
            EnumIndex = 1,
            NumericValue = 0
        };

        /// <summary>
        /// Populated shapes but blank text and a late enum member — the combination that reaches
        /// "required field missing" handling without collapsing to the empty-input sweep.
        /// </summary>
        public static readonly PopulationProfile Sparse = new()
        {
            Name = "sparse",
            BooleanValue = true,
            EnumIndex = int.MaxValue,
            NumericValue = 7,
            BlankStrings = true
        };

        /// <summary>Realistic data, but every outbound HTTP call comes back 500.</summary>
        public static readonly PopulationProfile HttpFailure = new()
        {
            Name = "http-failure",
            HttpStatus = System.Net.HttpStatusCode.InternalServerError
        };

        public static IEnumerable<PopulationProfile> All()
        {
            yield return Affirmative;
            yield return Negative;
            yield return Sparse;
            yield return HttpFailure;
        }
    }
}
