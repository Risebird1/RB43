using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAccountService
    {
        IEnumerable<Account> GetAllAccounts();
        Account GetAccount();
        Task<bool> AddAccount(Account accountDetails);
        Account GetAccountById(int id);
        bool UpdateAccount(Account model);
        Task<bool> ArchiveAccount(int id);
        Task<bool> UnArchiveAccount(int id);
        IEnumerable<AccountProjectMapping> GetProjectsbyClientId(int clientId);
        IEnumerable<Account> GetAccounts(string emailId, string profile);
        IEnumerable<AccountProjectMapping> GetAccountCluster(int accountId);
        IEnumerable<AccountProjectMapping> GetProjectbyClientId(int clientId, string projectList=null);
        string Signout(string EmailId);
    }
}
