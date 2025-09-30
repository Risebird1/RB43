using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class ZohoCreateMeetingViewModel
    {
    }

    public record ZohoCreateMeetingRequest(string Topic, string? Agenda, DateTime StartTime, int DurationMinutes, bool IsRecurring = false);

    public class ZohoMeetingResponse
    {
        public string meetingKey { get; set; } = string.Empty;
        public string startLink { get; set; } = string.Empty;
        public string joinLink { get; set; } = string.Empty;
        public string accountEmail { get; set; } = string.Empty;
    }
    public class ZohoRecordingInfo
    {
        public bool isTranscriptGenerated { get; set; }
        public string? downloadUrl { get; set; }
        public int duration { get; set; }
        public string? transcriptionDownloadUrl { get; set; }
        public string? erecordingId { get; set; }
    }
    public class ZohoParticipantReportItem
    {
        public string? participantId { get; set; }
        public string? email { get; set; }
        public string? inAndOutTime { get; set; }
        public int totalDurationMinutes { get; set; }
    }
    public class ZohoAccessToken
    {
        public string AccessToken { get; set; }
        public DateTime TokenExpiry { get; set; }
        public string TokenType { get; set; }
    }
    public static class ZooTokenTypes
    {
        public static readonly string Meeting = "Meeting";
        public static readonly string Recording = "Recording";
    }
}
