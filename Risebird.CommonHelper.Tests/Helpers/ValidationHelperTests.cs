using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class ValidationHelperTests
    {
        private static ValidationHelper CreateSut() =>
            new(new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object, TestConfiguration.Empty());

        // ── Positive ──────────────────────────────────────────────────────────

        [Theory]
        [InlineData("ada@risebird.io")]
        [InlineData("ada.lovelace@risebird.io")]
        [InlineData("ada+tag@risebird.io")]
        [InlineData("ada_lovelace@risebird.co.in")]
        [InlineData("ada-lovelace@sub.domain.risebird.io")]
        [InlineData("a@b.co")]
        [InlineData("123@456.io")]
        [InlineData("UPPERCASE@RISEBIRD.IO")]
        public void Accepts_well_formed_addresses(string email) =>
            Assert.True(CreateSut().ValidateEmailAddress(email));

        [Fact]
        public void Accepts_an_address_wrapped_in_a_display_name()
        {
            // Documents current (permissive) behaviour: System.Net.Mail.MailAddress parses
            // RFC-822 display-name form, so this is treated as valid. Callers that need a
            // bare address must trim the display name themselves.
            Assert.True(CreateSut().ValidateEmailAddress("Ada Lovelace <ada@risebird.io>"));
        }

        [Fact]
        public void Accepts_an_address_with_surrounding_whitespace()
        {
            // MailAddress trims, so this passes even though the raw string is not clean.
            Assert.True(CreateSut().ValidateEmailAddress("  ada@risebird.io  "));
        }

        // ── Negative ──────────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("     ")]
        public void Rejects_blank_input_without_throwing(string email) =>
            Assert.False(CreateSut().ValidateEmailAddress(email));

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@risebird.io")]
        [InlineData("ada@")]
        [InlineData("ada@@risebird.io")]
        [InlineData("@")]
        public void Rejects_malformed_addresses(string email) =>
            Assert.False(CreateSut().ValidateEmailAddress(email));

        [Fact]
        public void Swallows_the_parse_failure_instead_of_logging_it()
        {
            // The catch block returns false without touching the logger — asserting this
            // pins the contract that a bad e-mail is a validation result, not an incident.
            var logger = new Mock<IApplicationErrorLogger>(MockBehavior.Loose);
            var sut = new ValidationHelper(logger.Object, TestConfiguration.Empty());

            Assert.False(sut.ValidateEmailAddress("notanemail"));
            logger.VerifyNoOtherCalls();
        }

        [Fact]
        public void Is_deterministic_across_repeated_calls()
        {
            var sut = CreateSut();

            Assert.Equal(sut.ValidateEmailAddress("ada@risebird.io"), sut.ValidateEmailAddress("ada@risebird.io"));
            Assert.Equal(sut.ValidateEmailAddress("bad"), sut.ValidateEmailAddress("bad"));
        }
    }
}
