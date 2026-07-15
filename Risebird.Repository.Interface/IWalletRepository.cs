using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWalletRepository
    {
        Task<IEnumerable<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null);
        Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionRefNumber = null, string invoicePDF = null, DateTime? transactionDate = null, string transactionType = "Manual", int? clientPOId = null, int? invoiceId = null);
        Task<ClientWalletDashboardVM> GetClientWalletDashboard(int clientId, int month, int year, bool allDetails = false);
        Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null);
        Task<List<ClientPOVM>> GetClientPOList(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, bool openOnly = false);
        Task<List<WalletVM>> GetCashCreditHistory(int clientId);
        Task<bool> UpdatePOStatus(int poId, int clientId, int status);
        Task<AllClientsWalletVM> GetAllClientsWalletSummary(int fromMonth, int fromYear, int toMonth, int toYear);
        Task<List<WalletMonthDebitVM>> GetOwnerMonthlyDebits(int clientId, int billingType, int? billingOwnerId = null);
    }
}


