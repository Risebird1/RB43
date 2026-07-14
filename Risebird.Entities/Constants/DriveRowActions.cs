using System.Collections.Generic;

namespace Risebird.Entities.Constants
{
    public static class DriveRowActions
    {
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Options = new[]
        {
            new KeyValuePair<string, string>("magBatchCall", "MAG Batch Call"),
            new KeyValuePair<string, string>("magEmail", "MAG Email"),
            new KeyValuePair<string, string>("campaignEmail", "Campaign Email"),
            new KeyValuePair<string, string>("campaignBatchCall", "Campaign Batch Call"),
            new KeyValuePair<string, string>("processStuckApplicants", "Process Stuck Applicants"),
            new KeyValuePair<string, string>("inviteFresh", "Invite Fresh"),
            new KeyValuePair<string, string>("openInterviewNotification", "Open Interview Notification")
        };
    }
}
