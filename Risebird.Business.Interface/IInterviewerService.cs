using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerService
    {
        List<InterviewerProfileVM> GetInterviewers();
        //Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser);
        bool AddInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList, out int userId, out string uniqueid, bool? isApplicant = false);
        Exception LastAddProfileException { get; }
        //Task<bool> EditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser);
        bool EditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList, out string uniqueid);
        Task<bool> DeleteInterviewerProfile(int id);
        Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional);
        Task<bool> DeleteEmployerProfile(int id);

        //To retrieve the NickNames
        IEnumerable<string> GetNickNames(int UserId);
        IEnumerable<string> GetEmailIds(int UserId);
        IEnumerable<string> CheckEmailIdExists(string emailId);
        InterviewerProfileVM GetInterviewerProfileById(int userid);
        InterviewerProfileVM GetInterviewerMyProfileById(int userid);
        InterviewerProfessional GetEmployerById(int Id);
        List<LevelTech> GetLevelTech();
        List<ExperienceRange> GetExperienceRange();
        List<JobFunction> GetJobFunction();
        List<Industry> GetIndustry();
        Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile);
        Task<bool> SaveBankVerificationAsync(int interviewerProfileId, TransInterviewerAccDetails accountDetails, CashfreeBankVerificationResult verificationResult);
        Task<bool> SavePanVerificationAsync(int interviewerProfileId, string pan, CashfreePanVerificationResult verificationResult);
        IEnumerable<InterviewerInvites> GetInterviewerInvites(string emailId);
        List<Meeting> GetUnapprovedInterviewerScheduledInterviews(string emailID);
        Task<InterviewerProfileVM> GetInterviewerProfileByEmailId(string emailId, string profile);
        List<Meeting> GetOpenInterviewerAssessment(string emailId);
        List<Meeting> GetAssignedInterviewerAssessment(string emailID);
        string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        bool UnassignYourself(int meetingid, string email);
        Task<InterviewReport> GetInterviewerinteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewerReport(InterviewReport reportModel);
        IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        IEnumerable<AssignmentVM> GetInterviewerAssessment(string emailid);
        string AddAssessment(AssignmentVM _assessment);
        IEnumerable<AssignmentVM> FetchAssessmentGroups();
        string ChangeStatusAssessment(int assessmentid, int status);
        string UpdateAssessment(AssignmentVM _assessment);
        Task<IEnumerable<string>> GetAllTagsFromIRF();
        Task<InterviewReport> GetInterviewerRecommendationDetails(int Userid);
        Task<bool> InterviewerRecommendation(InterviewReport reportModel);
        Task<TransInterviewerAccDetails> fetchBankDetails(int interviewerid);
        Task<List<TransInterviewerAvailability>> fetchAvailDetails(int interviewerid);
        string GetUserRolebyEmailid(string Emailid);
        Task<List<InterviewerRating>> fetchClientFeedbackbyInterviewer(string emailid, string type);
        Task<List<CommonInterviewDetailsVM>> FetchAllInterviewerallUpcomingInterview(string type, int? irfId = null, int? agId = null);
        Task<List<string>> FetchAllPasswordResetDetails(int noofdays);
        Task<List<TdsCertificateReport>> FetchTdsCertificatesbyEmailid(string Emailid);
        bool AddTdsCertificates(TdsCertificateReport model);
        TdsCertificateReport FetchTdsCertificatebyid(int certificateid);
        Task<IEnumerable<IRFInterviewerMappingVM>> IRFInterviewerMappingByInterviewerid(string emailid);
        Task<IEnumerable<IRFInterviewerMembers>> GetAllInterviewerMembers();
        bool UpdateAllIntervieweravailbilityStatus(int status, string uniqueid);
        Task<List<CommonInterviewDetailsVM>> FetchAllStaffingAgencyAndClientHRwithTime(DateTime stdt, DateTime etdt);
        Task<IEnumerable<ClientFeedDataVM>> GetClientFeedForInterviewer(string interviewerEmail, DateTime? postWhichDate, int? interviewId);
        Task<bool> SendInviteCode(CodeInviteVM codeInviteVM, string loggedinUser);
        Task<IEnumerable<CodeInviteVM>> GetAllIntviteDetails();
        Task<CodeInviteReportVM> CheckCodeConsoleReport(string inviteid);
        Task<bool> AddCodeConsoleReport(CodeInviteReportVM model);
        Task<bool> AddCodeConsoleReportDetails(CodeInviteReportVM model);
        Task<bool> DeleteAllHistorybefore90days();
        Task<bool> AddInterviewerAssesssmentDetails(int Assessmentid, string emailid);
        Task<bool> UpdateInterviewerInterest(int status, string uniqueid);
        Task<bool> SaveInterviewerResume(InterviewerProfileVM model);
        Task<IEnumerable<SkillChannelDetailsForSME>> GetSkillChannelsAssignedToSME(string loggedInInterviewer);
        Task<IRFVm> GetSkillChannelDetailsForSMEByIdToUpdate(int interviewerProfileId, int irfId);
        Task<bool> UpdateSkillChannelDetailsBySME(IRFVm model, int interviewerProfileId);
        Task<bool> SaveSMEAudio(int irfId, int interviewerProfileId, string audioUrl);
        Task<IEnumerable<TransInterviewer>> GetSkillChannelInterviewersToVerify(int irfId);
        Task<bool> UpdateVerifiedInterviewers(IList<TransInterviewer> interviewers, string loggedInSME);
        Task<bool> CheckIfSME(string loggedInUser);
        Task<bool> IsSmeAssignedToIrfAsync(string loggedInUser, int irfId);
        Task<List<ClientIRFsFeedbackVM>> FetchClientIRFFeedbackList();
        Task<InterviewerProfileVM> GetInterviewerFlagDetails(string intervieweremailid);
        Task<bool> UpdateInterviewerFlag(string Emailid, bool? LeadInterviewer, bool? IsApproved, bool? IsInterviewerAccess, bool? IsBlocked);
        Task<IEnumerable<InterviewerStatsVM>> GetInterviewerStatsAndRatings(int? irfId = null, string clientEmailId = null);
        Task<bool> DeactivateSkillChannelInterviewer(int irfId, string interviewerEmail);
        Task<IEnumerable<StatsDetailsVM>> GetInterviewerStatsAndRatingsDetails(int interviewerUserId, string type, int? irfId = null, string clientEmailId = null);
        Task<bool> AddSuspiciousTypeService(SuspiciousInterviewerVM model);
        Task<bool> SaveInterviewerEmploymentVerification(InterviewerProfileVM _interviewer);
        Task<bool> UpdateVideoConfirmationStatus(int status, string Emailid);
        Task<InterviewerInsightsVM> GetInterviewerInsights(string Emailid, string IrfId);
        Task<IEnumerable<TransInterviewersReliabilityScoresVm>> GetReliabilityScoreDetails(string Emailid, string IrfId);
        Task<IEnumerable<InterviewerInsightsMarkersDetailsVM>> GetInterviewerInsightsMarkersDetails(string Emailid, string type, string Irfid);
        Task<bool> updateRedmarkerAcknowledge(string uniqueid);
        Task<bool> UpdateVideoKYCDetails(string videolink, string Emailid);
        Task<bool> SaveInterviewerCodedResume(InterviewerProfileVM model);
        Task<bool> SaveInterviewerResumesAndCodedResume(InterviewerProfileVM model);
        Task<bool> SaveInterviewerPreOnboardingVerdict(string emailId, string PrimarySpecialization, string screeningResponseJson, string loggedInUser);
        Task<SaveOperationResult> SaveInterviewerPreOnboardingVerdictV2(string emailId, string PrimarySpecialization, InterviewerResumeFitmentAnalysisSchema model, string loggedInUser);


        Task<UserList> UpdateInterviewerActiveStatus(int status, int userId, bool activateChannels = true);
        Task<IEnumerable<IRFVm>> GetApplicableSkillChannelsForInterviewerPostAdding(int userId);
        Task<IEnumerable<IRFVm>> GetApplicableSkillChannelsForApplicantUponEdit(int applicantId);
        Task<bool> AddTargetSkillChannelToInterviewerPreOnboarding(InterviewerProfileVM interviewerProfile);
        Task<InterviewerProfileVM> GetInterviewerPreOnboardingAIResponse(string emailId, string primarySpecialization);
        Task<IEnumerable<FLNConversionVM>> GetFLNConversionData(string loggedInUser, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> SubmitInterviewerApplication(InterviewerDirectApplicationVM model);
        Task<IEnumerable<InterviewerDirectApplicationVM>> GetInterviewerApplicants(string loggedInUser, DateTime? startDate = null, DateTime? endDate = null);
        Task<InterviewerDirectApplicationVM> GetApplicantById(int applicantId);
        Task<bool> SaveApplicantInterviewerAIVerdict(int id, string emailId, string PrimarySpecialization, string screeningResponseJson, string loggedInUser);
        Task<bool> SaveApplicantInterviewerAIVerdictV2(int id, InterviewerResumeFitmentAnalysisSchema model, string loggedInUser, string resumeLink = null);
        Task<InterviewerDirectApplicationVM> GetApplicantInterviewerAIEvaluation(int id);
        Task<bool> UpdateApplicantsEmploymentDetails(InterviewerDirectApplicationVM obj, string loggedInUser);

        Task<InterviewerDirectApplicationVM> InsertApplicantsRenewed(InterviewerDirectApplicationVM obj);
        Task<IEnumerable<FLNInterviewerAcknowledgementStatusVM>> GetFLNAcknowledgementStatus(string loggedInUser, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> UpdateInterviewerResume(int userId, string resumeLink, string loggedInUser);
        Task<bool> UpdateIRFInterviewerActiveStatus(int userId, bool status, int irfId, string loggedInUser, bool preserveAvailability = false, bool? isAvailable = null);
        Task<bool> UpdateInterviewerAvailabilityStatus(int userId, bool availabilityStatus, int irfId, string loggedInUser);
        Task<bool> UpdateInterviewerAcknowledgementStatus(string loggedInUser,string value);
        Task<bool> AddInternalInterviewerToIRFPostPositiveVerdict(InterviewerDirectApplicationVM model);
        Task<InterviewerExistsInSystemAndOrSkillChannelDTO> CheckIfInterviewerExistsInSystemAndOrSkillChannel(string emailId, int irfId);
        Task<bool> UpdateApplicantById(InterviewerDirectApplicationVM model, string loggedInUser);
        Task<bool> UpdateApplicantSkillChannelById(InterviewerDirectApplicationVM model, string loggedInUser);

        /// <summary>
        /// Inactivates an interviewer in the system and handles all related business rules
        /// </summary>
        /// <param name="interviewerId">The ID of the interviewer to inactivate</param>
        /// <param name="modifiedBy">Email of the user performing the action</param>
        /// <param name="comment">Reason for inactivating the interviewer</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> InactivateInterviewer(int interviewerId, string modifiedBy, string comment,bool blockinterviewer, string inactivationType = null);
        Task<bool> ReactivateInterviewer(int userId, string reactivatedBy);
        Task<bool> SaveApplicantErrorMessage(int id, string errorMessage);
        Task<bool> SaveExternalApplicantResume(int id, string resumeLink = null);
        Task<IEnumerable<InterviewerProfileVM>> GetHoldPaymentInterviewers();
    }
}
