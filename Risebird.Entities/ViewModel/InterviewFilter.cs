using System;

namespace Risebird.Entities.ViewModel
{
    public class InterviewFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] RFID { get; set; }
        public string[] ClientHR { get; set; }
        public string[] ConfirmationStatus { get; set; }
        public string[] MeetingStatus { get; set; }
        public string[] InterviewStatus { get; set; }
        public string[] ApprovalStatus { get; set; }
        public string[] SelectionStatus { get; set; }
        public string[] JoiningStatus { get; set; }
        public string[] OfferStatus { get; set; }
        public string[] Interviewer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] CandidateName { get; set; }
        public string[] CandidateLocation { get; set; }

        public string SearchFillter { get; set; }
        public string[] IsShortListed { get; set; }
        public string[] IsIntrested { get; set; }
        public int[] CandidateAvailable { get; set; }



    }
}
