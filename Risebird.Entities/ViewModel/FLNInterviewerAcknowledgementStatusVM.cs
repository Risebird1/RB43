using System;

namespace Risebird.Entities.ViewModel
{
    public class FLNInterviewerAcknowledgementStatusVM
    {
        public int id { get; set; }
        public string EmailId { get; set; }
        public string PrimarySpecialization { get; set; }
        public string AIVerdict { get; set; }
        public string FLN { get; set; }
        public DateTime AddedOn { get; set; }
        public string CompanyName { get; set; }
        public string ResumeLink { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public string AcknowledgementStatus { get; set; }
        public string TargetSkillChannel { get; set; }
        public string AssessmentGroup { get; set; }
    }
}
