using System;

namespace Risebird.Entities.Data
{
    public class PendingVideoMeetingsData
    {
        public int imid { get; set; }
        public string ZoomMeetingId { get; set; }
        public string zoomLink { get; set; }
        //public string ZoomId { get; set; }
        public string VideoLink { get; set; }
        public DateTime interviewDate { get; set; }
        public DateTime RequestedDate { get; set; }
        public string MeetingPlatform { get; set; }
        public string AudioTranscriptFileLink { get; set; }
        public int VideoDuration { get; set; }
        public string meetingtype { get; set; }
        public int ClientId { get; set; }
        public string ClientCompanyName { get; set; }
        public string InterviewMeetingPlatForm { get; set; }
        public bool generateTranscriptSummary { get; set; }
        public int irfIntId { get; set; }
    }
}
