using System;

namespace Risebird.Entities.ViewModel
{
    public class WeekendDriveSpotlightVM
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public string WeekendBucket { get; set; }
        public string ActionsTaken { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class AddClientUserFromRequirementVM
    {
        public int RequirementId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public int? SelectedClient { get; set; }
        public int? SelectedClientRole { get; set; }
        public string NewClientName { get; set; }
    }
}
