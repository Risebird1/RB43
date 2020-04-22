using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerProfessional
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string CompanyName { get; set; }
        public string Type { get; set; }
        public string CompanyStage { get; set; }
        public string Size { get; set; }
        public string JobFunction { get; set; }
        public string Industry { get; set; }
        public string Headquarters { get; set; }
        public string Role { get; set; }
        public string Domain { get; set; }
        public string SeniorityLevel { get; set; }
        public string Experience { get; set; }
        public string WorkNature { get; set; }
        public string PrimaryResponsibility { get; set; }
        public string CompanyLinkedinPage { get; set; }
        public string CompanyWebsite { get; set; }

    }
}
