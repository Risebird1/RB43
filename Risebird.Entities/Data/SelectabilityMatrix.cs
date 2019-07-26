using System.ComponentModel.DataAnnotations;
namespace Risebird.Entities.Data
{
    public class SelectabilityMatrix
    {
        public int id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Please Enter Selectability Matrix Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please Enter Selectability Matrix Description")]
        public string Description { get; set; }
    }
}
