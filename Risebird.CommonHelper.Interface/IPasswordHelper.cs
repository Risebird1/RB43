using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPasswordHelper
    {
        Task<string> GenerateUserPassword(UserDetails user, bool forcePasswordChangeNextLogin = false);
    }
}
