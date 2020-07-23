using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class UnapprovedInterviewersCollectionVM
    {
        public IList<UnApprovedInterviewer> UnApprovedInterviewersCollection { get; set; }
        public int ISCId { get; set; }
        public string ISCName { get; set; }
    }
}
