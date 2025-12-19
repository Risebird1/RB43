using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IWalletRepository
    {
        Task<IEnumerable<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null);
        Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null);
    }
}

