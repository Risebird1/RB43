using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class ZoomRecordingResponse
    {
        public string uuid { get; set; }
        public string topic { get; set; }
        public List<RecordingFilesData> recording_files { get; set; }
    }
}
