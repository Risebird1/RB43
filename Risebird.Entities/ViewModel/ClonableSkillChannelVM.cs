using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClonableSkillChannelVM : IRFVm
    {
        public int ClonableSkillChannelId { get; set; }
        public string Name { get; set; }
        public int CreatedFromIRFId { get; set; }

        public SelectList SeniorityLevelList { get; set; }
        //public SelectList CompanyTypeList { get; set; }
        public SelectList CompetencyLevelList { get; set; }
        public SelectList Level { get; set; }
        public SelectList LevelTech { get; set; }
        public SelectList Skills { get; set; }
    }
}
