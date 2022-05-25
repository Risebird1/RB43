using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IZoomHelper
    {
        string ZoomToken();
        Task<HttpStatusCode> CheckMeetingbyId(string meetingId);
    }
}
