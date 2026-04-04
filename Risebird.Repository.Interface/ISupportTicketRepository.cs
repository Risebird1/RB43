using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISupportTicketRepository
    {
        Task<int> InsertTicketAsync(SupportTicket ticket);

        Task<List<SupportTicketVM>> GetTicketsAsync(
            string   status      = null,
            string   intent      = null,
            string   senderEmail = null,
            DateTime? dateFrom   = null,
            DateTime? dateTo     = null,
            int      page        = 1,
            int      pageSize    = 25);

        Task<bool> UpdateTicketStatusAsync(int id, string status, string changedBy);

        Task<List<SupportTicketHistory>> GetTicketHistoryAsync(int ticketId);

        Task<List<string>> GetEligibleAssigneesAsync(int ticketId);

        Task<bool> UpdateTicketAssignmentAsync(int id, string assignedTo, string changedBy);

        Task<List<IrfMatchVM>> GetIRFsForSenderAsync(string senderEmail);

        /// <summary>Resolves an IRF by string IRFId column (Skill Channel name) if it exists and is valid.</summary>
        Task<IrfMatchVM> GetIRFByStringIdAsync(string irfStringId);

        Task<int> InsertCandidateResumeAsync(int irfId, string resumeLink, int addedBy, string clientHrEmailId, int? supportTicketId = null);

        /// <summary>Returns tickets that were assigned ONLY to "System" and never to any human HR.</summary>
        Task<List<SupportTicketVM>> GetSystemAssignedTicketsAsync(int page = 1, int pageSize = 25);

        /// <summary>Returns tickets that were assigned to BOTH "System" AND at least one human HR.</summary>
        Task<List<SupportTicketVM>> GetHybridTicketsAsync(int page = 1, int pageSize = 25);

        /// <summary>
        /// Auto-maps a client user to a matching IRF when no existing mapping exists.
        /// Creates a TransClientUser entry if a matching IRF is found for the user's client.
        /// </summary>
        Task<IrfMatchVM> AutoMapUserToIRFAsync(string senderEmail, string jobTitle, string specialization = null);

        /// <summary>
        /// Resolves the RisebirdHR email for a sender based on their client's existing IRF mappings.
        /// Does not require matching a specific IRF - just finds an HR who handles this client.
        /// </summary>
        Task<string> GetHRForSenderAsync(string senderEmail);

        /// <summary>
        /// Finds the best matching IRF for a sender's client based on job title/specialization.
        /// Works even if user already has existing mappings (unlike AutoMapUserToIRFAsync).
        /// Use when existing mappings don't match well and we need to find a better fit.
        /// </summary>
        Task<IrfMatchVM> FindBestIRFForClientAsync(string senderEmail, string jobTitle, string specialization = null, bool createMapping = true);

        /// <summary>
        /// Inserts a candidate resume with auto-scheduling metadata for email-based scheduling flow.
        /// </summary>
        Task<int> InsertCandidateResumeForAutoScheduleAsync(
            int irfId,
            string resumeLink,
            int addedBy,
            string clientHrEmailId,
            int? supportTicketId,
            string candidateName,
            string candidateEmail,
            DateTime? scheduledInterviewDateTime);

        /// <summary>
        /// Gets candidate resume records with auto-schedule data for a support ticket.
        /// Returns both extracted (from email) and parsed (from resume) data.
        /// </summary>
        Task<List<AutoScheduleCandidateVM>> GetAutoScheduleCandidatesForTicketAsync(int ticketId);

        /// <summary>
        /// Updates the HasAutoScheduleCandidates flag for a ticket.
        /// </summary>
        Task<bool> SetTicketHasAutoScheduleCandidatesAsync(int ticketId, bool value);

        /// <summary>
        /// Inserts a pending resume record for HR review.
        /// </summary>
        Task<int> InsertPendingResumeAsync(
            int supportTicketId,
            string resumeUrl,
            string candidateName,
            string candidateEmail,
            int? resolvedIRFId,
            string resolvedIRFStringId,
            string resolvedJobTitle,
            string resolvedHREmail,
            bool hasSchedulingData,
            DateTime? scheduledInterviewDateTime,
            string scheduleTimezone);

        /// <summary>
        /// Gets all pending resumes for a support ticket.
        /// </summary>
        Task<List<PendingResumeVM>> GetPendingResumesForTicketAsync(int ticketId);

        /// <summary>
        /// Gets a single pending resume by ID.
        /// </summary>
        Task<PendingResumeVM> GetPendingResumeByIdAsync(int pendingResumeId);

        /// <summary>
        /// Updates a pending resume's status after HR action.
        /// </summary>
        Task<bool> UpdatePendingResumeStatusAsync(int pendingResumeId, string status, int? ingestedResumeId, string processedBy);

        /// <summary>
        /// Checks if a ticket still has any pending resumes and updates HasPendingResumes flag.
        /// </summary>
        Task RefreshTicketPendingResumeFlagAsync(int ticketId);

        /// <summary>
        /// Gets all active IRFs for a client (by sender email) for the IRF dropdown.
        /// </summary>
        Task<List<IRFOptionVM>> GetIRFsForClientAsync(string senderEmail);

        /// <summary>
        /// Updates the resolved IRF for a pending resume (when HR overrides system's choice).
        /// Tracks the change for accuracy analysis.
        /// </summary>
        Task<bool> UpdatePendingResumeIRFAsync(int pendingResumeId, int newIRFId, string changedBy, string reason);

        /// <summary>
        /// Gets counts for all three tabs in a single optimized query.
        /// Returns (ManualReviewCount, SystemOnlyCount, HybridCount).
        /// </summary>
        Task<(int ManualReviewCount, int SystemOnlyCount, int HybridCount)> GetTicketCountsAsync();

        /// <summary>
        /// Gets the RisebirdHr user ID for a given IRF.
        /// </summary>
        Task<int> GetHRUserIdForIRFAsync(int irfId);

        /// <summary>
        /// Gets a fallback system user ID (for AddedBy when no HR found).
        /// </summary>
        Task<int> GetSystemUserIdAsync();

        /// <summary>
        /// Gets the tenant ID for a given IRF (from the associated client).
        /// </summary>
        Task<string> GetTenantIdForIRFAsync(int irfId);

        /// <summary>
        /// Logs a checkpoint during resume processing for debugging.
        /// </summary>
        Task<int> LogProcessingCheckpointAsync(
            int? supportTicketId,
            int? pendingResumeId,
            int? candidateResumeId,
            string checkpoint,
            string status,
            string message = null,
            string details = null,
            string processedBy = null,
            int? durationMs = null);

        /// <summary>
        /// Gets processing logs for a ticket or pending resume.
        /// </summary>
        Task<List<ProcessingLogVM>> GetProcessingLogsAsync(int? supportTicketId = null, int? pendingResumeId = null, int limit = 100);
    }
}
