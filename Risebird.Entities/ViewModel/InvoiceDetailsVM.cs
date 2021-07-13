using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

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
        public string AccountNumber { get; set; }
        public string AccountDetails { get; set; }
        public int PaymentStatus { get; set; }
        public SelectList PaymentStatusList { get; set; }
        public string Comment { get; set; }
        public string ReferenceNumber { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }
        public string filelink { get; set; }
    }
}
