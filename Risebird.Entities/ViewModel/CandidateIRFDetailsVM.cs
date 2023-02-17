using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class CandidateIRFDetailsVM
    {
        public CandidateIRFDetailsVM()
        {
            CandidateDetails = new Candidate();
            Meetings = new List<MeetingVM>();
        }
        public Candidate CandidateDetails { get; set; }
        //public IRFVm IRFDetails { get; set; }
        public List<MeetingVM> Meetings { get; set; }
        //public CodeInviteVM CodeInviteDetails { get; set; }
        //public MCQVMShare MCQVMShare { get; set; }
    }
}
