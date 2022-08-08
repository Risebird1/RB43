using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface ITrackPublicUrlAccessor
    {
        void SendTelemetry(string emailId, string requestedUrl);
    }
}
