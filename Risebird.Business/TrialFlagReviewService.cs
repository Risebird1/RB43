using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TrialFlagReviewService : ITrialFlagReviewService
    {
        private readonly ITrialFlagReviewRepository _repository;
        private readonly IMeetingService _meetingService;

        public TrialFlagReviewService(ITrialFlagReviewRepository repository, IMeetingService meetingService)
        {
            _repository = repository;
            _meetingService = meetingService;
        }

        public async Task<TrialFlagReviewPageVm> GetPageAsync(string tab)
        {
            var normalizedTab = NormalizeTab(tab);
            var (tableExists, totalRows) = await _repository.GetDiagnosticsAsync();
            var allRows = await _repository.GetTrialFlagsAsync(null);

            var pendingCount = allRows.Count(x => ReviewStatusMatches(x.ReviewStatus, TrialFlagConstants.ReviewPending));
            var keptCount = allRows.Count(x => ReviewStatusMatches(x.ReviewStatus, TrialFlagConstants.ReviewKept));
            var removedCount = allRows.Count(x => ReviewStatusMatches(x.ReviewStatus, TrialFlagConstants.ReviewRemoved));
            var filteredRows = allRows.Where(x => ReviewStatusMatches(x.ReviewStatus, normalizedTab)).ToList();

            return new TrialFlagReviewPageVm
            {
                ActiveTab = normalizedTab,
                Rows = filteredRows,
                PendingCount = pendingCount,
                KeptCount = keptCount,
                RemovedCount = removedCount,
                TrialFlagsTableExists = tableExists,
                TotalRowsInTable = totalRows,
                EmptyStateHint = BuildEmptyStateHint(tableExists, totalRows, allRows.Count, filteredRows.Count, normalizedTab, pendingCount, keptCount, removedCount)
            };
        }

        private static bool ReviewStatusMatches(string actual, string expected) =>
            string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static string BuildEmptyStateHint(
            bool tableExists,
            int totalRows,
            int loadedRows,
            int filteredRows,
            string activeTab,
            int pendingCount,
            int keptCount,
            int removedCount)
        {
            if (!tableExists)
                return "dbo.TrialFlags does not exist on this database. Run Risebird.DB/Tables/TrialFlags.sql, then redeploy usp_GetTrialFlagsForReview.";

            if (totalRows == 0)
                return "dbo.TrialFlags exists but has 0 rows. Rows are inserted only when trial judgment runs after QC AI merge on a first completed interview. If merges already finished before the producer was deployed, run POST /Dashboard/BackfillTrialFlags (or redeploy Azure Functions so future merges insert automatically). See Risebird.DB/HowTo_DiagnoseTrialFlagReview.sql.";

            if (loadedRows == 0 && totalRows > 0)
                return "TrialFlags has rows but usp_GetTrialFlagsForReview returned none. Redeploy the latest stored procedure on this database.";

            if (filteredRows == 0)
            {
                if (activeTab == TrialFlagConstants.ReviewPending && pendingCount == 0 && (keptCount > 0 || removedCount > 0))
                    return $"No pending flags. {keptCount} kept and {removedCount} removed — switch tabs above.";
                if (activeTab == TrialFlagConstants.ReviewKept && keptCount == 0)
                    return $"No kept flags. Pending: {pendingCount}, Removed: {removedCount}.";
                if (activeTab == TrialFlagConstants.ReviewRemoved && removedCount == 0)
                    return $"No removed flags. Pending: {pendingCount}, Kept: {keptCount}.";
                return "Rows exist but none match this tab. Check ReviewStatus values in TrialFlags (expected: pending, kept, removed).";
            }

            return null;
        }

        public async Task<TrialFlagReviewActionResultVm> KeepOnIrfAsync(int trialFlagId, string reviewerEmail, string comment)
        {
            var row = await _repository.GetTrialFlagByIdAsync(trialFlagId);
            if (row == null)
                return Fail("Trial flag not found.");

            if (!string.Equals(row.ReviewStatus, TrialFlagConstants.ReviewPending, StringComparison.OrdinalIgnoreCase))
                return Fail("This trial flag was already reviewed.");

            var ok = await _repository.UpdateReviewAsync(trialFlagId, TrialFlagConstants.ReviewKept, reviewerEmail, comment);
            return ok
                ? Success("Interviewer kept on IRF.")
                : Fail("Could not save review decision. Refresh and try again.");
        }

        public async Task<TrialFlagReviewActionResultVm> RemoveFromIrfAsync(int trialFlagId, string reviewerEmail, string comment)
        {
            var row = await _repository.GetTrialFlagByIdAsync(trialFlagId);
            if (row == null)
                return Fail("Trial flag not found.");

            if (!string.Equals(row.ReviewStatus, TrialFlagConstants.ReviewPending, StringComparison.OrdinalIgnoreCase))
                return Fail("This trial flag was already reviewed.");

            var interviewerKey = !string.IsNullOrWhiteSpace(row.InterviewerEmail)
                ? row.InterviewerEmail.Trim()
                : row.InterviewerName?.Trim();
            if (string.IsNullOrWhiteSpace(interviewerKey))
                return Fail("Interviewer email is missing for this trial flag.");

            var removed = await _meetingService.RemoveOrShortlistInterviewerFromIRF(
                row.IrfId,
                interviewerKey,
                "Rejected");
            if (!removed)
                return Fail("Could not remove interviewer from IRF. Try again or use DM Approval.");

            var ok = await _repository.UpdateReviewAsync(trialFlagId, TrialFlagConstants.ReviewRemoved, reviewerEmail, comment);
            if (!ok)
                return Fail("Interviewer was removed from IRF, but the trial flag could not be marked reviewed.");

            return Success("Interviewer removed from IRF.");
        }

        private static string NormalizeTab(string tab)
        {
            if (string.Equals(tab, TrialFlagConstants.ReviewKept, StringComparison.OrdinalIgnoreCase))
                return TrialFlagConstants.ReviewKept;
            if (string.Equals(tab, TrialFlagConstants.ReviewRemoved, StringComparison.OrdinalIgnoreCase))
                return TrialFlagConstants.ReviewRemoved;
            return TrialFlagConstants.ReviewPending;
        }

        private static TrialFlagReviewActionResultVm Success(string message) =>
            new TrialFlagReviewActionResultVm { Ok = true, Message = message };

        private static TrialFlagReviewActionResultVm Fail(string message) =>
            new TrialFlagReviewActionResultVm { Ok = false, Message = message };
    }
}
