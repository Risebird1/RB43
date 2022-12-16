using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class CandidateTimelineVM
    {
        public string CandidateName { get; set; }
        public string CandidateEmailId { get; set; }
        public string HREmail { get; set; }
        public string CurrentStatus { get; set; }
        public string Report { get; set; }

        public List<TimelineStatus> RisebirdAttempt { get; set; }
        public List<TimelineStatus> MeetingStatus { get; set; }
        public List<TimelineStatus> InterviewStatus { get; set; }
        public List<TimelineStatus> ScheduleStatus { get; set; }
        //public TimelineStatus OnboardingDetails { get; set; }
        //public TimelineStatus CurrentStatusDetails { get; set; }

        public int? InterviewId { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
        public int? CurrentInterviewStatus { get; set; }
        public int? QCStatus { get; set; }
        public int? CurrentMeetingStatus { get; set; }
        public int? confirmationStatus { get; set; }
        public string PrimarySkill { get; set; }
        public DateTime? CurrentStatusDate { get; set; }
        public string ProfileSource { get; set; }
        public bool IsIRFArchived { get; set; }
        public string IRFName { get; set; }
        public string Phone { get; set; }
        public DateTime interviewDate { get; set; }
        public string IRFname { get; set; }
        public int imid { get; set; }
        public string CalendlyLink { get; set; }
        public string staffingAgency { get; set; }
        public bool IsCandidateResponsed { get; set; }
        public int CandidateResponse { get; set; }
        public string ClientAction { get; set; }
        public string NickName { get; set; }
        public int reportcount { get; set; }
        public bool IsQCRequired { get; set; }
        public string Interviewer { get; set; }
        public bool IsRedoInterview { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNo { get; set; }

        public int MailCount { get; set; }
        public int WhatsappCount { get; set; }
        public int CallsCount { get; set; }
        public bool IsClientCancelInterview { get; set; }
    }

    public class TimelineStatus
    {
        public string Status { get; set; }
        public string StatusDate { get; set; }

        public string Date { get; set; }
        public string Day { get; set; }
        public string Time { get; set; }
        public DateTime ActualDate { get; set; }

    }


    public class CustomerDetailsVM
    {
        public string IRFName { get; set; }
        public string Name { get; set; }
        public string JobCountry { get; set; }
        public string CompanyType { get; set; }

    }


}
