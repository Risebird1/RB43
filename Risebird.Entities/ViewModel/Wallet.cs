using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Wallet
    {
        public int id { get; set; }
        public int Clientid { get; set; }
        public int Credit { get; set; }
        public int Debit { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string Comment { get; set; }
        public int Balance { get; set; }
        public string Name { get; set; }
        public int PerSuccessRate { get; set; }
        public string CreatedOnstring { get; set; }
    }
}
