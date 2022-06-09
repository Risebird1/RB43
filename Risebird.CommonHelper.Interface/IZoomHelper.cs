using System.Net;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IZoomHelper
    {
        string ZoomToken();
        Task<HttpStatusCode> CheckMeetingbyId(string meetingId, string token);
    }
}
