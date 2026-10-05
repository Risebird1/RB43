using System.Text;
using System.Text.RegularExpressions;
using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// <c>PasswordHelper.GenerateUserPassword</c> mints the first password for a new B2C user and
    /// hands it back to be emailed. The generated value is partly random, so the tests assert on
    /// its shape and on the name-derived prefix rather than on a fixed string — and on the one
    /// property that matters most: the password is only returned when the user was really created.
    /// </summary>
    public sealed class PasswordHelperTests
    {
        private static readonly Regex Shape = new(@"^[A-Z0-9]{1,4}@\d{3}$", RegexOptions.Compiled);

        private static Mock<IB2CClientService> B2C(
            B2CProvisioningOutcome outcome = B2CProvisioningOutcome.Created)
        {
            var mock = new Mock<IB2CClientService>(MockBehavior.Loose);
            mock.Setup(x => x.CreateUser(It.IsAny<UserList>(), It.IsAny<bool>())).ReturnsAsync(outcome);
            return mock;
        }

        /// <summary>
        /// GenerateUserPassword returns the provisioning outcome alongside the password; these tests
        /// are about the generated value, so they go through this.
        /// </summary>
        private static async Task<string> Generate(
            PasswordHelper sut, UserDetails user, bool forceChange = false) =>
            (await sut.GenerateUserPassword(user, forceChange)).Password;

        private static UserDetails User(string firstName = "Ashawantha", string lastName = "Rao") => new()
        {
            FirstName = firstName,
            LastName = lastName,
            EmailId = "user@acme.test"
        };

        [Fact]
        public async Task A_created_user_gets_a_password_back()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User());

            Assert.Matches(Shape, password);
        }

        [Fact]
        public async Task A_long_first_name_is_truncated_to_four_upper_case_characters()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Ashawantha", "Rao"));

            Assert.StartsWith("ASHA@", password);
        }

        [Fact]
        public async Task A_first_name_of_exactly_four_characters_is_used_whole()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Asha", "Rao"));

            Assert.StartsWith("ASHA@", password);
        }

        [Fact]
        public async Task A_short_first_name_is_padded_out_with_the_last_name()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Jo", "Baker"));

            Assert.StartsWith("JOBA@", password);
        }

        [Fact]
        public async Task A_short_combined_name_is_used_as_is_rather_than_padded()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Jo", "Li"));

            Assert.StartsWith("JOLI@", password);
        }

        [Fact]
        public async Task A_very_short_combined_name_produces_a_short_prefix()
        {
            // "A" + "B" is two characters, so the password is "AB@123" — shorter than most B2C
            // password policies allow. Recorded because it is a real edge the portal can hit.
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("A", "B"));

            Assert.StartsWith("AB@", password);
            Assert.Equal(6, password.Length);
        }

        [Fact]
        public async Task An_empty_last_name_leaves_only_the_first_name()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Jo", ""));

            Assert.StartsWith("JO@", password);
        }

        [Fact]
        public async Task A_null_last_name_is_treated_as_empty_by_string_concatenation()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("Jo", null));

            Assert.StartsWith("JO@", password);
        }

        [Fact]
        public async Task A_null_first_name_throws_which_is_an_unguarded_input_worth_knowing_about()
        {
            // FirstName.Length is read before anything else, so a user record without a first
            // name fails here rather than being reported as a validation problem upstream.
            var sut = new PasswordHelper(B2C().Object);

            await Assert.ThrowsAsync<NullReferenceException>(
                () => Generate(sut, User(null, "Rao")));
        }

        [Fact]
        public async Task An_empty_first_and_last_name_produces_a_password_that_is_only_the_suffix()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("", ""));

            Assert.StartsWith("@", password);
            Assert.Equal(4, password.Length);
        }

        [Fact]
        public async Task The_numeric_suffix_is_always_three_digits()
        {
            // rand.Next(1000) can return single digits, so the "D3" format is what stops
            // "ASHA@7" appearing and failing the B2C complexity rule.
            var sut = new PasswordHelper(B2C().Object);

            for (var attempt = 0; attempt < 50; attempt++)
            {
                var password = await Generate(sut, User());
                var suffix = password.Substring(password.IndexOf('@') + 1);
                Assert.Equal(3, suffix.Length);
                Assert.True(suffix.All(char.IsDigit));
            }
        }

        [Fact]
        public async Task The_suffix_is_within_the_documented_range()
        {
            var sut = new PasswordHelper(B2C().Object);

            for (var attempt = 0; attempt < 50; attempt++)
            {
                var password = await Generate(sut, User());
                var suffix = int.Parse(password.Substring(password.IndexOf('@') + 1));
                Assert.InRange(suffix, 0, 999);
            }
        }

        [Fact]
        public async Task A_failed_user_creation_returns_an_empty_string_and_never_the_password()
        {
            // The caller emails whatever comes back, so leaking a password for a user that does
            // not exist would send a working-looking credential to a real address.
            var sut = new PasswordHelper(B2C(B2CProvisioningOutcome.Failed).Object);

            Assert.Equal(string.Empty, await Generate(sut, User()));
        }

        [Fact]
        public async Task The_user_is_created_with_the_generated_password_and_the_given_identity()
        {
            var b2c = B2C();
            var sut = new PasswordHelper(b2c.Object);

            var password = await Generate(sut, User("Asha", "Rao"));

            b2c.Verify(x => x.CreateUser(
                It.Is<UserList>(u =>
                    u.FirstName == "Asha"
                    && u.LastName == "Rao"
                    && u.EmailId == "user@acme.test"
                    && u.UserPassword == password),
                false), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task The_force_change_flag_is_passed_straight_through(bool forceChange)
        {
            var b2c = B2C();
            var sut = new PasswordHelper(b2c.Object);

            await sut.GenerateUserPassword(User(), forceChange);

            b2c.Verify(x => x.CreateUser(It.IsAny<UserList>(), forceChange), Times.Once);
        }

        [Fact]
        public async Task The_force_change_flag_defaults_to_false()
        {
            var b2c = B2C();
            var sut = new PasswordHelper(b2c.Object);

            await Generate(sut, User());

            b2c.Verify(x => x.CreateUser(It.IsAny<UserList>(), false), Times.Once);
        }

        [Fact]
        public async Task Two_generated_passwords_are_not_guaranteed_to_differ_but_the_prefix_is_stable()
        {
            // Not a determinism assertion: Random is seeded per call, so two passwords in the
            // same tick can collide. What must hold is that the name-derived prefix does not
            // wander between calls.
            var sut = new PasswordHelper(B2C().Object);

            var first = await Generate(sut, User("Asha", "Rao"));
            var second = await Generate(sut, User("Asha", "Rao"));

            Assert.Equal(first.Substring(0, first.IndexOf('@')), second.Substring(0, second.IndexOf('@')));
        }

        [Fact]
        public async Task A_name_with_non_letter_characters_keeps_them_in_the_prefix()
        {
            var sut = new PasswordHelper(B2C().Object);

            var password = await Generate(sut, User("O'Brien", "X"));

            Assert.StartsWith("O'BR@", password);
        }

        [Fact]
        public async Task A_failed_creation_still_asked_B2C_once()
        {
            var b2c = B2C(B2CProvisioningOutcome.Failed);
            var sut = new PasswordHelper(b2c.Object);

            await Generate(sut, User());

            b2c.Verify(x => x.CreateUser(It.IsAny<UserList>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task A_null_user_throws_before_calling_B2C()
        {
            var b2c = B2C();
            var sut = new PasswordHelper(b2c.Object);

            await Assert.ThrowsAsync<NullReferenceException>(() => sut.GenerateUserPassword(null));

            b2c.Verify(x => x.CreateUser(It.IsAny<UserList>(), It.IsAny<bool>()), Times.Never);
        }
    }

    /// <summary>
    /// <c>FileMimeTypeHelper</c> sniffs the real content type from a file's magic bytes rather
    /// than trusting the extension or the browser-supplied content type — which is exactly what
    /// an upload gate needs, since both of those are attacker-controlled. The interesting cases
    /// are a mislabelled file and content the sniffer cannot place.
    /// </summary>
    public sealed class FileMimeTypeHelperTests
    {
        // Magic-byte prefixes, padded out so the sniffer has enough to work with.
        private static byte[] Png() => Pad(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        private static byte[] Gif() => Pad(Encoding.ASCII.GetBytes("GIF89a"));
        private static byte[] Jpeg() => Pad(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 });

        // A minimal but structurally real PDF. A bare "%PDF-" header with filler after it is
        // reported as application/postscript, because Illustrator files carry the same header.
        private static byte[] Pdf() => Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\nxref\n"
            + "trailer\n<< /Root 1 0 R >>\nstartxref\n%%EOF");

        // Content with no signature any definition matches, so the interrogator returns null.
        private static byte[] Unidentifiable() =>
            new byte[] { 0x3F, 0x9A, 0x11, 0xC4, 0x77, 0x02, 0xEE, 0x51, 0x88, 0x2B, 0x6D, 0xF0 };

        // Filler is 0x20 rather than 0x00: a run of null bytes is itself a Palm database
        // signature, so zero-padding a PNG makes the sniffer answer application/vnd.palm.
        private static byte[] Pad(byte[] prefix, int total = 512)
        {
            var buffer = new byte[Math.Max(total, prefix.Length)];
            buffer.AsSpan().Fill(0x20);
            prefix.CopyTo(buffer, 0);
            return buffer;
        }

        private static FileMimeTypeHelper Sut() => new();

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void A_PNG_is_detected_from_its_signature()
        {
            // Asserted on the subtype rather than the full string: the interrogator library owns
            // the exact spelling, and a test that pins it would break on a library bump without
            // anything in this codebase being wrong.
            Assert.Contains("png", Sut().GetFileMimeTypeFromByteArray(Png()));
        }

        [Fact]
        public void A_GIF_is_detected_from_its_signature()
        {
            Assert.Contains("gif", Sut().GetFileMimeTypeFromByteArray(Gif()));
        }

        [Fact]
        public void A_JPEG_is_detected_from_its_signature()
        {
            Assert.Contains("jpeg", Sut().GetFileMimeTypeFromByteArray(Jpeg()));
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void A_PDF_is_detected_from_its_signature()
        {
            Assert.Contains("pdf", Sut().GetFileMimeTypeFromByteArray(Pdf()));
        }

        [Fact]
        public void The_detected_type_is_always_lower_cased()
        {
            var mimeType = Sut().GetFileMimeTypeFromByteArray(Png());

            Assert.Equal(mimeType.ToLowerInvariant(), mimeType);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Content_the_sniffer_cannot_place_comes_back_as_an_empty_string_not_null()
        {
            // Callers compare the result against an allow-list, so a null here would turn a
            // rejected upload into a NullReferenceException.
            Assert.Equal(string.Empty, Sut().GetFileMimeTypeFromByteArray(Unidentifiable()));
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void A_file_renamed_to_look_like_a_PDF_is_still_reported_as_its_real_type()
        {
            // The whole reason this helper exists: resume.pdf that is really a PNG.
            var file = new FakeFormFile("resume.pdf", Png(), "application/pdf");

            Assert.Contains("png", Sut().GetFileMimeType(file));
        }

        [Fact]
        public void The_form_file_overload_agrees_with_the_byte_array_overload()
        {
            var bytes = Pdf();
            var file = new FakeFormFile("report.pdf", bytes, "application/pdf");

            Assert.Equal(Sut().GetFileMimeTypeFromByteArray(bytes), Sut().GetFileMimeType(file));
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void A_file_whose_content_is_too_short_to_identify_is_reported_as_unknown()
        {
            var file = new FakeFormFile("tiny.pdf", Unidentifiable(), "application/pdf");

            Assert.Equal(string.Empty, Sut().GetFileMimeType(file));
        }

        [Fact]
        public void A_null_byte_array_is_rejected_with_a_named_argument_exception()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => Sut().GetFileMimeTypeFromByteArray(null));

            Assert.Equal("fileBytes", exception.ParamName);
        }

        [Fact]
        public void An_empty_byte_array_is_rejected_the_same_way()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => Sut().GetFileMimeTypeFromByteArray(Array.Empty<byte>()));

            Assert.Equal("fileBytes", exception.ParamName);
        }

        [Fact]
        public void A_truncated_signature_is_not_enough_to_claim_a_type()
        {
            var twoBytes = new byte[] { 0x89, 0x50 };

            Assert.Equal(string.Empty, Sut().GetFileMimeTypeFromByteArray(twoBytes));
        }

        [Fact]
        public void Detection_does_not_depend_on_the_declared_content_type()
        {
            var lying = new FakeFormFile("x.png", Png(), "text/plain");
            var honest = new FakeFormFile("x.png", Png(), "image/png");

            Assert.Equal(Sut().GetFileMimeType(honest), Sut().GetFileMimeType(lying));
        }

        [Fact]
        public void Detection_is_repeatable_on_the_same_bytes()
        {
            var bytes = Jpeg();
            var sut = Sut();

            Assert.Equal(sut.GetFileMimeTypeFromByteArray(bytes), sut.GetFileMimeTypeFromByteArray(bytes));
        }
    }
}
