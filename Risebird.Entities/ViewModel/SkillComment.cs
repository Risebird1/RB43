using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class SkillComment
    {
        public int id { get; set; }
        public int IRFId { get; set; }

        //[Required]
        public string Skill { get; set; }
        public string SkillName { get; set; }

        [Required]
        public string Comment { get; set; }
        public int? UserId { get; set; }
        public string CreatedBy { get; set; }
    }
}
