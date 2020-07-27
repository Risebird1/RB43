using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class ClientInterviewVM
    {
        public int id { get; set; }
        public string clientName { get; set; }
        public string JobRole { get; set; }
        public string interviewDate { get; set; }
        public string interviewSlot { get; set; }
        public string interviewDuration { get; set; }
        public string primaryskill { get; set; }
        public string candidateEmail { get; set; }
        public string ClientHrContact { get; set; }
        public string resumeLink { get; set; }
        public string comfirmationStatus { get; set; }
        public string zoomLink { get; set; }
        public string zoomDetails { get; set; }
        public string risebirdHR { get; set; }
        public string interviewer { get; set; }
        public string interviewerComment { get; set; }
        public string intervieweStatus { get; set; }
        public string selectionStatus { get; set; }
        public string videoLink { get; set; }
        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string hrCoordinatorComment { get; set; }
        public string trainableComment { get; set; }
        public string interviewOnDevice { get; set; }
        public string sCreatedBy { get; set; }
        public DateTime dtCreatedOn { get; set; }
        public string IRFID { get; set; }
        public string PreferredLocation { get; set; }
        public string ExpectedPerHourRate { get; set; }
        public string staffingAgency { get; set; }
        public string joiningTime { get; set; }
        public int irfIntId { get; set; }
        public int? L2InterviewStatus { get; set; }
        public int? ClientInterviewStatus { get; set; }

        [NotMapped]
        public string L2InterviewStatDesc { get; set; }
        [NotMapped]
        public string ClientInterviewStatDesc { get; set; }
    }
}
