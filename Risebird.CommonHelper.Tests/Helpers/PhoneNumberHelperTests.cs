namespace Risebird.CommonHelper.Tests.Helpers
{
    public class PhoneNumberHelperTrySanitizeTests
    {
        private static readonly PhoneNumberHelper Sut = new();

        // ── Positive ──────────────────────────────────────────────────────────

        [Theory]
        [InlineData("9876543210")]            // bare national number
        [InlineData("+919876543210")]         // E.164
        [InlineData("+91 9876543210")]        // E.164 with space
        [InlineData("+91 98765-43210")]       // E.164 with space + dash
        [InlineData("919876543210")]          // country code, no plus
        [InlineData("00919876543210")]        // ISD prefix
        [InlineData("09876543210")]           // trunk zero
        [InlineData("(+91) 98765 43210")]     // parentheses
        [InlineData("98765 43210")]           // internal space
        [InlineData("98765-43210")]           // internal dash
        [InlineData("98.765.43210")]          // dots
        [InlineData("\t9876543210\n")]        // surrounding whitespace
        public void Normalizes_every_accepted_indian_mobile_format(string raw)
        {
            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.True(ok);
            Assert.Equal("+919876543210", sanitized);
            Assert.Null(error);
        }

        [Theory]
        [InlineData('6')]
        [InlineData('7')]
        [InlineData('8')]
        [InlineData('9')]
        public void Accepts_every_valid_leading_digit(char leading)
        {
            var raw = leading + "876543210";

            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.True(ok);
            Assert.Equal("+91" + raw, sanitized);
            Assert.Null(error);
        }

        [Fact]
        public void Keeps_only_the_last_ten_digits_when_extra_digits_are_present()
        {
            // 13 digits: the leading "001" is dropped, leaving the national number.
            var ok = Sut.TrySanitizeIndianPhoneNumber("0019876543210", out var sanitized, out _);

            Assert.True(ok);
            Assert.Equal("+919876543210", sanitized);
        }

        [Fact]
        public void Is_idempotent_when_fed_its_own_output()
        {
            Sut.TrySanitizeIndianPhoneNumber("09876543210", out var first, out _);
            var ok = Sut.TrySanitizeIndianPhoneNumber(first, out var second, out _);

            Assert.True(ok);
            Assert.Equal(first, second);
        }

        // ── Negative ──────────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Rejects_blank_input_with_the_empty_message(string raw)
        {
            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.False(ok);
            Assert.Null(sanitized);
            Assert.Equal("Phone number is empty.", error);
        }

        [Theory]
        [InlineData("abcdefghij")]
        [InlineData("+++")]
        [InlineData("()- ")]
        [InlineData("N/A")]
        public void Rejects_input_with_no_digits_at_all(string raw)
        {
            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.False(ok);
            Assert.Null(sanitized);
            Assert.Equal($"Invalid phone number: '{raw}'.", error);
        }

        [Theory]
        [InlineData("987654321")]   // 9 digits
        [InlineData("12345")]       // way too short
        [InlineData("9")]           // single digit
        public void Rejects_numbers_shorter_than_ten_digits(string raw)
        {
            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.False(ok);
            Assert.Null(sanitized);
            Assert.Contains("Expected a 10-digit mobile number", error);
        }

        [Theory]
        [InlineData("0876543210")]
        [InlineData("1876543210")]
        [InlineData("2876543210")]
        [InlineData("3876543210")]
        [InlineData("4876543210")]
        [InlineData("5876543210")]
        public void Rejects_leading_digits_below_six(string raw)
        {
            var ok = Sut.TrySanitizeIndianPhoneNumber(raw, out var sanitized, out var error);

            Assert.False(ok);
            Assert.Null(sanitized);
            Assert.Contains("Mobile numbers must start with 6-9", error);
        }

        [Fact]
        public void Rejects_a_landline_style_number_that_normalizes_to_a_bad_leading_digit()
        {
            // 91 + 11 (Delhi STD) + 8 digits → last 10 digits start with '1'.
            var ok = Sut.TrySanitizeIndianPhoneNumber("+91 11 2345 6789", out var sanitized, out var error);

            Assert.False(ok);
            Assert.Null(sanitized);
            Assert.Contains("Mobile numbers must start with 6-9", error);
        }

        [Fact]
        public void Clears_out_parameters_before_validating()
        {
            var sanitized = "stale";
            var error = "stale";

            Sut.TrySanitizeIndianPhoneNumber(null, out sanitized, out error);

            Assert.Null(sanitized);
            Assert.NotEqual("stale", error);
        }
    }

    public class PhoneNumberHelperStripCountryCodeTests
    {
        [Theory]
        [InlineData("+91 98765-43210", "9876543210")]
        [InlineData("0091 9876543210", "9876543210")]
        [InlineData("919876543210", "9876543210")]
        [InlineData("09876543210", "9876543210")]
        [InlineData("9876543210", "9876543210")]
        [InlineData("(+91) 98765 43210", "9876543210")]
        [InlineData("+1 415 555 0199", "4155550199")]   // non-Indian: still last 10
        public void Strips_formatting_and_country_code(string raw, string expected) =>
            Assert.Equal(expected, PhoneNumberHelper.StripCountryCode(raw));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Returns_blank_input_unchanged(string raw) =>
            Assert.Equal(raw, PhoneNumberHelper.StripCountryCode(raw));

        [Theory]
        [InlineData("abc")]
        [InlineData("+++")]
        public void Returns_original_when_there_are_no_digits(string raw) =>
            Assert.Equal(raw, PhoneNumberHelper.StripCountryCode(raw));

        [Theory]
        [InlineData("12345", "12345")]        // shorter than 10: only formatting removed
        [InlineData("1-2-3", "123")]
        public void Leaves_short_numbers_at_their_digit_length(string raw, string expected) =>
            Assert.Equal(expected, PhoneNumberHelper.StripCountryCode(raw));

        [Fact]
        public void Is_idempotent()
        {
            var once = PhoneNumberHelper.StripCountryCode("+91 98765-43210");
            Assert.Equal(once, PhoneNumberHelper.StripCountryCode(once));
        }
    }
}
