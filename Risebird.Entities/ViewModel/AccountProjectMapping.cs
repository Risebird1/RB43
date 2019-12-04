using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class AccountProjectMapping
    {
        public int id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int AccountId { get; set; }
        public bool IsAdded { get; set; }
        public string AccountName { get; set; }
        public string Client { get; set; }
        public int ClientId { get; set; }
    }
}
