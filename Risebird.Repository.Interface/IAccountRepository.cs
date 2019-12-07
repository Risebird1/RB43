using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAccountRepository
    {
        IEnumerable<Account> GetAllAccounts();
        Task<bool> AddAccount(Account accountDetails);
        IEnumerable<AccountProjectMapping> GetAccountCluster(int accountId);
        Account GetAccountById(int accountId);
        Task<bool> UpdateAccount(Account accountDetails);
        Task<bool> ArchiveAccount(int id);
        Task<bool> UnArchiveAccount(int id);
    }
}
