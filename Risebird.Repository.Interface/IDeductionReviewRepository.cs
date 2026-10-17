using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>Deduction review requests (My Quality phase 2) and the QC review queue.</summary>
    public interface IDeductionReviewRepository
    {
        /// <summary>usp_SaveDeductionReviewRequest. Returns (Result, RequestId); see the procedure for codes.</summary>
        Task<(int Result, int? RequestId)> SaveRequestAsync(string email, int interviewId, string reasonCategory, string note,
            DeductionReviewSettings settings, DateTime windowStart, DateTime dueAtUtc);

        /// <summary>usp_ExpireDeductionReviewRequests. With returnUnnotified, the expired requests not yet told to the QC lead.</summary>
        Task<IReadOnlyList<DeductionReviewExpiredRowVM>> ExpireAsync(bool returnUnnotified);

        /// <summary>usp_MarkDeductionReviewLeadNotified.</summary>
        Task MarkLeadNotifiedAsync(IEnumerable<int> ids);

        /// <summary>usp_GetDeductionReviewQueue: open (oldest first), recent decisions, stats per AI check.</summary>
        Task<(IReadOnlyList<DeductionReviewQueueRowVM> Open, IReadOnlyList<DeductionReviewQueueRowVM> Decided, IReadOnlyList<DeductionReviewCheckStatVM> Checks)>
            GetQueueAsync(int recentDays);

        /// <summary>usp_DecideDeductionReviewRequest.</summary>
        Task<DeductionReviewDecideResultVM> DecideAsync(int requestId, string decision, int? newPct, string note, string qcEmail);

        /// <summary>usp_SetDeductionReviewCheckPolicy.</summary>
        Task SetCheckPolicyAsync(string aiCheck, bool exclude, string modifiedBy);
    }
}
