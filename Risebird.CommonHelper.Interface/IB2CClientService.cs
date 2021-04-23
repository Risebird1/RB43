using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IB2CClientService
    {
        Task<bool> CreateUser(UserList externalUser, bool forcePasswordChangeNextLogin = false);
    }
}
