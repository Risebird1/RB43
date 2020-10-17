
using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IRisebirdPayRepository
    {
        IEnumerable<Wallet> FetchClientDetails();
        string UpdateClient(int PerSuccessRate, int ClientId);
        string DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
