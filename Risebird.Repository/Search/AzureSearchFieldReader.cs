using Azure.Search.Documents.Models;
using System;
using System.Globalization;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Reads typed values from Azure Search JSON documents.
    /// Use from any listing mapper when converting search hits to list-view models.
    /// </summary>
    public static class AzureSearchFieldReader
    {
        public static string GetString(SearchDocument document, string field) =>
            document.TryGetValue(field, out object value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : null;

        public static int GetInt(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return 0;

            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                string stringValue when int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
                _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
            };
        }

        public static int? GetNullableInt(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return null;

            return GetInt(document, field);
        }

        public static double GetDouble(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return 0;

            return value switch
            {
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                decimal decimalValue => (double)decimalValue,
                _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
            };
        }

        public static bool GetBool(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return false;

            return value switch
            {
                bool boolValue => boolValue,
                string stringValue when bool.TryParse(stringValue, out bool parsed) => parsed,
                _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
            };
        }

        public static DateTime GetDateTime(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return default;

            return value switch
            {
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                DateTime dateTime => dateTime,
                _ => DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
            };
        }

        public static DateTime? GetNullableDateTime(SearchDocument document, string field)
        {
            if (!document.TryGetValue(field, out object value) || value == null)
                return null;

            var parsed = GetDateTime(document, field);
            return parsed == default ? null : parsed;
        }
    }
}
