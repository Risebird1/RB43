using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class AssessmentGroupInterviewerDetails 
    {
        public int IRFId { get; set; }

        public string EmailId { get; set; }
        
        public string uniqueid { get; set; }
        public string InterviewerName { get; set; }
        
        public int Assessmentid { get; set; }
        
        //public int PerSuccessRate { get; set; }
        public string JDLink { get; set; }
        public int AvailbilityHour { get; set; }
        public int FlatRate { get; set; }
        public string BasicSkill { get; set; }
        
        
    }
}
