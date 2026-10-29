using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data.PlivoCXModels
{
    /// <summary>PlivoCXCampaign.Status values.</summary>
    public static class PlivoCXCampaignStatus
    {
        public const string Draft     = "Draft";
        public const string Running   = "Running";
        public const string Paused    = "Paused";
        public const string Completed = "Completed";
    }

    /// <summary>PlivoCXCampaignCandidate.State values.</summary>
    public static class PlivoCXCampaignCandidateState
    {
        public const string Pending   = "Pending";
        public const string Booked    = "Booked";
        public const string NotBooked = "NotBooked";
        public const string Cold      = "Cold";

        public static bool IsValid(string state) =>
            state == Pending || state == Booked || state == NotBooked || state == Cold;
    }

    /// <summary>
    /// The voice platform a campaign calls through (PlivoCXCampaign.Provider, PlivoCXAgent.Provider).
    /// Plivo and ElevenLabs are built; Vapi is listed on Create but not selectable yet.
    /// </summary>
    public static class PlivoCXCampaignProvider
    {
        public const string Plivo      = "Plivo";
        public const string ElevenLabs = "ElevenLabs";
        public const string Vapi       = "Vapi";

        /// <summary>Every provider, in the order Create shows them.</summary>
        public static readonly string[] All = { Plivo, ElevenLabs, Vapi };

        /// <summary>Providers a campaign can be created on today.</summary>
        public static bool IsEnabled(string provider) => provider == Plivo || provider == ElevenLabs;

        /// <summary>The canonical spelling, Plivo for blank (rows created before providers existed), null when unknown.</summary>
        public static string Normalize(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider)) return Plivo;
            foreach (var p in All)
                if (string.Equals(p, provider.Trim(), StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }
    }

    /// <summary>One agent from Dashboard/CreateElevenLabsCampaignAgents.</summary>
    public class ElevenLabsAgentProvisionResult
    {
        public string Purpose        { get; set; }
        public string Name           { get; set; }
        /// <summary>created | updated; null on failure.</summary>
        public string Action         { get; set; }
        /// <summary>The ElevenLabs agent_id.</summary>
        public string AgentId        { get; set; }
        /// <summary>The PlivoCXAgent row campaigns pick.</summary>
        public int    PlivoCXAgentId { get; set; }
        public string Error          { get; set; }
    }

    /// <summary>Where a campaign's candidates came from (PlivoCXCampaign.SourceType).</summary>
    public static class PlivoCXCampaignSource
    {
        public const string IRF          = "IRF";
        public const string NotScheduled = "NotScheduled";
        public const string Reschedule   = "Reschedule";
        public const string Unconfirmed  = "Unconfirmed";

        public static string ListName(string source) => source switch
        {
            NotScheduled => "Not-scheduled list",
            Reschedule   => "Reschedule list",
            Unconfirmed  => "Unconfirmed list",
            _            => null
        };
    }

    /// <summary>
    /// A Plivo AgentFlow a campaign can run (PlivoCXAgent). The flow is the behaviour: a
    /// Schedule agent collects a slot and books, a confirm-only agent (CollectsSlot = false)
    /// only records the candidate's yes/no.
    /// </summary>
    public class PlivoCXAgent
    {
        public int    Id           { get; set; }
        public string Name         { get; set; }
        public string Description  { get; set; }
        public string TriggerUrl   { get; set; }
        /// <summary>schedule | reschedule | confirm | availability | onboard</summary>
        public string Purpose      { get; set; }
        /// <summary>Candidate | Interviewer</summary>
        public string TargetType   { get; set; }
        public bool   CollectsSlot { get; set; } = true;
        /// <summary>Voice | WhatsApp</summary>
        public string Channel      { get; set; } = "Voice";
        public bool   IsActive     { get; set; } = true;
        /// <summary>Plivo | ElevenLabs | Vapi (PlivoCXCampaignProvider).</summary>
        public string Provider     { get; set; } = PlivoCXCampaignProvider.Plivo;
        /// <summary>The provider's id for the agent - the ElevenLabs agent_id. Plivo agents use TriggerUrl instead.</summary>
        public string ProviderAgentId { get; set; }

        public string Label => string.IsNullOrWhiteSpace(Description) ? Name : Name + " — " + Description;
    }

    /// <summary>One calling campaign with its calling policy (PlivoCXCampaign).</summary>
    public class PlivoCXCampaign
    {
        public int      Id              { get; set; }
        public string   Name            { get; set; }
        public int?     IRFId           { get; set; }
        public string   IRFName         { get; set; }
        public string   ClientName      { get; set; }
        public string   SourceType      { get; set; } = PlivoCXCampaignSource.IRF;
        /// <summary>Plivo | ElevenLabs | Vapi (PlivoCXCampaignProvider).</summary>
        public string   Provider        { get; set; } = PlivoCXCampaignProvider.Plivo;
        public int      AgentId         { get; set; }
        public string   CallPurpose     { get; set; }
        public string   Status          { get; set; } = PlivoCXCampaignStatus.Draft;

        // Calling policy
        public TimeSpan WindowOpens     { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan WindowCloses    { get; set; } = new TimeSpan(20, 0, 0);
        public string   TimeZone        { get; set; } = "Asia/Kolkata";
        public int      AttemptsPerDay  { get; set; } = 3;
        public int      TotalAttempts   { get; set; } = 6;
        public int      MinGapMinutes   { get; set; } = 180;
        public int      ConcurrentCalls { get; set; } = 20;   // 200+ calls in 30 minutes with headroom for longer calls (PlivoCXCampaignRules.EstimateCallsPer30Minutes)
        public DateTime? StartsOn       { get; set; }
        public DateTime? EndsOn         { get; set; }

        public string   CreatedBy       { get; set; }
        public DateTime CreatedAt       { get; set; }
        public DateTime? UpdatedAt      { get; set; }
    }

    /// <summary>One candidate in a campaign (PlivoCXCampaignCandidate). Times are UTC.</summary>
    public class PlivoCXCampaignCandidate
    {
        public int      Id                    { get; set; }
        public int      CampaignId            { get; set; }
        public int      CandidateId           { get; set; }
        public string   CandidateName         { get; set; }
        public string   CandidateEmail        { get; set; }
        public string   CandidatePhone        { get; set; }
        public int?     IRFId                 { get; set; }
        public string   IRFName               { get; set; }
        public string   JobTitle              { get; set; }
        public string   ClientName            { get; set; }
        public int?     InterviewId           { get; set; }
        public string   OriginalInterviewDate { get; set; }
        public string   State                 { get; set; } = PlivoCXCampaignCandidateState.Pending;
        public int      Attempts              { get; set; }
        public DateTime? LastCallAt           { get; set; }
        public DateTime? NextAttemptAt        { get; set; }
        public string   LastDisposition       { get; set; }
        public string   BookedSlot            { get; set; }
        public int?     LastCallLogId         { get; set; }
        public DateTime CreatedAt             { get; set; }
        public DateTime? UpdatedAt            { get; set; }

        // From the latest call log (detail table only)
        public string   LastRecordingUrl      { get; set; }
        public string   LastCallStatus        { get; set; }
    }

    /// <summary>A Campaigns list row: the campaign with its candidate states rolled up.</summary>
    public class PlivoCXCampaignVM
    {
        public int      Id          { get; set; }
        public string   Name        { get; set; }
        public int?     IRFId       { get; set; }
        public string   IRFName     { get; set; }
        public string   ClientName  { get; set; }
        public string   SourceType  { get; set; }
        public string   Provider    { get; set; }
        public int      AgentId     { get; set; }
        public string   AgentName   { get; set; }
        public string   CallPurpose { get; set; }
        public string   Status      { get; set; }
        public string   CreatedBy   { get; set; }
        public DateTime CreatedAt   { get; set; }
        public int      Candidates  { get; set; }
        public int      Booked      { get; set; }
        public int      NotBooked   { get; set; }
        public int      Cold        { get; set; }
        public int      Pending     { get; set; }
    }

    /// <summary>Call statistics of one campaign, for the Pick-up tile.</summary>
    public class PlivoCXCampaignCallStats
    {
        public int FinishedCalls { get; set; }
        public int AnsweredCalls { get; set; }
        public int InFlightCalls { get; set; }

        /// <summary>Answered share of finished calls, 0-100; null before any call has finished.</summary>
        public int? PickupPercent => FinishedCalls > 0 ? (int)Math.Round(AnsweredCalls * 100.0 / FinishedCalls) : (int?)null;
    }

    /// <summary>A row of usp_GetPlivoCXCampaignPickerForIRF.</summary>
    public class PlivoCXCampaignIRFPickerRow
    {
        public int    CandidateId           { get; set; }
        public int    IRFId                 { get; set; }
        public string IRFName               { get; set; }
        public string CandidateName         { get; set; }
        public string CandidateEmail        { get; set; }
        public string CandidatePhone        { get; set; }
        public string JobTitle              { get; set; }
        public string ClientName            { get; set; }
        public string PlivoCXDisposition    { get; set; }
        public bool   IsCold                { get; set; }
        public bool   HasMeeting            { get; set; }
        public int?   UpcomingInterviewId   { get; set; }
        public string UpcomingInterviewDate { get; set; }
        public string LastInterviewDate     { get; set; }
        public string ActiveCampaignName    { get; set; }
    }

    /// <summary>
    /// One row of the Create campaign picker, whatever the source. Also what Create posts back
    /// for each ticked candidate.
    /// </summary>
    public class PlivoCXCampaignPickerItem
    {
        public int    CandidateId    { get; set; }
        public string Name           { get; set; }
        public string Email          { get; set; }
        public string Phone          { get; set; }
        public int?   IrfId          { get; set; }
        public string IrfName        { get; set; }
        public string JobTitle       { get; set; }
        public string ClientName     { get; set; }
        public int?   InterviewId    { get; set; }
        public string OriginalDate   { get; set; }
        /// <summary>eligible | has-meeting | no-number | not-eligible | cold</summary>
        public string Eligibility    { get; set; }
        /// <summary>Why the candidate is not eligible, or the campaign that already holds them.</summary>
        public string Note           { get; set; }
        /// <summary>Ticked by default in the picker.</summary>
        public bool   Selected       { get; set; }
    }

    /// <summary>The Create campaign form post.</summary>
    public class PlivoCXCampaignCreateRequest
    {
        public string Name       { get; set; }
        /// <summary>Plivo | ElevenLabs (PlivoCXCampaignProvider); the agent must be on the same provider.</summary>
        public string Provider   { get; set; }
        public int    AgentId    { get; set; }
        public string SourceType { get; set; }
        public int?   IRFId      { get; set; }
        public List<PlivoCXCampaignPickerItem> Candidates { get; set; } = new List<PlivoCXCampaignPickerItem>();
    }

    /// <summary>An active IRF for the Create campaign dropdown.</summary>
    public class PlivoCXCampaignIRFOption
    {
        public int    Id         { get; set; }
        public string IRFId      { get; set; }
        public string JobTitle   { get; set; }
        public string ClientName { get; set; }
    }

    /// <summary>Outcome of dialling one campaign candidate.</summary>
    public class PlivoCXDialResult
    {
        public bool   Success   { get; set; }
        public int    CallLogId { get; set; }
        public string Error     { get; set; }
    }

    /// <summary>The next state of a campaign candidate after a call, and when to retry.</summary>
    public class PlivoCXCandidateTransition
    {
        public string    State         { get; set; }
        public string    Disposition   { get; set; }
        public string    BookedSlot    { get; set; }
        public DateTime? NextAttemptAt { get; set; }
    }

    /// <summary>The campaign detail page.</summary>
    public class PlivoCXCampaignDetailVM
    {
        public PlivoCXCampaign  Campaign   { get; set; }
        public PlivoCXAgent     Agent      { get; set; }
        public List<PlivoCXCampaignCandidate> Candidates { get; set; } = new List<PlivoCXCampaignCandidate>();
        public PlivoCXCampaignCallStats CallStats { get; set; } = new PlivoCXCampaignCallStats();
        public bool  WhatsAppConfigured { get; set; }
        public TimeZoneInfo TimeZone    { get; set; }
        /// <summary>Lines on the Plivo account all campaigns may use together (PlivoCX:CampaignConcurrencyBudget).</summary>
        public int ConcurrencyBudget    { get; set; } = 40;

        public int Count(string state)
        {
            int n = 0;
            foreach (var c in Candidates) if (c.State == state) n++;
            return n;
        }
    }

    /// <summary>The call detail page (/PlivoCXCampaign/CallDetail/{logId}).</summary>
    public class PlivoCXCampaignCallDetailVM
    {
        public PlivoCXCallLog  CallLog  { get; set; }
        public PlivoCXCampaign Campaign { get; set; }
        public PlivoCXAgent    Agent    { get; set; }
        public string          CandidateState { get; set; }
    }
}
