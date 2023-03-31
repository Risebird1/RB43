using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class StatsDerived : StatsVM
    {
        public string IRFnSkill { get; set; }
        public int TotalNoShow { get; set; }//all inclusive, both candidate and interviewer
        public string Role { get; set; }
        public string IRFId { get; set; }
        public int irfIntId { get; set; }
    }
}
