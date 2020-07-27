using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public interface IAuthenticationService
    {
        Task<string> GetAccessTokenAsync();
    }
}
