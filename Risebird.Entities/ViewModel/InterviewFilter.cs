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
        public string[] CandidateAvailable { get; set; }

        public string[] Client { get; set; }
        public string[] SecondarySkill { get; set; }
        public string[] CompentencyLevel { get; set; }
        public string[] AssessmentGroup { get; set; }
        public string[] Senioritylevel { get; set; }
        public string[] ProfileType { get; set; }
        public string[] IndianRate { get; set; }
        public string[] OutSideIndiaRate { get; set; }
        public string[] CompanyType { get; set; }
        public string[] Domain { get; set; }
        public string[] IsLeadInterviewer { get; set; }
        public bool[] IsCandidateResponsed { get; set; }
        public string[] TemplateName { get; set; }
        public bool[] AadhaarVerified { get; set; }
        public bool[] IsActive { get; set; }
    }
}
