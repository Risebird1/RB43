using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientDailySlotsStatus
    {
        public string IRFId { get; set; }
        public string IRFnSkill { get; set; }
        public int Morning { get; set; }
        public int Evening { get; set; }
        public int Proxy { get; set; }
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public int BorderLine { get; set; }
        public int Pending { get; set; }
        public int total { get; set; }
    }
}
