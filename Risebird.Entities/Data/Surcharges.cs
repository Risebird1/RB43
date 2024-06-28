using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class Surcharges
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public int IRFId { get; set; }
        public String SurchargeComment { get; set; }
        public String SkillChannel { get; set; }
        public String interviewer { get; set; }       
        public int Surcharge { get; set; }
        public bool IsSurcharge { get; set; }
        public DateTime? RequestAt { get; set; }
        public string RequestBy { get; set; }
        public string JDTiltle { get; set; }
        public string ClientName { get; set; }
        public string Type { get; set; }
        public int UserId { get; set; }
        public string candidateEmail { get; set; }
        public string ChannelOwner { get; set; }
        public DateTime? interviewDate { get; set; }
        public int? MeetingStatus { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
    }
}
