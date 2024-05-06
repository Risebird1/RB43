namespace Risebird.Entities.ViewModel
{
    public class InterviewerVideoKYCVM
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string VideoLink { get; set; }
        public bool IsVideoApproved { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public string EmailId { get; set; }
    }
}
