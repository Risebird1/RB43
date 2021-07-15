using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

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

    }
}
