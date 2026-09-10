using System.Globalization;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class IndiaTimeHelperUtcToIndiaWallClockTests
    {
        private static readonly TimeSpan IstOffset = new(5, 30, 0);

        [Fact]
        public void Adds_five_thirty_to_a_utc_instant()
        {
            var utc = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Utc);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(utc);

            Assert.Equal(new DateTime(2026, 01, 15, 11, 30, 00), ist);
        }

        [Fact]
        public void Treats_unspecified_kind_as_utc()
        {
            var unspecified = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Unspecified);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(unspecified);

            Assert.Equal(new DateTime(2026, 01, 15, 11, 30, 00), ist);
        }

        [Fact]
        public void Converts_local_kind_to_utc_first()
        {
            var local = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Local);
            var expected = local.ToUniversalTime() + IstOffset;

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(local);

            Assert.Equal(expected, ist);
        }

        [Fact]
        public void Rolls_over_to_the_next_indian_day_late_in_the_utc_evening()
        {
            var utc = new DateTime(2026, 03, 31, 20, 00, 00, DateTimeKind.Utc);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(utc);

            Assert.Equal(new DateTime(2026, 04, 01, 01, 30, 00), ist);
        }

        [Fact]
        public void Rolls_back_when_the_utc_instant_is_just_after_midnight()
        {
            var utc = new DateTime(2026, 04, 01, 00, 00, 00, DateTimeKind.Utc);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(utc);

            Assert.Equal(new DateTime(2026, 04, 01, 05, 30, 00), ist);
        }

        [Theory]
        [InlineData(2026, 01, 15)]   // northern winter
        [InlineData(2026, 06, 15)]   // northern summer — India has no DST
        [InlineData(2026, 11, 05)]   // after most DST transitions elsewhere
        public void Offset_is_constant_year_round_because_india_has_no_dst(int year, int month, int day)
        {
            var utc = new DateTime(year, month, day, 12, 00, 00, DateTimeKind.Utc);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(utc);

            Assert.Equal(IstOffset, ist - utc);
        }

        [Fact]
        public void Handles_min_value_without_overflowing()
        {
            var exception = Record.Exception(() => IndiaTimeHelper.UtcToIndiaWallClock(DateTime.MinValue));

            Assert.Null(exception);
        }

        [Fact]
        public void Preserves_sub_second_precision()
        {
            var utc = new DateTime(2026, 01, 15, 06, 00, 00, 123, DateTimeKind.Utc);

            var ist = IndiaTimeHelper.UtcToIndiaWallClock(utc);

            Assert.Equal(123, ist.Millisecond);
        }
    }

    public class IndiaTimeHelperFormatIstForDisplayTests
    {
        [Fact]
        public void Uses_the_default_ist_suffixed_format()
        {
            var utc = new DateTime(2026, 01, 15, 06, 05, 00, DateTimeKind.Utc);

            var text = IndiaTimeHelper.FormatIstForDisplay(utc);

            Assert.Equal("15 Jan 2026, 11:35 AM IST", text);
        }

        [Fact]
        public void Honours_a_custom_format_string()
        {
            var utc = new DateTime(2026, 01, 15, 06, 05, 00, DateTimeKind.Utc);

            var text = IndiaTimeHelper.FormatIstForDisplay(utc, "yyyy-MM-dd HH:mm");

            Assert.Equal("2026-01-15 11:35", text);
        }

        [Fact]
        public void Formats_with_invariant_culture_regardless_of_ambient_culture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var utc = new DateTime(2026, 01, 15, 06, 05, 00, DateTimeKind.Utc);

                var text = IndiaTimeHelper.FormatIstForDisplay(utc);

                Assert.Equal("15 Jan 2026, 11:35 AM IST", text);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Renders_noon_and_midnight_with_the_right_meridiem()
        {
            // 06:30 UTC → 12:00 IST (PM), 18:30 UTC → 00:00 IST next day (AM).
            Assert.Contains("12:00 PM", IndiaTimeHelper.FormatIstForDisplay(
                new DateTime(2026, 01, 15, 06, 30, 00, DateTimeKind.Utc)));
            Assert.Contains("12:00 AM", IndiaTimeHelper.FormatIstForDisplay(
                new DateTime(2026, 01, 15, 18, 30, 00, DateTimeKind.Utc)));
        }
    }

    public class IndiaTimeHelperToUtcIso8601Tests
    {
        [Fact]
        public void Returns_empty_string_for_null()
        {
            Assert.Equal(string.Empty, IndiaTimeHelper.ToUtcIso8601(null));
        }

        [Fact]
        public void Emits_round_trip_format_with_a_z_suffix()
        {
            var utc = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Utc);

            var text = IndiaTimeHelper.ToUtcIso8601(utc);

            Assert.StartsWith("2026-01-15T06:00:00", text);
            Assert.EndsWith("Z", text);
        }

        [Fact]
        public void Stamps_unspecified_kind_as_utc_without_shifting_the_clock()
        {
            var unspecified = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Unspecified);

            var text = IndiaTimeHelper.ToUtcIso8601(unspecified);

            Assert.StartsWith("2026-01-15T06:00:00", text);
            Assert.EndsWith("Z", text);
        }

        [Fact]
        public void Relabels_a_local_value_as_utc_rather_than_converting_it()
        {
            // SpecifyKind only changes the label — a local DateTime is NOT shifted.
            // This pins the (surprising) behaviour so a future "fix" is a deliberate choice.
            var local = new DateTime(2026, 01, 15, 06, 00, 00, DateTimeKind.Local);

            var text = IndiaTimeHelper.ToUtcIso8601(local);

            Assert.StartsWith("2026-01-15T06:00:00", text);
        }

        [Fact]
        public void Round_trips_back_to_the_same_instant()
        {
            var utc = new DateTime(2026, 07, 04, 23, 59, 58, DateTimeKind.Utc);

            var parsed = DateTime.Parse(
                IndiaTimeHelper.ToUtcIso8601(utc),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            Assert.Equal(utc, parsed);
        }
    }
}
