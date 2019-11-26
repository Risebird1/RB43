using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InvoiceModel
    {
        public int IRFId { get; set; }
        public string IRFName { get; set; }
        public string RisebirdAddress { get; set; }
        public string RisebirdTAN { get; set; }
        public string RisebirdPAN { get; set; }
        public string RisebirdGSTIN { get; set; }
        public string RisebirdCIN { get; set; }
        public string RisebirdAccountName { get; set; }
        public string RisebirdAccountNumber { get; set; }
        public string RisebirdBankName { get; set; }
        public string RisebirdIFSC { get; set; }
        public string RisebirdICICIBankSwiftCode { get; set; }
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public string ClientGSTIN { get; set; }
        public string CurrentMonth { get; set; }
        public string PONumber { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceReferenceCode { get; set; }
        public string SRNumber { get; set; }
        public int InterviewCount { get; set; }
        public int PartialInterviewCount { get; set; }
        public double PricePerInterview { get; set; }
        public double DiscountedPricePerInterview { get; set; }
        public double PartialPayable { get; set; }
        public double Netpayable { get; set; }
        public double IGST { get; set; }
        public double Discount { get; set; }
    }
}
