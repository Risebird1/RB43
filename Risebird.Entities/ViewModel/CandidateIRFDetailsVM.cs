using System.Collections.Generic;

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
        public string Uniqueid { get; set; }
        public string SchedulingLink { get; set; }
        public bool IsReschedule { get; set; }
        public int Timer { get; set; }
    }
}
