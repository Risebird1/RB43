using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class AssignmentVM
    {
        public int id { get; set; }
        public string AssessmentGroup { get; set; }
        public string PrimarySkills { get; set; }
        public string Overview { get; set; }
        public int? Senioritylevel { get; set; }
        public int? YearsofExprience { get; set; }
        public int? AssessmentType { get; set; }
        public string TestedSkill { get; set; }
        public int? Evaluation { get; set; }
        public string Environment { get; set; }
        public int? Difficultylevel { get; set; }
        public string EmployerType { get; set; }
        public int? EmployerStage { get; set; }
        public int? EmployerSize { get; set; }
        public string Delivery { get; set; }
        public int Assessmentcount { get; set; }
        public string Createdby { get; set; }
        public string Seniority { get; set; }
        public string YearsOfExperience { get; set; }
        public string AssessmentTypes { get; set; }
        public string Evaluations { get; set; }
        public string EmployerStages { get; set; }
        public string EmployerSizes { get; set; }
        public SelectList Senioritylevellist { get; set; }
        public SelectList AssessmentTypelist { get; set; }
        public SelectList Evaluationlist { get; set; }
        public SelectList EmployerStagelist { get; set; }
        public SelectList EmployerSizelist { get; set; }
        public SelectList YearsofExpriencelist { get; set; }
        public SelectList AssessmentLevellist { get; set; }
        public SelectList AssessmentGrouplist { get; set; }
        public List<AssessmentQuestions> Questionslist { get; set; }

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
    }
    public class AssessmentQuestions
    {
        public int id { get; set; }
        public string QuestionCovered { get; set; }
        public string Skills { get; set; }
        public int? Level { get; set; }
        public string Levels { get; set; }

        public SelectList LevelList { get; set; }
        public string Time { get; set; }
    }
}
