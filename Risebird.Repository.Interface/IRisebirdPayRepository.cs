
using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IRisebirdPayRepository
    {
        string DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
