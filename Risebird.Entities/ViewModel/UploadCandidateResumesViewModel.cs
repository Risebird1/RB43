using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class UploadCandidateResumesViewModel
    {
        public int CandidateId { get; set; }
        public int IRFId { get; set; }

        [Display(Name = "Candidate Name")]
        public string CandidateName { get; set; }

        [Display(Name = "Skill Channel")]
        public string IRFName { get; set; }

        [Display(Name = "Upload Resume(s)")]
        public List<IFormFile> ResumeFiles { get; set; }
    }
}
