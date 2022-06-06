using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class IRFRecommendations
    {
        public int id { get; set; }
        public int CloneIRFId { get; set; }

        [DisplayName("Assessment Group Category")]
        public int? CategoryId { get; set; }

        [DisplayName("Difficulty Level")]
        public int? CompetencyLevelId { get; set; }

        [DisplayName("Seniority Level")]
        public int? SeniorityLevelId { get; set; }

        [DisplayName("Country Type")]
        public int? CountryType { get; set; }

        [DisplayName("Company Type")]
        public int? CompanyType { get; set; }

        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool Status { get; set; }

        [BindProperty]
        public string StatusRadioValue { get; set; } = "true";
        public SelectList CategoryList { get; set; }

        [DisplayName("Assessment Group")]
        public int? AssessmentGroupId { get; set; }

        public SelectList AssessmentGroupsList { get; set; }

        public string AssessmentGroupName { get; set; }


    }
}
