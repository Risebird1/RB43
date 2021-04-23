using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ClientJdVM
    {
        public int id { get; set; }
        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        public string StartDatestring { get; set; }
        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }
        public string EndDatestring { get; set; }
        [Display(Name = "Assessment Group")]
        public int AssessmentGroup { get; set; }
        public string AssessmentGroupName { get; set; }

        [Required]
        [Display(Name = "Job Description")]

        public string JobDescription { get; set; }
        [Required]
        [Display(Name = "JD Title")]
        public string JDTitle { get; set; }
        [Display(Name = "JD File")]
        public IFormFile JdFile { get; set; }
        [Display(Name = "JD File Link")]
        public string Filelink { get; set; }
        public SelectList AssessmentGrouplist { get; set; }
        public SelectList Heuristicsscorelist { get; set; }
        [Display(Name = "Client Type")]
        public int Heuristicsscore { get; set; }
        public string CreatedBy { get; set; }
        public int ClientId { get; set; }
        public bool isClientUser { get; set; }
        public bool iscreated { get; set; }
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; }
        [Display(Name = "Salary Range")]
        public string JDBudget { get; set; }
        public string JDType { get; set; }
        public string Location { get; set; }
        [Display(Name = "Job Role")]
        public string JobRole { get; set; }
        [Display(Name = "Client Website URL")]
        public string ClientUrl { get; set; }
        public string Name { get; set; }
        [Display(Name = "Per Success Rate")]
        public int persuccessrate { get; set; }
    }
    public class ClientDetails
    {
        public ClientDetails()
        {
            _clientjd = new List<ClientJdVM>();
        }
        public List<ClientJdVM> _clientjd { get; set; }
        public ClientJdVM _clientjddetails { get; set; }
        public Clientfillter fillter { get; set; }

        //public Skilldetails _skilldetails { get; set; }
    }
    public class Clientfillter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientEmail { get; set; }
        public string[] AssessmentGroupName { get; set; }
    }
}
