
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
        public long MobileNumber { get; set; }
        public int IsActive { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastLogout { get; set; }
        public string ImageURL { get; set; }
        public int IdProfile { get; set; }
    }
}
