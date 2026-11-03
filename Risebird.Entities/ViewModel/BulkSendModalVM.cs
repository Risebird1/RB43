using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// The shared bulk send modal (Views/Shared/_BulkSendModal.cshtml) on the Not Scheduled, Reschedule and Not
    /// Confirmed pages: a date range, skill channels (none = all), Email and / or WhatsApp. Posts startDate,
    /// endDate, rfids[] and communicationMethods[] to <see cref="Endpoint"/>, which returns { success, message }.
    /// </summary>
    public class BulkSendModalVM
    {
        public string ModalId { get; set; } = "bulkSendModal";
        public string Title { get; set; }
        /// <summary>What the date range filters on, e.g. "Added between" or "Interview between".</summary>
        public string DateLabel { get; set; }
        public string ButtonText { get; set; }
        public string ConfirmText { get; set; }
        public string Endpoint { get; set; }
        public List<string> SkillChannelIds { get; set; } = new List<string>();
    }

    /// <summary>What one bulk send did, per medium, for the result message.</summary>
    public class BulkSendResult
    {
        public int Found { get; set; }
        public int EmailSent { get; set; }
        public int WhatsAppSent { get; set; }
        public int SkippedOptedOut { get; set; }
        public int SkippedAtLimit { get; set; }
        public int SkippedOther { get; set; }
        public int Failed { get; set; }

        public string Summary(bool email, bool whatsApp)
        {
            var parts = new List<string> { Found + (Found == 1 ? " candidate" : " candidates") };
            if (email) parts.Add("email sent " + EmailSent);
            if (whatsApp)
            {
                parts.Add("WhatsApp sent " + WhatsAppSent);
                parts.Add("skipped " + SkippedOptedOut + " opted out");
                parts.Add(SkippedAtLimit + " at limit");
                if (SkippedOther > 0) parts.Add(SkippedOther + " no number or link");
            }
            if (Failed > 0) parts.Add("failed " + Failed);
            return string.Join(" · ", parts);
        }
    }
}
