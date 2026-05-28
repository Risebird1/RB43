using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
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
        Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId, string emailid = null);
        Task<bool> DeleteCandidateResume(int id);
        Task<IEnumerable<string>> GetCandidateStaffingAgencyAndClientHRByInterviewId(int interviewId);
        Task<IEnumerable<CandidateTimelineVM>> GetCandidateTimelinebyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<FitmentReportModel> GetFitmentReportReport(int candidateId, int irfId);
        Task<bool> AddEditFitmentReport(FitmentReportModel model);
        Task<bool> ProcessCandidateResume(int id);
        Task<string> FetchCandidateResumeById(int id);
        Task<string> FetchBillingownerById(int id);
        Task<InterviewUsersDetails> FetchChannelOwnerByInterviewid(int interviewid);
        Task<IEnumerable<CandidateCommunicationDetailsVM>> GetCandidateCommunicationDetails(int communicationMedium, int? interviewId = null, int? irfId = null, string emailId = null);
        Task<CandidateIRFDetailsVM> GetIRFDetailsForCandidate(string emailId, int irfId);
        Task<int> GetuniqueCandidateByEmailId(string emailId, string id);
        Task<string> GetCandidateUniqueId(string emailId);
        Task<Candidate> GetCandidateIRFMailDetails(string emailId, int irfId);
        Task<IEnumerable<IRFCandidateReport>> FetchSkillChannelCandidateReprots(int irfId);
        Task<bool> SaveCandidateCodedResume(Candidate model);
        Task<List<MeetingVM>> GetColdCandidatesListByClient(string Email);
        Task<List<CandidateResume>> GetNotProcessedResumeByClient(string Email);
        Task<IEnumerable<CandidateExcelData>> GetIRFCandidateStatsForExcelDownLoad(int irfId, string emailid, int projectid);
        Task<Candidate> GetCandidateOpenAIScreeningResults(int irfId, int candidateMasterId);
        Task<bool> SaveCandidateOpenAIScreeningResults(int irfId, int candidateMasterId, string screeningResponseJson);
        Task<IEnumerable<InterviewChannelDetails>> FetchInterviewChannelCandidateInformation(int irfid, int id, string requestedBy, DateTime? startDate, DateTime? endDate, string jobId);
        Task<IEnumerable<CandidateTimelineVM>> GetAllCandidateSearchbyDateStatus(DateTime startDate, DateTime endDate, int dataType, string emailId = null);
        Task<IEnumerable<CountryCodeDetails>> GetAllCountryCodeDetails();
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithColdDate(DateTime startDate, DateTime endDate, int clientId);
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithoutColdDate(DateTime startDate, DateTime endDate, int clientId);
        Task<bool> UpdateCandidateAcknowledge(int interviewid, string interviewchecks);
        Task<ResumeSummaryVM> CandidateResumeSummary(int candidateId);
        Task<ResumeSummaryVM> ResumeSummaryFromLink(string resumeLink);
        Task<Candidate> GetDetailsForCandidateResumeScreeningByAI(int irfId, int candidateMasterId);
        Task<bool> SaveCandidateOpenAIScreeningResultsV2(int irfId, int candidateMasterId, string screeningResponseJson, string verdict, int resumeScreeningScore);
    }
}
