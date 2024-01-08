using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class IRFCandidateReport
    {
        public IRFCandidateReport()
        {
            TechSkillReportCollection = new List<TechSkillReport>();
        }
        public int id { get; set; }
        public string IRFId { get; set; }
        public DateTime interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string MobileNumber { get; set; }
        public string JDTiltle { get; set; }
        public string MeetingStatus { get; set; }
        public string InterviewStatus { get; set; }
        public string confirmationStatus { get; set; }
        public string InterviewerComment { get; set; }
        public string ClientHrContact { get; set; }
        public string VideoLink { get; set; }
        public IList<TechSkillReport> TechSkillReportCollection { get; set; }
        public IList<string> SkillList { get; set; }
    }
}
