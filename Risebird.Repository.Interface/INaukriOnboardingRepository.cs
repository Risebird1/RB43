using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface INaukriOnboardingRepository
    {
        Task InsertHistoryAsync(int recordId, string eventType, string fromValue, string toValue, string changedBy);
        Task<List<NaukriOnboardingHistory>> GetHistoryAsync(int recordId);

        Task<int> InsertAsync(NaukriProspectiveInterviewer record);

        Task<List<NaukriProspectiveInterviewerVM>> GetListAsync(
            string status, string candidateName, string magName, string source,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);

        Task<NaukriProspectiveInterviewerVM> GetByIdAsync(int id);

        Task<bool> UpdateStatusAsync(int id, string status, string changedBy);

        Task<(bool isExisting, int? existingUserId, int? existingProfileId)> UpdateResumeParseResultAsync(
            int id, string parsedEmail, string parsedPhone);

        Task<bool> UpdateEmploymentVerificationAsync(
            int id, bool? verified, string companyName, bool? isEmployed, string details);

        Task<bool> UpdateAIAnalysisAsync(
            int id, bool completed, string analysisResult, string verdict, int? score, string newStatus);

        Task<bool> UpdateOnboardingResultAsync(
            int id, int? userId, int? profileId, int? assessmentGroupId, bool ackSent);

        Task<bool> UpdateAckEmailSentAsync(int id);

        Task<bool> DeleteAsync(int id);

        Task<int?> GetInterviewerProfileIdByUserIdAsync(int userId);

        Task<bool> UpdateResumeLinkAsync(int id, string resumeLink);

        Task<List<MagDropdownItem>> GetAllMasterAssessmentGroupsAsync();

        Task<dynamic> SaveManualOverrideAsync(int id, string fieldName, string oldValue, string newValue, string changedBy);

        Task<bool> UpdateSelectedIRFAsync(int id, int irfId, string irfName, string applicationLink, int? assessmentGroupId);
        Task<bool> UpdateInvitationEmailSentAsync(int id);
        Task<bool> UpdateProspectResponseAsync(int id, string response);
        Task<List<ActiveIRFItem>> GetActiveIRFsByAssessmentGroupIdAsync(int assessmentGroupId);
        Task<bool> UpdateSelfRegistrationAsync(int id, bool completed, int? userId, int? profileId);

        Task<(int? UserId, int? ProfileId)> FindUserByEmailAsync(string email);

        Task<bool> UpdatePlivoCallStatusAsync(int id, string callStatus, string conversationId,
            DateTime? callAt, int? duration, string transcript, string summary,
            string disposition, string onboardingPreference, bool? kycAccepted, string calledBy);

        Task<int?> FindProspectIdByPhoneAndCallStatusAsync(string phone, string callStatus);

        // Automation
        Task<List<NaukriProspectiveInterviewerVM>> GetEligibleProspectsForAutomationAsync(int batchSize = 2);
        Task<bool> UpdateAutomationStatusAsync(int id, string automationStatus, bool incrementCallAttempt = false);
        Task<int?> GetLatestActiveIRFForAssessmentGroupAsync(int assessmentGroupId);
        Task<bool> UpdateMagNameAsync(int id, string magName);
    }

    public class ActiveIRFItem
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string AssessmentGroupId { get; set; }
        public string ClientName { get; set; }
    }

    public class MagDropdownItem
    {
        public int MagId { get; set; }
        public string MagName { get; set; }
        public int AssessmentGroupId { get; set; }
        public string AssessmentGroupName { get; set; }
    }
}
