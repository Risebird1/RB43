using Microsoft.AspNetCore.Http;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ResumeListViewModel
    {
        public ResumeListViewModel()
        {
            ResumeList = new List<CandidateResume>();
        }
        public string IRFName { get; set; }
        public List<CandidateResume> ResumeList { get; set; }
        public string Message { get; set; }
        public string Profile { get; set; }
        public int IRFId { get; set; }
        //public UploadCandidateResumesViewModel UploadResumeModel { get; set; }
        [Display(Name = "Upload Resume(s)")]
        public List<IFormFile> ResumeFiles { get; set; }

    }
}
