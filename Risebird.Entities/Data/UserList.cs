
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class UserList
    {
        public int Id { get; set;  }
        public string EmailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int IsActive { get; set; }
        public int ProfileId { get; set; }
    }
}
