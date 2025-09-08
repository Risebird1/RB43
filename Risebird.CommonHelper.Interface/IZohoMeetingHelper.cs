using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IZohoMeetingHelper
    {
        Task<ZohoMeetingResponse> CreateMeetingAsync(ZohoCreateMeetingRequest request);
        Task<bool> DeleteMeetingAsync(string meetingKey);
        Task<IEnumerable<ZohoRecordingInfo>> GetRecordingAsync(string meetingKey);
        Task DeleteRecordingAsync(string recordingid);
        Task<string> GetRecordingAccessTokenAsync();
    }
}
