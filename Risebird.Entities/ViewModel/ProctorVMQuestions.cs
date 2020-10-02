using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProctorVMQuestions
    {
        public int id { get; set; }
        public int VmId { get; set; }
        public int Priority { get; set; }
        public string Question { get; set; }
        public string Detail { get; set; }
        public string VideoLink { get; set; }
        public int Time { get; set; }
        public int Level { get; set; }
        public string Skill { get; set; }
    }
}
