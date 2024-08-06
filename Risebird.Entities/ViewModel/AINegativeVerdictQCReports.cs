using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class AINegativeVerdictQCReports
    {
        //public int id { get; set; }
        public int InterviewId { get; set; }
        //public bool IsQCdone { get; set; }
        //public string CreatedBy { get; set; }
        //public DateTime? CreatedOn { get; set; }
        public int irfIntId { get; set; }
        public string confirmationStatus { get; set; }
        public int? MeetingStatus { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public DateTime? interviewDate { get; set; }
        public int? InterviewStatus { get; set; }
        public string intervieweStatusStatDesc { get; set; }
        public string IRFId { get; set; }
        public string candidateName { get; set; }
        public string candidateEmail { get; set; }
        public string interviewer { get; set; }
        public string InterviewerMobileNumber { get; set; }
        public string ChannelOwner { get; set; }
        //public int QCPresent { get; set; }
    }
}
