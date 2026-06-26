using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerEmailAvailabilityRepository
    {
        Task InsertHistoryAsync(int recordId, string eventType, string fromValue, string toValue, string changedBy);
        Task<List<InterviewerEmailAvailabilityHistory>> GetHistoryAsync(int recordId);
        Task<bool> UpdateMagNameAsync(int id, string magName);
        Task<List<MagDropdownItem>> GetAllMasterAssessmentGroupsAsync();

        Task<int> InsertEmailAvailabilityAsync(InterviewerEmailAvailability record);

        Task<List<InterviewerEmailAvailabilityVM>> GetEmailAvailabilityAsync(
            string status, string senderEmail, string resumeFitment,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
            string source = null, bool? isAutoClosed = null, bool? isInterviewerActive = null,
            bool? recentlyAvailable = null);

        Task<InterviewerEmailAvailabilityVM> GetEmailAvailabilityByIdAsync(int id);

        Task<bool> UpdateEmailAvailabilityStatusAsync(int id, string status, string changedBy);

        Task<bool> UpdateEmailAvailabilityVerificationAsync(
            int id, bool? employmentVerified, bool? resumeAIAnalysisCompleted,
            string resumeAIAnalysisResult, string newStatus);

        Task<bool> AssignInterviewerToAssessmentGroupAsync(
            int emailAvailabilityId, int interviewerProfileId,
            int assessmentGroupId, string assignedBy);

        Task<bool> DeleteEmailAvailabilityAsync(int id);

        Task<bool> DropInterviewerProfileAsync(int interviewerProfileId, string modifiedBy);

        Task<bool> UpdateResumeLinkAsync(int id, string resumeLink);

        // Plivo call
        Task<bool> UpdatePlivoCallStatusAsync(int id, string callStatus, string conversationId,
            DateTime? callAt, int? duration, string transcript, string summary,
            string intent, bool? assignmentHelp, bool? emailReceived, bool? callbackRequested,
            string notes, string calledBy);
        Task<int?> FindByPhoneAndCallStatusAsync(string phone, string callStatus);

        // Batch call
        Task<(List<IABatchCallMagSummary> Items, int TotalCount)> GetMAGsWithCallableInterviewerCountsAsync(int page = 1, int pageSize = 20, string search = null);
        Task<List<IACallableInterviewer>> GetCallableInterviewersForMAGAsync(string magName, bool onlyUncalled = true);
        Task<int> InsertBatchCallLogAsync(string magName, string initiatedBy, int totalInterviewers, int? campaignId = null);
        Task CompleteBatchCallLogAsync(int id, int succeeded, int failed, int skipped, string status);
        Task<List<IABatchCallLogEntry>> GetBatchCallHistoryAsync(string magName);
        Task<(List<IAInterviewerCallOutcome> Items, int TotalCount)> GetInterviewerCallOutcomesForMAGAsync(string magName, int page = 1, int pageSize = 25);

        Task<int> EnsureEARecordForProfileInterviewerAsync(int interviewerProfileId, string magName, string source = null);
        Task<InterviewerEmailAvailabilityVM> GetProfileInterviewerContactAsync(int interviewerProfileId);
        Task<bool> SetUnifiedIntentAsync(int id, string intent, string intentSource, DateTime? confirmedAt);
        Task<bool> MarkAutoClosedAsync(int id, string autoClosedBy);

        // MAG lookup
        Task<string> GetMAGNameByAssessmentGroupIdAsync(int assessmentGroupId);

        // Centralized call history page
        Task<(List<IACallDisposition> Items, int TotalCount)> GetAllCallDispositionsAsync(
            int page, int pageSize, string magName, string intent, string callStatus,
            DateTime? dateFrom, DateTime? dateTo);
        Task<(List<IABatchCallLogEntry> Items, int TotalCount)> GetAllBatchCallLogsAsync(
            int page, int pageSize, string magName);
        Task<List<string>> GetDistinctCalledMagNamesAsync();

        // Campaign batch call (delayed Plivo calls for AG campaigns)
        Task<List<CampaignBatchCallEligible>> GetCampaignsEligibleForBatchCallAsync();
        // Operator-initiated (manual) trigger: resolves a campaign WITHOUT the 1-hour delay gate;
        // only an already-triggered batch call blocks it. Returns null if not found / already triggered.
        Task<CampaignBatchCallEligible> GetCampaignForManualBatchCallAsync(int campaignId);
        Task<List<CampaignUnrespondedInterviewer>> GetCampaignUnrespondedInterviewersAsync(int campaignId);
        Task MarkCampaignBatchCallTriggeredAsync(int campaignId);
    }

    public class IABatchCallMagSummary
    {
        public int TotalCount { get; set; }
        public string MagName { get; set; }
        public string AssessmentGroupName { get; set; }
        public int TotalInterviewers { get; set; }
        public int CallableCount { get; set; }
        public int AlreadyCalled { get; set; }
        public int UncalledCount { get; set; }
        public int Available { get; set; }
        public int NotAvailable { get; set; }
        public string LastBatchBy { get; set; }
        public DateTime? LastBatchAt { get; set; }
        public int? LastBatchCount { get; set; }
        public int? LastBatchSucceeded { get; set; }
        public string LastBatchStatus { get; set; }
        public int TotalBatches { get; set; }
    }

    public class IACallableInterviewer
    {
        public int Id { get; set; }
        public int? InterviewerProfileId { get; set; }
        public string InterviewerName { get; set; }
        public string SenderEmail { get; set; }
        public string Phone { get; set; }
        public decimal? ExperienceYears { get; set; }
        public string PlivoCallStatus { get; set; }
        public string Source { get; set; }
    }

    public class IABatchCallLogEntry
    {
        public int Id { get; set; }
        public string MagName { get; set; }
        public string InitiatedBy { get; set; }
        public DateTime InitiatedAt { get; set; }
        public int InterviewersAttempted { get; set; }
        public int InterviewersSucceeded { get; set; }
        public int InterviewersFailed { get; set; }
        public int InterviewersSkipped { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; }
    }

    public class IAInterviewerCallOutcome
    {
        public int Id { get; set; }
        public int? InterviewerProfileId { get; set; }
        public string InterviewerName { get; set; }
        public string SenderEmail { get; set; }
        public string Phone { get; set; }
        public decimal? Experience { get; set; }
        public string CurrentRole { get; set; }
        public string PlivoCallStatus { get; set; }
        public string PlivoIntent { get; set; }
        public bool? PlivoAssignmentHelp { get; set; }
        public bool? PlivoEmailReceived { get; set; }
        public bool? PlivoCallbackRequested { get; set; }
        public int? PlivoCallDuration { get; set; }
        public string PlivoCalledBy { get; set; }
        public string PlivoCallSummary { get; set; }
        public string PlivoNotes { get; set; }
        public DateTime? PlivoCallAt { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
    }

    public class IACallDisposition
    {
        public int TotalCount { get; set; }
        public int Id { get; set; }
        public int? InterviewerProfileId { get; set; }
        public string InterviewerName { get; set; }
        public string SenderEmail { get; set; }
        public string Phone { get; set; }
        public string MagName { get; set; }
        public string RecordStatus { get; set; }
        public string PlivoCallStatus { get; set; }
        public string PlivoConversationId { get; set; }
        public DateTime? PlivoCallAt { get; set; }
        public int? PlivoCallDuration { get; set; }
        public string PlivoCallTranscript { get; set; }
        public string PlivoCallSummary { get; set; }
        public string PlivoIntent { get; set; }
        public bool? PlivoAssignmentHelp { get; set; }
        public bool? PlivoEmailReceived { get; set; }
        public bool? PlivoCallbackRequested { get; set; }
        public string PlivoNotes { get; set; }
        public string PlivoCalledBy { get; set; }
        public decimal? Experience { get; set; }
        public string CurrentRole { get; set; }
        public string ResolvedPhone { get; set; }
    }

    public class CampaignBatchCallEligible
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; }
        public int AssessmentGroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MagName { get; set; }
    }

    public class CampaignUnrespondedInterviewer
    {
        public int InterviewerProfileId { get; set; }
        public string InterviewerName { get; set; }
        public string SenderEmail { get; set; }
        public string Phone { get; set; }
        public int UserId { get; set; }
    }
}
