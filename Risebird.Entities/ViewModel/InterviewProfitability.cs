using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Filters of the Interview Profitability grid.</summary>
    public class InterviewProfitabilityFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ClientId { get; set; }
        public int? IRFIntId { get; set; }
        public string InterviewerEmail { get; set; }

        /// <summary>All, AI or Human.</summary>
        public string InterviewType { get; set; } = "All";

        public bool IncludeCancelled { get; set; }

        /// <summary>All, Billable or NonBillable.</summary>
        public string Billability { get; set; } = "All";

        /// <summary>All, Loss or Profit.</summary>
        public string Margin { get; set; } = "All";

        /// <summary>Interview, IRF, Client, Interviewer or Day.</summary>
        public string GroupBy { get; set; } = "Interview";

        /// <summary>Rupees per US dollar for this run; the configured rate when left empty.</summary>
        public decimal? UsdToInr { get; set; }
    }

    /// <summary>Result set 1 of usp_GetInterviewProfitability: one interview, amounts as recorded.</summary>
    public class InterviewProfitabilityMeetingRow
    {
        public int MeetingId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string IRFCode { get; set; }
        public int? IRFIntId { get; set; }
        public string IRFTitle { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public string CandidateEmail { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public bool IsAIInterview { get; set; }
        public int? MeetingStatus { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsRedo { get; set; }
        public int? DurationMinutes { get; set; }
        public int BillingType { get; set; }
        public decimal UnitRate { get; set; }
        public decimal BillingFactor { get; set; }
        public string BillingBasis { get; set; }
        public string RevenueCurrency { get; set; }
        public decimal InterviewerPayout { get; set; }
        public int PayoutEntries { get; set; }
        public decimal AIDirectUSD { get; set; }
        public int AIDirectCalls { get; set; }
        public decimal VapiUSD { get; set; }
        public decimal PlivoCallCost { get; set; }
        public decimal RecordingUSD { get; set; }
        public decimal RecordingINR { get; set; }
    }

    /// <summary>Result set 2: costs recorded against an IRF rather than one of its interviews.</summary>
    public class InterviewProfitabilityIrfPoolRow
    {
        public int IRFIntId { get; set; }
        public string IRFCode { get; set; }
        public decimal AIUnattributedUSD { get; set; }
        public decimal VapiUnattributedUSD { get; set; }
        public decimal PlivoUnattributedCost { get; set; }
        public long PlivoCXSeconds { get; set; }
        public int HeldMeetings { get; set; }
    }

    /// <summary>Result set 3: interviews held on a day (across all clients) and AI spend tied to nothing.</summary>
    public class InterviewProfitabilityDayRow
    {
        public DateTime Day { get; set; }
        public int HeldMeetings { get; set; }
        public decimal SharedAIUSD { get; set; }
    }

    /// <summary>A running cost that is not recorded per interview (row of OperationalCostFactor).</summary>
    public class OperationalCostFactor
    {
        public const string PerDay = "PerDay";
        public const string PerMonth = "PerMonth";
        public const string PerInterview = "PerInterview";
        public const string PerAIInterview = "PerAIInterview";
        public const string PerHumanInterview = "PerHumanInterview";

        public static readonly string[] Bases = { PerDay, PerMonth, PerInterview, PerAIInterview, PerHumanInterview };
        public static readonly string[] Currencies = { "INR", "USD" };

        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Basis { get; set; } = PerDay;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public bool AppliesOn(DateTime day) =>
            IsActive && day.Date >= EffectiveFrom.Date && (!EffectiveTo.HasValue || day.Date <= EffectiveTo.Value.Date);
    }

    /// <summary>Everything usp_GetInterviewProfitability returns.</summary>
    public class InterviewProfitabilityRawData
    {
        public List<InterviewProfitabilityMeetingRow> Meetings { get; set; } = new();
        public List<InterviewProfitabilityIrfPoolRow> IrfPools { get; set; } = new();
        public List<InterviewProfitabilityDayRow> Days { get; set; } = new();
        public List<OperationalCostFactor> CostFactors { get; set; } = new();

        /// <summary>Call-cost tables that do not exist on this database; their costs are left out.</summary>
        public List<string> MissingCostTables { get; set; } = new();

        /// <summary>AI calls in the window logged with no tokens and $0, before the logging fix: spend the report cannot see.</summary>
        public int UnpricedAICalls { get; set; }

        /// <summary>Set instead of data when the report query failed: why, in words an admin can act on.</summary>
        public string LoadError { get; set; }
    }

    /// <summary>Rates the report needs that are not stored in the database.</summary>
    public class InterviewProfitabilitySettings
    {
        public decimal UsdToInr { get; set; } = 88m;

        /// <summary>Currency of PlivoCallLogs.CallCost (USD or INR), which depends on the Plivo account.</summary>
        public string PlivoCallCostCurrency { get; set; } = "USD";

        /// <summary>Rupees per minute of phone-screen campaign call (PlivoCXCallLogs records minutes, not cost).</summary>
        public decimal PlivoCXCostPerMinuteINR { get; set; }

        public int MaxRangeDays { get; set; } = 92;

        /// <summary>Days of OpenAIApiLogs kept by the nightly purge (Purge:OpenAIRetentionDays); older AI cost is gone.</summary>
        public int AILogRetentionDays { get; set; } = 60;

        /// <summary>Days of Vapi and Plivo call logs kept by the nightly purge (Purge:VapiPlivoRetentionDays).</summary>
        public int CallLogRetentionDays { get; set; } = 30;
    }

    /// <summary>One row of the grid: an interview, or a group of interviews. Money is in rupees.</summary>
    public class InterviewProfitabilityLine
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public int? MeetingId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string ClientName { get; set; }
        public string IRFCode { get; set; }
        public string IRFTitle { get; set; }
        public string CandidateEmail { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewType { get; set; }
        public string Status { get; set; }
        public string BillingBasis { get; set; }
        public bool IsBillable { get; set; }
        public bool IsCancelled { get; set; }
        public int Interviews { get; set; }

        public decimal Revenue { get; set; }
        public decimal InterviewerPayout { get; set; }
        public decimal AIDirect { get; set; }
        public decimal AIIrfShare { get; set; }
        public decimal AISharedShare { get; set; }
        public decimal VoiceDirect { get; set; }
        public decimal VoiceIrfShare { get; set; }

        /// <summary>Share of each operational cost factor, by factor Id.</summary>
        public Dictionary<int, decimal> Factors { get; set; } = new();

        public decimal AITotal => AIDirect + AIIrfShare + AISharedShare;
        public decimal VoiceTotal => VoiceDirect + VoiceIrfShare;
        public decimal OverheadTotal { get { decimal t = 0; foreach (var v in Factors.Values) t += v; return t; } }
        public decimal TotalCost => InterviewerPayout + AITotal + VoiceTotal + OverheadTotal;
        public decimal Margin => Revenue - TotalCost;
        public decimal? MarginPercent => Revenue > 0 ? Math.Round(Margin / Revenue * 100m, 1) : (decimal?)null;

        /// <summary>Anything the reader should know about this row's numbers (unconverted currency, ...).</summary>
        public string Note { get; set; }
    }

    /// <summary>A named slice of cost, for the breakdown above the grid.</summary>
    public class InterviewProfitabilityCostSlice
    {
        public string Name { get; set; }
        public string Kind { get; set; }
        public decimal Amount { get; set; }
        public decimal PercentOfCost { get; set; }
    }

    public class InterviewProfitabilityReport
    {
        public InterviewProfitabilityFilter Filter { get; set; }
        public InterviewProfitabilitySettings Settings { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<InterviewProfitabilityLine> Lines { get; set; } = new();
        public InterviewProfitabilityLine Totals { get; set; } = new();

        /// <summary>The cost factors shown as columns, in column order.</summary>
        public List<OperationalCostFactor> FactorColumns { get; set; } = new();

        public List<InterviewProfitabilityCostSlice> CostBreakdown { get; set; } = new();

        public int InterviewCount { get; set; }
        public int LossMakingCount { get; set; }

        /// <summary>
        /// Running cost and AI spend on days with no held interview: real money that no interview
        /// absorbs, reported so the grid's totals can be reconciled with the bills.
        /// </summary>
        public decimal UnabsorbedCost { get; set; }

        public List<string> Warnings { get; set; } = new();
        public string Error { get; set; }
    }

    public class InterviewProfitabilityOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
    }

    public class InterviewProfitabilityFilterOptions
    {
        public List<InterviewProfitabilityOption> Clients { get; set; } = new();
        public List<InterviewProfitabilityOption> IRFs { get; set; } = new();
    }

    /// <summary>One operation/model row of usp_GetOpenAICostOptimizationReport.</summary>
    public class OpenAICostOptimizationRow
    {
        public string OperationType { get; set; }
        public string Model { get; set; }
        public int Calls { get; set; }
        public int CacheHits { get; set; }
        public int FailedCalls { get; set; }
        public decimal CostUSD { get; set; }
        public double? AvgPromptTokens { get; set; }
        public double? AvgCompletionTokens { get; set; }
        public int DuplicateCalls { get; set; }
        public decimal DuplicateCostUSD { get; set; }
        public decimal AvoidedCostUSD { get; set; }

        /// <summary>True when the operation goes through OpenAIResponsesHelper, the only path OpenAI:CostOptimization affects.</summary>
        public bool ViaResponsesApi { get; set; }

        /// <summary>Filled from configuration for display: the model override and cache TTL in force.</summary>
        public string ConfiguredModelOverride { get; set; }
        public int? ConfiguredCacheTtlMinutes { get; set; }
    }
}
