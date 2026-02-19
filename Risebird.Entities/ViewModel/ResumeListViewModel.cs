using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            YetToBeProcessed = new List<CandidateResume>();
            NotProcessed = new List<CandidateResume>();
            Duplicates = new List<CandidateResume>();
            Processed = new List<CandidateResume>();
        }
        public string IRFName { get; set; }
        public List<CandidateResume> ResumeList { get; set; }
        public List<CandidateResume> YetToBeProcessed { get; set; }
        public List<CandidateResume> NotProcessed { get; set; }
        public List<CandidateResume> Duplicates { get; set; }
        public List<CandidateResume> Processed { get; set; }
        public string Message { get; set; }
        public string Profile { get; set; }
        public int IRFId { get; set; }
        //public UploadCandidateResumesViewModel UploadResumeModel { get; set; }
        [Display(Name = "Upload Resume(s)")]
        public List<IFormFile> ResumeFiles { get; set; }
        public SelectList ListHR { get; set; }

        [Display(Name = "Client HR")]
        public string ClientHREmailId { get; set; }


    }
}
