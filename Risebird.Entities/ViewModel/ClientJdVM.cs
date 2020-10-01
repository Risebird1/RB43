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
        [Required]
        public DateTime EndDate { get; set; }
        public int AssessmentGroup { get; set; }
        public string AssessmentGroupName { get; set; }

        [Required]
        public string JobDescription { get; set; }
        [Required]
        public string JDTitle { get; set; }
        public SelectList AssessmentGrouplist { get; set; }
        public string CreatedBy { get; set; }
        public int ClientId { get; set; }
        public bool isClientUser { get; set; }
    }
    public class ClientDetails
    {
        public ClientDetails()
        {
            _clientjd = new List<ClientJdVM>();
        }
        public List<ClientJdVM> _clientjd { get; set; }
        public ClientJdVM _clientjddetails { get; set; }
        //public Skilldetails _skilldetails { get; set; }
    }
}
