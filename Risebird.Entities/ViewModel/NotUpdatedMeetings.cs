using System;

namespace Risebird.Entities.ViewModel
{
    public class NotUpdatedMeetings
    {
        public int UnassignedInterviewsCount { get; set; }
        public int UnfilledMeetingStatusCount { get; set; }
        public int UnfilledInterviewStatusCount { get; set; }//Need TO remove
        public int UnfilledCommentsCount { get; set; }
        public int MissingZoomLink { get; set; }
        public int UnAttendedZoomLink { get; set; }
        public int MissingVideoLink { get; set; }
        public int UpcomingMeetingsDeletedFromZoom { get; set; }
        public int NotProcessedCandidateResume { get; set; }
    }
    public class MeetingDetailsForParticipant
    {
        public string candidateEmail { get; set; }
        public string risebirdHR { get; set; }
        public string interviewer { get; set; }
        public string emergencyinterviewer { get; set; }
        public string Meetingid { get; set; }
        public bool isInterviewerWaiting { get; set; }
        public string EmailType { get; set; }
        public string zoomlink { get; set; }
        public string CalendlyLink { get; set; }
        public string HRCoordinatorEmailId { get; set; }
        public string candidateName { get; set; }
        public string ClientName { get; set; }
        public string PrimarySkill { get; set; }
        public DateTime interviewDate { get; set; }
        public string ClientHrContact { get; set; }
        public int irfId { get; set; }
    }
}
