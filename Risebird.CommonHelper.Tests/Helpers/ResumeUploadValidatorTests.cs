using Microsoft.AspNetCore.Http;
using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class ResumeUploadValidatorTests
    {
        private static Mock<IFileMimeTypeHelper> Sniffer(string mimeType)
        {
            var mock = new Mock<IFileMimeTypeHelper>(MockBehavior.Loose);
            mock.Setup(x => x.GetFileMimeType(It.IsAny<IFormFile>())).Returns(mimeType);
            return mock;
        }

        private static Mock<IFileMimeTypeHelper> ThrowingSniffer()
        {
            var mock = new Mock<IFileMimeTypeHelper>(MockBehavior.Loose);
            mock.Setup(x => x.GetFileMimeType(It.IsAny<IFormFile>()))
                .Throws(new InvalidOperationException("sniff failed"));
            return mock;
        }

        // ── Extension gate ────────────────────────────────────────────────────

        [Theory]
        [InlineData("resume.pdf")]
        [InlineData("resume.doc")]
        [InlineData("resume.docx")]
        [InlineData("RESUME.PDF")]
        [InlineData("Resume.DocX")]
        [InlineData("my.resume.v2.pdf")]
        [InlineData("résumé çv.pdf")]
        public void Accepts_allowed_extensions_case_insensitively(string fileName)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile(fileName), Sniffer("application/pdf").Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("resume.exe")]
        [InlineData("resume.zip")]
        [InlineData("resume.txt")]
        [InlineData("resume.rtf")]
        [InlineData("resume.odt")]
        [InlineData("resume.pages")]
        [InlineData("resume.pdf.exe")]
        [InlineData("resume")]
        [InlineData("resume.")]
        [InlineData(".pdf.")]
        public void Rejects_disallowed_or_missing_extensions(string fileName)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile(fileName), Sniffer("application/pdf").Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        [Fact]
        public void Accepts_a_bare_dotfile_because_GetExtension_returns_the_whole_name()
        {
            // Path.GetExtension(".pdf") is ".pdf" (not empty), so a name that is nothing
            // but an allowed extension passes. Pinned so the behaviour is a choice, not a surprise.
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile(".pdf"), Sniffer("application/pdf").Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Fact]
        public void Rejects_a_name_that_ends_in_a_bare_period()
        {
            // Path.GetExtension("resume.") is empty — the trailing period yields no extension.
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume."), Sniffer("application/pdf").Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        // ── File presence ─────────────────────────────────────────────────────

        [Fact]
        public void Rejects_a_null_file()
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                null, Sniffer("application/pdf").Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        [Fact]
        public void Rejects_a_zero_length_file_before_looking_at_the_extension()
        {
            var sniffer = Sniffer("application/pdf");

            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                FakeFormFile.Empty("resume.pdf"), sniffer.Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
            sniffer.Verify(x => x.GetFileMimeType(It.IsAny<IFormFile>()), Times.Never);
        }

        // ── MIME allow-list ───────────────────────────────────────────────────

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/msword")]
        [InlineData("application/vnd.ms-word")]
        [InlineData("application/x-msword")]
        [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("application/vnd.ms-office")]
        [InlineData("application/zip")]
        [InlineData("application/x-zip-compressed")]
        [InlineData("application/x-zip")]
        [InlineData("application/octet-stream")]
        public void Accepts_every_allow_listed_mime_type(string mimeType)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.docx"), Sniffer(mimeType).Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("  APPLICATION/PDF  ")]
        [InlineData("Application/Zip")]
        public void Normalizes_case_and_whitespace_before_matching_the_allow_list(string mimeType)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.docx"), Sniffer(mimeType).Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        // ── MIME block-list ───────────────────────────────────────────────────

        [Theory]
        [InlineData("application/x-msdownload")]
        [InlineData("application/x-msdos-program")]
        [InlineData("application/x-executable")]
        [InlineData("application/x-dosexec")]
        [InlineData("application/java-archive")]
        [InlineData("application/x-sh")]
        [InlineData("text/html")]
        public void Rejects_an_executable_masquerading_as_a_pdf(string mimeType)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.pdf"), Sniffer(mimeType).Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        [Fact]
        public void Block_list_wins_over_a_docx_extension()
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.docx"), Sniffer("application/x-msdownload").Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        [Fact]
        public void Java_archive_is_blocked_for_docx_even_though_docx_is_a_zip()
        {
            // "application/java-archive" contains neither "zip" nor "openxml", so the
            // extension-compatibility shortcut does not rescue it.
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.docx"), Sniffer("application/java-archive").Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        // ── Extension/MIME compatibility shortcut ─────────────────────────────

        [Theory]
        [InlineData("resume.pdf", "application/x-pdf")]
        [InlineData("resume.pdf", "text/pdf")]
        [InlineData("resume.doc", "application/vnd.oasis.word")]
        [InlineData("resume.doc", "application/x-office-document")]
        [InlineData("resume.docx", "application/x-openxml-thing")]
        [InlineData("resume.docx", "application/some-zip-variant")]
        public void Accepts_a_mime_type_that_is_compatible_with_the_validated_extension(
            string fileName, string mimeType)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile(fileName), Sniffer(mimeType).Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        // ── Sniffer failure / silence ─────────────────────────────────────────

        [Fact]
        public void Trusts_the_validated_extension_when_the_sniffer_throws()
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.pdf"), ThrowingSniffer().Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Fact]
        public void Still_rejects_a_bad_extension_even_when_the_sniffer_throws()
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.exe"), ThrowingSniffer().Object, out var error);

            Assert.False(ok);
            Assert.Equal(ResumeUploadValidator.InvalidFileMessage, error);
        }

        [Fact]
        public void Trusts_the_extension_when_no_sniffer_is_supplied()
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.docx"), null, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Trusts_the_extension_when_the_sniffer_returns_nothing(string mimeType)
        {
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.doc"), Sniffer(mimeType).Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Fact]
        public void Trusts_the_extension_for_an_unrecognised_but_not_blocked_mime_type()
        {
            // Documents the deliberate "Office/PDF sniffers are inconsistent" fallthrough.
            var ok = ResumeUploadValidator.IsAllowedResumeUpload(
                new FakeFormFile("resume.pdf"), Sniffer("image/png").Object, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Fact]
        public void Error_message_is_the_single_shared_constant()
        {
            Assert.Equal(
                "Please select valid file for import(.pdf, .doc, .docx)!",
                ResumeUploadValidator.InvalidFileMessage);
        }
    }
}
