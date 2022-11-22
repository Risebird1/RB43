using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ICandidateService
    {
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null);
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null);
        Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail);
        Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid);
        Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail);
        Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid);
        Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid);
        Task<Candidate> GetCandidatesDetails(int irfId, int candidateId);
        Task<bool> AddResume(List<CandidateResume> resumeList);
        Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId);
        Task<bool> DeleteCandidateResume(int id);
        Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId);
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<FitmentReportModel> GetFitmentReportReport(int candidateId, int irfId);
        Task<bool> AddEditFitmentReport(FitmentReportModel model);
        Task<bool> ProcessCandidateResume(int id);
        Task<string> FetchCandidateResumeById(int id);
        Task<string> FetchBillingownerById(int id);
        Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid);
        Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int interviewId, int communicationMedium);
    }
}
