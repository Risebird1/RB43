using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class Profile
    {
        public int id { get; set; }

        [Required(ErrorMessage = "Please enter Profile Name")]
        [Display(Name = "Profile Name")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Please enter Profile Description")]
        [Display(Name = "Profile Description")]
        public string Description { get; set; }
        public int Precedence { get; set; }

        [Display(Name = "Admin User?")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Active User?")]
        public bool IsActive { get; set; }

        public bool isDelete { get; set; }
    }
}
