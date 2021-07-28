using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InvoiceDetailsVM
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string JobCountry { get; set; }
        public int CompletedInterview { get; set; }
        public int Price { get; set; }
        public int TotalAmount { get; set; }
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }
        [Display(Name = "Account Details")]
        public string AccountDetails { get; set; }
        [Display(Name = "Payment Status")]
        public int PaymentStatus { get; set; }
        public SelectList PaymentStatusList { get; set; }
        public string Comment { get; set; }
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }
        public string filelink { get; set; }
        [Display(Name = "Upload File")]
        public IFormFile fileupload { get; set; }
    }
}
