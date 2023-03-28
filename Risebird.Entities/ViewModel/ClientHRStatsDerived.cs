using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class ClientHRStatsDerived : StatsVM
    {
        public string IRFnSkill { get; set; }
        public int TotalNoShow { get; set; }//all inclusive, both candidate and interviewer
    }
}
