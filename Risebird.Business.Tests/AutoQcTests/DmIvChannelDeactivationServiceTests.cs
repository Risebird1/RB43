using Moq;
using Newtonsoft.Json.Linq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AutoQcTests
{
    /// <summary>
    /// <c>GetClientReportPreviewAsync</c> is what the client report calls before showing the
    /// "Remove Interviewer" confirmation: it decides which flag the confirmation warns about and
    /// how many future interviews the removal will orphan. Both values are read-only, so the
    /// interesting cases are role classification and the count passing through untouched.
    /// </summary>
    public sealed class DmIvChannelDeactivationPreviewTests
    {
        [Theory]
        [InlineData("Client", true)]
        [InlineData("client", true)]
        [InlineData("CLIENT", true)]
        [InlineData("  Client  ", true)]
        [InlineData("DM", false)]
        [InlineData("SME", false)]
        [InlineData("Public", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public async Task Preview_reports_client_role_exactly_when_the_profile_is_Client(string profileName, bool expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var preview = await sut.GetClientReportPreviewAsync(11, profileName);

            Assert.Equal(expected, preview.IsClientRole);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(int.MaxValue)]
        public async Task Preview_passes_the_future_assigned_count_through_unchanged(int count)
        {
            var fixture = new InterviewerLifecycleFixture().GivenFutureAssignedCount(count);
            var sut = fixture.CreateDeactivationSut();

            var preview = await sut.GetClientReportPreviewAsync(11, "Client");

            Assert.Equal(count, preview.FutureAssignedCount);
        }

        [Fact]
        public async Task Preview_reads_the_count_for_the_interview_it_was_asked_about()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.GetClientReportPreviewAsync(4242, "DM");

            fixture.Audit.Verify(x => x.GetFutureAssignedCountForInterviewAsync(4242), Times.Once);
        }

        [Fact]
        public async Task Preview_writes_nothing()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.GetClientReportPreviewAsync(11, "Client");

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task Preview_honours_a_cancelled_token_by_still_returning_because_it_never_checks_one()
        {
            // Documents current behaviour: the preview accepts a CancellationToken but never
            // observes it, so a pre-cancelled token does not stop the repository read. If that
            // ever changes to ThrowIfCancellationRequested, this test is the one to update.
            var fixture = new InterviewerLifecycleFixture().GivenFutureAssignedCount(3);
            var sut = fixture.CreateDeactivationSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var preview = await sut.GetClientReportPreviewAsync(11, "Client", cts.Token);

            Assert.Equal(3, preview.FutureAssignedCount);
        }
    }

    /// <summary>
    /// Authorisation and lookup guards. Every one of these must return before touching a single
    /// write — an unauthorised or unresolvable request that still flipped a vet flag would be the
    /// worst possible failure of this endpoint.
    /// </summary>
    public sealed class DmIvChannelDeactivationGuardTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData("   \t  ")]
        public async Task A_blank_acting_user_is_unauthorized(string changedByEmail)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", changedByEmail);

            Assert.False(result.Success);
            Assert.Equal("Unauthorized", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task An_unauthorized_request_does_not_even_look_the_interviewer_up()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", "");

            fixture.Audit.Verify(x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData("Public")]
        [InlineData("public")]
        [InlineData("  PUBLIC  ")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Neither_a_client_nor_an_internal_role_is_permitted(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(
                11, profileName, "dm@risebird.io");

            Assert.False(result.Success);
            Assert.Equal("You do not have permission to remove this interviewer.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_missing_channel_record_fails_with_the_not_found_message()
        {
            var fixture = new InterviewerLifecycleFixture().GivenNoContext();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(
                11, "Client", "hr@acme.test");

            Assert.False(result.Success);
            Assert.Equal("Interviewer channel record not found for this interview.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_failing_result_carries_no_identifiers_so_the_caller_cannot_mistake_it_for_a_success()
        {
            var fixture = new InterviewerLifecycleFixture().GivenNoContext();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(
                11, "Client", "hr@acme.test");

            Assert.Equal(0, result.TransInterviewerId);
            Assert.Equal(0, result.IrfId);
            Assert.Equal(0, result.InterviewerUserId);
            Assert.Equal(0, result.FutureAssignedCount);
            Assert.Null(result.FlagFlipped);
            Assert.Null(result.Role);
        }

        [Fact]
        public async Task The_context_is_looked_up_for_the_interview_that_was_asked_about()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(777, "Client", "hr@acme.test");

            fixture.Audit.Verify(x => x.GetContextForInterviewAsync(777, It.IsAny<int>()), Times.Once);
        }
    }

    /// <summary>
    /// The Client role rail: client vet goes to No, which on its own is enough to deactivate the
    /// channel because active requires both vets. RB vet is deliberately left alone here — a
    /// client rejecting one interviewer must not un-verify them for every other client.
    /// </summary>
    public sealed class DmIvChannelDeactivationClientRailTests
    {
        private const string Actor = "hr@acme.test";

        [Theory]
        [InlineData("Client")]
        [InlineData("client")]
        [InlineData("  Client  ")]
        public async Task A_client_removal_succeeds_and_reports_the_client_vet_flag(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, profileName, Actor);

            Assert.True(result.Success);
            Assert.Equal("Client Vet", result.FlagFlipped);
            Assert.Null(result.Message);
        }

        [Fact]
        public async Task The_role_on_the_result_is_the_trimmed_profile_name()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "  Client  ", Actor);

            Assert.Equal("Client", result.Role);
        }

        [Fact]
        public async Task The_result_echoes_the_identifiers_from_the_channel_context()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501, userId: 88, irfId: 42))
                .GivenFutureAssignedCount(4);
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.Equal(501, result.TransInterviewerId);
            Assert.Equal(42, result.IrfId);
            Assert.Equal(88, result.InterviewerUserId);
            Assert.Equal(4, result.FutureAssignedCount);
        }

        [Fact]
        public async Task The_client_rail_never_touches_the_RB_vet_flag()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.Empty(fixture.VetUpdates);
        }

        [Fact]
        public async Task The_IRF_cascade_rejects_the_interviewer_by_email_on_their_own_IRF()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(irfId: 42, interviewerEmail: "iv@x.test"));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            var cascade = fixture.TheIrfCascade;
            Assert.Equal(42, cascade.IrfId);
            Assert.Equal("iv@x.test", cascade.InterviewerEmail);
            Assert.Equal("Rejected", cascade.Action);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task The_channel_is_deactivated_regardless_of_the_prior_RB_vet_state(bool rbVetYes)
        {
            // Rejected client action forces active=false whatever RB vet says, because
            // active = rbVetYes AND clientVetYes.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(isRbVetYes: rbVetYes, userId: 88, irfId: 42));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            var update = fixture.TheActiveStatusUpdate;
            Assert.False(update.Status);
            Assert.False(update.IsAvailable);
            Assert.Equal(88, update.UserId);
            Assert.Equal(42, update.IrfId);
            Assert.Equal(Actor, update.LoggedInUser);
        }

        [Fact]
        public async Task The_active_status_update_does_not_ask_to_preserve_availability()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.False(fixture.TheActiveStatusUpdate.PreserveAvailability);
        }

        [Fact]
        public async Task The_audit_row_records_a_client_vet_flip_to_no()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvInactiveAuditEventTypes.ClientVetFlippedToNo, audit.EventType);
            Assert.Equal(501, audit.TransInterviewerId);
            Assert.Equal("Rejected", audit.ToValue);
            Assert.Equal("Client", audit.ReasonCategory);
            Assert.Equal(11, audit.TriggeringInterviewId);
            Assert.Equal(Actor, audit.ChangedBy);
            Assert.Equal("ClientReportRemove", audit.AuditSource);
        }

        [Theory]
        [InlineData("Shortlisted", "Shortlisted")]
        [InlineData("Rejected", "Rejected")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public async Task The_audit_from_value_is_the_previous_client_action_with_null_normalised_to_empty(
            string clientAction, string expectedFrom)
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(clientAction: clientAction));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.Equal(expectedFrom, fixture.TheAuditRow.FromValue);
        }

        [Fact]
        public async Task The_client_rail_does_not_stamp_an_audit_tag_or_a_backfill_batch()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.Null(fixture.TheAuditRow.AuditTag);
            Assert.Null(fixture.TheAuditRow.BackfillBatchId);
        }

        [Fact]
        public async Task The_audit_metadata_is_JSON_carrying_the_full_removal_provenance()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(userId: 88, irfId: 42));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            var metadata = JObject.Parse(fixture.TheAuditRow.ExtraMetadata);
            Assert.Equal("Client", metadata["role"].Value<string>());
            Assert.Equal(42, metadata["irfId"].Value<int>());
            Assert.Equal(11, metadata["interviewId"].Value<int>());
            Assert.Equal(88, metadata["interviewerUserId"].Value<int>());
            Assert.Equal("Client Vet", metadata["flagFlipped"].Value<string>());
            Assert.Equal(Actor, metadata["actingUserEmail"].Value<string>());
        }

        [Fact]
        public async Task The_client_rail_runs_cascade_then_active_status_then_audit_in_that_order()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.Equal(
                new[]
                {
                    "RemoveOrShortlistInterviewerFromIRF",
                    "UpdateIRFInterviewerActiveStatus",
                    "InsertAsync:" + DmIvInactiveAuditEventTypes.ClientVetFlippedToNo
                },
                fixture.CallOrder);
        }

        [Fact]
        public async Task A_failed_IRF_cascade_stops_before_the_active_status_update_and_the_audit()
        {
            var fixture = new InterviewerLifecycleFixture().GivenIrfCascadeFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.False(result.Success);
            Assert.Equal("Could not remove interviewer. Please try again.", result.Message);
            Assert.Single(fixture.IrfCascades);
            Assert.Empty(fixture.ActiveStatusUpdates);
            Assert.Empty(fixture.AuditInserts);
        }

        [Fact]
        public async Task A_failed_active_status_update_stops_before_the_audit()
        {
            var fixture = new InterviewerLifecycleFixture().GivenActiveStatusUpdateFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.False(result.Success);
            Assert.Equal("Could not remove interviewer. Please try again.", result.Message);
            Assert.Single(fixture.ActiveStatusUpdates);
            Assert.Empty(fixture.AuditInserts);
        }

        [Fact]
        public async Task A_partial_failure_still_returns_no_identifiers_even_though_writes_happened()
        {
            // Worth pinning: the IRF cascade already succeeded here, so the system is in a
            // half-applied state, yet the result deliberately carries nothing the caller could
            // use to render a success banner.
            var fixture = new InterviewerLifecycleFixture().GivenActiveStatusUpdateFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.False(result.Success);
            Assert.Equal(0, result.TransInterviewerId);
            Assert.Equal(0, result.IrfId);
            Assert.Null(result.FlagFlipped);
        }

        [Fact]
        public async Task A_null_interviewer_email_on_the_context_is_passed_through_to_the_cascade_as_is()
        {
            // No guard exists on the email in this rail, unlike the risk-audit rail. Recorded so
            // that adding one shows up as a deliberate change rather than a surprise.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(interviewerEmail: null));
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "Client", Actor);

            Assert.True(result.Success);
            Assert.Null(fixture.TheIrfCascade.InterviewerEmail);
        }
    }

    /// <summary>
    /// The internal rail: any authenticated non-Client, non-Public profile flips RB vet, which
    /// deactivates the interviewer on the channel for everyone. This is the heavier of the two
    /// rails, so it has an extra ordered step in front of the cascade.
    /// </summary>
    public sealed class DmIvChannelDeactivationInternalRailTests
    {
        private const string Actor = "dm@risebird.io";

        [Theory]
        [InlineData("DM")]
        [InlineData("SME")]
        [InlineData("Admin")]
        [InlineData("Ops")]
        [InlineData("QualityController")]
        [InlineData("  DM  ")]
        [InlineData("Clienta")]
        [InlineData("Publicity")]
        public async Task Any_internal_profile_takes_the_RB_vet_rail(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, profileName, Actor);

            Assert.True(result.Success);
            Assert.Equal("RB Vet", result.FlagFlipped);
            Assert.Equal(profileName.Trim(), result.Role);
        }

        [Fact]
        public async Task The_RB_rail_un_verifies_exactly_the_one_TransInterviewer_row()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            var vet = fixture.TheVetUpdate;
            Assert.Equal(Actor, vet.LoggedInSme);
            var row = Assert.Single(vet.Interviewers);
            Assert.Equal(501, row.id);
            Assert.False(row.IsSMEVerified);
        }

        [Fact]
        public async Task The_RB_rail_runs_vet_then_cascade_then_active_status_then_audit()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.Equal(
                new[]
                {
                    "UpdateVerifiedInterviewers",
                    "RemoveOrShortlistInterviewerFromIRF",
                    "UpdateIRFInterviewerActiveStatus",
                    "InsertAsync:" + DmIvInactiveAuditEventTypes.RbVetFlippedToNo
                },
                fixture.CallOrder);
        }

        [Fact]
        public async Task The_audit_row_records_an_RB_vet_flip_to_no()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "SME", Actor);

            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvInactiveAuditEventTypes.RbVetFlippedToNo, audit.EventType);
            Assert.Equal(501, audit.TransInterviewerId);
            Assert.Equal("0", audit.ToValue);
            Assert.Equal("SME", audit.ReasonCategory);
            Assert.Equal(11, audit.TriggeringInterviewId);
            Assert.Equal(Actor, audit.ChangedBy);
            Assert.Equal("ClientReportRemove", audit.AuditSource);
        }

        [Theory]
        [InlineData(true, "1")]
        [InlineData(false, "0")]
        public async Task The_audit_from_value_is_the_RB_vet_bit_before_the_flip(bool rbVetYes, string expectedFrom)
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(isRbVetYes: rbVetYes));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.Equal(expectedFrom, fixture.TheAuditRow.FromValue);
        }

        [Fact]
        public async Task Re_removing_an_already_unvetted_interviewer_is_recorded_as_a_no_op_flip()
        {
            // from and to are both "0". The row still gets written, which is intentional: the
            // audit trail is a record of who pressed the button, not only of state changes.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(isRbVetYes: false));
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.True(result.Success);
            Assert.Equal("0", fixture.TheAuditRow.FromValue);
            Assert.Equal("0", fixture.TheAuditRow.ToValue);
        }

        [Fact]
        public async Task The_audit_metadata_names_the_RB_vet_flag()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(userId: 88, irfId: 42));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(909, "DM", Actor);

            var metadata = JObject.Parse(fixture.TheAuditRow.ExtraMetadata);
            Assert.Equal("RB Vet", metadata["flagFlipped"].Value<string>());
            Assert.Equal("DM", metadata["role"].Value<string>());
            Assert.Equal(909, metadata["interviewId"].Value<int>());
            Assert.Equal(42, metadata["irfId"].Value<int>());
            Assert.Equal(88, metadata["interviewerUserId"].Value<int>());
        }

        [Fact]
        public async Task The_RB_rail_always_deactivates_the_channel()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(isRbVetYes: true, clientAction: "Shortlisted"));
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            var update = fixture.TheActiveStatusUpdate;
            Assert.False(update.Status);
            Assert.False(update.IsAvailable);
        }

        [Fact]
        public async Task A_failed_vet_update_stops_the_whole_rail()
        {
            var fixture = new InterviewerLifecycleFixture().GivenVetUpdateFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.False(result.Success);
            Assert.Equal("Could not remove interviewer. Please try again.", result.Message);
            Assert.Single(fixture.VetUpdates);
            Assert.Empty(fixture.IrfCascades);
            Assert.Empty(fixture.ActiveStatusUpdates);
            Assert.Empty(fixture.AuditInserts);
        }

        [Fact]
        public async Task A_failed_cascade_on_the_RB_rail_leaves_the_vet_flag_already_flipped()
        {
            // The vet update is not rolled back — this is the known non-transactional seam in
            // this rail, and it is pinned so a future fix (or a compensating write) is visible.
            var fixture = new InterviewerLifecycleFixture().GivenIrfCascadeFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.False(result.Success);
            Assert.Single(fixture.VetUpdates);
            Assert.Empty(fixture.AuditInserts);
        }

        [Fact]
        public async Task A_failed_active_status_update_on_the_RB_rail_stops_before_the_audit()
        {
            var fixture = new InterviewerLifecycleFixture().GivenActiveStatusUpdateFails();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, "DM", Actor);

            Assert.False(result.Success);
            Assert.Single(fixture.VetUpdates);
            Assert.Single(fixture.IrfCascades);
            Assert.Single(fixture.ActiveStatusUpdates);
            Assert.Empty(fixture.AuditInserts);
        }
    }

    /// <summary>
    /// Cross-rail invariants: whichever rail runs, exactly one audit row is written, the future
    /// count is read once, and the two rails never blend into each other.
    /// </summary>
    public sealed class DmIvChannelDeactivationInvariantTests
    {
        [Theory]
        [InlineData("Client", "Client Vet")]
        [InlineData("DM", "RB Vet")]
        public async Task Exactly_one_audit_row_is_written_per_successful_removal(string profileName, string expectedFlag)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(
                11, profileName, "actor@risebird.io");

            Assert.Equal(expectedFlag, result.FlagFlipped);
            Assert.Single(fixture.AuditInserts);
        }

        [Theory]
        [InlineData("Client")]
        [InlineData("DM")]
        public async Task Neither_rail_ever_writes_the_AutoQc_badge_fields(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, profileName, "actor@risebird.io");

            Assert.Empty(fixture.AutoQcFieldUpdates);
        }

        [Theory]
        [InlineData("Client")]
        [InlineData("DM")]
        public async Task The_future_assigned_count_is_read_once_per_removal(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture().GivenFutureAssignedCount(6);
            var sut = fixture.CreateDeactivationSut();

            var result = await sut.DeactivateInterviewerOnChannelFromClientReportAsync(
                11, profileName, "actor@risebird.io");

            Assert.Equal(6, result.FutureAssignedCount);
            fixture.Audit.Verify(x => x.GetFutureAssignedCountForInterviewAsync(11), Times.Once);
        }

        [Theory]
        [InlineData("Client")]
        [InlineData("DM")]
        public async Task Every_rail_rejects_rather_than_shortlists(string profileName)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateDeactivationSut();

            await sut.DeactivateInterviewerOnChannelFromClientReportAsync(11, profileName, "actor@risebird.io");

            Assert.Equal("Rejected", fixture.TheIrfCascade.Action);
        }

        [Fact]
        public void Fail_produces_an_unsuccessful_result_carrying_only_the_message()
        {
            var result = DmIvChannelDeactivationResult.Fail("nope");

            Assert.False(result.Success);
            Assert.Equal("nope", result.Message);
            Assert.Null(result.FlagFlipped);
            Assert.Null(result.Role);
            Assert.Equal(0, result.TransInterviewerId);
        }

        [Fact]
        public void Fail_accepts_a_null_message_without_throwing()
        {
            var result = DmIvChannelDeactivationResult.Fail(null);

            Assert.False(result.Success);
            Assert.Null(result.Message);
        }
    }
}
