using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class IRFVm
    {
        public int id { get; set; }

        [DisplayName("IRFId")]
        [Required]
        public string IRFId { get; set; }

        [DisplayName("End Client Name")]
        [Required]
        public string ClientName { get; set; }

        [DisplayName("JD Title")]
        [Required]
        public string JDTiltle { get; set; }

        [DisplayName("Company Type")]
        [Required]
        public string CompanyType { get; set; }

        [DisplayName("Total Budget")]
        public int TotalBudget { get; set; }

        [DisplayName("Budget Consumed")]
        public string BudgetConsumed { get; set; }

        [DisplayName("Job Level")]
        public int? JobLevel { get; set; }
        public int ClientId { get; set; }
        [DisplayName("Job Country")]
        [Required]
        [RegularExpression("^[a-zA-Z]{1,20}$", ErrorMessage = "Kindly Enter only letters.")]
        public string JobCountry { get; set; }

        [DisplayName("Start Date")]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime StartDate { get; set; }

        [DisplayName("End Date")]
        [DataType(DataType.DateTime)]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime EndDate { get; set; }

        [DisplayName("JD Link")]
        [Required]
        public string JDLink { get; set; }

        [DisplayName("No of Positions")]
        [Required]
        public int NoOfPosition { get; set; }

        [DisplayName("No of Interviews")]
        [Required]
        public int NoOfIntterview { get; set; }

        [DisplayName("Risebird HR")]
        [Required]
        public int RisebirdHr { get; set; }

        [DisplayName("Per Interview Rate")]
        [Required]
        public double PerMintuteRate { get; set; }
        public SelectList JobLevelList { get; set; }
        public IList<TransSkill> SkillCollection { get; set; }
        public IList<TransSkillGeneral> SkillGeneral { get; set; }
        [Required]
        public IList<TransJoinability> Joinability { get; set; }
        public IList<TransSelectability> Selectability { get; set; }
        public IList<TransClientUser> ClientCollection { get; set; }
        public IList<TransQualityController> QualityController { get; set; }
        public IList<TransInterviewer> InterviewerCollection { get; set; }
        public string ClientCompName { get; set; }

        public SelectList JoinabilityTypeList { get; set; }
        public SelectList SelectabilityTypeList { get; set; }
        public SelectList SkillGeneralTypeList { get; set; }

        public SelectList CompanyTypeList { get; set; }

        [Required]
        public IFormFile JDFile { get; set; }

        [DisplayName("Calendly Link")]
        public string CalendlyLink { get; set; }
        public int TotalInvites { get; set; }
    }

    public class IRF
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public string JDTiltle { get; set; }
        public string CompanyType { get; set; }
        public int ClientId { get; set; }
        public string ClientCompName { get; set; }
        public string JobCountry { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string JDLink { get; set; }
        public int NoOfPosition { get; set; }
        public int NoOfIntterview { get; set; }
        public int RisebirdHr { get; set; }
        public double PerMintuteRate { get; set; }

        public int JobLevel { get; set; }
        public bool IsClientUser { get; set; }
        public bool IsDeleted { get; set; }

        public int TotalMeetings { get; set; }
        public int TotalBudget { get; set; }
        public string BudgetConsumed { get; set; }
        public int SelectedMeetings { get; set; }
        public int ScheduleMeetings { get; set; }
        public string CalendlyLink { get; set; }
        public int TotalInvites { get; set; }

    }
}
