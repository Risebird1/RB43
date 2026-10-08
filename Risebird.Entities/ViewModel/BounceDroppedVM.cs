using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>One page of the Bounce &amp; Dropped Mail list (server-side DataTable).</summary>
    public class BounceDroppedPageVM
    {
        public IEnumerable<MailRecipientActivityDetailsVM> Rows { get; set; } = new List<MailRecipientActivityDetailsVM>();
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
    }

    /// <summary>Paging / search / sort / filter request for usp_GetEmailRecipientsActivityBounceDropped.</summary>
    public class BounceDroppedQuery
    {
        public string RecipientType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string EmailId { get; set; }
        public bool IncludeResolved { get; set; }
        public string[] SkillChannels { get; set; }
        public string[] SentBy { get; set; }
        public string[] Recipients { get; set; }
        public string[] Activities { get; set; }
        public string[] BounceReasons { get; set; }
        public string Search { get; set; }
        public string SortColumn { get; set; }
        public string SortDir { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>A (FilterName, FilterValue) pair from usp_GetBounceDroppedFilterOptions.</summary>
    public class BounceDroppedFilterOption
    {
        public string FilterName { get; set; }
        public string FilterValue { get; set; }
    }

    /// <summary>
    /// Row context returned by usp_GetBounceDroppedRowForAction. Only returned when the user may act on the row.
    /// </summary>
    public class BounceDroppedRowContext
    {
        public int SendgridEventId { get; set; }
        public string BouncedEmail { get; set; }
        public int SentMailDetailsId { get; set; }
        public int? IRFId { get; set; }
        public int? InterviewId { get; set; }
        public string SkillChannelName { get; set; }
        public int? CandidateId { get; set; }
        public int? CandidateUserId { get; set; }
        public string CandidateCurrentEmail { get; set; }
        public string ResolutionStatus { get; set; }
        public DateTime? LastResentAt { get; set; }
        /// <summary>Temporary (4.x.x, mailbox full, quota, try again) or Permanent.</summary>
        public string BounceKind { get; set; }
        public string BounceReason { get; set; }
    }

    /// <summary>Stored mail behind a bounce row (usp_GetBounceMailContent).</summary>
    public class BounceMailContent
    {
        public int SentMailDetailsId { get; set; }
        public string MailSubject { get; set; }
        public string MailBody { get; set; }
        public int? IRFId { get; set; }
        public int? InterviewId { get; set; }
        /// <summary>CC list of the original mail (comma or semicolon separated).</summary>
        public string MailCc { get; set; }
        public DateTime? InterviewDate { get; set; }
        /// <summary>None | Missing | Cancelled | Past | Upcoming (usp_GetBounceMailContent).</summary>
        public string InterviewState { get; set; }
        public DateTime? InterviewStartUtc { get; set; }
        public int InterviewDurationMinutes { get; set; }
        public string ZoomLink { get; set; }
        public string InterviewCandidateEmail { get; set; }
    }

    /// <summary>Result of usp_UpdateBouncedCandidateEmail.</summary>
    public class BounceEmailUpdateResult
    {
        /// <summary>OK | DUPLICATE | NOT_FOUND | SAME; null when the call failed.</summary>
        public string Result { get; set; }
        /// <summary>On DUPLICATE: who already uses the address.</summary>
        public string OwnerName { get; set; }
        public string OwnerProfile { get; set; }
    }

    /// <summary>What the Bounce &amp; Dropped Mail page needs to render its shell (rows come from the data endpoint).</summary>
    public class BounceDroppedPageSettingsVM
    {
        public string RecipientType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        /// <summary>Selected preset; null = Custom.</summary>
        public int? RangeDays { get; set; }
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
        public string RangeLabel { get; set; }
        /// <summary>Shown when the requested range was clamped.</summary>
        public string Notice { get; set; }
        public bool IncludeResolved { get; set; }
        public string[] SelectedSkillChannels { get; set; } = new string[0];
        public string[] SelectedSentBy { get; set; } = new string[0];
        public string[] SelectedRecipients { get; set; } = new string[0];
        public string[] SelectedActivities { get; set; } = new string[0];
        public string[] SelectedBounceReasons { get; set; } = new string[0];
        public List<string> SkillChannelOptions { get; set; } = new List<string>();
        public List<string> SentByOptions { get; set; } = new List<string>();
        public List<string> RecipientOptions { get; set; } = new List<string>();
        public List<string> ActivityOptions { get; set; } = new List<string>();
        public List<string> BounceReasonOptions { get; set; } = new List<string>();
    }
}
