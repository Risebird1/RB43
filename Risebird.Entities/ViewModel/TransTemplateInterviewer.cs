namespace Risebird.Entities.ViewModel
{
    public class TransTemplateInterviewer
    {
        public int id { get; set; }
        public int TemplateId { get; set; }

        //[Required]
        public int InterviewerId { get; set; }

        //[Required(ErrorMessage = "The Interviewer field is required")]
        public int? UserId { get; set; }
        public string PrimarySkill { get; set; }
        public bool IsSelected { get; set; }
    }
}
