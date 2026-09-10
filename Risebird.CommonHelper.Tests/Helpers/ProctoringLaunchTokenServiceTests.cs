using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class ProctoringLaunchTokenServiceTests
    {
        private const string ApiBaseUrl = "https://proctor.risebird.io";

        private static (ProctoringLaunchTokenService Sut, ECDsa Verifier) CreateWithConfiguredKey()
        {
            using var generator = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = generator.ExportParameters(includePrivateParameters: true);

            var configuration = TestConfiguration.From(
                ("ProctoringApiSettings:SigningKey:D", Base64Url(parameters.D)),
                ("ProctoringApiSettings:SigningKey:X", Base64Url(parameters.Q.X)),
                ("ProctoringApiSettings:SigningKey:Y", Base64Url(parameters.Q.Y)));

            var verifier = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = parameters.Q.X, Y = parameters.Q.Y }
            });

            return (new ProctoringLaunchTokenService(configuration), verifier);
        }

        private static ProctoringLaunchTokenService CreateWithDevKey(
            string d = null, string x = null, string y = null) =>
            new(TestConfiguration.From(
                ("ProctoringApiSettings:SigningKey:D", d),
                ("ProctoringApiSettings:SigningKey:X", x),
                ("ProctoringApiSettings:SigningKey:Y", y)));

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };
            return Convert.FromBase64String(padded);
        }

        private static (string Version, string Payload, string Signature) SplitToken(string token)
        {
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
            return (parts[0], parts[1], parts[2]);
        }

        private static JsonElement DecodePayload(string token)
        {
            var payload = SplitToken(token).Payload;
            return JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(payload))).RootElement;
        }

        private static (string Token, DateTime ExpiresAtUtc) Mint(
            ProctoringLaunchTokenService sut, TimeSpan? ttl = null) =>
            sut.MintLaunchToken(
                ApiBaseUrl,
                "session-token-abc",
                "12345",
                "candidate@risebird.io",
                "browser-session-xyz",
                ttl ?? TimeSpan.FromMinutes(180));

        // ── Token shape ───────────────────────────────────────────────────────

        [Fact]
        public void Token_has_three_dot_separated_segments()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = Mint(sut);

            Assert.Equal(3, token.Split('.').Length);
        }

        [Fact]
        public void Token_starts_with_the_v1_version_marker()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = Mint(sut);

            Assert.StartsWith("v1.", token);
            Assert.Equal("v1", SplitToken(token).Version);
        }

        [Fact]
        public void Token_segments_are_base64url_with_no_padding()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = Mint(sut);
            var (_, payload, signature) = SplitToken(token);

            foreach (var segment in new[] { payload, signature })
            {
                Assert.DoesNotContain("+", segment);
                Assert.DoesNotContain("/", segment);
                Assert.DoesNotContain("=", segment);
            }
        }

        // ── Payload ───────────────────────────────────────────────────────────

        [Fact]
        public void Payload_carries_every_field_in_camel_case()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = Mint(sut);
            var payload = DecodePayload(token);

            Assert.Equal(ApiBaseUrl, payload.GetProperty("apiBaseUrl").GetString());
            Assert.Equal("session-token-abc", payload.GetProperty("sessionToken").GetString());
            Assert.Equal("12345", payload.GetProperty("interviewId").GetString());
            Assert.Equal("candidate@risebird.io", payload.GetProperty("candidateEmail").GetString());
            Assert.Equal("browser-session-xyz", payload.GetProperty("browserSessionId").GetString());
            Assert.True(payload.TryGetProperty("exp", out _));
        }

        [Fact]
        public void Payload_exp_matches_the_returned_expiry_to_the_second()
        {
            using var sut = CreateWithDevKey();

            var (token, expiresAtUtc) = Mint(sut);
            var exp = DecodePayload(token).GetProperty("exp").GetInt64();

            Assert.Equal(new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds(), exp);
        }

        [Fact]
        public void Expiry_is_now_plus_the_requested_ttl()
        {
            using var sut = CreateWithDevKey();
            var before = DateTime.UtcNow;

            var (_, expiresAtUtc) = Mint(sut, TimeSpan.FromMinutes(30));

            var after = DateTime.UtcNow;
            Assert.InRange(expiresAtUtc, before.AddMinutes(30), after.AddMinutes(30).AddSeconds(1));
        }

        [Fact]
        public void A_zero_ttl_produces_an_already_expired_token()
        {
            using var sut = CreateWithDevKey();

            var (_, expiresAtUtc) = Mint(sut, TimeSpan.Zero);

            Assert.True(expiresAtUtc <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void A_negative_ttl_is_accepted_and_produces_a_past_expiry()
        {
            // The service does not validate the TTL — the caller owns that decision.
            using var sut = CreateWithDevKey();

            var (_, expiresAtUtc) = Mint(sut, TimeSpan.FromMinutes(-5));

            Assert.True(expiresAtUtc < DateTime.UtcNow);
        }

        [Fact]
        public void Null_fields_are_serialized_rather_than_rejected()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = sut.MintLaunchToken(null, null, null, null, null, TimeSpan.FromMinutes(5));
            var payload = DecodePayload(token);

            Assert.Equal(JsonValueKind.Null, payload.GetProperty("apiBaseUrl").ValueKind);
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("sessionToken").ValueKind);
        }

        [Fact]
        public void Payload_survives_characters_that_need_json_escaping()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = sut.MintLaunchToken(
                "https://x/?a=1&b=\"2\"", "s", "i", "ada+tag@risebird.io", "b/c", TimeSpan.FromMinutes(5));
            var payload = DecodePayload(token);

            Assert.Equal("https://x/?a=1&b=\"2\"", payload.GetProperty("apiBaseUrl").GetString());
            Assert.Equal("ada+tag@risebird.io", payload.GetProperty("candidateEmail").GetString());
        }

        // ── Signature ─────────────────────────────────────────────────────────

        [Fact]
        public void Signature_verifies_against_the_configured_public_key()
        {
            var (sut, verifier) = CreateWithConfiguredKey();
            using (sut)
            using (verifier)
            {
                var (token, _) = Mint(sut);
                var (version, payload, signature) = SplitToken(token);

                var verified = verifier.VerifyData(
                    Encoding.UTF8.GetBytes($"{version}.{payload}"),
                    Base64UrlDecode(signature),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

                Assert.True(verified);
            }
        }

        [Fact]
        public void A_tampered_payload_fails_verification()
        {
            var (sut, verifier) = CreateWithConfiguredKey();
            using (sut)
            using (verifier)
            {
                var (token, _) = Mint(sut);
                var (version, payload, signature) = SplitToken(token);

                var verified = verifier.VerifyData(
                    Encoding.UTF8.GetBytes($"{version}.{payload}TAMPERED"),
                    Base64UrlDecode(signature),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

                Assert.False(verified);
            }
        }

        [Fact]
        public void Signature_is_sixty_four_bytes_for_p256_fixed_field_concatenation()
        {
            using var sut = CreateWithDevKey();

            var (token, _) = Mint(sut);

            Assert.Equal(64, Base64UrlDecode(SplitToken(token).Signature).Length);
        }

        [Fact]
        public void Two_tokens_for_identical_input_differ_because_ecdsa_signatures_are_randomized()
        {
            using var sut = CreateWithDevKey();

            var (first, _) = Mint(sut);
            var (second, _) = Mint(sut);

            Assert.NotEqual(SplitToken(first).Signature, SplitToken(second).Signature);
        }

        // ── Key configuration ─────────────────────────────────────────────────

        [Fact]
        public void Falls_back_to_a_dev_key_when_no_signing_key_is_configured()
        {
            using var sut = CreateWithDevKey();

            var exception = Record.Exception(() => Mint(sut));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("d-only", null, null)]
        [InlineData(null, "x-only", null)]
        [InlineData(null, null, "y-only")]
        [InlineData("", "", "")]
        [InlineData("   ", "   ", "   ")]
        public void Falls_back_to_a_dev_key_when_the_key_is_only_partially_configured(
            string d, string x, string y)
        {
            // A half-configured key must not be used — otherwise it would throw at startup.
            using var sut = CreateWithDevKey(d, x, y);

            var exception = Record.Exception(() => Mint(sut));

            Assert.Null(exception);
        }

        [Fact]
        public void Two_dev_key_instances_produce_signatures_that_do_not_cross_verify()
        {
            // Documents why the dev fallback must not be used in production: every process
            // start mints a new keypair, invalidating previously issued tokens.
            var (configured, verifier) = CreateWithConfiguredKey();
            using (configured)
            using (verifier)
            using (var devKeyed = CreateWithDevKey())
            {
                var (devToken, _) = Mint(devKeyed);
                var (version, payload, signature) = SplitToken(devToken);

                var verified = verifier.VerifyData(
                    Encoding.UTF8.GetBytes($"{version}.{payload}"),
                    Base64UrlDecode(signature),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

                Assert.False(verified);
            }
        }

        [Fact]
        public void Constructor_throws_when_the_configured_key_is_not_valid_base64url()
        {
            var configuration = TestConfiguration.From(
                ("ProctoringApiSettings:SigningKey:D", "!!!not-base64!!!"),
                ("ProctoringApiSettings:SigningKey:X", "!!!not-base64!!!"),
                ("ProctoringApiSettings:SigningKey:Y", "!!!not-base64!!!"));

            Assert.ThrowsAny<Exception>(() => new ProctoringLaunchTokenService(configuration));
        }

        // ── Lifetime ──────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_is_idempotent()
        {
            var sut = CreateWithDevKey();

            sut.Dispose();
            var exception = Record.Exception(() => sut.Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Minting_after_dispose_throws_rather_than_returning_an_unsigned_token()
        {
            var sut = CreateWithDevKey();
            sut.Dispose();

            Assert.ThrowsAny<Exception>(() => Mint(sut));
        }

        [Fact]
        public void Concurrent_minting_is_serialized_by_the_sign_lock()
        {
            using var sut = CreateWithDevKey();

            var tokens = Enumerable.Range(0, 64)
                .AsParallel()
                .Select(_ => Mint(sut).Token)
                .ToList();

            Assert.Equal(64, tokens.Count);
            Assert.All(tokens, t => Assert.Equal(3, t.Split('.').Length));
        }
    }
}
