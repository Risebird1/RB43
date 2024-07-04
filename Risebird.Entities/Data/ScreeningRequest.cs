using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class ScreeningRequest
    {
        public string Skills { get; set; }
        public string JDTitle { get; set; }
        public string ResumeLink { get; set; }
        public string ReportUrl { get; set; }
        public string Type { get; set; } = ProfileNames.Candidate;
    }
}
