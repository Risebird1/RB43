using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.Business;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.FinanceTests
{
    /// <summary>
    /// The Interview Profitability grid decides which clients and interview types make money, so the
    /// allocation rules are pinned here: revenue mirrors invoicing, shared costs divide by every held
    /// interview (not just the filtered ones), cancelled interviews carry only their own costs, and
    /// nothing is silently dropped — what no interview absorbs is reported as idle-day cost.
    /// </summary>
    public class InterviewProfitabilityCalculatorTests
    {
        private static readonly DateTime Day1 = new(2026, 10, 1);
        private static readonly DateTime Day2 = new(2026, 10, 2);
        private static readonly InterviewProfitabilitySettings Settings = new() { UsdToInr = 80m, PlivoCXCostPerMinuteINR = 2m };

        private static InterviewProfitabilityMeetingRow Meeting(int id, DateTime? date = null, Action<InterviewProfitabilityMeetingRow> change = null)
        {
            var row = new InterviewProfitabilityMeetingRow
            {
                MeetingId = id,
                InterviewDate = (date ?? Day1).AddHours(11),
                IRFCode = "IRF-1",
                IRFIntId = 1,
                ClientId = 10,
                ClientName = "Acme",
                InterviewerEmail = "iv@x.com",
                InterviewerName = "Iv",
                MeetingStatus = 4,
                UnitRate = 1500m,
                BillingFactor = 1m,
                RevenueCurrency = "INR"
            };
            change?.Invoke(row);
            return row;
        }

        private static OperationalCostFactor Factor(int id, decimal amount, string basis, string currency = "INR",
            DateTime? from = null, DateTime? to = null) => new()
        {
            Id = id,
            Name = $"F{id}",
            Category = "Cloud",
            Amount = amount,
            Basis = basis,
            Currency = currency,
            EffectiveFrom = from ?? new DateTime(2026, 1, 1),
            EffectiveTo = to,
            IsActive = true
        };

        private static InterviewProfitabilityReport Build(InterviewProfitabilityRawData raw, InterviewProfitabilityFilter filter = null,
            DateTime? start = null, DateTime? end = null) =>
            InterviewProfitabilityCalculator.Build(raw, filter ?? new InterviewProfitabilityFilter(), Settings, start ?? Day1, end ?? Day1);

        private static InterviewProfitabilityLine Only(InterviewProfitabilityReport report) => Assert.Single(report.Lines);

        [Fact]
        public void A_billable_interview_earns_its_rate()
        {
            var report = Build(new InterviewProfitabilityRawData { Meetings = { Meeting(1) } });

            Assert.Equal(1500m, Only(report).Revenue);
            Assert.True(Only(report).IsBillable);
        }

        [Fact]
        public void Partial_billing_earns_its_fraction_of_the_rate()
        {
            var report = Build(new InterviewProfitabilityRawData { Meetings = { Meeting(1, change: m => m.BillingFactor = 0.30m) } });

            Assert.Equal(450m, Only(report).Revenue);
        }

        [Fact]
        public void A_non_billable_interview_earns_nothing_but_still_costs()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.BillingFactor = 0; m.InterviewerPayout = 700; }) }
            });

            Assert.Equal(0m, Only(report).Revenue);
            Assert.Equal(-700m, Only(report).Margin);
            Assert.False(Only(report).IsBillable);
        }

        [Fact]
        public void A_dollar_billed_client_is_converted_at_the_rate()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.UnitRate = 20m; m.RevenueCurrency = "USD"; }) }
            });

            Assert.Equal(1600m, Only(report).Revenue);
        }

        [Fact]
        public void A_currency_without_a_rate_is_shown_unconverted_and_flagged()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.UnitRate = 20m; m.RevenueCurrency = "EUR"; }) }
            });

            Assert.Equal(20m, Only(report).Revenue);
            Assert.NotNull(Only(report).Note);
            Assert.Contains(report.Warnings, w => w.Contains("EUR"));
        }

        [Fact]
        public void Direct_dollar_costs_are_converted_and_rupee_costs_are_not()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.AIDirectUSD = 0.5m; m.VapiUSD = 0.25m; m.PlivoCallCost = 0.25m; m.RecordingUSD = 0.1m; m.RecordingINR = 3m; }) }
            });

            Assert.Equal(40m, Only(report).AIDirect);
            Assert.Equal(20m + 20m + 8m + 3m, Only(report).VoiceDirect);
        }

        [Fact]
        public void A_per_day_cost_is_divided_by_every_interview_held_that_day_not_just_the_shown_ones()
        {
            // Four interviews were held on the day across all clients; the grid shows one of them.
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 4 } },
                CostFactors = { Factor(7, 20000m, OperationalCostFactor.PerDay) }
            });

            Assert.Equal(5000m, Only(report).Factors[7]);
        }

        [Fact]
        public void A_per_month_cost_is_spread_over_the_days_of_its_month()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 2 } },
                CostFactors = { Factor(7, 31000m, OperationalCostFactor.PerMonth) }
            });

            Assert.Equal(500m, Only(report).Factors[7]);
        }

        [Fact]
        public void A_dollar_running_cost_is_converted()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 1 } },
                CostFactors = { Factor(7, 10m, OperationalCostFactor.PerDay, "USD") }
            });

            Assert.Equal(800m, Only(report).Factors[7]);
        }

        [Fact]
        public void Per_interview_costs_go_only_to_the_interview_types_they_name()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => m.IsAIInterview = true), Meeting(2) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 2 } },
                CostFactors =
                {
                    Factor(1, 10m, OperationalCostFactor.PerInterview),
                    Factor(2, 30m, OperationalCostFactor.PerAIInterview),
                    Factor(3, 50m, OperationalCostFactor.PerHumanInterview)
                }
            });

            var ai = report.Lines.Single(l => l.MeetingId == 1);
            var human = report.Lines.Single(l => l.MeetingId == 2);
            Assert.Equal(10m + 30m, ai.OverheadTotal);
            Assert.Equal(10m + 50m, human.OverheadTotal);
        }

        [Fact]
        public void A_cost_is_not_charged_outside_its_effective_dates()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 1 } },
                CostFactors =
                {
                    Factor(1, 100m, OperationalCostFactor.PerDay, from: Day2),
                    Factor(2, 100m, OperationalCostFactor.PerDay, to: Day1.AddDays(-1)),
                    Factor(3, 100m, OperationalCostFactor.PerDay, from: Day1, to: Day1)
                }
            });

            Assert.Equal(new[] { 3 }, Only(report).Factors.Keys.ToArray());
        }

        [Fact]
        public void An_inactive_cost_is_never_charged()
        {
            var factor = Factor(1, 100m, OperationalCostFactor.PerInterview);
            factor.IsActive = false;

            var report = Build(new InterviewProfitabilityRawData { Meetings = { Meeting(1) }, CostFactors = { factor } });

            Assert.Empty(Only(report).Factors);
        }

        [Fact]
        public void IRF_level_costs_are_shared_across_the_IRFs_held_interviews()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                IrfPools =
                {
                    new InterviewProfitabilityIrfPoolRow
                    {
                        IRFIntId = 1, AIUnattributedUSD = 10m, VapiUnattributedUSD = 1m, PlivoUnattributedCost = 1m,
                        PlivoCXSeconds = 600, HeldMeetings = 5
                    }
                }
            });

            Assert.Equal(10m * 80m / 5, Only(report).AIIrfShare);
            Assert.Equal((80m + 80m + 10 * 2m) / 5, Only(report).VoiceIrfShare);
        }

        [Fact]
        public void Unattributed_AI_is_shared_across_the_days_held_interviews()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 4, SharedAIUSD = 2m } }
            });

            Assert.Equal(40m, Only(report).AISharedShare);
        }

        [Fact]
        public void A_cancelled_interview_carries_its_own_costs_but_no_shared_ones()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.IsCancelled = true; m.InterviewerPayout = 100; m.AIDirectUSD = 1m; m.BillingFactor = 0; }) },
                IrfPools = { new InterviewProfitabilityIrfPoolRow { IRFIntId = 1, AIUnattributedUSD = 10m, HeldMeetings = 2 } },
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 2, SharedAIUSD = 1m } },
                CostFactors = { Factor(1, 1000m, OperationalCostFactor.PerDay), Factor(2, 10m, OperationalCostFactor.PerInterview) }
            }, new InterviewProfitabilityFilter { IncludeCancelled = true });

            var line = Only(report);
            Assert.Equal(100m + 80m, line.TotalCost);
            Assert.Equal("Cancelled", line.Status);
        }

        [Fact]
        public void Running_cost_on_a_day_with_no_interview_is_reported_as_idle_cost()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, Day1) },
                Days =
                {
                    new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 1 },
                    new InterviewProfitabilityDayRow { Day = Day2, HeldMeetings = 0, SharedAIUSD = 1m }
                },
                CostFactors = { Factor(1, 20000m, OperationalCostFactor.PerDay) }
            }, start: Day1, end: Day2);

            Assert.Equal(20000m + 80m, report.UnabsorbedCost);
            Assert.Equal(20000m, Only(report).Factors[1]);
        }

        [Fact]
        public void When_every_held_interview_is_shown_a_daily_cost_is_fully_allocated()
        {
            var meetings = Enumerable.Range(1, 3).Select(i => Meeting(i)).ToList();
            var raw = new InterviewProfitabilityRawData
            {
                Days = { new InterviewProfitabilityDayRow { Day = Day1, HeldMeetings = 3 } },
                CostFactors = { Factor(1, 20000m, OperationalCostFactor.PerDay) }
            };
            raw.Meetings.AddRange(meetings);

            var report = Build(raw);

            Assert.Equal(20000m, Math.Round(report.Totals.Factors[1], 6));
            Assert.Equal(0m, report.UnabsorbedCost);
        }

        [Fact]
        public void The_loss_filter_keeps_only_loss_making_interviews()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => m.InterviewerPayout = 2000), Meeting(2, change: m => m.InterviewerPayout = 500) }
            }, new InterviewProfitabilityFilter { Margin = "Loss" });

            Assert.Equal(1, Only(report).MeetingId);
            Assert.Equal(1, report.LossMakingCount);
        }

        [Fact]
        public void The_billable_filter_drops_non_billable_interviews()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1), Meeting(2, change: m => m.BillingFactor = 0) }
            }, new InterviewProfitabilityFilter { Billability = "Billable" });

            Assert.Equal(1, Only(report).MeetingId);
        }

        [Fact]
        public void Grouping_by_client_sums_its_interviews_and_the_totals_match()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings =
                {
                    Meeting(1, change: m => m.InterviewerPayout = 1000),
                    Meeting(2, change: m => m.InterviewerPayout = 600),
                    Meeting(3, change: m => { m.ClientName = "Beta"; m.InterviewerPayout = 2000; })
                }
            }, new InterviewProfitabilityFilter { GroupBy = "Client" });

            Assert.Equal(2, report.Lines.Count);
            var acme = report.Lines.Single(l => l.Label == "Acme");
            Assert.Equal(2, acme.Interviews);
            Assert.Equal(3000m, acme.Revenue);
            Assert.Equal(1400m, acme.Margin);
            Assert.Equal(report.Lines.Sum(l => l.Margin), report.Totals.Margin);
            Assert.Equal("Beta", report.Lines[0].Label); // worst margin first
        }

        [Fact]
        public void Margin_percent_is_empty_without_revenue()
        {
            var report = Build(new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1, change: m => { m.BillingFactor = 0; m.InterviewerPayout = 10; }) }
            });

            Assert.Null(Only(report).MarginPercent);
        }

        [Fact]
        public void Uncosted_phone_screen_minutes_are_flagged()
        {
            var raw = new InterviewProfitabilityRawData
            {
                Meetings = { Meeting(1) },
                IrfPools = { new InterviewProfitabilityIrfPoolRow { IRFIntId = 1, PlivoCXSeconds = 1200, HeldMeetings = 1 } }
            };

            var report = InterviewProfitabilityCalculator.Build(raw, new InterviewProfitabilityFilter(),
                new InterviewProfitabilitySettings { UsdToInr = 80m }, Day1, Day1);

            Assert.Contains(report.Warnings, w => w.Contains("20 minutes"));
        }
    }

    public class InterviewProfitabilityServiceTests
    {
        private readonly Mock<IInterviewProfitabilityRepository> _repository = new(MockBehavior.Loose);

        private InterviewProfitabilityService Sut(Dictionary<string, string> settings = null) =>
            new(_repository.Object, new ConfigurationBuilder().AddInMemoryCollection(settings ?? new()).Build());

        [Fact]
        public async Task A_range_longer_than_the_limit_is_shortened_and_says_so()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null, null, null, false))
                .ReturnsAsync(new InterviewProfitabilityRawData());

            var report = await Sut(new() { ["InterviewProfitability:MaxRangeDays"] = "10" }).GetReportAsync(
                new InterviewProfitabilityFilter { StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31) });

            Assert.Equal(new DateTime(2026, 3, 22), report.StartDate);
            Assert.Contains(report.Warnings, w => w.Contains("10 days"));
        }

        [Fact]
        public async Task The_interview_type_filter_reaches_the_query()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null, null, true, false))
                .ReturnsAsync(new InterviewProfitabilityRawData());

            await Sut().GetReportAsync(new InterviewProfitabilityFilter { InterviewType = "AI" });

            _repository.Verify(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, null, null, true, false), Times.Once);
        }

        [Fact]
        public async Task A_failed_query_is_reported_rather_than_shown_as_an_empty_grid()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>()))
                .ReturnsAsync((InterviewProfitabilityRawData)null);

            var report = await Sut().GetReportAsync(new InterviewProfitabilityFilter());

            Assert.NotNull(report.Error);
        }

        [Fact]
        public async Task Call_cost_tables_missing_from_the_database_are_named_in_a_warning()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>()))
                .ReturnsAsync(new InterviewProfitabilityRawData { MissingCostTables = new() { "PlivoCallLogs" } });

            var report = await Sut().GetReportAsync(new InterviewProfitabilityFilter());

            Assert.Null(report.Error);
            Assert.Contains(report.Warnings, w => w.Contains("Plivo scheduling call") && w.Contains("PlivoCallLogs"));
        }

        [Fact]
        public async Task The_reason_a_query_failed_is_shown_on_the_page()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>()))
                .ReturnsAsync(new InterviewProfitabilityRawData { LoadError = "The Profitability report is not installed on this database yet." });

            var report = await Sut().GetReportAsync(new InterviewProfitabilityFilter());

            Assert.Equal("The Profitability report is not installed on this database yet.", report.Error);
            Assert.Empty(report.Lines);
        }

        [Fact]
        public async Task The_page_rate_overrides_the_configured_rate()
        {
            _repository.Setup(r => r.GetRawDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>()))
                .ReturnsAsync(new InterviewProfitabilityRawData());

            var report = await Sut(new() { ["InterviewProfitability:UsdToInr"] = "85" })
                .GetReportAsync(new InterviewProfitabilityFilter { UsdToInr = 90m });

            Assert.Equal(90m, report.Settings.UsdToInr);
        }

        [Theory]
        [InlineData("", "Cloud", 1, "INR", "PerDay")]
        [InlineData("Azure", "", 1, "INR", "PerDay")]
        [InlineData("Azure", "Cloud", -1, "INR", "PerDay")]
        [InlineData("Azure", "Cloud", 1, "EUR", "PerDay")]
        [InlineData("Azure", "Cloud", 1, "INR", "PerYear")]
        public void Invalid_cost_factors_are_rejected(string name, string category, decimal amount, string currency, string basis)
        {
            Assert.NotNull(InterviewProfitabilityService.Validate(new OperationalCostFactor
            {
                Name = name, Category = category, Amount = amount, Currency = currency, Basis = basis,
                EffectiveFrom = new DateTime(2026, 4, 1)
            }));
        }

        [Fact]
        public void An_end_date_before_the_start_date_is_rejected()
        {
            Assert.NotNull(InterviewProfitabilityService.Validate(new OperationalCostFactor
            {
                Name = "Azure", Category = "Cloud", Amount = 1, Currency = "INR", Basis = "PerDay",
                EffectiveFrom = new DateTime(2026, 4, 1), EffectiveTo = new DateTime(2026, 3, 31)
            }));
        }

        [Fact]
        public void A_valid_cost_factor_passes()
        {
            Assert.Null(InterviewProfitabilityService.Validate(new OperationalCostFactor
            {
                Name = "SendGrid email", Category = "Email", Amount = 3000, Currency = "INR", Basis = "PerDay",
                EffectiveFrom = new DateTime(2026, 4, 1)
            }));
        }
    }
}
