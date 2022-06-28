using System;

namespace Risebird.Entities.ViewModel
{
    public class SelectedPriorityViewFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] CandidateName { get; set; }
        public string[] ClientHR { get; set; }
        public string[] InterviewDate { get; set; }
        public string[] ClientName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ApprovalStatus { get; set; }
        public string[] SelectionStatus { get; set; }
        public string[] JoiningStatus { get; set; }
        public string[] OfferStatus { get; set; }
        public string[] Profile { get; set; }
        public bool[] IsActive { get; set; }
    }
}
