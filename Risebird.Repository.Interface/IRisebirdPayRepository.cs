
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IRisebirdPayRepository
    {
        Task<string> DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
