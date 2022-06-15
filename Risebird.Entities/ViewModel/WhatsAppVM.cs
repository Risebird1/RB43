using System;

namespace Risebird.Entities.ViewModel
{
    public class WhatsAppVM
    {
        public string WhatsAppNumber { get; set; }
        public string CountryCode { get; set; }
        public string UserName { get; set; }
        public string Emailid { get; set; }
        public string Userid { get; set; }
        public string MessageValue { get; set; }
        public string InterviewDate { get; set; }
        public string Event { get; set; }
        public string Header { get; set; }
        public string buttonlink { get; set; }
    }
    public class WhatsAppVMResponse
    {
        public string result { get; set; }
        public string message { get; set; }
        public string id { get; set; }
    }
    public class CandidateDetailsForWhatsApp
    {
        public string candidateEmail { get; set; }
        public int id { get; set; }
        public DateTime interviewDate { get; set; }
        public string CandidateName { get; set; }
        public string ClientName { get; set; }
        public string JobTitle { get; set; }
    }
    public class InterviewUsersDetails
    {
        public string interviewer { get; set; }
        public string EmailId { get; set; }
        public DateTime interviewDate { get; set; }
        public string ClientName { get; set; }
        public string CalendlyLink { get; set; }
        public string JDTiltle { get; set; }
        public string CandidateName { get; set; }
        public string IRFId { get; set; }
        public int irfIntId { get; set; }
        public string candidateEmail { get; set; }
        public string ZoomMeetingId { get; set; }
    }
    public class ResumeDetailsVM
    {
        public string EmailId { get; set; }
        public int TotalResumes { get; set; }
        public string IRFId { get; set; }
        public int id { get; set; }
        public string FirstName { get; set; }
    }
}

