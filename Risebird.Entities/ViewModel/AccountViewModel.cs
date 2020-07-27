using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class AccountViewModel
    {
        public bool IsClientUser { get; set; }
        public string Message { get; set; }
        public IEnumerable<Account> AccountList { get; set; }
    }
}
