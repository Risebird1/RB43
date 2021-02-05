using System;

namespace Risebird.Entities.ViewModel
{
    public class ISCInterviewer
    {
        public int InterviewerProfileId { get; set; }
        public int id { get; set; }
        public int UserId { get; set; }
        public int ISCId { get; set; }
        public string CurrentRole { get; set; }
        public string ResumeLink { get; set; }
        public string PreferredDomain { get; set; }
        public string FirstName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string City { get; set; }
        public float Experience { get; set; }
        public DateTime EndDate { get; set; }
        public int? imid { get; set; }
    }
}
