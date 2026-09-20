using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;
using Risebird.Entities.Enum;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// Round-trip and failure-mode coverage for the AES helpers. Ciphertext is
    /// non-deterministic (fresh IV per call), so tests assert on decrypt(encrypt(x)) == x
    /// and on the documented failure values, never on a fixed ciphertext string.
    /// </summary>
    public class EncryptionHelperTests
    {
        private static EncryptionHelper CreateSut(out Mock<IApplicationErrorLogger> logger)
        {
            logger = new Mock<IApplicationErrorLogger>(MockBehavior.Loose);
            logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            return new EncryptionHelper(logger.Object, TestConfiguration.Encryption());
        }

        private static EncryptionHelper CreateSut() => CreateSut(out _);

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_throws_when_the_primary_key_is_missing()
        {
            // key.ToString() on a null configuration value dereferences null.
            var logger = new Mock<IApplicationErrorLogger>(MockBehavior.Loose);

            Assert.ThrowsAny<Exception>(() =>
                new EncryptionHelper(logger.Object, TestConfiguration.Empty()));
        }

        [Fact]
        public void Constructor_succeeds_with_a_short_primary_key_because_it_is_padded_to_thirty_two()
        {
            var logger = new Mock<IApplicationErrorLogger>(MockBehavior.Loose);
            var config = TestConfiguration.Encryption(key: "short");

            var exception = Record.Exception(() => new EncryptionHelper(logger.Object, config));

            Assert.Null(exception);
        }

        // ── EncryptData / DecryptData ─────────────────────────────────────────

        [Theory]
        [InlineData("hello")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("ada@risebird.io")]
        [InlineData("with \"quotes\" and \\backslashes\\")]
        [InlineData("unicode ✓ ₹ 日本語")]
        [InlineData("line1\nline2\r\nline3")]
        public async Task Data_round_trips(string plaintext)
        {
            var sut = CreateSut();

            var cipher = await sut.EncryptData(plaintext);
            var recovered = await sut.DecryptData(cipher);

            Assert.Equal(plaintext, recovered);
        }

        [Fact]
        public async Task Data_round_trips_a_long_payload()
        {
            var sut = CreateSut();
            var plaintext = new string('x', 10_000);

            Assert.Equal(plaintext, await sut.DecryptData(await sut.EncryptData(plaintext)));
        }

        [Fact]
        public async Task EncryptData_produces_a_different_ciphertext_each_call()
        {
            var sut = CreateSut();

            var first = await sut.EncryptData("same input");
            var second = await sut.EncryptData("same input");

            Assert.NotEqual(first, second);
            Assert.Equal("same input", await sut.DecryptData(first));
            Assert.Equal("same input", await sut.DecryptData(second));
        }

        [Fact]
        public async Task EncryptData_returns_base64()
        {
            var sut = CreateSut();

            var cipher = await sut.EncryptData("payload");

            var exception = Record.Exception(() => Convert.FromBase64String(cipher));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not base64 at all!")]
        [InlineData("////")]
        public async Task DecryptData_returns_null_for_unusable_input(string cipher)
        {
            var sut = CreateSut(out var logger);

            var result = await sut.DecryptData(cipher);

            Assert.Null(result);
            logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task DecryptData_returns_null_for_tampered_ciphertext()
        {
            var sut = CreateSut();
            var cipher = await sut.EncryptData("secret");
            var bytes = Convert.FromBase64String(cipher);
            bytes[^1] ^= 0xFF;

            var result = await sut.DecryptData(Convert.ToBase64String(bytes));

            Assert.Null(result);
        }

        [Fact]
        public async Task DecryptData_returns_null_when_the_generic_key_differs()
        {
            var writer = CreateSut();
            var cipher = await writer.EncryptData("secret");

            var reader = new EncryptionHelper(
                new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object,
                TestConfiguration.Encryption(genericKey: "DifferentKey32BytesLongAES256!!!"));

            Assert.Null(await reader.DecryptData(cipher));
        }

        // ── EncryptDateTime / DecryptDateTime ─────────────────────────────────

        [Fact]
        public async Task DateTime_round_trips_to_the_second()
        {
            var sut = CreateSut();
            var value = new DateTime(2026, 09, 09, 13, 45, 00, DateTimeKind.Utc);

            var recovered = await sut.DecryptDateTime(await sut.EncryptDateTime(value));

            Assert.NotNull(recovered);
            Assert.Equal(value.ToUniversalTime(), recovered.Value.ToUniversalTime());
        }

        [Fact]
        public async Task DateTime_round_trips_min_and_max()
        {
            var sut = CreateSut();

            foreach (var value in new[] { DateTime.MinValue, DateTime.MaxValue, DateTime.UnixEpoch })
            {
                var recovered = await sut.DecryptDateTime(await sut.EncryptDateTime(value));
                Assert.NotNull(recovered);
            }
        }

        [Fact]
        public async Task EncryptDateTime_is_non_deterministic()
        {
            var sut = CreateSut();
            var value = new DateTime(2026, 09, 09, 13, 45, 00, DateTimeKind.Utc);

            Assert.NotEqual(await sut.EncryptDateTime(value), await sut.EncryptDateTime(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        public async Task DecryptDateTime_returns_null_for_unusable_input(string cipher)
        {
            var sut = CreateSut();

            Assert.Null(await sut.DecryptDateTime(cipher));
        }

        [Fact]
        public async Task DecryptDateTime_returns_null_when_handed_a_string_payload()
        {
            // EncryptData writes with the generic key; DecryptDateTime reads with the primary
            // key, so cross-feeding must fail closed rather than return a bogus date.
            var sut = CreateSut();
            var cipher = await sut.EncryptData("2026-09-09");

            Assert.Null(await sut.DecryptDateTime(cipher));
        }

        // ── EncryptInteger / DecryptInteger ───────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(42)]
        [InlineData(999999)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public async Task Integer_round_trips(int id)
        {
            var sut = CreateSut();

            var recovered = await sut.DecryptInteger(await sut.EncryptInteger(id));

            Assert.Equal(id, recovered);
        }

        [Fact]
        public async Task EncryptInteger_is_non_deterministic()
        {
            var sut = CreateSut();

            Assert.NotEqual(await sut.EncryptInteger(55), await sut.EncryptInteger(55));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-base64")]
        [InlineData("AAAA")]
        public async Task DecryptInteger_returns_null_for_unusable_input(string cipher)
        {
            var sut = CreateSut();

            Assert.Null(await sut.DecryptInteger(cipher));
        }

        [Fact]
        public async Task DecryptInteger_returns_null_for_tampered_ciphertext()
        {
            var sut = CreateSut();
            var bytes = Convert.FromBase64String(await sut.EncryptInteger(55));
            bytes[^1] ^= 0xFF;

            Assert.Null(await sut.DecryptInteger(Convert.ToBase64String(bytes)));
        }

        // ── EncryptIntegerValue / DecryptIntegerValue (URL-safe) ──────────────

        [Theory]
        [InlineData(0, null)]
        [InlineData(1, "Default")]
        [InlineData(55, "ExecutiveReport")]
        [InlineData(int.MaxValue, "ExecutiveReport")]
        [InlineData(int.MinValue, "Default")]
        public async Task IntegerValue_round_trips_per_key_type(int id, string type)
        {
            var sut = CreateSut();

            var recovered = await sut.DecryptIntegerValue(await sut.EncryptIntegerValue(id, type), type);

            Assert.Equal(id, recovered);
        }

        [Fact]
        public async Task IntegerValue_ciphertext_is_url_safe()
        {
            var sut = CreateSut();

            for (var id = 0; id < 40; id++)
            {
                var cipher = await sut.EncryptIntegerValue(id, "Default");
                Assert.DoesNotContain("+", cipher);
            }
        }

        [Fact]
        public async Task IntegerValue_encrypted_for_the_executive_report_key_cannot_be_read_with_the_default_key()
        {
            var sut = CreateSut();
            var cipher = await sut.EncryptIntegerValue(55, "ExecutiveReport");

            var wrongKeyResult = await sut.DecryptIntegerValue(cipher, "Default");

            // Failure sentinel for this API is 0, not null.
            Assert.Equal(0, wrongKeyResult);
        }

        [Fact]
        public async Task IntegerValue_type_matching_is_case_sensitive()
        {
            var sut = CreateSut();
            var cipher = await sut.EncryptIntegerValue(55, "ExecutiveReport");

            // "executivereport" != "ExecutiveReport", so the default key is used instead.
            Assert.Equal(0, await sut.DecryptIntegerValue(cipher, "executivereport"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nonsense")]
        public async Task DecryptIntegerValue_returns_zero_for_unusable_input(string cipher)
        {
            var sut = CreateSut();

            Assert.Equal(0, await sut.DecryptIntegerValue(cipher, "Default"));
        }

        [Fact]
        public async Task DecryptIntegerValue_cannot_distinguish_a_real_zero_from_a_failure()
        {
            // Pinned as a known limitation: callers must not treat 0 as "decrypted id 0".
            var sut = CreateSut();

            var realZero = await sut.DecryptIntegerValue(await sut.EncryptIntegerValue(0, "Default"), "Default");
            var failure = await sut.DecryptIntegerValue("garbage", "Default");

            Assert.Equal(realZero, failure);
        }

        // ── EncryptStringValue / DecryptStringValue (URL-safe) ────────────────

        [Theory]
        [InlineData("hello")]
        [InlineData("")]
        [InlineData("ada@risebird.io")]
        [InlineData("unicode ✓ ₹ 日本語")]
        [InlineData("a/b+c=d")]
        public async Task StringValue_round_trips(string value)
        {
            var sut = CreateSut();

            var recovered = await sut.DecryptStringValue(await sut.EncryptStringValue(value));

            Assert.Equal(value, recovered);
        }

        [Fact]
        public async Task StringValue_ciphertext_is_url_safe()
        {
            var sut = CreateSut();

            for (var i = 0; i < 40; i++)
            {
                var cipher = await sut.EncryptStringValue("payload-" + i);
                Assert.DoesNotContain("+", cipher);
            }
        }

        [Fact]
        public async Task StringValue_writes_plain_text_not_json_so_it_is_not_interchangeable_with_DecryptData()
        {
            var sut = CreateSut();
            var cipher = await sut.EncryptStringValue("hello");

            // Different key (byte key vs generic key) → must never yield the original value.
            Assert.NotEqual("hello", await sut.DecryptData(cipher.Replace("_", "+")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nonsense")]
        public async Task DecryptStringValue_returns_null_for_unusable_input(string cipher)
        {
            var sut = CreateSut();

            Assert.Null(await sut.DecryptStringValue(cipher));
        }

        [Fact]
        public async Task Every_decrypt_failure_is_logged_as_an_exception()
        {
            var sut = CreateSut(out var logger);

            await sut.DecryptData("garbage");
            await sut.DecryptDateTime("garbage");
            await sut.DecryptInteger("garbage");
            await sut.DecryptIntegerValue("garbage", "Default");
            await sut.DecryptStringValue("garbage");

            logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()),
                Times.Exactly(5));
        }
    }
}
