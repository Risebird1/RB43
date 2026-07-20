using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IWalletService
    {
        Task<List<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null);
        Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionRefNumber = null, string invoicePDF = null, DateTime? transactionDate = null, string transactionType = "Manual", int? clientPOId = null, int? invoiceId = null);
        Task<ClientWalletDashboardVM> GetClientWalletDashboard(int clientId, int month, int year, bool allDetails = false);
        Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null, int? poQuantity = null, DateTime? addedDate = null);
        Task<List<ClientPOVM>> GetClientPOList(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, bool openOnly = false);
        Task<List<ClientPOVM>> GetPOHistory(int clientId, int billingType, int? month = null, int? year = null, int? billingOwnerId = null, string ownerEmail = null);
        Task<List<WalletVM>> GetCashCreditHistory(int clientId);
        /// <summary>
        /// Returns the BillingOwnerIds of owners whose LATEST PO has consumed >= <paramref name="threshold"/> of its credit.
        /// Uses FIFO ledger for accuracy. Only meaningful when BillingType = 1 (Billing Owner).
        /// </summary>
        Task<HashSet<int?>> GetHighConsumptionOwnerIds(int clientId, decimal threshold = 0.90m);
        /// <summary>
        /// Returns PO Opening Balance per billing owner for the selected period.
        /// Opening Balance = (Total PO credit registered before 1st of the month)
        ///                 − (Total wallet debits consumed before 1st of the month).
        /// This is the net carry-forward balance entering the selected month.
        /// </summary>
        Task<Dictionary<int, decimal>> GetOwnerOpeningBalances(int clientId, int selectedMonth, int selectedYear);
        Task<bool> UpdatePOStatus(int poId, int clientId, int status);
        Task<bool> UpdateClientPO(int poId, int clientId, string poNumber, decimal poCreditAmount, int poMonth, int poYear, int? poQuantity, DateTime? addedDate, string poDocumentUrl = null, bool updateDocument = false);
        Task<AllClientsWalletVM> GetAllClientsWalletSummary(int fromMonth, int fromYear, int toMonth, int toYear);

        /// <summary>
        /// Fetches all PO + debit data in a single SP call and applies per-client/per-owner
        /// FIFO distribution in memory. Returns one <see cref="ClientPOGroupVM"/> per client
        /// that has at least one PO, sorted by descending overall consumption percentage.
        /// </summary>
        Task<List<ClientPOGroupVM>> GetAllClientsPOSummary();
    }
}


