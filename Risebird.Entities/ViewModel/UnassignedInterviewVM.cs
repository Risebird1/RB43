using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class UnassignedInterviewVM
    {
        public int Interviewid { get; set; }
        public int Irfid { get; set; }
        public string CandidateEmail { get; set; }
        public DateTime interviewDate { get; set; }
        public string FirstName { get; set; }
        public int Typefunction { get; set; }
        public int candidateid { get; set; }
        public string uniqueid { get; set; }
        public string staffingAgency { get; set; }
    }
}
