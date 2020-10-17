using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IRisebirdPayService
    {
        IEnumerable<Wallet> FetchClientDetails();
        string UpdateClient(int PerSuccessRate,int ClientId);
        string DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
