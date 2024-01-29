using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class AGCampaignModel
    {
        public int? id { get; set; }
        public string CampaignName { get; set; }
        public int AssessmentGroupId { get; set; }
        public string AssessmentGroupName { get; set; }
        public IList<AssignmentVM> AssessmentGroups { get; set; } // the property name AssignmentVM is a clerical error; it should be AssessmentGroupVM
        public IList<IRFVm> SkillChannels { get; set; }
        public string CurrentForm { get; set; }
        public string OutcomeMessage { get; set; }
        public IEnumerable<AssessmentGroupInterviewerDetails> Interviewers { get; set; }
    }
}
