using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class BlackListCompanyVM
    {
        public bool IsBlackList { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get;set; }
        public DateTime ModifyDate { get; set; }
        public string ModifyBy { get; set;}
        public string CompanyName { get; set;}
        public bool IsApprovedBlacklistCompany { get; set;}
        public int id { get; set; }
    }
}
