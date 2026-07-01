using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITdsRepository
    {
        Task<IList<InterviewerQuarterlyTdsRow>> FetchInterviewerQuarterlyTdsAsync(DateTime startDate, DateTime endDate);

        Task<IList<PaymentHistoryItem>> FetchInterviewerPaymentItemsAsync(DateTime startDate, DateTime endDate);

        Task<IList<WeeklyPayoutVM>> FetchWeeklyPayoutsAsync(string financialYear, string quarter);

        Task<IList<TdsChallanVM>> FetchTdsChallansAsync(string financialYear, string quarter);

        Task<int> AddOrUpdateTdsChallanAsync(TdsChallanVM vm, string operatorEmail);

        Task<IList<ChallanInterviewerVM>> FetchChallanInterviewersAsync(int challanId);

        Task DeleteTdsChallanAsync(int id);

        Task<TdsReturnFilingVM> FetchTdsReturnFilingAsync(string financialYear, string quarter);

        Task AddOrUpdateTdsReturnFilingAsync(TdsReturnFilingVM vm, string operatorEmail);

        Task<InterviewerPanInfo> FetchInterviewerByPanAsync(string pan);

        Task<IList<InterviewerChallanLink>> FetchInterviewerChallanNumbersAsync(string financialYear, string quarter);

        Task<IList<PendingTdsWeekVM>> FetchPendingTdsByWeekAsync(string financialYear, string quarter);

        Task<IList<UnverifiedWalletInterviewer>> FetchUnverifiedWalletInterviewersAsync(DateTime asOfDate);

        /// <summary>
        /// Logs a reminder if not already sent; returns true if inserted (not a duplicate).
        /// </summary>
        Task<bool> LogLapseReminderAsync(int userid, string paymentReference, int reminderIndex, string emailTo);

        /// <summary>
        /// Writes a Form16A certificate link to InterviewerCertificates (the existing TDS cert store).
        /// Matches usp_AddCertificates signature: @Emailid, @year, @month, @clink, @comment, @id=0.
        /// </summary>
        Task AddForm16CertificateAsync(string emailId, string financialYear, string quarter, string blobUrl);
    }
}
