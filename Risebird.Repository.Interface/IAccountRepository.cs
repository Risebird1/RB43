using Risebird.Entities.ViewModel;
using System.Collections.Generic;
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
        Task<IEnumerable<Account>> GetAccounts(string emailID, string profileName);
        IEnumerable<AccountProjectMapping> GetProjectsbyClientId(int clientId);
        IEnumerable<AccountProjectMapping> GetProjectByClientId(int clientId, string projectIdList = null);
        string Signout(string EmailId, string SessionId);
        InterviewerProfileVM FetchUserProfile(string EmailId);
        Task<bool> UpdateUserProfile(InterviewerProfileVM userProfile);
    }
}
