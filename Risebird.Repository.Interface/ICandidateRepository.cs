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
        Task<bool> UpdateCandidateResumeInBulk(List<CandidateResume> resumeList, bool ismcqdb = false);
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
        Task<IEnumerable<CandidateResume>> GetCandidateResumesForParsing(bool ismcqdb = false);
        Task<IEnumerable<IRFCandidateReport>> FetchSkillChannelCandidateReprots(int irfId);
        Task<bool> SaveCandidateCodedResume(Candidate model);
        Task<List<MeetingVM>> GetColdCandidatesByClientRepository(string Email);
        Task<List<CandidateResume>> GetNotProcessedResumeByClientRepository(string Email);
        Task<IEnumerable<CandidateExcelData>> GetIRFCandidateStatsForExcelDownLoad(int irfId, string emailid, int projectid);
        Task<Candidate> GetCandidateOpenAIScreeningResults(int irfId, int candidateMasterId);
        Task<bool> SaveCandidateOpenAIScreeningResults(int irfId, int candidateMasterId, string screeningResponseJson);
        Task<IEnumerable<InterviewChannelDetails>> GetCandidateLatestStatusForApi(string skillChannelName, string candidateEmailId, string requestedByClientUser, string jobId);
        Task<IEnumerable<InterviewChannelDetails>> FetchInterviewChannelCandidateInformation(int irfid, int id, string requestedBy, DateTime? startDate, DateTime? endDate, string jobId);
        Task<IEnumerable<CandidateStatusWebHookData>> GetCandidateStatusForWebHook(bool isStaging = false);
        Task<bool> DeleteDeliveredCandidateStatusForWebHook(List<int> data, bool isStaging = false);
        Task<IEnumerable<CandidateTimelineVM>> GetAllCandidateSearchbyDateStatus(DateTime startDate, DateTime endDate, int datatype, string emailId = null);
        Task<IEnumerable<CountryCodeDetails>> GetAllCountryCodeDetails();
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithColdDate(DateTime startDate, DateTime endDate, int clientId);
        Task<IEnumerable<CandidateExcelData>> GetCandidateStatusDataWithoutColdDate(DateTime startDate, DateTime endDate, int clientId);
        Task<bool> UpdateCandidateAcknowledge(int interviewid, string interviewchecks);
        Task<AddCandidatesResponse> AddMCQSkillChannelCandidateList(List<CandidateQuickAddModel> candidateList);
        Task<Candidate> GetCandidateResumeSummaryById(int candidateId);
        Task<bool> SaveCandidateResumeSummaryAsJsonStringById(int candidateId, string resumeSummaryAsJsonString);
        Task<Candidate> GetDetailsForCandidateResumeScreeningByAI(int irfId, int candidateId);
        Task<bool> SaveCandidateOpenAIScreeningResultsV2(int irfId, int candidateMasterId, string screeningResponseJson, string verdict, int resumeScreeningScore);
        
        /// <summary>Gets a single CandidateResume by ID.</summary>
        Task<CandidateResume> GetCandidateResumeById(int candidateResumeId);
        
        /// <summary>Gets candidate info by resume ID after parsing.</summary>
        Task<CandidateResumeInfo> GetCandidateByResumeId(int candidateResumeId);
    }

    public class CandidateResumeInfo
    {
        public int CandidateResumeId { get; set; }
        public int? CandidateId { get; set; }
        public int? CandidateMasterId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
    }
}
