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
    public class CallCatalogueVM
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public string JDTiltle { get; set; }
        public string ClientName { get; set; }
        public DateTime CalledOn { get; set; }
        public string WhoCalled { get; set; }
        public string ClientHr { get; set; }
    }
}
