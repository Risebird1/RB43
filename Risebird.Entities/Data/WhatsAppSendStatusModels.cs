using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// One candidate's WhatsApp state for one IRF, for the send buttons on the candidate lists: opted out of
    /// WhatsApp for the role, and sends of the page's stage used toward the 3-per-IRF limit.
    /// </summary>
    public class WhatsAppSendStatusRow
    {
        public string Email     { get; set; }
        public int    IRFId     { get; set; }
        public bool   OptedOut  { get; set; }
        public int    SendsUsed { get; set; }
    }

    /// <summary>A dbo.WhatsAppOptOut row, for the admin re-enable page.</summary>
    public class WhatsAppOptOutRow
    {
        public int      Id         { get; set; }
        public string   Audience   { get; set; }
        public string   EmailId    { get; set; }
        public int?     ProspectId { get; set; }
        public int?     IRFId      { get; set; }
        /// <summary>IRF.IRFId business id, when the opt-out is for one IRF.</summary>
        public string   IRFName    { get; set; }
        public string   Source     { get; set; }
        /// <summary>The Interakt event of the message the unsubscribe came from (the link's src), if known.</summary>
        public string   TemplateEvent { get; set; }
        /// <summary>The reason picked on the confirmation page, if any.</summary>
        public string   Reason     { get; set; }
        public DateTime CreatedAt  { get; set; }
        /// <summary>Set when an admin re-enabled WhatsApp; the row then stays only as history.</summary>
        public DateTime? ReEnabledAt { get; set; }
        public string   ReEnabledBy { get; set; }

        public bool IsActive => !ReEnabledAt.HasValue;
    }

    /// <summary>A portal user whose WhatsApp number matched an incoming Interakt message (Stop replies).</summary>
    public class WhatsAppNumberOwner
    {
        public string Email       { get; set; }
        /// <summary>The user has candidate records (opt-out audience Candidate); otherwise Interviewer.</summary>
        public bool   IsCandidate { get; set; }
    }

    /// <summary>The reasons offered on the WhatsApp unsubscribe page.</summary>
    public static class WhatsAppOptOutReasons
    {
        public static readonly string[] Candidate = { "Not interested in this role", "Got another job", "Too many messages", "Other" };
        public static readonly string[] Interviewer = { "Not taking interviews right now", "Too many messages", "Prefer email only", "Other" };
    }
}
