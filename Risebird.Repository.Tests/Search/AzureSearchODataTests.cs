using System.Globalization;
using Risebird.Repository.Search;

namespace Risebird.Repository.Tests.Search
{
    public class AzureSearchODataEscapeTests
    {
        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("O'Brien", "O''Brien")]
        [InlineData("'", "''")]
        [InlineData("''", "''''")]
        [InlineData("a'b'c", "a''b''c")]
        [InlineData("", "")]
        public void Doubles_every_single_quote(string input, string expected) =>
            Assert.Equal(expected, AzureSearchOData.Escape(input));

        [Fact]
        public void Treats_null_as_an_empty_literal() =>
            Assert.Equal(string.Empty, AzureSearchOData.Escape(null));

        [Fact]
        public void Leaves_double_quotes_and_backslashes_alone() =>
            Assert.Equal("a\"b\\c", AzureSearchOData.Escape("a\"b\\c"));

        [Fact]
        public void Escaping_twice_doubles_again_so_callers_must_escape_exactly_once()
        {
            // Pinned: Escape is NOT idempotent. Never escape an already-escaped literal.
            Assert.Equal("''''", AzureSearchOData.Escape(AzureSearchOData.Escape("'")));
        }
    }

    public class AzureSearchODataFormatDateTests
    {
        [Fact]
        public void Formats_a_utc_instant_as_iso_8601_with_milliseconds_and_z()
        {
            var date = new DateTime(2026, 09, 09, 13, 45, 30, 123, DateTimeKind.Utc);

            Assert.Equal("2026-09-09T13:45:30.123Z", AzureSearchOData.FormatDate(date));
        }

        [Fact]
        public void Converts_a_local_instant_to_utc_first()
        {
            var local = new DateTime(2026, 09, 09, 13, 45, 30, DateTimeKind.Local);
            var expected = local.ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

            Assert.Equal(expected, AzureSearchOData.FormatDate(local));
        }

        [Fact]
        public void Treats_an_unspecified_instant_as_local_because_ToUniversalTime_does()
        {
            var unspecified = new DateTime(2026, 09, 09, 13, 45, 30, DateTimeKind.Unspecified);
            var expected = unspecified.ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

            Assert.Equal(expected, AzureSearchOData.FormatDate(unspecified));
        }

        [Fact]
        public void Uses_invariant_culture_regardless_of_ambient_culture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                var date = new DateTime(2026, 09, 09, 13, 45, 30, DateTimeKind.Utc);

                Assert.Equal("2026-09-09T13:45:30.000Z", AzureSearchOData.FormatDate(date));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Truncates_below_millisecond_precision()
        {
            var date = new DateTime(2026, 09, 09, 13, 45, 30, DateTimeKind.Utc).AddTicks(9999);

            Assert.Equal("2026-09-09T13:45:30.000Z", AzureSearchOData.FormatDate(date));
        }

        [Fact]
        public void Always_emits_a_fixed_width_string()
        {
            foreach (var date in new[] { DateTime.UtcNow, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) })
                Assert.Equal(24, AzureSearchOData.FormatDate(date).Length);
        }
    }

    public class AzureSearchODataAppendInFilterTests
    {
        [Fact]
        public void Emits_a_single_equality_clause_for_one_value()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { "India" });

            Assert.Equal(new[] { "country eq 'India'" }, filters);
        }

        [Fact]
        public void Emits_a_parenthesised_or_group_for_several_values()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { "India", "USA", "UK" });

            Assert.Equal(
                new[] { "(country eq 'India' or country eq 'USA' or country eq 'UK')" },
                filters);
        }

        [Fact]
        public void Trims_each_value_and_escapes_quotes()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "name", new[] { "  O'Brien  " });

            Assert.Equal(new[] { "name eq 'O''Brien'" }, filters);
        }

        [Fact]
        public void Drops_blank_values_before_building_clauses()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { "India", null, "", "   ", "UK" });

            Assert.Equal(new[] { "(country eq 'India' or country eq 'UK')" }, filters);
        }

        [Fact]
        public void Collapses_to_a_single_clause_when_only_one_value_survives_filtering()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { null, "India", "   " });

            Assert.Equal(new[] { "country eq 'India'" }, filters);
        }

        [Fact]
        public void Keeps_duplicate_values_because_de_duplication_is_the_callers_job()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { "India", "India" });

            Assert.Equal(new[] { "(country eq 'India' or country eq 'India')" }, filters);
        }

        [Fact]
        public void Appends_to_an_existing_filter_list_without_disturbing_it()
        {
            var filters = new List<string> { "isActive eq true" };

            AzureSearchOData.AppendInFilter(filters, "country", new[] { "India" });

            Assert.Equal(new[] { "isActive eq true", "country eq 'India'" }, filters);
        }

        [Fact]
        public void Adds_nothing_when_every_value_is_blank()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", new[] { null, "", "   " });

            Assert.Empty(filters);
        }

        [Fact]
        public void Adds_nothing_for_an_empty_value_sequence()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", Array.Empty<string>());

            Assert.Empty(filters);
        }

        [Fact]
        public void Adds_nothing_when_the_value_sequence_is_null()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "country", null);

            Assert.Empty(filters);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Adds_nothing_when_the_field_name_is_blank(string fieldName)
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, fieldName, new[] { "India" });

            Assert.Empty(filters);
        }

        [Fact]
        public void Tolerates_a_null_filter_list()
        {
            var exception = Record.Exception(() =>
                AzureSearchOData.AppendInFilter(null, "country", new[] { "India" }));

            Assert.Null(exception);
        }

        [Fact]
        public void Enumerates_the_value_sequence_only_once()
        {
            var enumerations = 0;

            IEnumerable<string> Values()
            {
                enumerations++;
                yield return "India";
                yield return "UK";
            }

            var filters = new List<string>();
            AzureSearchOData.AppendInFilter(filters, "country", Values());

            Assert.Equal(1, enumerations);
            Assert.Single(filters);
        }

        [Fact]
        public void An_injection_attempt_stays_inside_the_quoted_literal()
        {
            var filters = new List<string>();

            AzureSearchOData.AppendInFilter(filters, "name", new[] { "x' or isDeleted eq false or 'a" });

            Assert.Equal(
                new[] { "name eq 'x'' or isDeleted eq false or ''a'" },
                filters);
        }
    }

    public class AzureSearchOptionsTests
    {
        [Fact]
        public void Section_name_is_the_shared_azure_search_root() =>
            Assert.Equal("AzureSearch", AzureSearchOptions.SectionName);

        [Theory]
        [InlineData("https://svc.search.windows.net", "key", true)]
        [InlineData(null, "key", false)]
        [InlineData("", "key", false)]
        [InlineData("   ", "key", false)]
        [InlineData("https://svc.search.windows.net", null, false)]
        [InlineData("https://svc.search.windows.net", "", false)]
        [InlineData("https://svc.search.windows.net", "   ", false)]
        [InlineData(null, null, false)]
        public void Is_configured_only_when_both_endpoint_and_key_are_present(
            string endpoint, string apiKey, bool expected)
        {
            var options = new AzureSearchOptions { Endpoint = endpoint, ApiKey = apiKey };

            Assert.Equal(expected, options.IsConfigured);
        }
    }
}
