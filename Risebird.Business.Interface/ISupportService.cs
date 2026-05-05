using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ISupportService
    {
        Task<List<Support>> GetAllSupportHR(string emailId = null);
        Task<bool> AddSupportHR(Support hrDetails);
        Task<bool> UpdateSupportHR(Support hrDetails);
        Task<Support> GetSupportHR(int id);
        Task<bool> DeleteSupportHR(int id);
        Task<bool> SaveRaisedIssueDetails(RaiseBugsVM model);
        Task<IEnumerable<RaiseBugsVM>> GetRaisedIssueDetails(string profileName);
        Task<bool> UpdateRaisedRequestStatus(RaiseBugsVM model);

        // My Tickets / History
        Task<IEnumerable<RaiseBugsVM>> GetMyRaisedIssues(string requestedBy);
        Task<RaiseBugsVM> GetRaisedIssueById(int id);
        Task<bool> ReopenRaisedIssue(int id, string comment, string actionBy, string actionByRole);
        Task<IEnumerable<RaisedIssueHistoryVM>> GetRaisedIssueHistory(int raisedIssueId);

        // AI-driven Payment ticket review
        Task<IEnumerable<PaymentTicketReviewVM>> GetOpenPaymentTicketsForReview();
        Task<bool> SaveRaisedIssueAIResponse(int ticketId, string aiResponseJson, string statusComment,
            string closedBy, System.DateTime closedOn, bool closeTicket);
        Task<bool> UpdateQCPenaltyFromAIReview(int interviewId, int revisedPenaltyPct,
            string updatedMergedDetails, string reasoningComment);
    }
}
