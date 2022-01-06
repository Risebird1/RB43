using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerTdsVM
    {
        public List<SelectListItem> TdsYears { get; set; }
        public string InterviewerName { get; set; }
        public string TdsSelectYear { get; set; }
        public IList<TdsCertificateReport> TdsCertificateCollection { get; set; }
        public string InterviewerEmailid { get; set; }
    }
    public class TdsCertificateReport
    {
        public int id { get; set; }
        public int InterviewerId { get; set; }
        public string month { get; set; }
        public string comment { get; set; }
        public IFormFile cretificate { get; set; }
        public string certificatelink { get; set; }
        public string year { get; set; }
        public string emailid { get; set; }
        public List<SelectListItem> MonthsList { get; set; }
    }
}
