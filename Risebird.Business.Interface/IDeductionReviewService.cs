using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Deduction review (My Quality phase 2): interviewers request a review of one deduction under fixed rules;
    /// QC makes one final decision; the upheld rate per AI check decides what stays in auto-approval.
    /// </summary>
    public interface IDeductionReviewService
    {
        DeductionReviewSettings Settings { get; }

        /// <summary>Interviewer side. The email is the session email; never a request value.</summary>
        Task<CoachingActionResultVM> RequestReviewAsync(string sessionEmail, DeductionReviewRequestInputVM input);

        /// <summary>QC queue: closes overdue requests first, then open (oldest first), recent decisions, AI accuracy by check.</summary>
        Task<DeductionReviewQueueVM> GetQueueAsync(bool canManagePolicy);

        /// <summary>Stands / Reduce to n% / Refund. Final; logged in InterviewerActionLog.</summary>
        Task<CoachingActionResultVM> DecideAsync(string qcEmail, DeductionReviewDecisionInputVM input);

        /// <summary>Takes an AI check out of auto-approval, or puts it back.</summary>
        Task<CoachingActionResultVM> SetCheckPolicyAsync(string aiCheck, bool exclude, string modifiedBy);

        /// <summary>Timer: closes overdue requests (deduction stands) and emails the QC lead about them. Returns how many were reported.</summary>
        Task<int> ExpireAndNotifyLeadAsync();
    }
}
