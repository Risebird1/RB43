using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data.WhatsAppCampaignModels
{
    /// <summary>WhatsAppCampaign.Status values.</summary>
    public static class WhatsAppCampaignStatus
    {
        public const string Draft     = "Draft";
        public const string Running   = "Running";
        public const string Paused    = "Paused";
        public const string Completed = "Completed";
    }

    /// <summary>WhatsAppCampaignCandidate.State values.</summary>
    public static class WhatsAppCampaignCandidateState
    {
        public const string Pending    = "Pending";
        public const string Sent       = "Sent";
        public const string Converted  = "Converted";
        public const string Declined   = "Declined";
        public const string NoResponse = "NoResponse";
        public const string NoNumber   = "NoNumber";
        public const string Failed     = "Failed";

        public static readonly string[] All = { Pending, Sent, Converted, Declined, NoResponse, NoNumber, Failed };

        public static bool IsValid(string state) => Array.IndexOf(All, state) >= 0;

        /// <summary>Converted reads as Booked or Confirmed depending on what the campaign asks for.</summary>
        public static string Label(string state, string purpose) => state switch
        {
            Converted  => purpose == WhatsAppCampaignPurpose.Confirm ? "Confirmed" : "Booked",
            NoResponse => "No response",
            NoNumber   => "No number",
            _          => state
        };
    }

    /// <summary>
    /// What a campaign asks for, which template it sends and which candidate sources fit it. The campaign's
    /// source decides its purpose: By IRF and Not Scheduled send C1, Reschedule sends C2, Not Confirmed sends C3.
    /// </summary>
    public static class WhatsAppCampaignPurpose
    {
        public const string Schedule   = "schedule";
        public const string Reschedule = "reschedule";
        public const string Confirm    = "confirm";

        /// <summary>The Interakt event (and so the template) each purpose sends.</summary>
        public static string EventFor(string purpose) => purpose switch
        {
            Reschedule => "Reschedule Interview",
            Confirm    => "Interview Reminder",
            _          => "Schedule Interview"
        };

        public static string ForSource(string source) => source switch
        {
            PlivoCXCampaignSource.Reschedule  => Reschedule,
            PlivoCXCampaignSource.Unconfirmed => Confirm,
            _                                 => Schedule
        };

        public static string TemplateLabel(string purpose) => purpose switch
        {
            Reschedule => "C2 · Reschedule invite",
            Confirm    => "C3 · Interview confirmation reminder",
            _          => "C1 · Schedule interview invite"
        };

        /// <summary>
        /// The template body as approved, with its {{n}} slots named, for the read-only box on Create. Mirrors
        /// the hand-off text; Interakt holds the real template.
        /// </summary>
        public static string TemplateText(string purpose) => purpose switch
        {
            Reschedule => "Hi {name}, due to certain reasons your interview for the {JD title} role with {Client} could not take place as scheduled. "
                        + "Risebird is {Client}'s official interview partner and will conduct your interview. Please pick a new time that suits you using the button below.\n\n"
                        + "[Pick a New Time]  [Unsubscribe]",
            Confirm    => "Hi {name}, this is a reminder for your {Client} interview for {JD title}, scheduled on {date} ({time zone}).\n"
                        + "Please confirm your availability by tapping \"Yes! Available\" below.\n"
                        + "If you need to reschedule, are not available or need clarity, tap \"Reschedule / Others\".\n"
                        + "Please join the interview from a laptop, as it may include coding.\n"
                        + "Need help? Write to candidatesupport@risebird.io\n\n"
                        + "[Yes! Available]  [Reschedule / Others]  [Stop]",
            _          => "Hi {name}, in continuation of your application for the {JD title} role, {Client} has invited you to an interview. "
                        + "Risebird is {Client}'s official interview partner and will conduct your interview. Please pick a time that suits you using the button below.\n\n"
                        + "[Schedule Interview]  [Unsubscribe]"
        };

        /// <summary>C1 and C2 count toward the 3-per-candidate-per-IRF limit; C3 does not.</summary>
        public static bool IsLimited(string purpose) => purpose == Schedule || purpose == Reschedule;
    }

    /// <summary>One WhatsApp campaign with its sending policy (WhatsAppCampaign).</summary>
    public class WhatsAppCampaign
    {
        public int      Id               { get; set; }
        public string   Name             { get; set; }
        public string   SourceType       { get; set; } = PlivoCXCampaignSource.IRF;
        public string   Purpose          { get; set; } = WhatsAppCampaignPurpose.Schedule;
        public string   EventName        { get; set; }
        public int?     IRFId            { get; set; }
        public string   IRFName          { get; set; }
        public string   ClientName       { get; set; }
        public string   Status           { get; set; } = WhatsAppCampaignStatus.Draft;

        // Sending policy
        public TimeSpan WindowOpens      { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan WindowCloses     { get; set; } = new TimeSpan(20, 0, 0);
        public string   TimeZone         { get; set; } = "Asia/Kolkata";
        public int      MaxSends         { get; set; } = 2;
        public int      ResendAfterHours { get; set; } = 24;
        public int      BatchSize        { get; set; } = 50;
        public DateTime? StartsOn        { get; set; }
        public DateTime? EndsOn          { get; set; }

        public string   CreatedBy        { get; set; }
        public DateTime CreatedAt        { get; set; }
        public DateTime? UpdatedAt       { get; set; }
    }

    /// <summary>One candidate in a WhatsApp campaign (WhatsAppCampaignCandidate). Times are UTC.</summary>
    public class WhatsAppCampaignCandidate
    {
        public int      Id                    { get; set; }
        public int      CampaignId            { get; set; }
        public int      CandidateId           { get; set; }
        public string   CandidateName         { get; set; }
        public string   CandidateEmail        { get; set; }
        public string   Phone                 { get; set; }
        public int?     IRFId                 { get; set; }
        public string   IRFName               { get; set; }
        public string   JobTitle              { get; set; }
        public string   ClientName            { get; set; }
        public int?     InterviewId           { get; set; }
        public string   OriginalInterviewDate { get; set; }
        public string   State                 { get; set; } = WhatsAppCampaignCandidateState.Pending;
        public int      Sends                 { get; set; }
        public int      FailedSends           { get; set; }
        public DateTime? FirstSentAt          { get; set; }
        public DateTime? LastSentAt           { get; set; }
        public DateTime? NextSendAt           { get; set; }
        public string   LastResult            { get; set; }
        public string   LastError             { get; set; }
        public string   InteraktId            { get; set; }
        public DateTime? ConvertedAt          { get; set; }
        public string   OutcomeNote           { get; set; }
        public DateTime? SendLockUntil        { get; set; }
        public DateTime CreatedAt             { get; set; }
        public DateTime? UpdatedAt            { get; set; }
    }

    /// <summary>A Campaigns list row: the campaign with its candidate states rolled up.</summary>
    public class WhatsAppCampaignVM
    {
        public int      Id          { get; set; }
        public string   Name        { get; set; }
        public string   SourceType  { get; set; }
        public string   Purpose     { get; set; }
        public string   EventName   { get; set; }
        public int?     IRFId       { get; set; }
        public string   IRFName     { get; set; }
        public string   ClientName  { get; set; }
        public string   Status      { get; set; }
        public string   CreatedBy   { get; set; }
        public DateTime CreatedAt   { get; set; }
        public int      Candidates  { get; set; }
        public int      Sent        { get; set; }
        public int      Converted   { get; set; }
        public int      Declined    { get; set; }
        public int      NoResponse  { get; set; }
        public int      NoNumber    { get; set; }
        public int      Failed      { get; set; }
        public int      Pending     { get; set; }
    }

    /// <summary>
    /// The WhatsApp facts of one candidate for one IRF, merged into the Create picker: the number a send would
    /// use, the WhatsApp opt-out, sends of this campaign's event used toward the 3-per-IRF limit, and another
    /// open WhatsApp campaign that already holds them.
    /// </summary>
    public class WhatsAppCampaignEligibilityRow
    {
        public int    CandidateId        { get; set; }
        public string Email              { get; set; }
        public int?   IRFId              { get; set; }
        public string WhatsAppNumber     { get; set; }
        public bool   OptedOut           { get; set; }
        public int    SendsUsed          { get; set; }
        public string ActiveCampaignName { get; set; }
    }

    /// <summary>
    /// One row of the Create picker. The candidate lists are the PlivoCX ones; WhatsApp adds the number a
    /// send would use and the 3-per-IRF usage. Also what Create posts back for each ticked candidate.
    /// </summary>
    public class WhatsAppCampaignPickerItem : PlivoCXCampaignPickerItem
    {
        /// <summary>Sends of this campaign's event already used for the candidate's IRF (C1 / C2).</summary>
        public int  SendsUsed { get; set; }
        /// <summary>The per-IRF limit for this campaign's event; 0 = not limited (C3).</summary>
        public int  SendLimit { get; set; }
        public bool OptedOut  { get; set; }
    }

    /// <summary>The Create campaign form post.</summary>
    public class WhatsAppCampaignCreateRequest
    {
        public string Name       { get; set; }
        public string SourceType { get; set; }
        public int?   IRFId      { get; set; }
        public List<WhatsAppCampaignPickerItem> Candidates { get; set; } = new List<WhatsAppCampaignPickerItem>();
    }

    /// <summary>What a Send now or a scheduler pass did.</summary>
    public class WhatsAppCampaignSendResult
    {
        public int    Sent      { get; set; }
        public int    Skipped   { get; set; }
        public int    Failed    { get; set; }
        public int    Remaining { get; set; }
        public string Message   { get; set; }
    }

    /// <summary>A candidate row's next state after one send attempt.</summary>
    public class WhatsAppCandidateTransition
    {
        public string    State       { get; set; }
        /// <summary>Whether the attempt counts as a send (Interakt accepted it).</summary>
        public bool      Counted     { get; set; }
        public int       FailedSends { get; set; }
        public DateTime? NextSendAt  { get; set; }
        public string    OutcomeNote { get; set; }
    }

    /// <summary>The campaign detail page.</summary>
    public class WhatsAppCampaignDetailVM
    {
        public WhatsAppCampaign Campaign { get; set; }
        public List<WhatsAppCampaignCandidate> Candidates { get; set; } = new List<WhatsAppCampaignCandidate>();
        public TimeZoneInfo TimeZone       { get; set; }
        public bool SchedulerEnabled       { get; set; }

        public int Count(string state)
        {
            int n = 0;
            foreach (var c in Candidates) if (c.State == state) n++;
            return n;
        }
    }
}
