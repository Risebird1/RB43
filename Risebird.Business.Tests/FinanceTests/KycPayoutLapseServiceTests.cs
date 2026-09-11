using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.FinanceTests
{
    public sealed class KycPayoutLapseFixture
    {
        public Mock<IKycPayoutLapseRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);

        public KycPayoutLapseService CreateSut(int[] reminderOffsets = null)
        {
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            Repository.Setup(x => x.AddLapseTrackingAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            return new KycPayoutLapseService(Repository.Object, Logger.Object, reminderOffsets);
        }

        public void VerifyExceptionLogged(Times times) =>
            Logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), times);
    }

    public class KycPayoutLapseServiceReminderIndexTests
    {
        [Theory]
        // Default offsets: day 0, 5, 10, 15, 20.
        [InlineData(-1, -1)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(4, 0)]
        [InlineData(5, 1)]
        [InlineData(9, 1)]
        [InlineData(10, 2)]
        [InlineData(14, 2)]
        [InlineData(15, 3)]
        [InlineData(19, 3)]
        [InlineData(20, 4)]
        [InlineData(21, 4)]
        [InlineData(365, 4)]
        public void Resolves_the_due_reminder_index_from_days_elapsed(int daysElapsed, int expected)
        {
            var sut = new KycPayoutLapseFixture().CreateSut();

            Assert.Equal(expected, sut.GetDueReminderIndex(daysElapsed));
        }

        [Fact]
        public void Returns_minus_one_before_the_first_offset()
        {
            var sut = new KycPayoutLapseFixture().CreateSut(new[] { 3, 7 });

            Assert.Equal(-1, sut.GetDueReminderIndex(0));
            Assert.Equal(-1, sut.GetDueReminderIndex(2));
            Assert.Equal(0, sut.GetDueReminderIndex(3));
        }

        [Fact]
        public void Honours_custom_offsets()
        {
            var sut = new KycPayoutLapseFixture().CreateSut(new[] { 1, 2, 3 });

            Assert.Equal(-1, sut.GetDueReminderIndex(0));
            Assert.Equal(0, sut.GetDueReminderIndex(1));
            Assert.Equal(1, sut.GetDueReminderIndex(2));
            Assert.Equal(2, sut.GetDueReminderIndex(3));
            Assert.Equal(2, sut.GetDueReminderIndex(99));
        }

        [Fact]
        public void Caps_the_index_at_the_maximum_reminder_count_even_with_extra_offsets()
        {
            // Eight configured offsets, but never more than five reminders.
            var sut = new KycPayoutLapseFixture().CreateSut(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            Assert.Equal(sut.MaxReminderCount - 1, sut.GetDueReminderIndex(7));
            Assert.Equal(sut.MaxReminderCount - 1, sut.GetDueReminderIndex(999));
        }

        [Fact]
        public void The_reminder_cap_is_five() =>
            Assert.Equal(5, new KycPayoutLapseFixture().CreateSut().MaxReminderCount);

        [Fact]
        public void Index_never_exceeds_the_cap_for_any_days_elapsed()
        {
            var sut = new KycPayoutLapseFixture().CreateSut(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            for (var days = -5; days <= 400; days++)
                Assert.InRange(sut.GetDueReminderIndex(days), -1, sut.MaxReminderCount - 1);
        }

        [Fact]
        public void Index_is_monotonic_in_days_elapsed()
        {
            var sut = new KycPayoutLapseFixture().CreateSut();
            var previous = -2;

            for (var days = -1; days <= 40; days++)
            {
                var current = sut.GetDueReminderIndex(days);
                Assert.True(current >= previous, $"index went backwards at day {days}");
                previous = current;
            }
        }

        [Fact]
        public void An_empty_offset_array_never_yields_a_reminder()
        {
            var sut = new KycPayoutLapseFixture().CreateSut(Array.Empty<int>());

            Assert.Equal(-1, sut.GetDueReminderIndex(0));
            Assert.Equal(-1, sut.GetDueReminderIndex(1000));
        }
    }

    public class KycPayoutLapseServiceProcessLapseCandidatesTests
    {
        private static KycLapseCandidate Candidate(
            int userid = 9,
            string month = "2026-01",
            decimal pendingAmount = 1500.5m,
            bool isBankVerified = false,
            bool isPanVerified = false) => new()
            {
                Userid = userid,
                Email = "iv@risebird.io",
                FirstName = "Ada",
                LastName = "Lovelace",
                Month = month,
                PendingAmount = pendingAmount,
                IsBankVerified = isBankVerified,
                IsPanVerified = isPanVerified
            };

        [Fact]
        public async Task Returns_zero_when_the_repository_returns_null()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync((IList<KycLapseCandidate>)null);

            Assert.Equal(0, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
            fixture.Repository.Verify(
                x => x.AddLapseTrackingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Returns_zero_when_there_are_no_candidates()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate>());

            Assert.Equal(0, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
        }

        [Fact]
        public async Task Scans_with_todays_date_and_the_one_hundred_twenty_day_minimum_age()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate>());

            await sut.ProcessLapseCandidatesAsync("ops@risebird.io");

            fixture.Repository.Verify(
                x => x.FetchKycIncompletePayoutCandidatesAsync(DateTime.Today, 120), Times.Once);
        }

        [Fact]
        public async Task Counts_only_the_rows_the_repository_actually_inserted()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate>
                {
                    Candidate(userid: 1), Candidate(userid: 2), Candidate(userid: 3)
                });
            fixture.Repository
                .Setup(x => x.AddLapseTrackingAsync(2, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .ReturnsAsync(false);   // already tracked

            Assert.Equal(2, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
        }

        [Fact]
        public async Task Attempts_an_insert_for_every_candidate_even_when_some_are_duplicates()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate> { Candidate(1), Candidate(2) });
            fixture.Repository
                .Setup(x => x.AddLapseTrackingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            Assert.Equal(0, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
            fixture.Repository.Verify(
                x => x.AddLapseTrackingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Passes_the_operator_email_and_pending_amount_through_to_the_insert()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate> { Candidate(userid: 9, month: "2026-01", pendingAmount: 1500.5m) });

            await sut.ProcessLapseCandidatesAsync("ops@risebird.io");

            fixture.Repository.Verify(
                x => x.AddLapseTrackingAsync(9, "2026-01", It.IsAny<string>(), 1500.5m, "ops@risebird.io"),
                Times.Once);
        }

        [Theory]
        [InlineData(false, false, "Bank:Missing", "PAN:Missing")]
        [InlineData(true, false, "Bank:Verified", "PAN:Missing")]
        [InlineData(false, true, "Bank:Missing", "PAN:Verified")]
        [InlineData(true, true, "Bank:Verified", "PAN:Verified")]
        public async Task Remark_records_which_kyc_documents_are_missing(
            bool bankVerified, bool panVerified, string expectedBank, string expectedPan)
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            string remark = null;
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate>
                {
                    Candidate(isBankVerified: bankVerified, isPanVerified: panVerified)
                });
            fixture.Repository
                .Setup(x => x.AddLapseTrackingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .Returns((int _, string __, string r, decimal? ___, string ____) =>
                {
                    remark = r;
                    return Task.FromResult(true);
                });

            await sut.ProcessLapseCandidatesAsync("ops@risebird.io");

            Assert.Contains(expectedBank, remark);
            Assert.Contains(expectedPan, remark);
        }

        [Fact]
        public async Task Remark_formats_the_pending_amount_to_at_most_two_decimals()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            string remark = null;
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate> { Candidate(pendingAmount: 1500.456m, month: "2026-01") });
            fixture.Repository
                .Setup(x => x.AddLapseTrackingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .Returns((int _, string __, string r, decimal? ___, string ____) =>
                {
                    remark = r;
                    return Task.FromResult(true);
                });

            await sut.ProcessLapseCandidatesAsync("ops@risebird.io");

            Assert.StartsWith("1500.46 points pending since 2026-01;", remark);
            Assert.Contains("KYC incomplete", remark);
        }

        [Fact]
        public async Task Swallows_a_repository_failure_logs_it_and_returns_zero()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task A_failure_part_way_through_abandons_the_run_and_returns_zero()
        {
            // The loop is not individually try/caught, so one bad insert aborts the batch.
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate> { Candidate(1), Candidate(2) });
            fixture.Repository
                .Setup(x => x.AddLapseTrackingAsync(2, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal?>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("deadlock"));

            Assert.Equal(0, await sut.ProcessLapseCandidatesAsync("ops@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Tolerates_a_null_operator_email()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.FetchKycIncompletePayoutCandidatesAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KycLapseCandidate> { Candidate() });

            Assert.Equal(1, await sut.ProcessLapseCandidatesAsync(null));
        }
    }

    public class KycPayoutLapseServiceReadAndWriteTests
    {
        [Fact]
        public async Task Reminder_targets_are_passed_straight_through()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            var asOf = new DateTime(2026, 09, 09);
            var targets = new List<KycLapseReminderTarget> { new() { Id = 1, Userid = 9 } };
            fixture.Repository.Setup(x => x.FetchReminderTargetsAsync(asOf)).ReturnsAsync(targets);

            var result = await sut.GetReminderTargetsAsync(asOf);

            Assert.Same(targets, result);
        }

        [Fact]
        public async Task Reminder_targets_return_null_and_log_on_failure()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.FetchReminderTargetsAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetReminderTargetsAsync(DateTime.Today));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Mark_completed_sets_the_completed_status()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();

            await sut.MarkCompletedAsync(7);

            fixture.Repository.Verify(x => x.UpdateTrackingStatusAsync(7, "Completed"), Times.Once);
        }

        [Fact]
        public async Task Mark_completed_swallows_and_logs_a_failure()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdateTrackingStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            var exception = await Record.ExceptionAsync(() => sut.MarkCompletedAsync(7));

            Assert.Null(exception);
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Mark_reminder_sent_forwards_the_new_count()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();

            await sut.MarkReminderSentAsync(7, 3);

            fixture.Repository.Verify(x => x.MarkReminderSentAsync(7, 3), Times.Once);
        }

        [Fact]
        public async Task Mark_reminder_sent_swallows_and_logs_a_failure()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.MarkReminderSentAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await Record.ExceptionAsync(() => sut.MarkReminderSentAsync(7, 3)));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Lapse_writes_off_the_points_before_marking_the_tracking_row()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            var order = new List<string>();
            fixture.Repository.Setup(x => x.LapseInterviewPointsAsync(9, "2026-01"))
                .Returns(() => { order.Add("points"); return Task.CompletedTask; });
            fixture.Repository.Setup(x => x.UpdateTrackingStatusAsync(7, "Lapsed"))
                .Returns(() => { order.Add("status"); return Task.CompletedTask; });

            await sut.LapseAsync(7, 9, "2026-01");

            Assert.Equal(new[] { "points", "status" }, order);
        }

        [Fact]
        public async Task Lapse_does_not_mark_the_row_lapsed_when_writing_off_the_points_fails()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.LapseInterviewPointsAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await Record.ExceptionAsync(() => sut.LapseAsync(7, 9, "2026-01")));

            fixture.Repository.Verify(
                x => x.UpdateTrackingStatusAsync(It.IsAny<int>(), "Lapsed"), Times.Never);
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Tracking_list_is_passed_straight_through()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            var rows = new List<KycPayoutLapseTrackingRow> { new() { Id = 1 } };
            fixture.Repository.Setup(x => x.FetchTrackingListAsync()).ReturnsAsync(rows);

            Assert.Same(rows, await sut.GetTrackingListAsync());
        }

        [Fact]
        public async Task Tracking_list_returns_null_and_logs_on_failure()
        {
            var fixture = new KycPayoutLapseFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.FetchTrackingListAsync())
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetTrackingListAsync());
            fixture.VerifyExceptionLogged(Times.Once());
        }
    }
}
