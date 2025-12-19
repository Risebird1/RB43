using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IWalletService
    {
        Task<List<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null);
        Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null);
    }
}

