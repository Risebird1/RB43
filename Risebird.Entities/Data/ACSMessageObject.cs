using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class ACSMessageObject
    {
        public List<string> ToList { get; set; }
        public List<string> CcList { get; set; }
        public List<string> BccList { get; set; }
        public string MessageBody { get; set; }
        public string MessageSubject { get; set; }
        public string SentFrom { get; set; }
    }
}
