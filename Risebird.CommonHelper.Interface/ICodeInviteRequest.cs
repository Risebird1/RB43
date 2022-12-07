using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ICodeInviteRequest
    {
        Task<string> SendCodeInvitePostRequest(string json);
    }
}
