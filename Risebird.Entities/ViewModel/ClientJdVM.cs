using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientJdVM
    {
        public int id { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public string StartDatestring { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public string EndDatestring { get; set; }
        public int AssessmentGroup { get; set; }
        public string AssessmentGroupName { get; set; }

        [Required]
        public string JobDescription { get; set; }
        [Required]
        public string JDTitle { get; set; }
        public IFormFile File { get; set; }
        public string Filelink { get; set; }
        public SelectList AssessmentGrouplist { get; set; }
        public SelectList Heuristicsscorelist { get; set; }
        public int Heuristicsscore { get; set; }
        public string CreatedBy { get; set; }
        public int ClientId { get; set; }
        public bool isClientUser { get; set; }
        public string ClientEmail { get; set; }
        public string JDBudget { get; set; }
        public string JDType { get; set; }
        public string Location { get; set; }
        public string JobRole { get; set; }
        public string ClientUrl { get; set; }
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
