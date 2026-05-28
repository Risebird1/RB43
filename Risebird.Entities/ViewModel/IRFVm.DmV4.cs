namespace Risebird.Entities.ViewModel
{
    /// <summary>DM Approval V4 — interviewer policy fields on IRF.</summary>
    public partial class IRFVm
    {
        public bool DmIvLockNew { get; set; }
        public string DmAssignmentPolicy { get; set; }
        public int DmDefaultInterviewCap { get; set; }
    }
}
