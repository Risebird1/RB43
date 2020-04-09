using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerProjects
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string ProjectName { get; set; }
        public string ClientName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Role { get; set; }
        public string ProjectDescriptions { get; set; }
    }
}
