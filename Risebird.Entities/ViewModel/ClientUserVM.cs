using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientUserVM
    {
        public int id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public long MobileNumber { get; set; }
        public int idProfile { get; set; }
        public string ClientName { get; set; }
        public string Role { get; set; }
    }
}
