using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class Support
    {
        public int id { get; set; }

        [Required(ErrorMessage = "Please enter Name.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter EmailId")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string EmailId { get; set; }

        //[Required(ErrorMessage = "Please enter Mobile Number")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        //[Required(ErrorMessage = "Please enter Mobile Number")]
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNumber { get; set; }

        [Required(ErrorMessage = "Please enter availability hours.")]
        [Display(Name = "Availability Hours")]
        public string AvailabilityHours { get; set; }
        public bool OnVoiceCall { get; set; }
        public bool OnWhatsapp { get; set; }
        public bool OnEmail { get; set; }
        public int IsOnline { get; set; }
        public int? applicableToProfileId { get; set; }
    }

    public class SupportList
    {
        public List<Support> Supports { get; set; }
        public string BackMethod { get; set; }
        public string BackController { get; set; }
    }
}
