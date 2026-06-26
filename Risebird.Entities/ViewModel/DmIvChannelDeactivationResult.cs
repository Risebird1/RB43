namespace Risebird.Entities.ViewModel
{
    public class DmIvChannelDeactivationPreview
    {
        public bool IsClientRole { get; set; }
        public int FutureAssignedCount { get; set; }
    }

    public class DmIvChannelDeactivationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FlagFlipped { get; set; }
        public string Role { get; set; }
        public int TransInterviewerId { get; set; }
        public int IrfId { get; set; }
        public int InterviewerUserId { get; set; }
        public int FutureAssignedCount { get; set; }

        public static DmIvChannelDeactivationResult Fail(string message) =>
            new DmIvChannelDeactivationResult { Success = false, Message = message };
    }
}
