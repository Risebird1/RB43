using System.Net.Http;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// <c>ElevenLabsAgentService.VerifyWebhookSignature</c> is the only thing standing between a
    /// public webhook endpoint and anyone who can guess the URL: a post-call webhook writes
    /// candidate screening results. It is pure HMAC over the request body, so it is fully
    /// testable — and every negative case here is a way in if the check were wrong.
    /// </summary>
    public sealed class ElevenLabsWebhookSignatureTests
    {
        private const string Secret = "the-webhook-shared-secret";
        private const string Body = "{\"event\":\"post_call\",\"conversation_id\":\"conv-1\"}";

        private static ElevenLabsAgentService Sut(string secret = Secret) =>
            new(TestConfiguration.From(("ElevenLabs:WebhookSecret", secret)), Mock.Of<IHttpClientFactory>());

        private static string Hex(string secret, string timestamp, string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        private static string ValidHeader(string body = Body, string secret = Secret, string timestamp = null)
        {
            timestamp ??= Now();
            return $"t={timestamp},v0={Hex(secret, timestamp, body)}";
        }

        [Fact]
        public void A_correctly_signed_body_is_accepted()
        {
            Assert.True(Sut().VerifyWebhookSignature(Body, ValidHeader()));
        }

        [Fact]
        public void The_signature_is_case_insensitive_on_the_hex_digest()
        {
            var timestamp = Now();
            var header = $"t={timestamp},v0={Hex(Secret, timestamp, Body).ToUpperInvariant()}";

            Assert.True(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_v0_prefix_repeated_inside_the_value_is_stripped_before_comparison()
        {
            // ElevenLabs has shipped both "v0=<hex>" and "v0=v0=<hex>" shapes; both must verify.
            var timestamp = Now();
            var header = $"t={timestamp},v0=v0={Hex(Secret, timestamp, Body)}";

            Assert.True(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void Whitespace_around_the_header_parts_is_tolerated()
        {
            var timestamp = Now();
            var header = $" t = {timestamp} , v0 = {Hex(Secret, timestamp, Body)} ";

            // The key is trimmed but the value keeps its leading space only until Trim() runs,
            // so this documents that both sides are trimmed.
            Assert.True(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void Extra_unknown_header_parts_are_ignored()
        {
            var timestamp = Now();
            var header = $"t={timestamp},v1=ignored,v0={Hex(Secret, timestamp, Body)},junk";

            Assert.True(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_body_that_differs_by_one_byte_is_rejected()
        {
            var header = ValidHeader(Body);

            Assert.False(Sut().VerifyWebhookSignature(Body + " ", header));
        }

        [Fact]
        public void A_signature_computed_with_a_different_secret_is_rejected()
        {
            var timestamp = Now();
            var header = $"t={timestamp},v0={Hex("the-wrong-secret", timestamp, Body)}";

            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_signature_bound_to_a_different_timestamp_is_rejected()
        {
            // The timestamp is part of the signed payload, so replaying a valid digest under a
            // fresh timestamp must not verify.
            var signedAt = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds().ToString();
            var header = $"t={Now()},v0={Hex(Secret, signedAt, Body)}";

            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_truncated_digest_is_rejected_rather_than_matching_on_a_prefix()
        {
            var timestamp = Now();
            var digest = Hex(Secret, timestamp, Body);
            var header = $"t={timestamp},v0={digest.Substring(0, 32)}";

            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void An_over_long_digest_is_rejected()
        {
            var timestamp = Now();
            var header = $"t={timestamp},v0={Hex(Secret, timestamp, Body)}00";

            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Theory]
        [InlineData(-31)]
        [InlineData(-60)]
        [InlineData(-1440)]
        public void A_signature_older_than_the_thirty_minute_window_is_rejected(int minutesAgo)
        {
            var timestamp = DateTimeOffset.UtcNow.AddMinutes(minutesAgo).ToUnixTimeSeconds().ToString();

            Assert.False(Sut().VerifyWebhookSignature(Body, ValidHeader(timestamp: timestamp)));
        }

        [Theory]
        [InlineData(31)]
        [InlineData(120)]
        public void A_signature_from_too_far_in_the_future_is_also_rejected(int minutesAhead)
        {
            // The window is absolute, so a badly skewed sender is refused in both directions.
            var timestamp = DateTimeOffset.UtcNow.AddMinutes(minutesAhead).ToUnixTimeSeconds().ToString();

            Assert.False(Sut().VerifyWebhookSignature(Body, ValidHeader(timestamp: timestamp)));
        }

        [Theory]
        [InlineData(-29)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(29)]
        public void A_signature_inside_the_window_is_accepted(int minutesOffset)
        {
            var timestamp = DateTimeOffset.UtcNow.AddMinutes(minutesOffset).ToUnixTimeSeconds().ToString();

            Assert.True(Sut().VerifyWebhookSignature(Body, ValidHeader(timestamp: timestamp)));
        }

        [Fact]
        public void A_non_numeric_timestamp_skips_the_freshness_check_but_still_must_sign_correctly()
        {
            // long.TryParse fails, so the replay window is not enforced — but the digest is
            // computed over the literal timestamp string, so a forged header still fails.
            var header = $"t=not-a-number,v0={Hex(Secret, "not-a-number", Body)}";

            Assert.True(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_non_numeric_timestamp_with_a_wrong_digest_is_still_rejected()
        {
            Assert.False(Sut().VerifyWebhookSignature(Body, "t=not-a-number,v0=deadbeef"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_signature_header_is_rejected(string header)
        {
            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_blank_body_is_rejected_even_with_a_well_formed_header(string body)
        {
            Assert.False(Sut().VerifyWebhookSignature(body, ValidHeader()));
        }

        [Fact]
        public void A_whitespace_body_is_verified_because_only_null_and_empty_are_refused()
        {
            var whitespaceBody = " ";

            Assert.True(Sut().VerifyWebhookSignature(whitespaceBody, ValidHeader(body: whitespaceBody)));
        }

        [Theory]
        [InlineData("no-equals-anywhere")]
        [InlineData("t=123")]
        [InlineData("v0=deadbeef")]
        [InlineData("t=,v0=")]
        [InlineData("t=123,v0=")]
        [InlineData("t=,v0=deadbeef")]
        [InlineData("x=1,y=2")]
        public void A_malformed_header_is_rejected(string header)
        {
            Assert.False(Sut().VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_digest_that_is_not_hex_at_all_is_rejected_rather_than_throwing()
        {
            var timestamp = Now();

            Assert.False(Sut().VerifyWebhookSignature(Body, $"t={timestamp},v0=not!hex!at!all"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void With_no_secret_configured_every_request_is_accepted(string secret)
        {
            // A deliberate open-by-default so the feature can be switched on before the secret is
            // provisioned. It is also the single most important line in this method to notice:
            // an unset ElevenLabs:WebhookSecret means the endpoint is unauthenticated.
            var sut = Sut(secret);

            Assert.True(sut.VerifyWebhookSignature(Body, "t=1,v0=deadbeef"));
            Assert.True(sut.VerifyWebhookSignature(Body, null));
            Assert.True(sut.VerifyWebhookSignature(null, null));
        }

        [Fact]
        public void With_no_secret_key_present_in_configuration_at_all_requests_are_accepted()
        {
            var sut = new ElevenLabsAgentService(TestConfiguration.Empty(), Mock.Of<IHttpClientFactory>());

            Assert.True(sut.VerifyWebhookSignature(Body, "t=1,v0=deadbeef"));
        }

        [Fact]
        public void Verification_is_stable_across_repeated_calls()
        {
            var sut = Sut();
            var header = ValidHeader();

            Assert.True(sut.VerifyWebhookSignature(Body, header));
            Assert.True(sut.VerifyWebhookSignature(Body, header));
            Assert.False(sut.VerifyWebhookSignature(Body + "x", header));
            Assert.True(sut.VerifyWebhookSignature(Body, header));
        }

        [Fact]
        public void A_large_body_verifies_without_special_handling()
        {
            var largeBody = "{\"transcript\":\"" + new string('a', 200_000) + "\"}";

            Assert.True(Sut().VerifyWebhookSignature(largeBody, ValidHeader(body: largeBody)));
        }

        [Fact]
        public void A_body_with_multibyte_characters_verifies_over_its_UTF8_bytes()
        {
            var body = "{\"name\":\"\u0930\u093E\u092E\"}";

            Assert.True(Sut().VerifyWebhookSignature(body, ValidHeader(body: body)));
        }

        [Fact]
        public void A_secret_with_multibyte_characters_is_used_as_its_UTF8_bytes()
        {
            const string secret = "\u00E9\u00E8-secret";

            Assert.True(Sut(secret).VerifyWebhookSignature(Body, ValidHeader(secret: secret)));
        }
    }
}
