using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Shared OData helpers for Azure AI Search filters. Safe to reuse in every listing repository.
    /// </summary>
    public static class AzureSearchOData
    {
        /// <summary>Escapes single quotes in OData string literals.</summary>
        public static string Escape(string value) =>
            (value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal);

        /// <summary>Formats a date for OData date filters (UTC ISO-8601).</summary>
        public static string FormatDate(DateTime date) =>
            date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        /// <summary>
        /// Appends <c>field eq 'a'</c> or <c>(field eq 'a' or field eq 'b')</c> when values are present.
        /// </summary>
        public static void AppendInFilter(IList<string> filters, string fieldName, IEnumerable<string> values)
        {
            if (filters == null || string.IsNullOrWhiteSpace(fieldName) || values == null)
                return;

            var clauses = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => $"{fieldName} eq '{Escape(v.Trim())}'")
                .ToList();

            if (clauses.Count == 0)
                return;

            filters.Add(clauses.Count == 1 ? clauses[0] : $"({string.Join(" or ", clauses)})");
        }
    }
}
