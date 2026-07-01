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
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
            string automationStatus = null, string callStatus = null, string disposition = null,
            string funnelStage = null, string onboardingType = null);

        Task<Dictionary<string, int>> GetFunnelStageCountsAsync(
            string status, string candidateName, string magName, string source,
            DateTime? dateFrom, DateTime? dateTo,
            string automationStatus = null, string callStatus = null, string disposition = null,
            string funnelStage = null, string onboardingType = null);

        Task<Dictionary<string, int>> GetFunnelReachCountsAsync(
            string status, string candidateName, string magName, string source,
            DateTime? dateFrom, DateTime? dateTo,
            string automationStatus = null, string callStatus = null, string disposition = null,
            string funnelStage = null, string onboardingType = null);

        // SendGrid delivery status of the invitation email for a recipient (null if none sent yet).
        Task<NaukriInvitationEmailStatus> GetInvitationEmailStatusAsync(string email);

        // SendGrid delivery + engagement events for BOTH the invitation and acknowledgement emails
        // sent to a recipient (summaries + full event timeline).
        Task<NaukriProspectEmailEvents> GetProspectEmailEventsAsync(string email);

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

        // Prospect Onboarding redesign (Phase 2): unified intent + funnel/ticket separation + onboarding type
        Task<bool> SetUnifiedIntentAsync(int id, string intent, string intentSource, DateTime? confirmedAt);
        Task<bool> AdvanceFunnelStageAsync(int id, string funnelStage, string ticketStatus = null);
        Task<bool> SetFunnelOutcomeAsync(int id, string funnelStage, string ticketStatus = null);
        Task<bool> SetOnboardingTypeAsync(int id, string onboardingType);

        // Returns the prospect record awaiting acknowledgement for a given onboarded user id, if any.
        Task<int?> FindProspectIdByOnboardedUserIdAsync(int userId);

        Task<(int? UserId, int? ProfileId)> FindUserByEmailAsync(string email);

        // Resolves an interviewer account by email REGARDLESS of active status (also returns IsActive).
        // Used by the onboarding "adopt existing interviewer" path to find accounts that exist but are
        // inactive (which usp_FindActiveUserByEmail / FindUserByEmailAsync would exclude).
        Task<(int? UserId, int? ProfileId, bool? IsActive)> FindInterviewerByEmailAnyStatusAsync(string email);

        // Maps an EXISTING interviewer to an Assessment Group (appends the AG id to the profile and adds
        // the AG's skill channels as available-only / inactive) WITHOUT rewriting the profile's fields.
        // Used to adopt an existing-but-inactive interviewer into a Naukri MAG during onboarding.
        Task<bool> MapInterviewerToAssessmentGroupChannelsAsync(int profileId, int userId, int assessmentGroupId, string assignedBy);

        Task<bool> UpdatePlivoCallStatusAsync(int id, string callStatus, string conversationId,
            DateTime? callAt, int? duration, string transcript, string summary,
            string disposition, string onboardingPreference, bool? kycAccepted, string calledBy);

        Task<int?> FindProspectIdByPhoneAndCallStatusAsync(string phone, string callStatus);

        // Automation
        Task<List<NaukriProspectiveInterviewerVM>> GetEligibleProspectsForAutomationAsync(int batchSize = 2);
        Task<bool> UpdateAutomationStatusAsync(int id, string automationStatus, bool incrementCallAttempt = false);

        // Priority intent-collection push (parallel automation service)
        Task<List<NaukriPriorityMag>> GetPriorityMagsAsync();
        Task<PrioritySetResult> SetPriorityMagAsync(int assessmentGroupId, string magName, string setBy, int maxPriority = 2);
        Task<bool> ClearPriorityMagAsync(int assessmentGroupId);
        Task<List<NaukriProspectiveInterviewerVM>> GetPriorityIntentCallProspectsAsync(int batchSize = 5, int cooldownMinutes = 15);
        Task<bool> RecordPriorityCallAttemptAsync(int id);
        Task<int?> GetLatestActiveIRFForAssessmentGroupAsync(int assessmentGroupId);
        Task<bool> UpdateMagNameAsync(int id, string magName);
        Task<bool> UpdateParsedEmailAsync(int id, string email);
        Task<bool> UpdateParsedPhoneAsync(int id, string phone);

        // Batch call
        Task<List<BatchCallMagSummary>> GetMAGsWithCallableProspectCountsAsync();
        Task<List<NaukriProspectiveInterviewerVM>> GetCallableProspectsForMAGAsync(string magName, bool onlyUncalled = true);
        Task<int> InsertBatchCallLogAsync(string magName, string initiatedBy, int totalProspects);
        Task CompleteBatchCallLogAsync(int id, int succeeded, int failed, int skipped, string status);
        Task<List<BatchCallLogEntry>> GetBatchCallHistoryAsync(string magName);
        Task<(List<NaukriProspectiveInterviewerVM> Items, int TotalCount)> GetProspectCallOutcomesForMAGAsync(string magName, int page = 1, int pageSize = 25);
    }

    public class BatchCallMagSummary
    {
        public string MagName { get; set; }
        public string AssessmentGroupName { get; set; }
        public int TotalProspects { get; set; }
        public int CallableCount { get; set; }
        public int AlreadyCalled { get; set; }
        public int UncalledCount { get; set; }
        public int Interested { get; set; }
        public int NotInterested { get; set; }
        public string LastBatchBy { get; set; }
        public DateTime? LastBatchAt { get; set; }
        public int? LastBatchCount { get; set; }
        public int? LastBatchSucceeded { get; set; }
        public string LastBatchStatus { get; set; }
        public int TotalBatches { get; set; }
    }

    public class BatchCallLogEntry
    {
        public int Id { get; set; }
        public string MagName { get; set; }
        public string InitiatedBy { get; set; }
        public DateTime InitiatedAt { get; set; }
        public int ProspectsAttempted { get; set; }
        public int ProspectsSucceeded { get; set; }
        public int ProspectsFailed { get; set; }
        public int ProspectsSkipped { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; }
    }

    public class ActiveIRFItem
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string AssessmentGroupId { get; set; }
        public string ClientName { get; set; }
    }

    public class NaukriInvitationEmailStatus
    {
        public string Email { get; set; }
        public DateTime? SentAt { get; set; }
        public string MailSubject { get; set; }
        public string DeliveryEvent { get; set; }   // processed | delivered | deferred | bounce | dropped
        public string DeliveryReason { get; set; }
        public string DeliveryStatus { get; set; }
        public string DeliveryResponse { get; set; }
        public bool HasBounced { get; set; }
        public bool Opened { get; set; }
        public bool Clicked { get; set; }
    }

    // Per-email SendGrid delivery summary (one per email type that was actually sent).
    public class NaukriEmailSummary
    {
        public string EmailType { get; set; }       // Invitation | Acknowledgement
        public string MailSubject { get; set; }
        public DateTime? SentAt { get; set; }
        public string DeliveryEvent { get; set; }   // processed | delivered | deferred | bounce | dropped
        public string DeliveryReason { get; set; }
        public string DeliveryStatus { get; set; }
        public string DeliveryResponse { get; set; }
        public bool HasBounced { get; set; }
        public bool Opened { get; set; }
        public bool Clicked { get; set; }
    }

    // A single SendGrid event row (timeline).
    public class NaukriEmailEvent
    {
        public string EmailType { get; set; }
        public string Event { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public string Url { get; set; }
        public DateTime? EventAt { get; set; }
    }

    // Combined SendGrid events for a prospect's invitation + acknowledgement emails.
    public class NaukriProspectEmailEvents
    {
        public List<NaukriEmailSummary> Summaries { get; set; } = new List<NaukriEmailSummary>();
        public List<NaukriEmailEvent> Events { get; set; } = new List<NaukriEmailEvent>();
    }

    public class MagDropdownItem
    {
        public int MagId { get; set; }
        public string MagName { get; set; }
        public int AssessmentGroupId { get; set; }
        public string AssessmentGroupName { get; set; }
    }

    // A MAG currently flagged as priority for today's priority-intent-collection push.
    public class NaukriPriorityMag
    {
        public int Id { get; set; }
        public int AssessmentGroupId { get; set; }
        public string MagName { get; set; }
        public string SetBy { get; set; }
        public DateTime SetAt { get; set; }
    }

    // Outcome of attempting to flag a MAG as priority.
    public class PrioritySetResult
    {
        // 'Set', 'AlreadySet', or 'LimitReached'.
        public string Status { get; set; }
        // Number of MAGs currently active as priority for today.
        public int PriorityCount { get; set; }
    }
}
