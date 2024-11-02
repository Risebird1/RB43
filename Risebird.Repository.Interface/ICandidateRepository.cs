using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
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
        Task<bool> UpdateCandidateResumeInBulk(List<CandidateResume> resumeList);
        Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId, string emailid = null);
        Task<bool> DeleteCandidateResume(int id);
        Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId);
        Task<string> GetCandidateStaffingAgency(int candidateId);
        Task<IEnumerable<CandidateTimelineVM>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<bool> AddEditFitmentReport(FitmentReportModel model);
        Task<Candidate> GetCandidateForFitmentReport(int candidateMasterId, int irfId);
        Task<bool> ProcessCandidateResume(int id);
        Task<string> FetchCandidateResumeById(int id);
        Task<string> FetchBillingownerById(int id);
        Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid);
        Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int communicationMedium, int? interviewId = null, int? irfId = null, string emailId = null);
        Task<CandidateIRFDetailsVM> GetIRFDetailsForCandidate(string emailId, int irfId);
        Task<int> GetuniqueCandidateByEmailId(string emailId, string id);
        Task<string> GetCandidateUniqueId(string emailId);
        Task<Candidate> GetCandidateIRFMailDetails(string EmailId, int IRFid);
        Task<IEnumerable<CandidateResume>> GetCandidateResumesForParsing();
        Task<IEnumerable<IRFCandidateReport>> FetchSkillChannelCandidateReprots(int irfId);
        Task<bool> SaveCandidateCodedResume(Candidate model);
        Task<List<MeetingVM>> GetColdCandidatesByClientRepository(string Email);
        Task<List<CandidateResume>> GetNotProcessedResumeByClientRepository(string Email);
        Task<IEnumerable<CandidateExcelData>> GetIRFCandidateStatsForExcelDownLoad(int irfId, string emailid);
        Task<Candidate> GetCandidateOpenAIScreeningResults(int irfId, int candidateMasterId);
        Task<bool> SaveCandidateOpenAIScreeningResults(int irfId, int candidateMasterId, string screeningResponseJson);
        Task<IEnumerable<InterviewChannelDetails>> GetCandidateLatestStatusForApi(string skillChannelName, string candidateEmailId, string requestedByClientUser, string jobId);
        Task<IEnumerable<InterviewChannelDetails>> FetchInterviewChannelCandidateInformation(int irfid, int id, string requestedBy, DateTime? startDate, DateTime? endDate, string jobId);
        Task<IEnumerable<CandidateStatusWebHookData>> GetCandidateStatusForWebHook();
        Task<bool> DeleteDeliveredCandidateStatusForWebHook(List<int> data);
        Task<IEnumerable<CandidateTimelineVM>> GetAllCandidateSearchbyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<IEnumerable<CountryCodeDetails>> GetAllCountryCodeDetails();
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithColdDate(DateTime startDate, DateTime endDate, int clientId);
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithoutColdDate(DateTime startDate, DateTime endDate, int clientId);
    }
}
