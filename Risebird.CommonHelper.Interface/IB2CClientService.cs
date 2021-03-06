using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IB2CClientService
    {
        Task<bool> CreateUser(UserList externalUser, bool forcePasswordChangeNextLogin = false);
    }
}
