using System;
using System.Collections.Generic;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// This decides what gets deleted from a container that also holds real candidate resumes, so
    /// the tests that matter most are the ones asserting what is NOT purgeable.
    /// </summary>
    public class AIScreeningBlobRetentionTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
        private static DateTimeOffset DaysAgo(double days) => Now.AddDays(-days);

        private static HashSet<string> Referenced(params string[] urls) =>
            AIScreeningBlobRetention.ToReferencedBlobNames(urls);

        private static bool Purgeable(string name, DateTimeOffset? modified, HashSet<string> referenced = null, int days = 7) =>
            AIScreeningBlobRetention.IsPurgeable(name, modified, Now, days, referenced ?? Referenced());

        #region What must never be deleted

        /// <summary>
        /// AccountController uploads candidate resumes to the same container under the file name the
        /// candidate chose. None of these may be mistaken for intake leftovers, and none of them is
        /// in IRF.JDLink, so the name test is the only thing standing between them and deletion.
        /// </summary>
        [Theory]
        [InlineData("resume.pdf")]
        [InlineData("d_resume.pdf")]                  // starts with d_ but no draft id
        [InlineData("d_final_v2.pdf")]                // the loose-prefix trap
        [InlineData("d_12345.pdf")]                   // five hex but no trailing underscore
        [InlineData("d_123456_resume.pdf")]           // six characters, not five
        [InlineData("d_1234_resume.pdf")]             // four characters, not five
        [InlineData("d_zzzzz_resume.pdf")]            // five characters, not hex
        [InlineData("x_a3f91_resume.pdf")]            // wrong leading character
        [InlineData("pre_d_a3f91_resume.pdf")]        // not anchored at the start
        [InlineData("d_a3f91_")]                      // draft id but no file name after it
        public void A_blob_that_is_not_an_intake_leftover_is_never_purgeable(string blobName) =>
            Assert.False(Purgeable(blobName, DaysAgo(900)));

        [Fact]
        public void A_job_description_still_referenced_by_a_channel_is_kept()
        {
            var referenced = Referenced("https://acct.blob.core.windows.net/resumes/d_a3f91_JD.pdf");

            Assert.False(Purgeable("d_a3f91_JD.pdf", DaysAgo(400), referenced));
        }

        /// <summary>
        /// IRF.JDLink is stored without a SAS token but handed around with one, and the host differs
        /// between environments. Comparing whole urls would miss the reference and delete a live JD.
        /// </summary>
        [Theory]
        [InlineData("https://acct.blob.core.windows.net/resumes/d_a3f91_JD.pdf?sv=2021&sig=abc%3D")]
        [InlineData("https://other-host.example/resumes/d_a3f91_JD.pdf")]
        [InlineData("https://acct.blob.core.windows.net/resumes/d_a3f91_JD.pdf#frag")]
        public void A_reference_is_recognised_through_sas_tokens_and_host_differences(string storedUrl) =>
            Assert.False(Purgeable("d_a3f91_JD.pdf", DaysAgo(400), Referenced(storedUrl)));

        [Fact]
        public void An_escaped_url_still_matches_the_blob_it_names() =>
            Assert.False(Purgeable(
                "d_a3f91_Senior Engineer JD.pdf",
                DaysAgo(400),
                Referenced("https://acct.blob.core.windows.net/resumes/d_a3f91_Senior%20Engineer%20JD.pdf")));

        [Fact]
        public void Nothing_is_purgeable_when_the_reference_list_could_not_be_read() =>
            Assert.False(AIScreeningBlobRetention.IsPurgeable(
                "d_a3f91_JD.pdf", DaysAgo(400), Now, 7, referencedBlobNames: null));

        [Fact]
        public void A_blob_of_unknown_age_is_left_alone() =>
            Assert.False(Purgeable("d_a3f91_JD.pdf", null));

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(6.9)]
        public void A_blob_inside_the_retention_window_is_kept(double ageDays) =>
            Assert.False(Purgeable("d_a3f91_JD.pdf", DaysAgo(ageDays)));

        /// <summary>
        /// A misconfigured retention of 0 or less would otherwise delete an intake that is still on
        /// screen. It falls back to the default window instead of being honoured.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void A_nonsense_retention_falls_back_to_the_default_rather_than_deleting_everything(int days)
        {
            Assert.False(Purgeable("d_a3f91_JD.pdf", DaysAgo(1), days: days));
            Assert.True(Purgeable("d_a3f91_JD.pdf", DaysAgo(AIScreeningBlobRetention.DefaultRetentionDays + 1), days: days));
        }

        #endregion

        #region What should be deleted

        [Theory]
        [InlineData("d_a3f91_JD.pdf")]
        [InlineData("d_A3F91_JD.pdf")]                 // hex is case-insensitive
        [InlineData("d_00000_Job Description.docx")]
        public void An_orphaned_job_description_past_the_window_is_purgeable(string blobName) =>
            Assert.True(Purgeable(blobName, DaysAgo(8)));

        [Theory]
        [InlineData("ai_screening_instruction_d_a3f91.wav")]
        [InlineData("AI_SCREENING_INSTRUCTION_d_a3f91.webm")]
        public void An_instruction_recording_past_the_window_is_purgeable(string blobName) =>
            Assert.True(Purgeable(blobName, DaysAgo(8)));

        /// <summary>
        /// Instruction recordings are never written to IRF, but if that ever changed the blob would
        /// stop being disposable - the reference test applies to both families, not just to JDs.
        /// </summary>
        [Fact]
        public void A_referenced_instruction_recording_would_also_be_kept() =>
            Assert.False(Purgeable(
                "ai_screening_instruction_d_a3f91.wav",
                DaysAgo(400),
                Referenced("https://acct.blob.core.windows.net/audios/ai_screening_instruction_d_a3f91.wav")));

        #endregion

        #region Url parsing

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("?sv=only-a-query")]
        [InlineData("https://acct.blob.core.windows.net/resumes/")]
        public void A_url_that_names_no_blob_yields_nothing(string url) =>
            Assert.Null(AIScreeningBlobRetention.BlobNameFromUrl(url));

        [Fact]
        public void A_reference_list_with_unusable_entries_still_builds()
        {
            var names = AIScreeningBlobRetention.ToReferencedBlobNames(
                new[] { null, "", "   ", "https://acct.blob.core.windows.net/resumes/d_a3f91_JD.pdf" });

            Assert.Single(names);
            Assert.Contains("d_a3f91_JD.pdf", names);
        }

        [Fact]
        public void A_null_reference_list_builds_an_empty_set() =>
            Assert.Empty(AIScreeningBlobRetention.ToReferencedBlobNames(null));

        #endregion
    }
}
