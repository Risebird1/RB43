using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerVM
    {
        public int id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public long MobileNumber { get; set; }
        public int idProfile { get; set; }
        public string PrimarySkill { get; set; }
        public string ResumeLink { get; set; }
        public string Slots { get; set; }
        public int NoOfInterview { get; set; }
        public double SuccessRate { get; set; }
        public bool IsApproved { get; set; }
    }
}
