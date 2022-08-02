using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Risebird.Entities.ViewModel
{
    public class IRFRecommendations
    {
        public int id { get; set; }
        public int CloneIRFId { get; set; }

        [DisplayName("Assessment Group Category")]
        public int? CategoryId { get; set; }

        //[DisplayName("Difficulty Level")]
        public int? CompetencyLevelId { get; set; }

        [DisplayName("Seniority Level")]
        //public int? SeniorityLevelId { get; set; }
        public string SeniorityLevelId { get; set; }//based on Job Profile Enum
        public string[] SeniorityLevelArray { get; set; }
        public List<SelectListItem> SeniorityLevelList { get; set; }

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

        [DisplayName("Difficulty Level")]
        public string DifficultyLevel { get; set; }
        public string[] DifficultyLevelArray { get; set; }
        public List<SelectListItem> DifficultyLevelList { get; set; }

        [DisplayName("Employment Type")]
        public string EmploymentType { get; set; }
        public string[] EmploymentTypeArray { get; set; }
        public List<SelectListItem> EmploymentTypeList { get; set; }

        [DisplayName("Per Hour Rate")]
        public int? PerHourRate { get; set; }

        [DisplayName("Primary Skills")]
        public string PrimarySkills { get; set; }

        [DisplayName("Clients")]
        public string Clients { get; set; }
        public SelectList ClientList { get; set; }
        public string[] ClientArray { get; set; }


    }
}
