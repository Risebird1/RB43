namespace Risebird.Entities.Data
{
    public class MailActivityFilter : Filters
    {
        public string[] Activity { get; set; }
        public string[] SentBy { get; set; }
        public string[] Recipient { get; set; }
        public string RecipientType { get; set; }
        public string CampaignName { get; set; }
        public int returnBack { get; set; }
        /// <summary>Bounce &amp; Dropped Mail page: date-range preset (2, 7, 15, 30, 45); null = custom dates.</summary>
        public int? RangeDays { get; set; }
        /// <summary>Bounce &amp; Dropped Mail page: bounce reason multi-select.</summary>
        public string[] BounceReason { get; set; }
        /// <summary>Bounce &amp; Dropped Mail page: also show Resolved / Unreachable / Resent rows.</summary>
        public bool IncludeResolved { get; set; }
    }
}
