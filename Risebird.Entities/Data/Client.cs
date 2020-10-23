using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class Client
    {
        public int id { get; set; }

        [Display(Name = "Client Name")]
        [Required(ErrorMessage = "Please Enter Client Name")]
        public string Name { get; set; }
        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please Enter Client Description")]
        public string Description { get; set; }
        [Display(Name = "GST")]

        public string GST { get; set; }
        [Display(Name = "OfficeAddress")]

        public string OfficeAddress { get; set; }
        [Display(Name = "RBP Balance")]
        public int Balance { get; set; }
        public int PerSuccessRate { get; set; }
        public string Createdby { get; set; }
    }
}
