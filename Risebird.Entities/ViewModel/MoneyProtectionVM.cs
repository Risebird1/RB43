using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class MoneyProtectionVM
    {
        public int id { get; set; }
        public DateTime? AddedOn { get; set; }
        public string Comment { get; set; }
        public bool IsConverted { get; set; }
        public string ClientName { get; set; }
        public int Point { get; set; }
        public int BillingType { get; set; }
        public DateTime? interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string IRFId { get; set; }
    }
}
