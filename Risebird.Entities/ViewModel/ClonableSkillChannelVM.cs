using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClonableSkillChannelVM : IRFVm
    {
        //public int id { get; set; }
        public string Name { get; set; }
        public int CreatedFromIRFId { get; set; }
    }
}
