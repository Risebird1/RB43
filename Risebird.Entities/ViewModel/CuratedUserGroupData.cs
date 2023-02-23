using System;

namespace Risebird.Entities.ViewModel
{
    public class CuratedUserGroupData
    {
        public int CuratedGroupId { get; set; }
        public string CuratedGroupName { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? SubId { get; set; }
        public int TotalIRFs { get; set; }
        public string SubscriptionStatus { get; set; }
        public string ArchiveStatus { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public int InterviewesCount { get; set; }
    }
}
