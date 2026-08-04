using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Anonymous mini Add Requirement intake — keeps internal ChannelVM Required set intact.</summary>
    public class PublicChannelVM
    {
        [Required(ErrorMessage = "Please enter your work email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email")]
        [Display(Name = "Requester Email")]
        public string RequesterEmailid { get; set; }

        [Required(ErrorMessage = "Please enter the JD title")]
        [Display(Name = "JD Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Display(Name = "JD Link")]
        [StringLength(500)]
        public string JdLink { get; set; }

        [Display(Name = "JD File")]
        public IFormFile JdFile { get; set; }

        [Display(Name = "Primary Skill")]
        [StringLength(100)]
        public string PrimarySkill { get; set; }

        /// <summary>Stashed by client-side email verification when known.</summary>
        public int? VerifiedClientId { get; set; }
    }
}
