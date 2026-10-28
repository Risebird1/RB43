using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewProfitabilityService : IInterviewProfitabilityService
    {
        public const string SettingsSection = "InterviewProfitability";

        private readonly IInterviewProfitabilityRepository _repository;
        private readonly IConfiguration _configuration;

        public InterviewProfitabilityService(IInterviewProfitabilityRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public InterviewProfitabilitySettings GetSettings()
        {
            var defaults = new InterviewProfitabilitySettings();
            return new InterviewProfitabilitySettings
            {
                UsdToInr = Positive(_configuration.GetValue<decimal?>($"{SettingsSection}:UsdToInr")) ?? defaults.UsdToInr,
                PlivoCallCostCurrency = string.Equals(_configuration[$"{SettingsSection}:PlivoCallCostCurrency"], "INR", StringComparison.OrdinalIgnoreCase)
                    ? "INR" : "USD",
                PlivoCXCostPerMinuteINR = Math.Max(0, _configuration.GetValue<decimal?>($"{SettingsSection}:PlivoCXCostPerMinuteINR") ?? 0),
                MaxRangeDays = _configuration.GetValue<int?>($"{SettingsSection}:MaxRangeDays") is int days && days > 0
                    ? days : defaults.MaxRangeDays
            };
        }

        public async Task<InterviewProfitabilityReport> GetReportAsync(InterviewProfitabilityFilter filter)
        {
            filter ??= new InterviewProfitabilityFilter();
            var settings = GetSettings();
            if (Positive(filter.UsdToInr) is decimal rate)
                settings.UsdToInr = rate;

            var warnings = new List<string>();
            var end = (filter.EndDate ?? DateTime.Today).Date;
            var start = (filter.StartDate ?? end.AddDays(-29)).Date;
            if (start > end)
                (start, end) = (end, start);
            if ((end - start).TotalDays + 1 > settings.MaxRangeDays)
            {
                start = end.AddDays(-(settings.MaxRangeDays - 1));
                warnings.Add($"The date range was shortened to the last {settings.MaxRangeDays} days ending {end:dd MMM yyyy} to keep the report fast.");
            }
            filter.StartDate = start;
            filter.EndDate = end;

            bool? isAI = filter.InterviewType switch
            {
                "AI" => true,
                "Human" => false,
                _ => null
            };

            var raw = await _repository.GetRawDataAsync(start, end, filter.ClientId, filter.IRFIntId,
                filter.InterviewerEmail, isAI, filter.IncludeCancelled);

            if (raw == null || raw.LoadError != null)
            {
                return new InterviewProfitabilityReport
                {
                    Filter = filter,
                    Settings = settings,
                    StartDate = start,
                    EndDate = end,
                    Error = raw?.LoadError
                        ?? "The report could not be loaded. The error has been logged; please try a shorter date range or try again later."
                };
            }

            var report = InterviewProfitabilityCalculator.Build(raw, filter, settings, start, end);
            foreach (var table in raw.MissingCostTables ?? new List<string>())
                warnings.Add($"{CostSourceName(table)} costs are not included: the {table} table does not exist on this database.");
            report.Warnings.InsertRange(0, warnings);
            return report;
        }

        private static string CostSourceName(string table) => table switch
        {
            "VapiCallLogs" => "Vapi call",
            "PlivoCallLogs" => "Plivo scheduling call",
            "PlivoCXCallLogs" => "Phone-screen campaign",
            "CallRecordingData" => "Call recording",
            _ => table
        };

        public Task<InterviewProfitabilityFilterOptions> GetFilterOptionsAsync(int? clientId) =>
            _repository.GetFilterOptionsAsync(clientId);

        public Task<List<OperationalCostFactor>> GetCostFactorsAsync() => _repository.GetCostFactorsAsync();

        public async Task<string> SaveCostFactorAsync(OperationalCostFactor factor, string changedBy)
        {
            var error = Validate(factor);
            if (error != null)
                return error;

            factor.Name = factor.Name.Trim();
            factor.Category = factor.Category.Trim();
            factor.Notes = string.IsNullOrWhiteSpace(factor.Notes) ? null : factor.Notes.Trim();

            var id = await _repository.SaveCostFactorAsync(factor, changedBy);
            return id > 0 ? null : "The cost could not be saved. The error has been logged.";
        }

        public static string Validate(OperationalCostFactor factor)
        {
            if (factor == null)
                return "Nothing to save.";
            if (string.IsNullOrWhiteSpace(factor.Name) || factor.Name.Trim().Length > 100)
                return "Enter a name of up to 100 characters.";
            if (string.IsNullOrWhiteSpace(factor.Category) || factor.Category.Trim().Length > 50)
                return "Enter a category of up to 50 characters.";
            if (factor.Amount < 0)
                return "The amount cannot be negative.";
            if (!OperationalCostFactor.Currencies.Contains(factor.Currency))
                return "Currency must be INR or USD.";
            if (!OperationalCostFactor.Bases.Contains(factor.Basis))
                return "Choose how the cost is charged.";
            if (factor.EffectiveFrom == default)
                return "Enter the date the cost starts.";
            if (factor.EffectiveTo.HasValue && factor.EffectiveTo.Value.Date < factor.EffectiveFrom.Date)
                return "The end date must be on or after the start date.";
            if (factor.Notes != null && factor.Notes.Trim().Length > 500)
                return "Notes can be up to 500 characters.";
            return null;
        }

        private static decimal? Positive(decimal? value) => value.HasValue && value.Value > 0 ? value : null;
    }

    /// <summary>
    /// Turns the facts from usp_GetInterviewProfitability into revenue, cost and margin per interview.
    /// Pure, so the allocation rules can be tested without a database.
    ///
    /// Cost of an interview, in rupees:
    ///   interviewer payout                        (InterviewPoints)
    /// + AI recorded against the interview          (OpenAIApiLogs by MeetingId or AI-interview session)
    /// + its share of AI recorded against its IRF    (IRF pool / interviews of the IRF held in the window)
    /// + its share of AI recorded against nothing    (that day's pool / interviews held that day)
    /// + voice and telephony on the interview        (Vapi, Plivo, call recordings)
    /// + its share of the IRF's unanswered calls     (IRF pool, as for AI)
    /// + its share of each running cost              (OperationalCostFactor, by Basis)
    /// Shares go only to held interviews, divided by all interviews held (not just the filtered ones).
    /// </summary>
    public static class InterviewProfitabilityCalculator
    {
        private static readonly Dictionary<int, string> MeetingStatusNames = new()
        {
            [1] = "No Show (Candidate)",
            [2] = "Scheduled",
            [3] = "Dropped",
            [4] = "Completed",
            [5] = "No Show (Interviewer)",
            [6] = "Dropped (Selected)",
            [7] = "Dropped (Rejected)",
            [8] = "Dropped (Border Line)",
            [9] = "No Show"
        };

        public static InterviewProfitabilityReport Build(
            InterviewProfitabilityRawData raw,
            InterviewProfitabilityFilter filter,
            InterviewProfitabilitySettings settings,
            DateTime start,
            DateTime end)
        {
            var report = new InterviewProfitabilityReport
            {
                Filter = filter,
                Settings = settings,
                StartDate = start.Date,
                EndDate = end.Date,
                FactorColumns = raw.CostFactors.OrderBy(f => f.Category).ThenBy(f => f.Name).ToList()
            };

            var fx = settings.UsdToInr;
            var plivoFx = settings.PlivoCallCostCurrency == "INR" ? 1m : fx;

            var heldByDay = raw.Days.GroupBy(d => d.Day.Date).ToDictionary(g => g.Key, g => g.Sum(d => d.HeldMeetings));
            var sharedAIByDay = raw.Days.GroupBy(d => d.Day.Date).ToDictionary(g => g.Key, g => g.Sum(d => d.SharedAIUSD));
            var pools = raw.IrfPools.GroupBy(p => p.IRFIntId).ToDictionary(g => g.Key, g => g.First());
            var unconvertedCurrencies = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            var interviews = new List<InterviewProfitabilityLine>();
            foreach (var m in raw.Meetings)
            {
                var line = new InterviewProfitabilityLine
                {
                    Key = m.MeetingId.ToString(),
                    Label = $"#{m.MeetingId}",
                    MeetingId = m.MeetingId,
                    InterviewDate = m.InterviewDate,
                    ClientName = m.ClientName,
                    IRFCode = m.IRFCode,
                    IRFTitle = m.IRFTitle,
                    CandidateEmail = m.CandidateEmail,
                    InterviewerName = string.IsNullOrWhiteSpace(m.InterviewerName) ? m.InterviewerEmail : m.InterviewerName,
                    InterviewerEmail = m.InterviewerEmail,
                    InterviewType = m.IsAIInterview ? "AI" : "Human",
                    Status = StatusText(m),
                    BillingBasis = m.BillingBasis,
                    IsBillable = m.BillingFactor > 0,
                    IsCancelled = m.IsCancelled,
                    Interviews = 1,
                    InterviewerPayout = m.InterviewerPayout,
                    AIDirect = m.AIDirectUSD * fx,
                    VoiceDirect = m.VapiUSD * fx + m.PlivoCallCost * plivoFx + m.RecordingUSD * fx + m.RecordingINR
                };

                var revenue = m.UnitRate * m.BillingFactor;
                var currency = string.IsNullOrWhiteSpace(m.RevenueCurrency) ? "INR" : m.RevenueCurrency.Trim().ToUpperInvariant();
                if (currency == "USD")
                    revenue *= fx;
                else if (currency != "INR" && revenue != 0)
                {
                    unconvertedCurrencies.Add(currency);
                    line.Note = $"Billed in {currency}; revenue shown unconverted.";
                }
                line.Revenue = revenue;

                if (!m.IsCancelled)
                {
                    if (m.IRFIntId.HasValue && pools.TryGetValue(m.IRFIntId.Value, out var pool) && pool.HeldMeetings > 0)
                    {
                        line.AIIrfShare = pool.AIUnattributedUSD * fx / pool.HeldMeetings;
                        line.VoiceIrfShare = (pool.VapiUnattributedUSD * fx
                                              + pool.PlivoUnattributedCost * plivoFx
                                              + pool.PlivoCXSeconds / 60m * settings.PlivoCXCostPerMinuteINR)
                                             / pool.HeldMeetings;
                    }

                    if (m.InterviewDate.HasValue)
                    {
                        var day = m.InterviewDate.Value.Date;
                        var held = heldByDay.TryGetValue(day, out var h) ? h : 0;
                        if (held > 0 && sharedAIByDay.TryGetValue(day, out var sharedUsd))
                            line.AISharedShare = sharedUsd * fx / held;

                        foreach (var factor in raw.CostFactors.Where(f => f.AppliesOn(day)))
                        {
                            var share = FactorShare(factor, day, held, m.IsAIInterview, fx);
                            if (share != 0)
                                line.Factors[factor.Id] = share;
                        }
                    }
                }

                interviews.Add(line);
            }

            report.UnabsorbedCost = UnabsorbedCost(raw, start.Date, end.Date, heldByDay, sharedAIByDay, fx, plivoFx, settings);

            interviews = ApplyResultFilters(interviews, filter).ToList();
            report.InterviewCount = interviews.Count;
            report.LossMakingCount = interviews.Count(l => l.Margin < 0);
            report.Totals = Aggregate("total", "Total", interviews);
            report.Lines = Group(interviews, filter?.GroupBy);
            report.CostBreakdown = Breakdown(report.Totals, report.FactorColumns);

            if (raw.CostFactors.Count == 0)
                report.Warnings.Add("No running costs (cloud, email, licences, ...) are in force for these dates, so overheads are not included. Add them under Running costs.");
            if (unconvertedCurrencies.Count > 0)
                report.Warnings.Add($"Some clients are billed in {string.Join(", ", unconvertedCurrencies)}; their revenue is shown unconverted.");
            if (settings.PlivoCXCostPerMinuteINR == 0 && raw.IrfPools.Any(p => p.PlivoCXSeconds > 0))
            {
                var minutes = raw.IrfPools.Sum(p => p.PlivoCXSeconds) / 60;
                report.Warnings.Add($"{minutes:N0} minutes of phone-screen campaign calls are not costed because InterviewProfitability:PlivoCXCostPerMinuteINR is not set.");
            }

            return report;
        }

        /// <summary>One interview's share of a running cost on <paramref name="day"/>.</summary>
        public static decimal FactorShare(OperationalCostFactor factor, DateTime day, int heldThatDay, bool isAIInterview, decimal usdToInr)
        {
            var amount = factor.Amount * (factor.Currency == "USD" ? usdToInr : 1m);
            switch (factor.Basis)
            {
                case OperationalCostFactor.PerDay:
                    return heldThatDay > 0 ? amount / heldThatDay : 0;
                case OperationalCostFactor.PerMonth:
                    return heldThatDay > 0 ? amount / DateTime.DaysInMonth(day.Year, day.Month) / heldThatDay : 0;
                case OperationalCostFactor.PerInterview:
                    return amount;
                case OperationalCostFactor.PerAIInterview:
                    return isAIInterview ? amount : 0;
                case OperationalCostFactor.PerHumanInterview:
                    return isAIInterview ? 0 : amount;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Daily running costs and unattributed AI on days with no held interview, plus IRF-level costs
        /// of IRFs with no held interview: money spent that no interview in the grid carries.
        /// </summary>
        private static decimal UnabsorbedCost(
            InterviewProfitabilityRawData raw, DateTime start, DateTime end,
            Dictionary<DateTime, int> heldByDay, Dictionary<DateTime, decimal> sharedAIByDay,
            decimal fx, decimal plivoFx, InterviewProfitabilitySettings settings)
        {
            decimal total = 0;
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                if (heldByDay.TryGetValue(day, out var held) && held > 0)
                    continue;

                if (sharedAIByDay.TryGetValue(day, out var sharedUsd))
                    total += sharedUsd * fx;

                foreach (var factor in raw.CostFactors.Where(f => f.AppliesOn(day)
                             && (f.Basis == OperationalCostFactor.PerDay || f.Basis == OperationalCostFactor.PerMonth)))
                {
                    var amount = factor.Amount * (factor.Currency == "USD" ? fx : 1m);
                    total += factor.Basis == OperationalCostFactor.PerDay
                        ? amount
                        : amount / DateTime.DaysInMonth(day.Year, day.Month);
                }
            }

            foreach (var pool in raw.IrfPools.Where(p => p.HeldMeetings == 0))
            {
                total += pool.AIUnattributedUSD * fx + pool.VapiUnattributedUSD * fx + pool.PlivoUnattributedCost * plivoFx
                         + pool.PlivoCXSeconds / 60m * settings.PlivoCXCostPerMinuteINR;
            }

            return total;
        }

        private static IEnumerable<InterviewProfitabilityLine> ApplyResultFilters(
            IEnumerable<InterviewProfitabilityLine> lines, InterviewProfitabilityFilter filter)
        {
            if (filter?.Billability == "Billable")
                lines = lines.Where(l => l.IsBillable);
            else if (filter?.Billability == "NonBillable")
                lines = lines.Where(l => !l.IsBillable);

            if (filter?.Margin == "Loss")
                lines = lines.Where(l => l.Margin < 0);
            else if (filter?.Margin == "Profit")
                lines = lines.Where(l => l.Margin >= 0);

            return lines;
        }

        private static List<InterviewProfitabilityLine> Group(List<InterviewProfitabilityLine> interviews, string groupBy)
        {
            Func<InterviewProfitabilityLine, (string Key, string Label)> keyOf = groupBy switch
            {
                "IRF" => l => (l.IRFCode ?? "", string.IsNullOrWhiteSpace(l.IRFCode) ? "(no IRF)" : l.IRFCode),
                "Client" => l => (l.ClientName ?? "", string.IsNullOrWhiteSpace(l.ClientName) ? "(no client)" : l.ClientName),
                "Interviewer" => l => (l.InterviewerEmail ?? "", string.IsNullOrWhiteSpace(l.InterviewerName) ? "(no interviewer)" : l.InterviewerName),
                "Day" => l => (l.InterviewDate?.ToString("yyyy-MM-dd") ?? "", l.InterviewDate?.ToString("dd MMM yyyy (ddd)") ?? "(no date)"),
                _ => null
            };

            if (keyOf == null)
                return interviews;

            return interviews
                .GroupBy(keyOf)
                .Select(g =>
                {
                    var line = Aggregate(g.Key.Key, g.Key.Label, g.ToList());
                    var first = g.First();
                    if (groupBy == "IRF")
                    {
                        line.IRFCode = first.IRFCode;
                        line.IRFTitle = first.IRFTitle;
                        line.ClientName = first.ClientName;
                    }
                    else if (groupBy == "Day")
                        line.InterviewDate = first.InterviewDate?.Date;
                    else if (groupBy == "Interviewer")
                        line.InterviewerEmail = first.InterviewerEmail;
                    return line;
                })
                .OrderBy(l => groupBy == "Day" ? l.Key : null)
                .ThenBy(l => groupBy == "Day" ? 0m : l.Margin)
                .ToList();
        }

        private static InterviewProfitabilityLine Aggregate(string key, string label, IReadOnlyCollection<InterviewProfitabilityLine> lines)
        {
            var total = new InterviewProfitabilityLine
            {
                Key = key,
                Label = label,
                Interviews = lines.Sum(l => l.Interviews),
                Revenue = lines.Sum(l => l.Revenue),
                InterviewerPayout = lines.Sum(l => l.InterviewerPayout),
                AIDirect = lines.Sum(l => l.AIDirect),
                AIIrfShare = lines.Sum(l => l.AIIrfShare),
                AISharedShare = lines.Sum(l => l.AISharedShare),
                VoiceDirect = lines.Sum(l => l.VoiceDirect),
                VoiceIrfShare = lines.Sum(l => l.VoiceIrfShare),
                IsBillable = lines.Any(l => l.IsBillable)
            };

            foreach (var pair in lines.SelectMany(l => l.Factors))
                total.Factors[pair.Key] = (total.Factors.TryGetValue(pair.Key, out var sum) ? sum : 0) + pair.Value;

            return total;
        }

        private static List<InterviewProfitabilityCostSlice> Breakdown(
            InterviewProfitabilityLine totals, List<OperationalCostFactor> factors)
        {
            var slices = new List<InterviewProfitabilityCostSlice>
            {
                new() { Name = "Interviewer payout", Kind = "People", Amount = totals.InterviewerPayout },
                new() { Name = "AI – this interview", Kind = "AI", Amount = totals.AIDirect },
                new() { Name = "AI – IRF share", Kind = "AI", Amount = totals.AIIrfShare },
                new() { Name = "AI – shared", Kind = "AI", Amount = totals.AISharedShare },
                new() { Name = "Voice & telephony – this interview", Kind = "Voice", Amount = totals.VoiceDirect },
                new() { Name = "Voice & telephony – IRF share", Kind = "Voice", Amount = totals.VoiceIrfShare }
            };
            slices.AddRange(factors.Select(f => new InterviewProfitabilityCostSlice
            {
                Name = f.Name,
                Kind = f.Category,
                Amount = totals.Factors.TryGetValue(f.Id, out var amount) ? amount : 0
            }));

            var totalCost = totals.TotalCost;
            foreach (var slice in slices)
                slice.PercentOfCost = totalCost > 0 ? Math.Round(slice.Amount / totalCost * 100m, 1) : 0;

            return slices.Where(s => s.Amount != 0).OrderByDescending(s => s.Amount).ToList();
        }

        private static string StatusText(InterviewProfitabilityMeetingRow m)
        {
            var status = m.IsCancelled
                ? "Cancelled"
                : m.MeetingStatus.HasValue && MeetingStatusNames.TryGetValue(m.MeetingStatus.Value, out var name)
                    ? name
                    : "Status pending";
            return m.IsRedo ? status + " (redo)" : status;
        }
    }
}
