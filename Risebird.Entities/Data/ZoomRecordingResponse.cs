using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class ZoomRecordingResponse
    {
        public string uuid { get; set; }
        public string topic { get; set; }
        public List<RecordingFilesData> recording_files { get; set; }
    }
    public class ZoomParticipentResponse
    {
        public string plainToken { get; set; }
        public string encryptedToken { get; set; }
    }
}
