using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IRisebirdPayService
    {
        string DeductWalletPoints(int ClientId, string emailid, int points, string comment);
    }
}
