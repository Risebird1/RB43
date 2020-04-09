using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerAccDetails
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string Location { get; set; }
    }
}
