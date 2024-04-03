using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class CallibrationCallVM
    {
        public int id { get; set; }
        public int Irfid { get; set; }
        public DateTime MeetingDate { get; set; }
        public string MeetingLink { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string VideoLink { get; set; }
        public string IRFName { get; set; }
        public string JDTiltle { get; set; }
    }
}
