using System;

namespace Risebird.Entities.Data
{
    public class Meeting
    {
        public int id { get; set; }
        public string clientName { get; set; }
        public string JobRole { get; set; }
        public string interviewDate { get; set; }
        public string interviewSlot { get; set; }
        public string interviewDuration { get; set; }
        public string primaryskill { get; set; }
        public string candidateEmail { get; set; }
        public string ClientHrContact { get; set;}
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
        public DateTime dtCreatedOn { get; set; }

    }
}
