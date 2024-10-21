using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class CandidateExcelData
    {

        public CandidateExcelData()
        {
            TechSkillReportCollection = new List<TechSkillReport>();
            TechSkillReportTwoCollection = new List<TechSkillReport>();
        }
        public int id { get; set; }

        public string ExecutiveReportLink { get; set; }
        public string Cancellationsource { get; set; }
        public int Irfintid { get; set; }
        public string IRFId { get; set; }
        public DateTime? interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string MobileNumber { get; set; }
        public string JDTiltle { get; set; }
        public string MeetingStatus { get; set; }
        public string InterviewStatus { get; set; }
        public string confirmationStatus { get; set; }
        public string InterviewerComment { get; set; }
        public string ClientHrContact { get; set; }
        public string VideoLink { get; set; }
        public IList<TechSkillReport> TechSkillReportCollection { get; set; }
        public IList<TechSkillReport> TechSkillReportTwoCollection { get; set; }
        public IList<string> SkillList { get; set; }
        public int CommunicationalProficiency { get; set; }
        public string CandidateLatestStatus { get; set; }
        public string ResumeLink { get; set; }
        public string ReportLink { get; set; }
        public DateTime? AddedOnDate { get; set; }
        public DateTime? InvitationSentOnDate { get; set; }
        public DateTime? FirstScheduledOnDate { get; set; }
        public DateTime? FirstScheduledOninterviewDate { get; set; }
        public DateTime? completedDate { get; set; }
        public string CandidateType { get; set; }
        public int UserId { get; set; }
        public string staffingAgency { get; set; }
        public bool? isQcDone { get; set; }
        public int ClientId { get; set; }
        public bool IsDrive { get; set; }//if the irf is marked as a Drive
        public string InterviewerCodedName { get; set; }

        public int? MeetingStatusTwo { get; set; }
        public int? InterviewStatusTwo { get; set; }
        public string InterviewerCommentTwo { get; set; }
        public string MeetingStatusTwoDesc { get; set; }
        public string InterviewStatusTwoDesc { get; set; }
        public int numberOfTechStacks { get; set; }
        public IList<string> SkillListTwo { get; set; }
        public string JDTitleTwo { get; set; }
        public int TechScoreone { get; set; }
        public int TechScoretwo { get; set; }
        public string CodeSnippet { get; set; }
        public string CodeSnippetTwo { get; set; }
        public string StatusValue { get; set; }
        public string PreReportLink { get; set; }
        public string PreReportUsername { get; set; }
        public string PreReportPassword { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime? CurrentStatusDate { get; set; }
    }
}
