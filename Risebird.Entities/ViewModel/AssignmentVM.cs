using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class AssignmentVM
    {
        public int backid { get; set; }
        public int id { get; set; }
        [Display(Name = "Assessment Group")]
        public string AssessmentGroup { get; set; }
        public string PrimarySkills { get; set; }
        public string Overview { get; set; }
        [Display(Name = "Seniority Level")]
        public int? Senioritylevel { get; set; }
        [Display(Name = "Years Of Exprience")]
        public int? YearsofExprience { get; set; }
        [Display(Name = "Assessment Type")]
        public int? AssessmentType { get; set; }
        [Display(Name = "Tested Skills")]
        public string TestedSkill { get; set; }
        public int? Evaluation { get; set; }
        public string Environment { get; set; }
        [Display(Name = "Difficulty Level")]
        public int? Difficultylevel { get; set; }
        [Display(Name = "Employer Type")]
        public string EmployerType { get; set; }
        [Display(Name = "Employer Stage")]
        public int? EmployerStage { get; set; }
        [Display(Name = "Employer Size")]
        public int? EmployerSize { get; set; }
        public string Delivery { get; set; }
        public int Assessmentcount { get; set; }
        public string Createdby { get; set; }
        public string Seniority { get; set; }
        [Display(Name = "Years  OfExperience")]
        public string YearsOfExperience { get; set; }
        public string AssessmentTypes { get; set; }
        public string Evaluations { get; set; }
        public string EmployerStages { get; set; }
        public string EmployerSizes { get; set; }
        public int IsApproved { get; set; }
        public SelectList Senioritylevellist { get; set; }
        public SelectList AssessmentTypelist { get; set; }
        public SelectList Evaluationlist { get; set; }
        public SelectList EmployerStagelist { get; set; }
        public SelectList EmployerSizelist { get; set; }

        public SelectList YearsofExpriencelist { get; set; }
        public SelectList AssessmentLevellist { get; set; }
        public SelectList AssessmentGrouplist { get; set; }
        public List<AssessmentQuestions> Questionslist { get; set; }
        public bool Isinstaavailable { get; set; }
        public string CategoryName { get; set; }
        public int IrfCount { get; set; }
        public bool IsRelevant { get; set; }
        public int CompanyType { get; set; }
        public int ProfileType { get; set; }
        public int CompetencyLevel { get; set; }
        public int Country { get; set; }
        public int PerHourRate { get; set; }
    }
    public class Assessment
    {
        public Assessment()
        {
            _assessment = new List<AssignmentVM>();
            _skilldetails = new Skilldetails();
        }
        public List<AssignmentVM> _assessment { get; set; }
        public Skilldetails _skilldetails { get; set; }
    }
    public class Skilldetails
    {
        [Display(Name = "Skills")]
        public SelectList PrimarySkills { get; set; }
        public string PrimarySkill { get; set; }
        public int[] SelectedSkillIds { get; set; }
        [Display(Name = "Assessment Group Name")]
        public string AssessmentGroupName { get; set; }
        public int id { get; set; }
        public List<SelectListItem> PrimarySkillsList { get; set; }
        public bool Isinstaavailable { get; set; }
        public SelectList CategoryNameList { get; set; }
        public int Category { get; set; }
        public bool IsRelevant { get; set; }
        public int CompanyType { get; set; }
        public SelectList CompanyTypeList { get; set; }
        public int ProfileType { get; set; }
        public int CompetencyLevel { get; set; }
        public int Country { get; set; }
        public int PerHourRate { get; set; }
    }
    public class AssessmentQuestions
    {
        public int id { get; set; }
        public string QuestionCovered { get; set; }
        public string Skills { get; set; }
        public int? Level { get; set; }
        public string Levels { get; set; }
        public string VideoLink { get; set; }
        public SelectList LevelList { get; set; }
        public string Time { get; set; }
    }
}
