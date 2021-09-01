using System;

namespace Risebird.Entities.Data
{
    public class PendingVideoMeetingsData
    {
        public int imid { get; set; }
        public string ZoomMeetingId { get; set; }
        public string zoomLink { get; set; }
        //public string ZoomId { get; set; }
        //public string VideoLink { get; set; }
        public DateTime interviewDate { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}
