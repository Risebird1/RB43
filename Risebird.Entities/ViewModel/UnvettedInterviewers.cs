using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class UnvettedInterviewers
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string PrimaryAssessmentGroupIds { get; set; }
        public string[] PrimaryAssessmentGroupArray { get; set; }
        public List<SelectListItem> AssessmentGroupList { get; set; }
        public int YearsOfExperience { get; set; }
        public int ProfileType { get; set; }
        public string ResumeLink { get; set; }
        public bool? IsVetted { get; set; } // NULL means not vetted yet by anyone, false means discarded by the Vetter, true means approved by the vetter(the changes suggested by the vetter can be checked in the InterviewerProfile table)

        public string VettedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

    }
}
