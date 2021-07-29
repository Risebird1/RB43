using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CallRecordingDetailsVM
    {
        public string WhoCalled { get; set; }
        public string RecordingUrl { get; set; }
        public DateTime CalledOn { get; set; }
    }
}
