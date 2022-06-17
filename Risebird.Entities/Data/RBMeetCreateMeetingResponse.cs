using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class RBMeetCreateMeetingResponse
    {
        public string roomId { get; set; }
        public string meetingAgent { get; set; }
        public string meetingAttendee { get; set; }
        public string videoUrl { get; set; }
        public float duration { get; set; }
    }
}
