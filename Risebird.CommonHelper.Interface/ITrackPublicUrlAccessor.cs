using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ITrackPublicUrlAccessor
    {
        void SendTelemetry(string emailId, string requestedUrl);
        //Task FetchTelemetryData();
        Task<IEnumerable<AppInsightsPageViews>> FetchTelemetryData(); 
            
    }
}
