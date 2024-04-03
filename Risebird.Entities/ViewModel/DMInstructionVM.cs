using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class DMInstructionVM
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string JDTiltle { get; set; }
        public string JDLink { get; set; }
        public string ChannelOwner { get; set; }
        public DateTime StartDate { get; set; }
    }
}
