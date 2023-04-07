using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewRequestVM
    {
        public int id { get; set; }
        [Display(Name = "Primary Skill")]
        public string PrimarySkill { get; set; }
        public SelectList PrimarySkillsList { get; set; }
        [Display(Name = "Job Title")]
        public string JdTitle { get; set; }
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }
        [Display(Name = "Job Description")]
        public IFormFile JDFile { get; set; }
        public string JDLink { get; set; }
        [Display(Name = "Seniority Level")]
        public int JobLevel { get; set; }
        [Display(Name = "Expected No of Interviews")]
        public int ExpectedInterviews { get; set; }
        public int Country { get; set; }
        public IList<InstaTechSkill> TechSkill { get; set; }
        public IList<TransSkill> TechSkills { get; set; }
        public string RequiredSkills { get; set; }
        public DateTime CreatedOn { get; set; }
        public string JobLevelName { get; set; }
        public string CountryName { get; set; }
        public string Assignto { get; set; }
        public bool IsIRFCreated { get; set; }
        public bool IsSetToComplete { get; set; }
        public string IRFId { get; set; }
        public string CalendlyLink { get; set; }
        public string uniqueid { get; set; }

        [Display(Name = "Project Name")]
        public string ClientName { get; set; }
        public int BillingOwner { get; set; }
        [Display(Name = "Assessment Group")]
        public int AssessmentGroupId { get; set; }
        public int ClientId { get; set; }
        [Display(Name = "Category")]
        public int Categoryid { get; set; }
        public SelectList AssessmentGroupsList { get; set; }
        public string ClientCompanyName { get; set; }

        [Display(Name = "Company Type")]
        public int CompanyTypes { get; set; }

        [Display(Name = "Competency Level")]
        public int CompetencyLevel { get; set; }

        [Display(Name = "Difficulty Level")]
        public int DifficultyLevel { get; set; }  //referred to as Profile Type at a few places

        [Display(Name = "Employment Type")]
        public int EmploymentType { get; set; }

        [Display(Name = "Primary Skills")]
        public string[] PrimarySkills { get; set; }
        public bool IsClientLogin { get; set; }
    }
}
