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
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }
        public string ModifiedBy { get; set; }
        public IEnumerable<AssessmentGroupInterviewerDetails> Interviewers { get; set; }
        public bool CreateMasterAG { get; set; }
        public IEnumerable<AssignmentVM> RemainingAssessmentGroups { get; set; }
        public List<AssignmentVM> RemainingAssessmentGroupsList { get; set; }
        public IEnumerable<AssignmentVM> RemainingAssessmentGroupsForSummary { get; set; }// filtered from the above RemaingingAssessmentGroups property
        public string SerializedRemainingAssessmentGroupsForSummary { get; set; }// filtered from the above RemaingingAssessmentGroups property
        public List<int> SelectedRemainingAssessmentGroups { get; set; }
        public string SerializedRemainingAGList { get; set; }
        public bool IsAlreadyMasterAG { get; set; } //need to set this property in order to show or hide the option of creating a master ag in the form 
        public bool CreateMAGOnly { get; set; }//creates mag withouot sending emails

        public IEnumerable<SecondaryAssessmentGroups> SecondaryAGListForMAG { get; set; }//used in edit MAG form, to show the list of secondary ags already added to a mag

    }
}
