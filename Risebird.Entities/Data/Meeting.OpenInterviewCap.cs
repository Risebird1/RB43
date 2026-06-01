using System;

namespace Risebird.Entities.Data
{
    public partial class Meeting
    {
        public bool DmIvLockNew { get; set; }
        public int? InterviewCapacity { get; set; }
        public int DmDefaultInterviewCap { get; set; }
        public string DmAssignmentPolicy { get; set; }
        public int IvFutureAssignedCount { get; set; }
    }
}
