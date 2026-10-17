using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITrialJudgmentRepository
    {
        Task<TrialJudgmentContextVm> GetContextAsync(int interviewId);

        Task<(bool Inserted, int TrialFlagId)> InsertTrialFlagAsync(TrialJudgmentResultVm judgment, TrialJudgmentContextVm context, int interviewId);

        Task<IReadOnlyList<TrialFlagBackfillCandidateVm>> GetBackfillCandidatesAsync();

        Task<(QcInterviewAIMergeAssistantResponse MergeResult, int IrfId)?> GetStoredMergeResultAsync(int interviewId);

        /// <summary>Stamps one "Backfill trial flags" run with who ran it (dbo.TrialFlagBackfillRun; QC Activity TrialBackfill).</summary>
        Task RecordBackfillRunAsync(string executedBy, int? interviewId, int inserted, int skipped, int failed, string message);
    }
}
