using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class UnapprovedInterviewersCollectionVM
    {
        public IList<UnApprovedInterviewer> UnApprovedInterviewersCollection { get; set; }
        public int ISCId { get; set; }
        public string ISCName { get; set; }
    }
}
