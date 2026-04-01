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
    }
}
