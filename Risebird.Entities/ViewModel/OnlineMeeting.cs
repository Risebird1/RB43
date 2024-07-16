using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class Body
    {
        public string contentType { get; set; }
        public string content { get; set; }
    }

    public class Start
    {
        public DateTime dateTime { get; set; }
        public string timeZone { get; set; }
    }

    public class End
    {
        public DateTime dateTime { get; set; }
        public string timeZone { get; set; }
    }

    public class Location
    {
        public string displayName { get; set; }
    }

    public class EmailAddress
    {
        public string address { get; set; }
        public string name { get; set; }
    }

    public class Attendee
    {
        public EmailAddress emailAddress { get; set; }
        public string type { get; set; }
    }

    public class OnlineMeeting
    {
        public string subject { get; set; }
        public Body body { get; set; }
        public Start start { get; set; }
        public End end { get; set; }
        public Location location { get; set; }
        public IList<Attendee> attendees { get; set; }
        public bool isOnlineMeeting { get; set; }
        public string onlineMeetingProvider { get; set; }

    }
    public class OnlineMeetingTwo
    {
        public string subject { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class MetingResponse
    {
        public string joinUrl { get; set; }
        public string id { get; set; }
        public string meetingCode { get; set; }
        public string meetingId { get; set; }
        public string recordingContentUrl { get; set; }
    }
    public class OnlineMeetingResponse
    {
        public MetingResponse onlineMeeting { get; set; }
    }
    public class OnlineMeetingResult
    {
        public List<MetingResponse> value { get; set; }
    }
    public class OnlineMeetingUpdate
    {
        public bool recordAutomatically { get; set; }
    }
    public class OnlineMeetingRecording
    {
        public List<MetingResponse> value { get; set; }
    }
}
