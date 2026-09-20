using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AutoQcTests
{
    /// <summary>
    /// <c>TryApplyAutoQcRemovalAsync</c> is the forward trigger: it fires from the QC merge and is
    /// the only entry point that decides for itself whether a removal is warranted. Everything
    /// hinges on <c>ResolveVerdict</c>, so the no-op cases matter as much as the firing ones —
    /// a false positive here silently fires an interviewer.
    /// </summary>
    public sealed class DmIvAutoQcRemovalForwardTriggerTests
    {
        [Theory]
        [InlineData("Yes")]
        [InlineData("yes")]
        [InlineData("YES")]
        [InlineData(" Yes ")]
        [InlineData("Y")]
        [InlineData("y")]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        public async Task An_affirmative_suspicious_flag_removes_the_interviewer_as_suspicious(string suspicious)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, suspicious, redoInterview: false);

            Assert.Equal(DmIvAutoQcAuditConstants.VerdictSuspicious, fixture.TheAuditRow.ToValue);
        }

        [Fact]
        public async Task A_redo_verdict_removes_the_interviewer_as_redo()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "No", redoInterview: true);

            Assert.Equal(DmIvAutoQcAuditConstants.VerdictRedo, fixture.TheAuditRow.ToValue);
        }

        [Fact]
        public async Task Suspicious_wins_over_redo_when_both_are_set()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", redoInterview: true);

            Assert.Equal(DmIvAutoQcAuditConstants.VerdictSuspicious, fixture.TheAuditRow.ToValue);
        }

        [Theory]
        [InlineData("No")]
        [InlineData("no")]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData("pass")]
        [InlineData("conditional")]
        [InlineData("manual review")]
        [InlineData("maybe")]
        [InlineData("Yess")]
        [InlineData("Yes please")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task A_non_triggering_outcome_with_no_redo_is_a_complete_no_op(string suspicious)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, suspicious, redoInterview: false);

            Assert.True(fixture.NothingWasWritten);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        public async Task A_null_or_false_redo_flag_does_not_trigger_on_its_own(bool? redoInterview)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "No", redoInterview);

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_no_op_verdict_does_not_even_load_the_channel_context()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "No", redoInterview: false);

            fixture.Audit.Verify(x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_missing_channel_context_is_a_logged_no_op_rather_than_a_throw()
        {
            var fixture = new InterviewerLifecycleFixture().GivenNoContext();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", redoInterview: null);

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task An_IRF_mismatch_is_reported_but_does_not_stop_the_removal()
        {
            // The context's own IRF wins — the caller's irfId is only a sanity check. This is
            // deliberate: the removal must land on the channel the interview actually belongs to.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(irfId: 42));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, irfId: 99, "Yes", redoInterview: null);

            Assert.Equal(42, fixture.TheIrfCascade.IrfId);
            Assert.Equal(42, fixture.TheActiveStatusUpdate.IrfId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task A_non_positive_caller_IRF_skips_the_mismatch_check_entirely(int irfId)
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(irfId: 42));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, irfId, "Yes", redoInterview: null);

            Assert.Single(fixture.AuditInserts);
        }

        [Fact]
        public async Task A_matching_IRF_removes_without_complaint()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(irfId: 42));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, irfId: 42, "Yes", redoInterview: null);

            Assert.Single(fixture.AuditInserts);
        }

        [Fact]
        public async Task The_forward_trigger_stamps_the_forward_source_and_the_removal_tag()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", redoInterview: null);

            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvAutoQcAuditConstants.SourceForwardTrigger, audit.AuditSource);
            Assert.Equal(DmIvAutoQcAuditConstants.Tag, audit.AuditTag);
            Assert.Null(audit.BackfillBatchId);
            Assert.Null(audit.ExtraMetadata);
        }

        [Fact]
        public async Task The_forward_trigger_badges_the_interview_it_fired_from()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(909, 3, "Yes", redoInterview: null);

            Assert.Equal(909, fixture.TheAuditRow.TriggeringInterviewId);
            var badge = Assert.Single(fixture.AutoQcFieldUpdates);
            Assert.Equal(501, badge.TransInterviewerId);
            Assert.Equal(DmIvAutoQcAuditConstants.VerdictSuspicious, badge.AutoQcVerdict);
            Assert.Equal(909, badge.TriggeringInterviewId);
        }

        [Fact]
        public async Task The_forward_trigger_never_skips_an_already_inactive_interviewer()
        {
            // SkipIfAlreadyInactive is false on this rail: a fresh suspicious verdict must be
            // audited even if the interviewer was already un-vetted for another reason.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(rbVetAlreadyNo: true, isActive: false));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", redoInterview: null);

            Assert.Single(fixture.AuditInserts);
            Assert.Equal("Inactive", fixture.TheAuditRow.FromValue);
        }

        [Fact]
        public async Task A_duplicate_forward_removal_for_the_same_interview_is_skipped()
        {
            var fixture = new InterviewerLifecycleFixture().GivenDuplicateRemovalAudit();
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", redoInterview: null);

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task The_duplicate_check_is_scoped_to_this_interviewer_interview_and_source()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContext(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateRemovalSut();

            await sut.TryApplyAutoQcRemovalAsync(909, 3, "Yes", redoInterview: null);

            fixture.Audit.Verify(
                x => x.HasAutoQcRemovalAuditForInterviewAsync(
                    501, 909, DmIvAutoQcAuditConstants.SourceForwardTrigger),
                Times.Once);
        }

        [Fact]
        public async Task A_cancelled_token_throws_before_any_work()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.TryApplyAutoQcRemovalAsync(11, 3, "Yes", true, cts.Token));

            Assert.True(fixture.NothingWasWritten);
        }
    }

    /// <summary>
    /// <c>ApplyAutoQcRemovalForContextAsync</c> is the shared rail the forward trigger, the 60-day
    /// backfill and the per-IRF backfill all funnel through. Its options object is what makes the
    /// three callers behave differently, so each option gets its own cases.
    /// </summary>
    public sealed class DmIvAutoQcRemovalRailTests
    {
        private static AutoQcRemovalOptions ForwardOptions() => new()
        {
            Source = DmIvAutoQcAuditConstants.SourceForwardTrigger,
            Tag = DmIvAutoQcAuditConstants.Tag,
            SkipIfAlreadyInactive = false
        };

        private static AutoQcRemovalOptions BackfillOptions(Guid? batchId = null) => new()
        {
            Source = DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day,
            Tag = DmIvAutoQcAuditConstants.Tag,
            SkipIfAlreadyInactive = true,
            BackfillBatchId = batchId
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("\t")]
        public async Task A_blank_verdict_is_refused(string verdict)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, verdict, ForwardOptions());

            Assert.False(applied);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_null_options_object_falls_back_to_the_forward_trigger_defaults()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", options: null);

            Assert.True(applied);
            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvAutoQcAuditConstants.SourceForwardTrigger, audit.AuditSource);
            Assert.Equal(DmIvAutoQcAuditConstants.Tag, audit.AuditTag);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_source_is_treated_as_the_forward_trigger(string source)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo",
                new AutoQcRemovalOptions { Source = source, Tag = null });

            Assert.Equal(DmIvAutoQcAuditConstants.SourceForwardTrigger, fixture.TheAuditRow.AuditSource);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_source_also_gets_the_duplicate_check_because_it_became_the_forward_trigger(string source)
        {
            var fixture = new InterviewerLifecycleFixture().GivenDuplicateRemovalAudit();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo",
                new AutoQcRemovalOptions { Source = source });

            Assert.False(applied);
            Assert.True(fixture.NothingWasWritten);
        }

        [Theory]
        [InlineData(DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day)]
        [InlineData(DmIvAutoQcAuditConstants.SourcePerIrfBackfill)]
        [InlineData("some_other_source")]
        public async Task A_non_forward_source_skips_the_duplicate_check(string source)
        {
            // Backfills do their own de-duplication in SQL over a whole window, so the
            // per-interview check would only cost a round trip.
            var fixture = new InterviewerLifecycleFixture().GivenDuplicateRemovalAudit();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo",
                new AutoQcRemovalOptions { Source = source });

            Assert.True(applied);
            fixture.Audit.Verify(
                x => x.HasAutoQcRemovalAuditForInterviewAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Backfill_skips_an_interviewer_whose_RB_vet_is_already_no()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(rbVetAlreadyNo: true), 11, "redo", BackfillOptions());

            Assert.False(applied);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task Backfill_still_removes_an_interviewer_whose_RB_vet_is_yes()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(rbVetAlreadyNo: false), 11, "redo", BackfillOptions());

            Assert.True(applied);
            Assert.Single(fixture.AuditInserts);
        }

        [Fact]
        public async Task The_skip_already_inactive_option_is_what_gates_it_not_the_IsActive_flag()
        {
            // RbVetAlreadyNo, not IsActive, is the gate: an interviewer can be inactive because a
            // client rejected them while RB vet is still yes, and that case must still be removed.
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(isActive: false, rbVetAlreadyNo: false),
                11, "redo", BackfillOptions());

            Assert.True(applied);
        }

        [Fact]
        public async Task Backfill_carries_its_batch_id_and_metadata_onto_the_audit_row()
        {
            var batchId = Guid.NewGuid();
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions(batchId);
            options.ExtraMetadata = "{\"triggers\":[1,2,3]}";

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", options);

            var audit = fixture.TheAuditRow;
            Assert.Equal(batchId, audit.BackfillBatchId);
            Assert.Equal("{\"triggers\":[1,2,3]}", audit.ExtraMetadata);
            Assert.Equal(DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day, audit.AuditSource);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_tag_falls_back_to_the_removal_tag(string tag)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo",
                new AutoQcRemovalOptions { Source = DmIvAutoQcAuditConstants.SourcePerIrfBackfill, Tag = tag });

            // Only null coalesces — an empty or whitespace tag is stored verbatim, unlike source.
            var expected = tag ?? DmIvAutoQcAuditConstants.Tag;
            Assert.Equal(expected, fixture.TheAuditRow.AuditTag);
        }

        [Fact]
        public async Task A_custom_tag_is_kept()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo",
                new AutoQcRemovalOptions { Source = DmIvAutoQcAuditConstants.SourcePerIrfBackfill, Tag = "custom_tag" });

            Assert.Equal("custom_tag", fixture.TheAuditRow.AuditTag);
        }

        [Fact]
        public async Task A_badge_verdict_overrides_the_removal_verdict_on_both_the_audit_and_the_badge()
        {
            // Backfill uses this when one interviewer has several triggering interviews: the
            // audit records the worst verdict, not the one for the row being replayed.
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions();
            options.BadgeVerdict = DmIvAutoQcAuditConstants.VerdictSuspicious;

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, DmIvAutoQcAuditConstants.VerdictRedo, options);

            Assert.Equal(DmIvAutoQcAuditConstants.VerdictSuspicious, fixture.TheAuditRow.ToValue);
            Assert.Equal(
                DmIvAutoQcAuditConstants.VerdictSuspicious,
                Assert.Single(fixture.AutoQcFieldUpdates).AutoQcVerdict);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_badge_verdict_falls_back_to_the_removal_verdict(string badgeVerdict)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions();
            options.BadgeVerdict = badgeVerdict;

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, DmIvAutoQcAuditConstants.VerdictRedo, options);

            Assert.Equal(DmIvAutoQcAuditConstants.VerdictRedo, fixture.TheAuditRow.ToValue);
        }

        [Fact]
        public async Task A_badge_interview_id_overrides_the_triggering_interview()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions();
            options.BadgeTriggeringInterviewId = 777;

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", options);

            Assert.Equal(777, fixture.TheAuditRow.TriggeringInterviewId);
            Assert.Equal(777, Assert.Single(fixture.AutoQcFieldUpdates).TriggeringInterviewId);
        }

        [Fact]
        public async Task A_null_badge_interview_id_falls_back_to_the_triggering_interview()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions();
            options.BadgeTriggeringInterviewId = null;

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", options);

            Assert.Equal(11, fixture.TheAuditRow.TriggeringInterviewId);
        }

        [Fact]
        public async Task A_badge_interview_id_of_zero_is_kept_because_only_null_coalesces()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            var options = BackfillOptions();
            options.BadgeTriggeringInterviewId = 0;

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", options);

            Assert.Equal(0, fixture.TheAuditRow.TriggeringInterviewId);
        }

        [Fact]
        public async Task Removal_always_acts_as_the_AI_system_not_as_a_person()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.Equal("AISystem", fixture.TheAuditRow.ChangedBy);
            Assert.Equal("AISystem", fixture.TheVetUpdate.LoggedInSme);
            Assert.Equal("AISystem", fixture.TheActiveStatusUpdate.LoggedInUser);
        }

        [Theory]
        [InlineData(true, "Active")]
        [InlineData(false, "Inactive")]
        public async Task The_audit_from_value_is_the_prior_active_state_as_a_word(bool isActive, string expectedFrom)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(isActive: isActive), 11, "redo", ForwardOptions());

            Assert.Equal(expectedFrom, fixture.TheAuditRow.FromValue);
        }

        [Fact]
        public async Task Removal_records_no_reason_category_because_the_verdict_is_the_reason()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.Null(fixture.TheAuditRow.ReasonCategory);
            Assert.Equal(DmIvInactiveAuditEventTypes.AutoQcRemoval, fixture.TheAuditRow.EventType);
        }

        [Fact]
        public async Task The_rail_runs_vet_then_cascade_then_active_status_then_audit_then_badge()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.Equal(
                new[]
                {
                    "UpdateVerifiedInterviewers",
                    "RemoveOrShortlistInterviewerFromIRF",
                    "UpdateIRFInterviewerActiveStatus",
                    "InsertAsync:" + DmIvInactiveAuditEventTypes.AutoQcRemoval,
                    "UpdateAutoQcFieldsAsync"
                },
                fixture.CallOrder);
        }

        [Fact]
        public async Task A_failed_vet_update_aborts_the_rail_and_reports_it_did_not_apply()
        {
            var fixture = new InterviewerLifecycleFixture().GivenVetUpdateFails();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.False(applied);
            Assert.Single(fixture.VetUpdates);
            Assert.Empty(fixture.IrfCascades);
            Assert.Empty(fixture.AuditInserts);
            Assert.Empty(fixture.AutoQcFieldUpdates);
        }

        [Fact]
        public async Task A_failed_IRF_cascade_does_not_stop_this_rail_unlike_the_client_report_rail()
        {
            // Documented asymmetry: the auto-QC rail ignores the cascade's return value, so a
            // failed IRF removal still produces an audited, badged removal. Pinned deliberately —
            // this is the divergence from DmIvChannelDeactivationService worth knowing about.
            var fixture = new InterviewerLifecycleFixture().GivenIrfCascadeFails();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.True(applied);
            Assert.Single(fixture.AuditInserts);
            Assert.Single(fixture.AutoQcFieldUpdates);
        }

        [Fact]
        public async Task A_failed_active_status_update_also_does_not_stop_this_rail()
        {
            var fixture = new InterviewerLifecycleFixture().GivenActiveStatusUpdateFails();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.True(applied);
            Assert.Single(fixture.AuditInserts);
        }

        [Fact]
        public async Task The_cascade_rejects_the_interviewer_by_email_on_the_context_IRF()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(irfId: 42, interviewerEmail: "iv@x.test"),
                11, "redo", ForwardOptions());

            var cascade = fixture.TheIrfCascade;
            Assert.Equal(42, cascade.IrfId);
            Assert.Equal("iv@x.test", cascade.InterviewerEmail);
            Assert.Equal("Rejected", cascade.Action);
        }

        [Fact]
        public async Task The_channel_is_deactivated_and_marked_unavailable()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(userId: 88, irfId: 42), 11, "redo", ForwardOptions());

            var update = fixture.TheActiveStatusUpdate;
            Assert.Equal(88, update.UserId);
            Assert.Equal(42, update.IrfId);
            Assert.False(update.Status);
            Assert.False(update.IsAvailable);
            Assert.False(update.PreserveAvailability);
        }

        [Fact]
        public async Task Exactly_one_TransInterviewer_row_is_un_verified()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(transInterviewerId: 501), 11, "redo", ForwardOptions());

            var row = Assert.Single(fixture.TheVetUpdate.Interviewers);
            Assert.Equal(501, row.id);
            Assert.False(row.IsSMEVerified);
        }

        [Fact]
        public async Task The_badge_update_does_not_carry_a_flag_source_on_the_removal_rail()
        {
            // Only the flag service passes a flagSource; removal leaves it null so the badge
            // reads as an auto-QC removal rather than a bad-quality flag.
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions());

            Assert.Null(Assert.Single(fixture.AutoQcFieldUpdates).FlagSource);
        }

        [Fact]
        public async Task A_cancelled_token_throws_before_any_work()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.ApplyAutoQcRemovalForContextAsync(
                    InterviewerLifecycleFixture.Context(), 11, "redo", ForwardOptions(), cts.Token));

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_null_context_throws_rather_than_silently_doing_nothing()
        {
            // Callers of this overload have already resolved a context; passing null is a bug in
            // the caller, and a NullReferenceException is preferable to a silent no-op.
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await Assert.ThrowsAsync<NullReferenceException>(
                () => sut.ApplyAutoQcRemovalForContextAsync(null, 11, "redo", ForwardOptions()));
        }

        [Fact]
        public async Task A_null_context_with_a_blank_verdict_returns_false_before_dereferencing_it()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var applied = await sut.ApplyAutoQcRemovalForContextAsync(null, 11, "", ForwardOptions());

            Assert.False(applied);
        }
    }

    /// <summary>
    /// The IRF-audit removal rail: ops-initiated, so it validates far harder than the automated
    /// rail and returns a message the operator sees. Every guard here is a distinct message, and
    /// a wrong one would tell an operator the wrong thing about a live interviewer.
    /// </summary>
    public sealed class DmIvAutoQcRemovalRiskAuditRemoveTests
    {
        private const string Actor = "ops@risebird.io";

        [Theory]
        [InlineData(0, 3)]
        [InlineData(-1, 3)]
        [InlineData(100, 0)]
        [InlineData(100, -5)]
        [InlineData(0, 0)]
        [InlineData(int.MinValue, int.MinValue)]
        public async Task Non_positive_identifiers_are_refused_before_any_lookup(int transInterviewerId, int irfId)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(transInterviewerId, irfId, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Invalid interviewer or IRF.", result.Message);
            Assert.True(fixture.NothingWasWritten);
            fixture.Audit.Verify(x => x.GetContextByTransInterviewerIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_missing_channel_is_refused()
        {
            var fixture = new InterviewerLifecycleFixture().GivenNoContextByTransInterviewerId();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("TransInterviewer channel not found.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task An_interviewer_on_a_different_IRF_is_refused()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(irfId: 42));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, irfId: 99, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Interviewer is not on the selected IRF.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task An_already_inactive_interviewer_is_refused()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(isActive: false));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Interviewer is already inactive on this IRF.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_missing_interviewer_email_is_refused_because_the_cascade_needs_it(string email)
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(interviewerEmail: email));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Interviewer email is missing; cannot run removal rail.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_valid_removal_succeeds_with_the_operator_message()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, 3, Actor, "3 high-severity flags", 55);

            Assert.True(result.Ok);
            Assert.Equal("Interviewer removed from IRF.", result.Message);
        }

        [Theory]
        [InlineData(null, "System")]
        [InlineData("", "System")]
        [InlineData("   ", "System")]
        [InlineData("ops@risebird.io", "ops@risebird.io")]
        [InlineData("  ops@risebird.io  ", "ops@risebird.io")]
        public async Task A_blank_actor_becomes_System_and_a_real_one_is_trimmed(string changedBy, string expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, changedBy, "risk", null);

            Assert.Equal(expected, fixture.TheAuditRow.ChangedBy);
            Assert.Equal(expected, fixture.TheVetUpdate.LoggedInSme);
            Assert.Equal(expected, fixture.TheActiveStatusUpdate.LoggedInUser);
        }

        [Fact]
        public async Task The_audit_row_records_a_manual_risk_audit_removal()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(501, 3, Actor, "risk", null);

            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvInactiveAuditEventTypes.RiskAuditRemoval, audit.EventType);
            Assert.Equal(501, audit.TransInterviewerId);
            Assert.Equal("Active", audit.FromValue);
            Assert.Equal("risk_audit", audit.ToValue);
            Assert.Equal(DmIvAutoQcAuditConstants.RiskAuditTag, audit.AuditTag);
            Assert.Equal(DmIvAutoQcAuditConstants.SourceRiskAuditManual, audit.AuditSource);
            Assert.Null(audit.BackfillBatchId);
        }

        [Theory]
        [InlineData(null, "Risk audit removal")]
        [InlineData("", "Risk audit removal")]
        [InlineData("   ", "Risk audit removal")]
        [InlineData("3 high flags", "3 high flags")]
        [InlineData("  3 high flags  ", "3 high flags")]
        public async Task A_blank_risk_summary_falls_back_to_a_default_reason(string riskSummary, string expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, riskSummary, null);

            Assert.Equal(expected, fixture.TheAuditRow.ReasonCategory);
        }

        [Fact]
        public async Task A_comment_is_appended_to_the_reason_detail()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "3 high flags", null, comment: "  spoke to client  ");

            Assert.Equal("3 high flags; comment=spoke to client", fixture.TheAuditRow.ReasonCategory);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_comment_is_not_appended(string comment)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "3 high flags", null, comment);

            Assert.Equal("3 high flags", fixture.TheAuditRow.ReasonCategory);
        }

        [Fact]
        public async Task A_comment_on_a_blank_summary_is_appended_to_the_default_reason()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, null, null, comment: "manual");

            Assert.Equal("Risk audit removal; comment=manual", fixture.TheAuditRow.ReasonCategory);
        }

        [Fact]
        public async Task A_reason_detail_of_exactly_500_characters_is_kept_whole()
        {
            var summary = new string('x', 500);
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, summary, null);

            Assert.Equal(500, fixture.TheAuditRow.ReasonCategory.Length);
        }

        [Fact]
        public async Task An_over_long_reason_detail_is_truncated_to_500_characters()
        {
            var summary = new string('x', 900);
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, summary, null);

            var reason = fixture.TheAuditRow.ReasonCategory;
            Assert.Equal(500, reason.Length);
            Assert.Equal(new string('x', 500), reason);
        }

        [Fact]
        public async Task Truncation_happens_after_the_comment_is_appended_so_a_long_comment_can_be_cut_off()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(
                100, 3, Actor, new string('a', 480), null, comment: new string('b', 100));

            var reason = fixture.TheAuditRow.ReasonCategory;
            Assert.Equal(500, reason.Length);
            Assert.StartsWith(new string('a', 480) + "; comment=", reason);
        }

        [Fact]
        public async Task The_reason_detail_is_mirrored_into_the_metadata_column()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "3 high flags", null, comment: "ok");

            var audit = fixture.TheAuditRow;
            Assert.Equal(audit.ReasonCategory, audit.ExtraMetadata);
        }

        [Theory]
        [InlineData(55, 55)]
        [InlineData(1, 1)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public async Task A_positive_worst_severity_interview_becomes_the_triggering_interview(
            int worstSeverityInterviewId, int expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", worstSeverityInterviewId);

            Assert.Equal(expected, fixture.TheAuditRow.TriggeringInterviewId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task A_missing_or_non_positive_worst_severity_interview_leaves_the_trigger_null(
            int? worstSeverityInterviewId)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", worstSeverityInterviewId);

            Assert.Null(fixture.TheAuditRow.TriggeringInterviewId);
        }

        [Fact]
        public async Task The_risk_audit_rail_runs_vet_then_cascade_then_active_status_then_audit()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.Equal(
                new[]
                {
                    "UpdateVerifiedInterviewers",
                    "RemoveOrShortlistInterviewerFromIRF",
                    "UpdateIRFInterviewerActiveStatus",
                    "InsertAsync:" + DmIvInactiveAuditEventTypes.RiskAuditRemoval
                },
                fixture.CallOrder);
        }

        [Fact]
        public async Task A_failed_vet_update_aborts_with_its_own_message()
        {
            var fixture = new InterviewerLifecycleFixture().GivenVetUpdateFails();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Could not update RB vet status.", result.Message);
            Assert.Empty(fixture.IrfCascades);
            Assert.Empty(fixture.AuditInserts);
        }

        [Fact]
        public async Task The_risk_audit_rail_never_writes_the_AutoQc_badge()
        {
            // A manual removal must not make the interviewer look auto-flagged by QC.
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", 55);

            Assert.Empty(fixture.AutoQcFieldUpdates);
        }

        [Fact]
        public async Task The_risk_audit_rail_deactivates_the_channel()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(
                    InterviewerLifecycleFixture.Context(userId: 88, irfId: 3, isRbVetYes: true));
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null);

            var update = fixture.TheActiveStatusUpdate;
            Assert.Equal(88, update.UserId);
            Assert.False(update.Status);
            Assert.False(update.IsAvailable);
        }

        [Fact]
        public async Task A_cancelled_token_throws_before_any_work()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.RemoveForRiskAuditAsync(100, 3, Actor, "risk", null, null, cts.Token));

            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task The_context_is_resolved_by_TransInterviewer_id_not_by_interview()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(501, 3, Actor, "risk", 55);

            fixture.Audit.Verify(x => x.GetContextByTransInterviewerIdAsync(501), Times.Once);
            fixture.Audit.Verify(
                x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }

    /// <summary>
    /// The IRF-audit "keep" rail: a review decision only. Its whole point is that it changes no
    /// state — if it ever starts writing a vet flag or an IsActive bit, these tests fail.
    /// </summary>
    public sealed class DmIvAutoQcRemovalRiskAuditKeepTests
    {
        private const string Actor = "ops@risebird.io";

        [Theory]
        [InlineData(0, 3)]
        [InlineData(-1, 3)]
        [InlineData(100, 0)]
        [InlineData(100, -5)]
        public async Task Non_positive_identifiers_are_refused(int transInterviewerId, int irfId)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(transInterviewerId, irfId, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Invalid interviewer or IRF.", result.Message);
            Assert.True(fixture.NothingWasWritten);
        }

        [Fact]
        public async Task A_missing_channel_is_refused()
        {
            var fixture = new InterviewerLifecycleFixture().GivenNoContextByTransInterviewerId();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("TransInterviewer channel not found.", result.Message);
        }

        [Fact]
        public async Task An_interviewer_on_a_different_IRF_is_refused()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(irfId: 42));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(100, irfId: 99, Actor, "risk", null);

            Assert.False(result.Ok);
            Assert.Equal("Interviewer is not on the selected IRF.", result.Message);
        }

        [Fact]
        public async Task Keeping_an_already_inactive_interviewer_is_allowed_unlike_removing_one()
        {
            // There is no IsActive guard on this rail: recording "we reviewed and kept them" is
            // meaningful even for a channel that some other rail has since deactivated.
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(isActive: false));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.True(result.Ok);
            Assert.Equal("Inactive", fixture.TheAuditRow.FromValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_missing_interviewer_email_does_not_block_a_keep_because_no_cascade_runs(string email)
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(interviewerEmail: email));
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", null);

            Assert.True(result.Ok);
        }

        [Fact]
        public async Task A_keep_writes_one_audit_row_and_changes_nothing_else()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            var result = await sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", 55);

            Assert.True(result.Ok);
            Assert.Equal("Interviewer kept on IRF.", result.Message);
            Assert.Single(fixture.AuditInserts);
            Assert.Empty(fixture.VetUpdates);
            Assert.Empty(fixture.IrfCascades);
            Assert.Empty(fixture.ActiveStatusUpdates);
            Assert.Empty(fixture.AutoQcFieldUpdates);
        }

        [Fact]
        public async Task The_audit_row_records_a_manual_risk_audit_keep()
        {
            var fixture = new InterviewerLifecycleFixture()
                .GivenContextByTransInterviewerId(InterviewerLifecycleFixture.Context(transInterviewerId: 501));
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(501, 3, Actor, "risk", 55);

            var audit = fixture.TheAuditRow;
            Assert.Equal(DmIvInactiveAuditEventTypes.RiskAuditKept, audit.EventType);
            Assert.Equal(501, audit.TransInterviewerId);
            Assert.Equal("Active", audit.FromValue);
            Assert.Equal("risk_audit_kept", audit.ToValue);
            Assert.Equal(DmIvAutoQcAuditConstants.RiskAuditTag, audit.AuditTag);
            Assert.Equal(DmIvAutoQcAuditConstants.SourceRiskAuditManual, audit.AuditSource);
            Assert.Equal(55, audit.TriggeringInterviewId);
            Assert.Equal(Actor, audit.ChangedBy);
        }

        [Theory]
        [InlineData(null, "Risk audit keep")]
        [InlineData("", "Risk audit keep")]
        [InlineData("   ", "Risk audit keep")]
        [InlineData("looks fine", "looks fine")]
        [InlineData("  looks fine  ", "looks fine")]
        public async Task A_blank_risk_summary_falls_back_to_the_keep_default(string riskSummary, string expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, Actor, riskSummary, null);

            Assert.Equal(expected, fixture.TheAuditRow.ReasonCategory);
        }

        [Fact]
        public async Task A_comment_is_appended_to_the_keep_reason()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, Actor, "looks fine", null, comment: " reviewed with DM ");

            Assert.Equal("looks fine; comment=reviewed with DM", fixture.TheAuditRow.ReasonCategory);
        }

        [Fact]
        public async Task An_over_long_keep_reason_is_truncated_to_500_characters()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, Actor, new string('k', 800), null);

            Assert.Equal(500, fixture.TheAuditRow.ReasonCategory.Length);
        }

        [Theory]
        [InlineData(null, "System")]
        [InlineData("  ", "System")]
        [InlineData("  ops@risebird.io ", "ops@risebird.io")]
        public async Task A_blank_actor_becomes_System_on_the_keep_rail_too(string changedBy, string expected)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, changedBy, "risk", null);

            Assert.Equal(expected, fixture.TheAuditRow.ChangedBy);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task A_non_positive_worst_severity_interview_leaves_the_trigger_null(int? worstSeverityInterviewId)
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", worstSeverityInterviewId);

            Assert.Null(fixture.TheAuditRow.TriggeringInterviewId);
        }

        [Fact]
        public async Task The_keep_reason_is_mirrored_into_the_metadata_column()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.KeepForRiskAuditAsync(100, 3, Actor, "looks fine", null);

            Assert.Equal("looks fine", fixture.TheAuditRow.ExtraMetadata);
        }

        [Fact]
        public async Task A_cancelled_token_throws_before_any_work()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.KeepForRiskAuditAsync(100, 3, Actor, "risk", null, null, cts.Token));

            Assert.True(fixture.NothingWasWritten);
        }
    }

    /// <summary>
    /// Invariants that must hold across every rail on this service, whichever entry point ran.
    /// These are the ones that would let a bug in one rail quietly corrupt shared state.
    /// </summary>
    public sealed class DmIvAutoQcRemovalInvariantTests
    {
        [Fact]
        public async Task Every_rail_that_deactivates_rejects_rather_than_shortlists()
        {
            var forward = new InterviewerLifecycleFixture();
            await forward.CreateRemovalSut().TryApplyAutoQcRemovalAsync(11, 3, "Yes", null);

            var riskAudit = new InterviewerLifecycleFixture();
            await riskAudit.CreateRemovalSut().RemoveForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);

            Assert.Equal("Rejected", forward.TheIrfCascade.Action);
            Assert.Equal("Rejected", riskAudit.TheIrfCascade.Action);
        }

        [Fact]
        public async Task Only_the_automated_rail_writes_the_AutoQc_badge_fields()
        {
            var forward = new InterviewerLifecycleFixture();
            await forward.CreateRemovalSut().TryApplyAutoQcRemovalAsync(11, 3, "Yes", null);

            var removed = new InterviewerLifecycleFixture();
            await removed.CreateRemovalSut().RemoveForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);

            var kept = new InterviewerLifecycleFixture();
            await kept.CreateRemovalSut().KeepForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);

            Assert.Single(forward.AutoQcFieldUpdates);
            Assert.Empty(removed.AutoQcFieldUpdates);
            Assert.Empty(kept.AutoQcFieldUpdates);
        }

        [Fact]
        public async Task Each_rail_writes_its_own_distinct_event_type()
        {
            var forward = new InterviewerLifecycleFixture();
            await forward.CreateRemovalSut().TryApplyAutoQcRemovalAsync(11, 3, "Yes", null);

            var removed = new InterviewerLifecycleFixture();
            await removed.CreateRemovalSut().RemoveForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);

            var kept = new InterviewerLifecycleFixture();
            await kept.CreateRemovalSut().KeepForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);

            var eventTypes = new[]
            {
                forward.TheAuditRow.EventType,
                removed.TheAuditRow.EventType,
                kept.TheAuditRow.EventType
            };

            Assert.Equal(
                new[]
                {
                    DmIvInactiveAuditEventTypes.AutoQcRemoval,
                    DmIvInactiveAuditEventTypes.RiskAuditRemoval,
                    DmIvInactiveAuditEventTypes.RiskAuditKept
                },
                eventTypes);
            Assert.Equal(3, eventTypes.Distinct().Count());
        }

        [Fact]
        public async Task Exactly_one_audit_row_is_written_per_successful_call_on_every_rail()
        {
            var forward = new InterviewerLifecycleFixture();
            await forward.CreateRemovalSut().TryApplyAutoQcRemovalAsync(11, 3, "Yes", null);
            Assert.Single(forward.AuditInserts);

            var rail = new InterviewerLifecycleFixture();
            await rail.CreateRemovalSut().ApplyAutoQcRemovalForContextAsync(
                InterviewerLifecycleFixture.Context(), 11, "redo", null);
            Assert.Single(rail.AuditInserts);

            var removed = new InterviewerLifecycleFixture();
            await removed.CreateRemovalSut().RemoveForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);
            Assert.Single(removed.AuditInserts);

            var kept = new InterviewerLifecycleFixture();
            await kept.CreateRemovalSut().KeepForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", null);
            Assert.Single(kept.AuditInserts);
        }

        [Fact]
        public void The_two_risk_audit_rails_share_a_tag_but_not_an_event_type()
        {
            Assert.Equal("irf_audit_risk_removal", DmIvAutoQcAuditConstants.RiskAuditTag);
            Assert.NotEqual(DmIvAutoQcAuditConstants.RiskAuditTag, DmIvAutoQcAuditConstants.Tag);
            Assert.NotEqual(
                DmIvInactiveAuditEventTypes.RiskAuditRemoval,
                DmIvInactiveAuditEventTypes.RiskAuditKept);
        }

        [Fact]
        public async Task Removal_and_keep_never_touch_a_context_resolved_by_interview_id()
        {
            var fixture = new InterviewerLifecycleFixture();
            var sut = fixture.CreateRemovalSut();

            await sut.RemoveForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", 55);
            await sut.KeepForRiskAuditAsync(100, 3, "ops@risebird.io", "risk", 55);

            fixture.Audit.Verify(
                x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
