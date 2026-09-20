using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class DmIvVetDisplayHelperResolveRbVetYesTests
    {
        private static TransInterviewer Row(
            bool isActive = true,
            bool isAvailable = true,
            bool isSmeVerified = false,
            bool? isSmeVerifiedStored = null,
            string clientAction = null) =>
            new()
            {
                IsActive = isActive,
                IsAvailable = isAvailable,
                IsSMEVerified = isSmeVerified,
                IsSMEVerifiedStored = isSmeVerifiedStored,
                ClientAction = clientAction
            };

        [Fact]
        public void A_null_row_is_never_vetted()
        {
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(false, null));
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(true, null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void An_explicit_stored_no_always_wins(bool channelLocked)
        {
            var row = Row(isActive: true, isSmeVerified: true, isSmeVerifiedStored: false);

            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked, row));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void An_inactive_row_never_shows_yes_even_with_a_stored_yes(bool channelLocked)
        {
            var row = Row(isActive: false, isSmeVerifiedStored: true, isSmeVerified: true);

            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked, row));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void An_explicit_stored_yes_on_an_active_row_shows_yes(bool channelLocked)
        {
            var row = Row(isActive: true, isSmeVerifiedStored: true);

            Assert.True(DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked, row));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void A_legacy_true_flag_with_no_stored_value_shows_yes(bool channelLocked)
        {
            var row = Row(isActive: true, isSmeVerified: true, isSmeVerifiedStored: null);

            Assert.True(DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked, row));
        }

        [Fact]
        public void A_preserved_active_available_panel_row_with_a_null_vet_shows_yes_on_a_locked_channel()
        {
            var row = Row(isActive: true, isAvailable: true, isSmeVerifiedStored: null, clientAction: "Pending");

            Assert.True(DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked: true, row));
        }

        [Fact]
        public void A_rejected_client_action_blocks_the_implicit_yes()
        {
            var row = Row(isActive: true, isAvailable: true, isSmeVerifiedStored: null, clientAction: "Rejected");

            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(true, row));
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(false, row));
        }

        [Fact]
        public void Rejected_matching_is_case_insensitive()
        {
            var row = Row(isActive: true, isAvailable: true, isSmeVerifiedStored: null, clientAction: "rEjEcTeD");

            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(true, row));
        }

        [Fact]
        public void An_unavailable_active_row_with_a_null_vet_shows_no()
        {
            var row = Row(isActive: true, isAvailable: false, isSmeVerifiedStored: null);

            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(true, row));
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetYes(false, row));
        }
    }

    public class DmIvVetDisplayHelperResolveClientVetYesTests
    {
        private static TransInterviewer Row(
            bool isActive = true,
            bool isAvailable = true,
            string clientAction = null) =>
            new() { IsActive = isActive, IsAvailable = isAvailable, ClientAction = clientAction };

        [Fact]
        public void A_null_row_is_never_client_vetted() =>
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(false, null, rbVetYes: true));

        [Theory]
        [InlineData("Rejected")]
        [InlineData("rejected")]
        [InlineData("REJECTED")]
        public void A_rejected_row_is_always_no(string clientAction)
        {
            var row = Row(clientAction: clientAction);

            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(true, row, true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(false, row, true));
        }

        [Fact]
        public void An_inactive_row_is_always_no()
        {
            var row = Row(isActive: false, clientAction: "Shortlisted");

            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(true, row, true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(false, row, true));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void An_explicit_shortlist_shows_yes_regardless_of_rb_vet(bool channelLocked)
        {
            var row = Row(clientAction: "Shortlisted");

            Assert.True(DmIvVetDisplayHelper.ResolveClientVetYes(channelLocked, row, rbVetYes: false));
        }

        [Fact]
        public void A_preserved_active_available_panel_row_inherits_client_yes_from_rb_yes_when_locked()
        {
            var row = Row(isActive: true, isAvailable: true, clientAction: "Pending");

            Assert.True(DmIvVetDisplayHelper.ResolveClientVetYes(channelLocked: true, row, rbVetYes: true));
        }

        [Fact]
        public void An_unlocked_channel_also_inherits_client_yes_from_rb_yes_when_available()
        {
            var row = Row(isActive: true, isAvailable: true, clientAction: null);

            Assert.True(DmIvVetDisplayHelper.ResolveClientVetYes(channelLocked: false, row, rbVetYes: true));
        }

        [Fact]
        public void Without_rb_yes_a_non_shortlisted_row_stays_no()
        {
            var row = Row(isActive: true, isAvailable: true, clientAction: "Pending");

            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(true, row, rbVetYes: false));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(false, row, rbVetYes: false));
        }

        [Fact]
        public void An_unavailable_row_does_not_inherit_client_yes()
        {
            var row = Row(isActive: true, isAvailable: false, clientAction: "Pending");

            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(true, row, rbVetYes: true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetYes(false, row, rbVetYes: true));
        }
    }

    public class DmIvVetDisplayHelperStoredValueTests
    {
        private static TransInterviewer Row(
            bool isActive = true,
            bool isAvailable = true,
            bool isSmeVerified = false,
            bool? isSmeVerifiedStored = null,
            string clientAction = null) =>
            new()
            {
                IsActive = isActive,
                IsAvailable = isAvailable,
                IsSMEVerified = isSmeVerified,
                IsSMEVerifiedStored = isSmeVerifiedStored,
                ClientAction = clientAction
            };

        [Fact]
        public void Stored_rb_vet_is_no_for_null_and_inactive_rows()
        {
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetStored(null));
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetStored(
                Row(isActive: false, isSmeVerifiedStored: true)));
        }

        [Fact]
        public void Stored_rb_vet_honours_an_explicit_false()
        {
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetStored(
                Row(isSmeVerified: true, isSmeVerifiedStored: false)));
        }

        [Fact]
        public void Stored_rb_vet_honours_an_explicit_true()
        {
            Assert.True(DmIvVetDisplayHelper.ResolveRbVetStored(Row(isSmeVerifiedStored: true)));
        }

        [Fact]
        public void Stored_rb_vet_infers_yes_for_an_active_available_non_rejected_null_row()
        {
            Assert.True(DmIvVetDisplayHelper.ResolveRbVetStored(
                Row(isAvailable: true, isSmeVerifiedStored: null, clientAction: "Pending")));
        }

        [Fact]
        public void Stored_rb_vet_does_not_infer_yes_for_a_rejected_row()
        {
            Assert.False(DmIvVetDisplayHelper.ResolveRbVetStored(
                Row(isSmeVerifiedStored: null, clientAction: "Rejected")));
        }

        [Fact]
        public void Stored_client_vet_is_no_for_null_inactive_and_rejected_rows()
        {
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetStored(null, true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetStored(Row(isActive: false), true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetStored(Row(clientAction: "Rejected"), true));
        }

        [Fact]
        public void Stored_client_vet_is_yes_for_an_explicit_shortlist()
        {
            Assert.True(DmIvVetDisplayHelper.ResolveClientVetStored(Row(clientAction: "Shortlisted"), false));
        }

        [Fact]
        public void Stored_client_vet_inherits_from_stored_rb_vet_when_available()
        {
            Assert.True(DmIvVetDisplayHelper.ResolveClientVetStored(
                Row(isAvailable: true, clientAction: "Pending"), rbVetStored: true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetStored(
                Row(isAvailable: false, clientAction: "Pending"), rbVetStored: true));
            Assert.False(DmIvVetDisplayHelper.ResolveClientVetStored(
                Row(isAvailable: true, clientAction: "Pending"), rbVetStored: false));
        }

        [Fact]
        public void Stored_and_display_resolvers_agree_on_the_common_cases()
        {
            foreach (var isActive in new[] { true, false })
                foreach (var isAvailable in new[] { true, false })
                    foreach (bool? stored in new bool?[] { null, true, false })
                        foreach (var action in new[] { null, "Pending", "Shortlisted", "Rejected" })
                        {
                            var row = Row(isActive, isAvailable, false, stored, action);

                            // On a locked channel the display and stored rules are the same set
                            // of guards, so they must never disagree.
                            Assert.Equal(
                                DmIvVetDisplayHelper.ResolveRbVetStored(row),
                                DmIvVetDisplayHelper.ResolveRbVetYes(channelLocked: true, row));
                        }
        }
    }

    public class DmIvBadQualityFlagHelperTests
    {
        private static TransInterviewer Row(bool isActive, string autoQcVerdict) =>
            new() { IsActive = isActive, AutoQcVerdict = autoQcVerdict };

        [Fact]
        public void A_null_row_has_no_flag() =>
            Assert.False(DmIvBadQualityFlagHelper.HasBadQualityFlag(null));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_verdict_is_not_a_flag(string verdict) =>
            Assert.False(DmIvBadQualityFlagHelper.HasBadQualityFlag(Row(true, verdict)));

        [Fact]
        public void An_inactive_row_never_carries_the_active_badge() =>
            Assert.False(DmIvBadQualityFlagHelper.HasBadQualityFlag(Row(false, "suspicious")));

        [Fact]
        public void An_active_row_with_a_verdict_carries_the_flag() =>
            Assert.True(DmIvBadQualityFlagHelper.HasBadQualityFlag(Row(true, "suspicious")));

        [Fact]
        public void No_badge_is_returned_when_there_is_no_flag()
        {
            Assert.Null(DmIvBadQualityFlagHelper.ResolveActiveBadge(null));
            Assert.Null(DmIvBadQualityFlagHelper.ResolveActiveBadge(Row(true, null)));
            Assert.Null(DmIvBadQualityFlagHelper.ResolveActiveBadge(Row(false, "redo")));
        }

        [Theory]
        [InlineData("suspicious")]
        [InlineData("SUSPICIOUS")]
        [InlineData("  Suspicious  ")]
        public void A_suspicious_verdict_produces_the_warning_badge(string verdict)
        {
            var badge = DmIvBadQualityFlagHelper.ResolveActiveBadge(Row(true, verdict));

            Assert.NotNull(badge);
            Assert.Equal("suspicious", badge.Verdict);
            Assert.Equal("Flagged — Suspicious (Auto QC)", badge.Label);
            Assert.Equal("warning", badge.Tone);
        }

        [Theory]
        [InlineData("redo")]
        [InlineData("REDO")]
        [InlineData("  Redo  ")]
        public void A_redo_verdict_produces_the_danger_badge(string verdict)
        {
            var badge = DmIvBadQualityFlagHelper.ResolveActiveBadge(Row(true, verdict));

            Assert.NotNull(badge);
            Assert.Equal("redo", badge.Verdict);
            Assert.Equal("Flagged — Redo (Auto QC)", badge.Label);
            Assert.Equal("danger", badge.Tone);
        }

        [Theory]
        [InlineData("bad_quality")]
        [InlineData("something_else")]
        public void An_unrecognised_verdict_falls_back_to_the_generic_bad_quality_badge(string verdict)
        {
            var badge = DmIvBadQualityFlagHelper.ResolveActiveBadge(Row(true, verdict));

            Assert.NotNull(badge);
            Assert.Equal(verdict.ToLowerInvariant(), badge.Verdict);
            Assert.Equal("Flagged — Bad Quality (Auto QC)", badge.Label);
            Assert.Equal("warning", badge.Tone);
        }

        [Fact]
        public void Badge_constructor_defaults_the_tone_and_never_returns_null_strings()
        {
            var badge = new DmIvActiveBadQualityBadge(null, null, null);

            Assert.Equal(string.Empty, badge.Verdict);
            Assert.Equal(string.Empty, badge.Label);
            Assert.Equal("warning", badge.Tone);
        }
    }
}
