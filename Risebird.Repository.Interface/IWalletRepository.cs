using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWalletRepository
    {
        Task<IEnumerable<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null);
        Task<string> AddWalletCredit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, string transactionType = "Manual");
        Task<string> AddWalletDebit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, int? interviewId = null, int? irfId = null, string transactionType = "Manual");
        Task<string> DebitWalletByInterview(int interviewId, int? billingType = null, string transactionType = "InterviewBilling");
        Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null);
    }
}

