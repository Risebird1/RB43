using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null);
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null);
        Task<IEnumerable<CandidateLastStatus>> GetCandidateLastStatus();
        Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid);
        Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail);
        Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid);
        Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid);
        Task<Candidate> GetCandidatesDetails(int irfId, int candidateId);
        Task<bool> AddResume(List<CandidateResume> resumeList);
        Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId);
        Task<bool> DeleteCandidateResume(int id);
        Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId);
        Task<string> GetCandidateStaffingAgency(int candidateId);
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<bool> AddEditFitmentReport(FitmentReportModel model);
        Task<Candidate> GetCandidateForFitmentReport(int candidateMasterId, int irfId);
        Task<bool> ProcessCandidateResume(int id);
        Task<string> FetchCandidateResumeById(int id);
        Task<string> FetchBillingownerById(int id);
        Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid);
        Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int interviewId, int communicationMedium);
    }
}
