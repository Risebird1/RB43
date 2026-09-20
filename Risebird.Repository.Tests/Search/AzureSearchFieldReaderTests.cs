using Azure.Search.Documents.Models;
using Risebird.Repository.Search;

namespace Risebird.Repository.Tests.Search
{
    public class AzureSearchFieldReaderTests
    {
        private static SearchDocument Doc(params (string Field, object Value)[] values)
        {
            var document = new SearchDocument();
            foreach (var (field, value) in values)
                document[field] = value;
            return document;
        }

        // ── GetString ─────────────────────────────────────────────────────────

        [Fact]
        public void GetString_returns_the_stored_string() =>
            Assert.Equal("Ada", AzureSearchFieldReader.GetString(Doc(("firstName", "Ada")), "firstName"));

        [Fact]
        public void GetString_returns_null_for_a_missing_field() =>
            Assert.Null(AzureSearchFieldReader.GetString(Doc(), "firstName"));

        [Fact]
        public void GetString_returns_null_for_an_explicit_null_value() =>
            Assert.Null(AzureSearchFieldReader.GetString(Doc(("firstName", null)), "firstName"));

        [Fact]
        public void GetString_preserves_an_empty_string_rather_than_nulling_it() =>
            Assert.Equal(string.Empty, AzureSearchFieldReader.GetString(Doc(("firstName", "")), "firstName"));

        [Theory]
        [InlineData(42, "42")]
        [InlineData(true, "True")]
        [InlineData(1.5, "1.5")]
        public void GetString_coerces_non_string_values_with_invariant_culture(object stored, string expected) =>
            Assert.Equal(expected, AzureSearchFieldReader.GetString(Doc(("f", stored)), "f"));

        // ── GetInt / GetNullableInt ───────────────────────────────────────────

        [Fact]
        public void GetInt_returns_the_stored_int() =>
            Assert.Equal(42, AzureSearchFieldReader.GetInt(Doc(("n", 42)), "n"));

        [Fact]
        public void GetInt_narrows_a_long() =>
            Assert.Equal(42, AzureSearchFieldReader.GetInt(Doc(("n", 42L)), "n"));

        [Fact]
        public void GetInt_parses_a_numeric_string() =>
            Assert.Equal(42, AzureSearchFieldReader.GetInt(Doc(("n", "42")), "n"));

        [Fact]
        public void GetInt_parses_a_negative_numeric_string() =>
            Assert.Equal(-7, AzureSearchFieldReader.GetInt(Doc(("n", "-7")), "n"));

        [Fact]
        public void GetInt_rounds_a_double_via_Convert() =>
            Assert.Equal(3, AzureSearchFieldReader.GetInt(Doc(("n", 3.4d)), "n"));

        [Fact]
        public void GetInt_returns_zero_for_a_missing_field() =>
            Assert.Equal(0, AzureSearchFieldReader.GetInt(Doc(), "n"));

        [Fact]
        public void GetInt_returns_zero_for_an_explicit_null() =>
            Assert.Equal(0, AzureSearchFieldReader.GetInt(Doc(("n", null)), "n"));

        [Fact]
        public void GetInt_throws_on_a_non_numeric_string_rather_than_silently_returning_zero()
        {
            // Pinned: the string branch only applies when int.TryParse succeeds; anything
            // else falls through to Convert.ToInt32, which throws.
            Assert.ThrowsAny<Exception>(() => AzureSearchFieldReader.GetInt(Doc(("n", "abc")), "n"));
        }

        [Fact]
        public void GetNullableInt_returns_null_for_a_missing_field_or_explicit_null()
        {
            Assert.Null(AzureSearchFieldReader.GetNullableInt(Doc(), "n"));
            Assert.Null(AzureSearchFieldReader.GetNullableInt(Doc(("n", null)), "n"));
        }

        [Fact]
        public void GetNullableInt_returns_a_real_zero_when_zero_is_stored() =>
            Assert.Equal(0, AzureSearchFieldReader.GetNullableInt(Doc(("n", 0)), "n"));

        [Fact]
        public void GetNullableInt_returns_the_stored_value() =>
            Assert.Equal(42, AzureSearchFieldReader.GetNullableInt(Doc(("n", 42)), "n"));

        // ── GetDouble ─────────────────────────────────────────────────────────

        [Fact]
        public void GetDouble_returns_the_stored_double() =>
            Assert.Equal(1.5d, AzureSearchFieldReader.GetDouble(Doc(("d", 1.5d)), "d"));

        [Fact]
        public void GetDouble_widens_a_float() =>
            Assert.Equal(1.5d, AzureSearchFieldReader.GetDouble(Doc(("d", 1.5f)), "d"));

        [Fact]
        public void GetDouble_converts_a_decimal() =>
            Assert.Equal(1.5d, AzureSearchFieldReader.GetDouble(Doc(("d", 1.5m)), "d"));

        [Fact]
        public void GetDouble_converts_an_int() =>
            Assert.Equal(3d, AzureSearchFieldReader.GetDouble(Doc(("d", 3)), "d"));

        [Fact]
        public void GetDouble_parses_a_numeric_string() =>
            Assert.Equal(1.5d, AzureSearchFieldReader.GetDouble(Doc(("d", "1.5")), "d"));

        [Fact]
        public void GetDouble_returns_zero_for_a_missing_field_or_explicit_null()
        {
            Assert.Equal(0d, AzureSearchFieldReader.GetDouble(Doc(), "d"));
            Assert.Equal(0d, AzureSearchFieldReader.GetDouble(Doc(("d", null)), "d"));
        }

        // ── GetBool ───────────────────────────────────────────────────────────

        [Fact]
        public void GetBool_returns_the_stored_bool()
        {
            Assert.True(AzureSearchFieldReader.GetBool(Doc(("b", true)), "b"));
            Assert.False(AzureSearchFieldReader.GetBool(Doc(("b", false)), "b"));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        public void GetBool_parses_a_boolean_string(string stored, bool expected) =>
            Assert.Equal(expected, AzureSearchFieldReader.GetBool(Doc(("b", stored)), "b"));

        [Fact]
        public void GetBool_returns_false_for_a_missing_field_or_explicit_null()
        {
            Assert.False(AzureSearchFieldReader.GetBool(Doc(), "b"));
            Assert.False(AzureSearchFieldReader.GetBool(Doc(("b", null)), "b"));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public void GetBool_converts_numeric_flags(object stored, bool expected) =>
            Assert.Equal(expected, AzureSearchFieldReader.GetBool(Doc(("b", stored)), "b"));

        [Fact]
        public void GetBool_throws_on_a_non_boolean_string() =>
            Assert.ThrowsAny<Exception>(() => AzureSearchFieldReader.GetBool(Doc(("b", "yes")), "b"));

        // ── GetDateTime / GetNullableDateTime ─────────────────────────────────

        [Fact]
        public void GetDateTime_returns_the_stored_datetime()
        {
            var value = new DateTime(2026, 09, 09, 13, 45, 00, DateTimeKind.Utc);

            Assert.Equal(value, AzureSearchFieldReader.GetDateTime(Doc(("dt", value)), "dt"));
        }

        [Fact]
        public void GetDateTime_unwraps_a_datetimeoffset()
        {
            var offset = new DateTimeOffset(2026, 09, 09, 13, 45, 00, TimeSpan.Zero);

            Assert.Equal(offset.DateTime, AzureSearchFieldReader.GetDateTime(Doc(("dt", offset)), "dt"));
        }

        [Fact]
        public void GetDateTime_parses_an_iso_string()
        {
            var parsed = AzureSearchFieldReader.GetDateTime(Doc(("dt", "2026-09-09T13:45:00Z")), "dt");

            Assert.Equal(new DateTime(2026, 09, 09, 13, 45, 00), parsed.ToUniversalTime());
        }

        [Fact]
        public void GetDateTime_returns_default_for_a_missing_field_or_explicit_null()
        {
            Assert.Equal(default(DateTime), AzureSearchFieldReader.GetDateTime(Doc(), "dt"));
            Assert.Equal(default(DateTime), AzureSearchFieldReader.GetDateTime(Doc(("dt", null)), "dt"));
        }

        [Fact]
        public void GetDateTime_throws_on_an_unparseable_string() =>
            Assert.ThrowsAny<Exception>(() => AzureSearchFieldReader.GetDateTime(Doc(("dt", "nonsense")), "dt"));

        [Fact]
        public void GetNullableDateTime_returns_null_for_a_missing_field_or_explicit_null()
        {
            Assert.Null(AzureSearchFieldReader.GetNullableDateTime(Doc(), "dt"));
            Assert.Null(AzureSearchFieldReader.GetNullableDateTime(Doc(("dt", null)), "dt"));
        }

        [Fact]
        public void GetNullableDateTime_returns_the_stored_value()
        {
            var value = new DateTime(2026, 09, 09, 13, 45, 00, DateTimeKind.Utc);

            Assert.Equal(value, AzureSearchFieldReader.GetNullableDateTime(Doc(("dt", value)), "dt"));
        }

        [Fact]
        public void GetNullableDateTime_maps_a_stored_default_datetime_to_null()
        {
            // Pinned: a sentinel DateTime.MinValue in the index reads back as "no date".
            Assert.Null(AzureSearchFieldReader.GetNullableDateTime(Doc(("dt", default(DateTime))), "dt"));
        }

        [Fact]
        public void Readers_never_mutate_the_document()
        {
            var document = Doc(("n", 42), ("s", "x"));

            AzureSearchFieldReader.GetInt(document, "missing");
            AzureSearchFieldReader.GetString(document, "missing");
            AzureSearchFieldReader.GetBool(document, "missing");
            AzureSearchFieldReader.GetDouble(document, "missing");
            AzureSearchFieldReader.GetDateTime(document, "missing");

            Assert.Equal(2, document.Count);
        }
    }
}
