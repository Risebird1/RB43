using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
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
        [Display(Name = "RB Pay Balance")]
        public int Balance { get; set; }
        public int PerSuccessRate { get; set; }
        public string Createdby { get; set; }

        [Display(Name = "Logo URL")]
        public string LogoLink { get; set; }

        [Display(Name = "Upload Company Logo")]
        public IFormFile LogoFile { get; set; }

        [Display(Name = "Company URL")]
        public string CompanyLink { get; set; }

        [Display(Name = "Company Video(GIF)")]
        public string CompanyVideoLink { get; set; }

        [Display(Name = "Upload Company Video(GIF)")]
        public IFormFile VideoFile { get; set; }

        [Display(Name = "Reset Password after first login/Password aging")]
        public bool IsPasswordReset { get; set; }
        [Display(Name = "Check candidate availability")]
        public bool IsCandidateavailability { get; set; }
        [Display(Name = "Participant Waiting Notification")]
        public bool IsMeetingNotificationSend { get; set; }
        [Display(Name = "Per Interview Rate")]
        public int PerInterviewRate { get; set; }
        [Display(Name = "Billing Type")]
        public string BillingType { get; set; }
        public List<SelectListItem> BillingTypeDrop { get; set; }
        public string LoggedEmail { get; set; }
        [Display(Name = "Interview Duration")]
        public int InterviewDuration { get; set; }
        public SelectList InterviewDurationList { get; set; }
        public bool IsMeetingAutoCancelled { get; set; }
        [Display(Name = "QC By Client")]
        public bool IsQCRequired { get; set; }
        [Display(Name = "Redo Interview By Client")]
        public bool IsRedoInterview { get; set; }
        [Display(Name = "Cancel Interview")]
        public bool IsClientCancelInterview { get; set; }
        public string OldLogoLink { get; set; }
    }
}
