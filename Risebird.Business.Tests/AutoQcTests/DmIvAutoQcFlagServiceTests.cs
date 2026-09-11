using Microsoft.Extensions.Logging;
using Moq;
using Risebird.Business;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.AutoQcTests
{
    public sealed class DmIvAutoQcFlagFixture
    {
        public Mock<ITransInterviewerAuditRepository> Audit { get; } = new(MockBehavior.Loose);
        public Mock<ILogger<DmIvAutoQcFlagService>> Logger { get; } = new(MockBehavior.Loose);

        public DmIvAutoQcFlagService CreateSut()
        {
            Audit.Setup(x => x.HasAutoQcFlagAuditForInterviewAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            return new DmIvAutoQcFlagService(Audit.Object, Logger.Object);
        }

        public static TransInterviewerInterviewContext Context(
            int transInterviewerId = 100,
            int irfId = 3,
            string autoQcVerdict = null) => new()
            {
                TransInterviewerId = transInterviewerId,
                UserId = 9,
                IrfId = irfId,
                IsActive = true,
                InterviewerEmail = "iv@risebird.io",
                InterviewerCodedId = "AdLo1234",
                AutoQcVerdict = autoQcVerdict
            };

        /// <summary>Any AutoQcFlag audit insert, whatever the verdict and source.</summary>
        public void VerifyInsert(Times times) =>
            Audit.Verify(
                x => x.InsertAsync(
                    It.IsAny<int>(),
                    DmIvInactiveAuditEventTypes.AutoQcFlag,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<string>()),
                times);

        /// <summary>An AutoQcFlag audit insert carrying exactly this verdict.</summary>
        public void VerifyInsertWithVerdict(string verdict, Times times) =>
            Audit.Verify(
                x => x.InsertAsync(
                    It.IsAny<int>(),
                    DmIvInactiveAuditEventTypes.AutoQcFlag,
                    It.IsAny<string>(),
                    verdict,
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<string>()),
                times);

        /// <summary>An AutoQcFlag audit insert carrying exactly this audit source.</summary>
        public void VerifyInsertWithSource(string source, Times times) =>
            Audit.Verify(
                x => x.InsertAsync(
                    It.IsAny<int>(),
                    DmIvInactiveAuditEventTypes.AutoQcFlag,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    source,
                    It.IsAny<Guid?>(),
                    It.IsAny<string>()),
                times);
    }

    public class DmIvAutoQcFlagServiceTryApplyTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("No", false)]
        [InlineData("", null)]
        [InlineData("pass", false)]
        [InlineData("conditional", null)]
        [InlineData("manual review", false)]
        public async Task Does_nothing_when_the_merge_verdict_does_not_trigger(string suspicious, bool? redo)
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.TryApplyAutoQcFlagAsync(1, 3, suspicious, redo);

            fixture.Audit.Verify(x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            fixture.VerifyInsert(Times.Never());
        }

        [Theory]
        [InlineData("Yes", null)]
        [InlineData("Y", false)]
        [InlineData("true", null)]
        public async Task A_suspicious_verdict_flags_the_row(string suspicious, bool? redo)
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 3))
                .ReturnsAsync(DmIvAutoQcFlagFixture.Context());

            await sut.TryApplyAutoQcFlagAsync(1, 3, suspicious, redo);

            fixture.VerifyInsertWithVerdict(DmIvAutoQcAuditConstants.VerdictSuspicious, Times.Once());
        }

        [Fact]
        public async Task A_redo_verdict_flags_the_row()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 3))
                .ReturnsAsync(DmIvAutoQcFlagFixture.Context());

            await sut.TryApplyAutoQcFlagAsync(1, 3, "No", true);

            fixture.VerifyInsertWithVerdict(DmIvAutoQcAuditConstants.VerdictRedo, Times.Once());
        }

        [Fact]
        public async Task Stops_when_no_trans_interviewer_row_can_be_found()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 3))
                .ReturnsAsync((TransInterviewerInterviewContext)null);

            await sut.TryApplyAutoQcFlagAsync(1, 3, "Yes", null);

            fixture.VerifyInsert(Times.Never());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task An_irf_mismatch_is_only_a_warning_and_the_flag_still_lands()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 999))
                .ReturnsAsync(DmIvAutoQcFlagFixture.Context(irfId: 3));

            await sut.TryApplyAutoQcFlagAsync(1, 999, "Yes", null);

            fixture.VerifyInsert(Times.Once());
        }

        [Fact]
        public async Task A_zero_irf_id_skips_the_mismatch_check()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 0))
                .ReturnsAsync(DmIvAutoQcFlagFixture.Context(irfId: 3));

            await sut.TryApplyAutoQcFlagAsync(1, 0, "Yes", null);

            fixture.VerifyInsert(Times.Once());
        }

        [Fact]
        public async Task Uses_the_forward_trigger_source_and_flag_tag()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.GetContextForInterviewAsync(1, 3))
                .ReturnsAsync(DmIvAutoQcFlagFixture.Context());

            await sut.TryApplyAutoQcFlagAsync(1, 3, "Yes", null);

            fixture.Audit.Verify(
                x => x.InsertAsync(
                    100,
                    DmIvInactiveAuditEventTypes.AutoQcFlag,
                    "unset",
                    DmIvAutoQcAuditConstants.VerdictSuspicious,
                    null,
                    1,
                    "AISystem",
                    DmIvAutoQcAuditConstants.FlagTag,
                    DmIvAutoQcAuditConstants.SourceForwardTrigger,
                    null,
                    null),
                Times.Once);
        }

        [Fact]
        public async Task Honours_a_cancellation_request_before_doing_any_work()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.TryApplyAutoQcFlagAsync(1, 3, "Yes", null, cts.Token));

            fixture.Audit.Verify(
                x => x.GetContextForInterviewAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }

    public class DmIvAutoQcFlagServiceApplyForContextTests
    {
        [Fact]
        public async Task Returns_false_for_a_null_context()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            Assert.False(await sut.ApplyAutoQcFlagForContextAsync(null, 1, "suspicious", null));
            fixture.VerifyInsert(Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Returns_false_for_a_blank_verdict(string verdict)
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            Assert.False(await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, verdict, null));
            fixture.VerifyInsert(Times.Never());
        }

        [Fact]
        public async Task Skips_a_duplicate_flag_for_the_same_triggering_interview()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            fixture.Audit.Setup(x => x.HasAutoQcFlagAuditForInterviewAsync(100, 1, null)).ReturnsAsync(true);

            Assert.False(await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious", null));

            fixture.VerifyInsert(Times.Never());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task The_duplicate_check_ignores_the_audit_source_so_any_earlier_flag_counts()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious",
                new AutoQcFlagOptions { Source = DmIvAutoQcAuditConstants.SourcePerIrfBackfill });

            fixture.Audit.Verify(x => x.HasAutoQcFlagAuditForInterviewAsync(100, 1, null), Times.Once);
        }

        [Fact]
        public async Task Writes_the_audit_row_and_updates_the_badge_fields()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context();

            Assert.True(await sut.ApplyAutoQcFlagForContextAsync(context, 1, "suspicious", null));

            fixture.VerifyInsert(Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(100, "suspicious", 1, DmIvAutoQcAuditConstants.SourceForwardTrigger),
                Times.Once);
        }

        [Fact]
        public async Task Mutates_the_context_so_callers_see_the_new_verdict()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context();

            await sut.ApplyAutoQcFlagForContextAsync(context, 1, "suspicious", null);

            Assert.Equal("suspicious", context.AutoQcVerdict);
        }

        [Fact]
        public async Task Records_unset_as_the_from_value_when_no_verdict_was_stored()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(autoQcVerdict: null), 1, "suspicious", null);

            fixture.Audit.Verify(
                x => x.InsertAsync(It.IsAny<int>(), It.IsAny<string>(), "unset", It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Records_the_previous_verdict_as_the_from_value()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(autoQcVerdict: "redo"), 2, "suspicious", null);

            fixture.Audit.Verify(
                x => x.InsertAsync(It.IsAny<int>(), It.IsAny<string>(), "redo", It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task An_escalation_from_redo_to_suspicious_updates_the_badge()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: "redo");

            await sut.ApplyAutoQcFlagForContextAsync(context, 2, "suspicious", null);

            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(100, "suspicious", 2, It.IsAny<string>()), Times.Once);
            Assert.Equal("suspicious", context.AutoQcVerdict);
        }

        [Fact]
        public async Task A_second_suspicious_flag_writes_the_audit_row_but_leaves_the_badge_alone()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: "suspicious");

            Assert.True(await sut.ApplyAutoQcFlagForContextAsync(context, 2, "suspicious", null));

            fixture.VerifyInsert(Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task A_de_escalation_from_suspicious_to_redo_leaves_the_badge_alone()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: "suspicious");

            Assert.True(await sut.ApplyAutoQcFlagForContextAsync(context, 2, "redo", null));

            fixture.VerifyInsert(Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
            Assert.Equal("suspicious", context.AutoQcVerdict);
        }

        [Fact]
        public async Task Escalation_matching_is_case_insensitive()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: "SUSPICIOUS");

            await sut.ApplyAutoQcFlagForContextAsync(context, 2, "suspicious", null);

            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Skip_if_already_flagged_blocks_a_badge_update_on_an_already_flagged_row()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: "redo");

            Assert.True(await sut.ApplyAutoQcFlagForContextAsync(
                context, 2, "suspicious", new AutoQcFlagOptions { SkipIfAlreadyFlagged = true }));

            fixture.VerifyInsert(Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
            Assert.Equal("redo", context.AutoQcVerdict);
        }

        [Fact]
        public async Task Skip_if_already_flagged_still_allows_the_first_badge_update()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var context = DmIvAutoQcFlagFixture.Context(autoQcVerdict: null);

            await sut.ApplyAutoQcFlagForContextAsync(
                context, 2, "suspicious", new AutoQcFlagOptions { SkipIfAlreadyFlagged = true });

            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(100, "suspicious", 2, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task A_null_options_object_falls_back_to_the_forward_trigger_defaults()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(DmIvAutoQcFlagFixture.Context(), 1, "suspicious", null);

            fixture.VerifyInsertWithSource(DmIvAutoQcAuditConstants.SourceForwardTrigger, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_blank_source_falls_back_to_the_forward_trigger(string source)
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious",
                new AutoQcFlagOptions { Source = source });

            fixture.VerifyInsertWithSource(DmIvAutoQcAuditConstants.SourceForwardTrigger, Times.Once());
        }

        [Fact]
        public async Task A_custom_source_is_recorded_on_both_the_audit_row_and_the_badge_update()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious",
                new AutoQcFlagOptions { Source = DmIvAutoQcAuditConstants.SourcePerIrfBackfill });

            fixture.VerifyInsertWithSource(DmIvAutoQcAuditConstants.SourcePerIrfBackfill, Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(
                    100, "suspicious", 1, DmIvAutoQcAuditConstants.SourcePerIrfBackfill),
                Times.Once);
        }

        [Fact]
        public async Task A_custom_tag_and_backfill_batch_id_are_carried_through()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var batchId = Guid.NewGuid();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious",
                new AutoQcFlagOptions
                {
                    Tag = DmIvAutoQcAuditConstants.RiskAuditTag,
                    BackfillBatchId = batchId,
                    ExtraMetadata = """{"note":"manual"}"""
                });

            fixture.Audit.Verify(
                x => x.InsertAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                    DmIvAutoQcAuditConstants.RiskAuditTag, It.IsAny<string>(), batchId,
                    """{"note":"manual"}"""),
                Times.Once);
        }

        [Fact]
        public async Task A_null_tag_falls_back_to_the_flag_tag()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "suspicious", new AutoQcFlagOptions { Tag = null });

            fixture.Audit.Verify(
                x => x.InsertAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                    DmIvAutoQcAuditConstants.FlagTag, It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task An_overridden_badge_verdict_and_interview_id_are_used_for_the_badge_only()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "redo",
                new AutoQcFlagOptions
                {
                    BadgeVerdict = DmIvAutoQcAuditConstants.VerdictSuspicious,
                    BadgeTriggeringInterviewId = 77
                });

            // Audit row records the raw verdict; the badge records the override.
            fixture.VerifyInsertWithVerdict("redo", Times.Once());
            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(100, DmIvAutoQcAuditConstants.VerdictSuspicious, 77, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task A_blank_badge_verdict_falls_back_to_the_raw_verdict()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(
                DmIvAutoQcFlagFixture.Context(), 1, "redo",
                new AutoQcFlagOptions { BadgeVerdict = "   " });

            fixture.Audit.Verify(
                x => x.UpdateAutoQcFieldsAsync(100, "redo", 1, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Writes_the_audit_row_before_updating_the_badge()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            var order = new List<string>();
            fixture.Audit.Setup(x => x.InsertAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .Returns(() => { order.Add("insert"); return Task.CompletedTask; });
            fixture.Audit.Setup(x => x.UpdateAutoQcFieldsAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(() => { order.Add("badge"); return Task.CompletedTask; });

            await sut.ApplyAutoQcFlagForContextAsync(DmIvAutoQcFlagFixture.Context(), 1, "suspicious", null);

            Assert.Equal(new[] { "insert", "badge" }, order);
        }

        [Fact]
        public async Task Honours_a_cancellation_request_before_doing_any_work()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.ApplyAutoQcFlagForContextAsync(
                    DmIvAutoQcFlagFixture.Context(), 1, "suspicious", null, cts.Token));

            fixture.VerifyInsert(Times.Never());
        }

        [Fact]
        public async Task Never_touches_active_or_vet_state_because_this_is_a_flag_only_path()
        {
            var fixture = new DmIvAutoQcFlagFixture();
            var sut = fixture.CreateSut();

            await sut.ApplyAutoQcFlagForContextAsync(DmIvAutoQcFlagFixture.Context(), 1, "suspicious", null);

            fixture.Audit.Verify(x => x.ClearAutoQcFieldsAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
