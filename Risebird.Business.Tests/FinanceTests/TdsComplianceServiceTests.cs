using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.FinanceTests
{
    public sealed class TdsComplianceFixture
    {
        public Mock<ITdsRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);

        public TdsComplianceService CreateSut(int[] reminderOffsets = null)
        {
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            return new TdsComplianceService(Repository.Object, Logger.Object, reminderOffsets);
        }

        public void VerifyExceptionLogged(Times times) =>
            Logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), times);
    }

    public class TdsComplianceServiceResolveQuarterDatesTests
    {
        private static TdsComplianceService Sut() => new TdsComplianceFixture().CreateSut();

        [Fact]
        public void Quarter_one_runs_april_to_june_of_the_first_fy_year()
        {
            var (start, end) = Sut().ResolveQuarterDates("2026-2027", "Quarter 1");

            Assert.Equal(new DateTime(2026, 04, 01), start);
            Assert.Equal(new DateTime(2026, 06, 30), end);
        }

        [Fact]
        public void Quarter_two_runs_july_to_september_of_the_first_fy_year()
        {
            var (start, end) = Sut().ResolveQuarterDates("2026-2027", "Quarter 2");

            Assert.Equal(new DateTime(2026, 07, 01), start);
            Assert.Equal(new DateTime(2026, 09, 30), end);
        }

        [Fact]
        public void Quarter_three_runs_october_to_december_of_the_first_fy_year()
        {
            var (start, end) = Sut().ResolveQuarterDates("2026-2027", "Quarter 3");

            Assert.Equal(new DateTime(2026, 10, 01), start);
            Assert.Equal(new DateTime(2026, 12, 31), end);
        }

        [Fact]
        public void Quarter_four_crosses_into_the_second_fy_year()
        {
            var (start, end) = Sut().ResolveQuarterDates("2026-2027", "Quarter 4");

            Assert.Equal(new DateTime(2027, 01, 01), start);
            Assert.Equal(new DateTime(2027, 03, 31), end);
        }

        [Fact]
        public void The_four_quarters_tile_the_financial_year_without_gaps_or_overlaps()
        {
            var sut = Sut();
            var quarters = new[] { "Quarter 1", "Quarter 2", "Quarter 3", "Quarter 4" }
                .Select(q => sut.ResolveQuarterDates("2026-2027", q))
                .ToList();

            Assert.Equal(new DateTime(2026, 04, 01), quarters[0].Start);
            Assert.Equal(new DateTime(2027, 03, 31), quarters[3].End);

            for (var i = 1; i < quarters.Count; i++)
                Assert.Equal(quarters[i - 1].End.AddDays(1), quarters[i].Start);
        }

        [Fact]
        public void Quarter_four_of_a_leap_financial_year_still_ends_on_the_thirty_first_of_march()
        {
            var (start, end) = Sut().ResolveQuarterDates("2023-2024", "Quarter 4");

            Assert.Equal(new DateTime(2024, 01, 01), start);
            Assert.Equal(new DateTime(2024, 03, 31), end);
        }

        [Fact]
        public void Only_the_leading_year_of_the_fy_string_is_used()
        {
            // "2026-2027" and "2026-9999" resolve identically — the second half is ignored.
            var sut = Sut();

            Assert.Equal(
                sut.ResolveQuarterDates("2026-2027", "Quarter 1"),
                sut.ResolveQuarterDates("2026-9999", "Quarter 1"));
        }

        [Fact]
        public void A_bare_year_without_a_dash_is_accepted()
        {
            var (start, _) = Sut().ResolveQuarterDates("2026", "Quarter 1");

            Assert.Equal(new DateTime(2026, 04, 01), start);
        }

        [Theory]
        [InlineData("Quarter 5")]
        [InlineData("Q1")]
        [InlineData("quarter 1")]
        [InlineData("")]
        [InlineData(null)]
        public void An_unknown_quarter_name_is_rejected(string quarter) =>
            Assert.Throws<ArgumentException>(() => Sut().ResolveQuarterDates("2026-2027", quarter));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-year")]
        [InlineData("FY2026")]
        public void A_malformed_financial_year_throws(string financialYear) =>
            Assert.ThrowsAny<Exception>(() => Sut().ResolveQuarterDates(financialYear, "Quarter 1"));
    }

    public class TdsComplianceServiceBuildSelectListsTests
    {
        private static TdsComplianceService Sut() => new TdsComplianceFixture().CreateSut();

        private static int CurrentFinancialYearStart() =>
            DateTime.Today.Month >= 4 ? DateTime.Today.Year : DateTime.Today.Year - 1;

        [Fact]
        public void Offers_six_financial_years()
        {
            var (years, _) = Sut().BuildSelectLists(null, null);

            Assert.Equal(6, years.Count);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Financial_years_span_the_current_year_and_the_five_before_it()
        {
            var current = CurrentFinancialYearStart();

            var (years, _) = Sut().BuildSelectLists(null, null);

            Assert.Equal($"{current - 5}-{current - 4}", years[0].Value);
            Assert.Equal($"{current}-{current + 1}", years[^1].Value);
        }

        [Fact]
        public void Financial_year_text_is_prefixed_with_fy()
        {
            var (years, _) = Sut().BuildSelectLists(null, null);

            Assert.All(years, y => Assert.StartsWith("FY ", y.Text));
        }

        [Fact]
        public void Financial_year_values_are_distinct_and_ascending()
        {
            var (years, _) = Sut().BuildSelectLists(null, null);
            var values = years.Select(y => y.Value).ToList();

            Assert.Equal(values.Count, values.Distinct().Count());
            Assert.Equal(values.OrderBy(v => v, StringComparer.Ordinal), values);
        }

        [Fact]
        public void Marks_the_matching_financial_year_as_selected()
        {
            var current = CurrentFinancialYearStart();
            var selected = $"{current}-{current + 1}";

            var (years, _) = Sut().BuildSelectLists(selected, null);

            Assert.Single(years, y => y.Selected);
            Assert.Equal(selected, years.Single(y => y.Selected).Value);
        }

        [Fact]
        public void Nothing_is_selected_when_the_year_is_not_in_range()
        {
            var (years, _) = Sut().BuildSelectLists("1999-2000", null);

            Assert.DoesNotContain(years, y => y.Selected);
        }

        [Fact]
        public void Nothing_is_selected_for_a_null_or_blank_year()
        {
            foreach (var selected in new[] { null, "", "   " })
            {
                var (years, _) = Sut().BuildSelectLists(selected, null);
                Assert.DoesNotContain(years, y => y.Selected);
            }
        }

        [Fact]
        public void Offers_the_four_quarters_in_order()
        {
            var (_, quarters) = Sut().BuildSelectLists(null, null);

            Assert.Equal(
                new[] { "Quarter 1", "Quarter 2", "Quarter 3", "Quarter 4" },
                quarters.Select(q => q.Value));
        }

        [Fact]
        public void Quarter_text_matches_its_value()
        {
            var (_, quarters) = Sut().BuildSelectLists(null, null);

            Assert.All(quarters, q => Assert.Equal(q.Value, q.Text));
        }

        [Fact]
        public void Marks_the_matching_quarter_as_selected()
        {
            var (_, quarters) = Sut().BuildSelectLists(null, "Quarter 3");

            Assert.Single(quarters, q => q.Selected);
            Assert.Equal("Quarter 3", quarters.Single(q => q.Selected).Value);
        }

        [Theory]
        [InlineData("quarter 3")]
        [InlineData("Q3")]
        [InlineData(null)]
        [InlineData("")]
        public void Quarter_selection_is_an_exact_match(string selected)
        {
            var (_, quarters) = Sut().BuildSelectLists(null, selected);

            Assert.DoesNotContain(quarters, q => q.Selected);
        }

        [Fact]
        public void Every_offered_financial_year_and_quarter_pair_resolves_to_real_dates()
        {
            var sut = Sut();
            var (years, quarters) = sut.BuildSelectLists(null, null);

            foreach (var year in years)
                foreach (var quarter in quarters)
                {
                    var (start, end) = sut.ResolveQuarterDates(year.Value, quarter.Value);
                    Assert.True(start < end);
                }
        }
    }

    public class TdsComplianceServiceReminderTests
    {
        [Theory]
        // Default offsets: 3, 7, 15, 30, 45, 60, 75, 82, 87, 89.
        [InlineData(0, -1)]
        [InlineData(2, -1)]
        [InlineData(3, 0)]
        [InlineData(6, 0)]
        [InlineData(7, 1)]
        [InlineData(14, 1)]
        [InlineData(15, 2)]
        [InlineData(29, 2)]
        [InlineData(30, 3)]
        [InlineData(45, 4)]
        [InlineData(60, 5)]
        [InlineData(75, 6)]
        [InlineData(82, 7)]
        [InlineData(87, 8)]
        [InlineData(89, 9)]
        [InlineData(200, 9)]
        public void Resolves_the_due_reminder_index_from_days_elapsed(int daysElapsed, int expected)
        {
            var sut = new TdsComplianceFixture().CreateSut();

            Assert.Equal(expected, sut.GetDueReminderIndex(daysElapsed));
        }

        [Fact]
        public void A_negative_days_elapsed_yields_no_reminder() =>
            Assert.Equal(-1, new TdsComplianceFixture().CreateSut().GetDueReminderIndex(-10));

        [Fact]
        public void There_is_no_hard_cap_here_unlike_the_kyc_lapse_service()
        {
            // TDS uses all ten configured offsets; pinned so the two services do not drift.
            var sut = new TdsComplianceFixture().CreateSut();

            Assert.Equal(9, sut.GetDueReminderIndex(1000));
        }

        [Fact]
        public void Honours_custom_offsets()
        {
            var sut = new TdsComplianceFixture().CreateSut(new[] { 1, 5 });

            Assert.Equal(-1, sut.GetDueReminderIndex(0));
            Assert.Equal(0, sut.GetDueReminderIndex(1));
            Assert.Equal(0, sut.GetDueReminderIndex(4));
            Assert.Equal(1, sut.GetDueReminderIndex(5));
            Assert.Equal(1, sut.GetDueReminderIndex(500));
        }

        [Fact]
        public void An_empty_offset_array_never_yields_a_reminder()
        {
            var sut = new TdsComplianceFixture().CreateSut(Array.Empty<int>());

            Assert.Equal(-1, sut.GetDueReminderIndex(0));
            Assert.Equal(-1, sut.GetDueReminderIndex(1000));
        }

        [Fact]
        public void Index_is_monotonic_in_days_elapsed()
        {
            var sut = new TdsComplianceFixture().CreateSut();
            var previous = -2;

            for (var days = -1; days <= 120; days++)
            {
                var current = sut.GetDueReminderIndex(days);
                Assert.True(current >= previous, $"index went backwards at day {days}");
                previous = current;
            }
        }

        [Fact]
        public async Task Reminder_targets_are_passed_straight_through()
        {
            var fixture = new TdsComplianceFixture();
            var sut = fixture.CreateSut();
            var asOf = new DateTime(2026, 09, 09);
            var targets = new List<UnverifiedWalletInterviewer>();
            fixture.Repository.Setup(x => x.FetchUnverifiedWalletInterviewersAsync(asOf)).ReturnsAsync(targets);

            Assert.Same(targets, await sut.GetReminderTargetsAsync(asOf));
        }

        [Fact]
        public async Task Reminder_targets_return_null_and_log_on_failure()
        {
            var fixture = new TdsComplianceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.FetchUnverifiedWalletInterviewersAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetReminderTargetsAsync(DateTime.Today));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Mark_reminder_sent_forwards_every_argument_and_returns_the_repository_result()
        {
            var fixture = new TdsComplianceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.LogLapseReminderAsync(9, "PAY-1", 2, "iv@risebird.io"))
                .ReturnsAsync(true);

            Assert.True(await sut.MarkReminderSentAsync(9, "PAY-1", 2, "iv@risebird.io"));
        }

        [Fact]
        public async Task Mark_reminder_sent_returns_false_for_a_duplicate()
        {
            var fixture = new TdsComplianceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.LogLapseReminderAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            Assert.False(await sut.MarkReminderSentAsync(9, "PAY-1", 2, "iv@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Never());
        }

        [Fact]
        public async Task Mark_reminder_sent_returns_false_and_logs_on_failure()
        {
            var fixture = new TdsComplianceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository
                .Setup(x => x.LogLapseReminderAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.MarkReminderSentAsync(9, "PAY-1", 2, "iv@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }
    }
}
