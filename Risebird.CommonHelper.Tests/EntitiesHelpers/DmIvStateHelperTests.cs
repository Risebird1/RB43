using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvActiveHelperTests
    {
        [Theory]
        [InlineData("Shortlisted")]
        [InlineData("shortlisted")]
        [InlineData("SHORTLISTED")]
        [InlineData("ShortListed")]
        public void Client_vet_is_yes_only_for_shortlisted(string clientAction) =>
            Assert.True(DmIvActiveHelper.IsClientVetYes(clientAction));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Rejected")]
        [InlineData("Pending")]
        [InlineData(" Shortlisted ")]   // not trimmed by the helper
        [InlineData("Shortlist")]
        public void Client_vet_is_no_for_anything_else(string clientAction) =>
            Assert.False(DmIvActiveHelper.IsClientVetYes(clientAction));

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void Active_requires_both_vets(bool rbVetYes, bool clientVetYes, bool expected) =>
            Assert.Equal(expected, DmIvActiveHelper.ComputeActive(rbVetYes, clientVetYes));

        [Theory]
        [InlineData(true, "Shortlisted", true)]
        [InlineData(true, "Rejected", false)]
        [InlineData(true, null, false)]
        [InlineData(false, "Shortlisted", false)]
        [InlineData(false, "Rejected", false)]
        public void Active_overload_derives_client_vet_from_the_client_action(
            bool rbVetYes, string clientAction, bool expected) =>
            Assert.Equal(expected, DmIvActiveHelper.ComputeActive(rbVetYes, clientAction));

        [Theory]
        [InlineData(false, null, true)]
        [InlineData(false, "", true)]
        [InlineData(false, "Pending", true)]
        [InlineData(false, "Shortlisted", true)]
        [InlineData(false, "Rejected", false)]
        [InlineData(false, "rejected", false)]
        [InlineData(true, null, false)]
        [InlineData(true, "Rejected", false)]
        public void Awaiting_rb_vet_means_no_rb_yes_and_not_client_rejected(
            bool rbVetYes, string clientAction, bool expected) =>
            Assert.Equal(expected, DmIvActiveHelper.IsAwaitingRbVet(rbVetYes, clientAction));

        [Fact]
        public void Active_and_awaiting_rb_vet_are_mutually_exclusive()
        {
            foreach (var rbVetYes in new[] { true, false })
                foreach (var action in new[] { null, "", "Pending", "Shortlisted", "Rejected" })
                    Assert.False(
                        DmIvActiveHelper.ComputeActive(rbVetYes, action)
                        && DmIvActiveHelper.IsAwaitingRbVet(rbVetYes, action));
        }
    }

    public class DmIvRecordHelperTests
    {
        [Fact]
        public void A_free_channel_row_starts_active_available_and_pre_vetted()
        {
            var (isActive, isAvailable, isSmeVerified, clientAction) =
                DmIvRecordHelper.GetNewRecordState(channelLocked: false);

            Assert.True(isActive);
            Assert.True(isAvailable);
            Assert.True(isSmeVerified);
            Assert.Equal("Shortlisted", clientAction);
        }

        [Fact]
        public void A_locked_channel_row_starts_available_but_not_active_with_undecided_vets()
        {
            var (isActive, isAvailable, isSmeVerified, clientAction) =
                DmIvRecordHelper.GetNewRecordState(channelLocked: true);

            Assert.False(isActive);
            Assert.True(isAvailable);
            Assert.False(isSmeVerified);
            Assert.Null(clientAction);
        }

        [Fact]
        public void A_free_channel_default_state_is_self_consistent_with_ComputeActive()
        {
            var (isActive, _, isSmeVerified, clientAction) = DmIvRecordHelper.GetNewRecordState(false);

            Assert.Equal(isActive, DmIvActiveHelper.ComputeActive(isSmeVerified, clientAction));
        }

        [Theory]
        [InlineData(true, "Shortlisted", true, true)]
        [InlineData(true, "Rejected", false, false)]
        [InlineData(true, null, false, false)]
        [InlineData(false, "Shortlisted", false, false)]
        [InlineData(false, "Rejected", false, false)]
        [InlineData(false, null, false, false)]
        public void Vet_toggle_drives_both_flags_together(
            bool rbVetYes, string clientAction, bool expectedActive, bool expectedAvailable)
        {
            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(rbVetYes, clientAction);

            Assert.Equal(expectedActive, isActive);
            Assert.Equal(expectedAvailable, isAvailable);
        }

        [Fact]
        public void Flags_are_never_split_after_a_vet_toggle()
        {
            foreach (var rbVetYes in new[] { true, false })
                foreach (var action in new[] { null, "", "Pending", "Shortlisted", "Rejected" })
                {
                    var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(rbVetYes, action);
                    Assert.Equal(isActive, isAvailable);
                }
        }
    }

    public class DmIvClientReportRoleHelperTests
    {
        [Theory]
        [InlineData("Client")]
        [InlineData("client")]
        [InlineData("CLIENT")]
        [InlineData("  Client  ")]
        public void Recognises_the_client_role(string profileName) =>
            Assert.True(DmIvClientReportRoleHelper.IsClientRole(profileName));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Clients")]
        [InlineData("ClientHR")]
        [InlineData("Public")]
        [InlineData("DM")]
        public void Rejects_non_client_roles(string profileName) =>
            Assert.False(DmIvClientReportRoleHelper.IsClientRole(profileName));

        [Theory]
        [InlineData("DM")]
        [InlineData("SME")]
        [InlineData("Admin")]
        [InlineData("Ops")]
        [InlineData("Interviewer")]
        [InlineData("  DM  ")]
        [InlineData("ClientHR")]
        public void Treats_every_other_authenticated_role_as_internal(string profileName) =>
            Assert.True(DmIvClientReportRoleHelper.IsInternalRole(profileName));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Client")]
        [InlineData("client")]
        [InlineData("Public")]
        [InlineData("public")]
        [InlineData("  PUBLIC  ")]
        public void Excludes_client_public_and_blank_from_internal(string profileName) =>
            Assert.False(DmIvClientReportRoleHelper.IsInternalRole(profileName));

        [Fact]
        public void Client_and_internal_are_never_both_true()
        {
            foreach (var role in new[] { null, "", "Client", "Public", "DM", "SME", "ClientHR" })
                Assert.False(
                    DmIvClientReportRoleHelper.IsClientRole(role)
                    && DmIvClientReportRoleHelper.IsInternalRole(role));
        }
    }

    public class DmIvSurchargeHelperTests
    {
        [Fact]
        public void Cap_is_five_hundred_rupees()
        {
            Assert.Equal(500, DmIvSurchargeHelper.MaxSurchargeInr);
            Assert.Equal(500, DmIvSurchargeHelper.GetMaxSurcharge());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(250)]
        [InlineData(499)]
        [InlineData(500)]
        public void Accepts_values_in_the_allowed_band(int surcharge)
        {
            var ok = DmIvSurchargeHelper.TryValidate(surcharge, out var error);

            Assert.True(ok);
            Assert.Null(error);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-500)]
        [InlineData(int.MinValue)]
        public void Rejects_negative_values(int surcharge)
        {
            var ok = DmIvSurchargeHelper.TryValidate(surcharge, out var error);

            Assert.False(ok);
            Assert.Equal("Surcharge cannot be negative.", error);
        }

        [Theory]
        [InlineData(501)]
        [InlineData(1000)]
        [InlineData(int.MaxValue)]
        public void Rejects_values_above_the_cap(int surcharge)
        {
            var ok = DmIvSurchargeHelper.TryValidate(surcharge, out var error);

            Assert.False(ok);
            Assert.Contains("500", error);
        }

        [Fact]
        public void Clears_a_stale_error_message_on_success()
        {
            var error = "stale";

            DmIvSurchargeHelper.TryValidate(100, out error);

            Assert.Null(error);
        }
    }

    public class DmIvQcVerdictAggregateHelperTests
    {
        [Theory]
        [InlineData(0, 10, "0/10 suspicious")]
        [InlineData(3, 10, "3/10 suspicious")]
        [InlineData(10, 10, "10/10 suspicious")]
        [InlineData(1, 1, "1/1 suspicious")]
        public void Shows_a_ratio_when_completed_interviews_exist(int suspicious, int completed, string expected) =>
            Assert.Equal(expected, DmIvQcVerdictAggregateHelper.FormatSuspiciousRatio(suspicious, completed));

        [Theory]
        [InlineData(0, 0, "0 suspicious")]
        [InlineData(2, 0, "2 suspicious")]
        [InlineData(1, -1, "1 suspicious")]
        public void Drops_the_denominator_when_there_are_no_completed_interviews(
            int suspicious, int completed, string expected) =>
            Assert.Equal(expected, DmIvQcVerdictAggregateHelper.FormatSuspiciousRatio(suspicious, completed));

        [Theory]
        [InlineData(0, "0 redo")]
        [InlineData(1, "1 redo")]
        [InlineData(2, "2 redo")]
        [InlineData(17, "17 redo")]
        public void Singularises_only_the_count_of_one(int redo, string expected) =>
            Assert.Equal(expected, DmIvQcVerdictAggregateHelper.FormatRedoCount(redo));
    }

    public class DmIvCodedCvHelperTests
    {
        [Theory]
        [InlineData("https://blob/resumesummary_123.json")]
        [InlineData("https://blob/RESUMESUMMARY_123.JSON")]
        [InlineData("https://blob/x/resumesummary_abc")]
        [InlineData("https://blob/anything.json")]
        [InlineData("https://blob/anything.json?sig=abc&se=2026")]
        [InlineData("https://blob/resumesummary_1.json?sv=2020")]
        public void Recognises_openai_resume_summary_links(string link) =>
            Assert.True(DmIvCodedCvHelper.IsOpenAiResumeSummaryLink(link));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("https://blob/redacted.pdf")]
        [InlineData("https://blob/affinda.xml")]
        [InlineData("https://blob/resume.docx")]
        [InlineData("https://blob/x.json.pdf")]
        [InlineData("https://blob/resume.pdf?name=resumesummary_1.json")]
        public void Rejects_legacy_and_blank_links(string link) =>
            Assert.False(DmIvCodedCvHelper.IsOpenAiResumeSummaryLink(link));

        [Fact]
        public void Display_link_is_trimmed_when_displayable()
        {
            Assert.Equal(
                "https://blob/resumesummary_1.json",
                DmIvCodedCvHelper.GetDisplayCodedCvLink("  https://blob/resumesummary_1.json  "));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://blob/redacted.pdf")]
        public void Display_link_is_null_when_not_displayable(string link) =>
            Assert.Null(DmIvCodedCvHelper.GetDisplayCodedCvLink(link));

        [Theory]
        [InlineData("https://blob/resumesummary_1.json", true)]
        [InlineData("https://blob/redacted.pdf", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        public void HasDisplayCodedCv_agrees_with_GetDisplayCodedCvLink(string link, bool expected) =>
            Assert.Equal(expected, DmIvCodedCvHelper.HasDisplayCodedCv(link));
    }
}
