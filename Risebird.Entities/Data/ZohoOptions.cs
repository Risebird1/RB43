using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class ZohoOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RecordingRefreshToken { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty; // zsoid
        public string AccountsBaseUrl { get; set; } = "https://accounts.zoho.in";
        public string MeetingApiBaseUrl { get; set; } = "https://meeting.zoho.com/api/v2";
        public string RedirectUri { get; set; } = string.Empty;
        public string Scopes { get; set; } = "ZohoMeeting.meeting.READ,ZohoMeeting.meeting.CREATE";
        public string RecordingScopes { get; set; } = string.Empty;
    }
}
