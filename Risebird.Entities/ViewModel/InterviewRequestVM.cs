using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewRequestVM
    {
        public int id { get; set; }
        [Display(Name = "Primary Skill")]
        public string PrimarySkill { get; set; }
        public SelectList PrimarySkillsList { get; set; }
        [Display(Name = "JD Title")]
        public string JdTitle { get; set; }
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }
        [Display(Name = "JD File")]
        public IFormFile JDFile { get; set; }
        public string JDLink { get; set; }
        public int JobLevel { get; set; }
        [Display(Name = "Expected No of Interviews")]
        public int ExpectedInterviews { get; set; }
        public int Country { get; set; }
        public IList<InstaTechSkill> TechSkill { get; set; }
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
    }
}
