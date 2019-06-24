using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class ClientUser
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string Description { get; set; }
        public string EmailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int IsActive { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastLogout { get; set; }
        public string ImageURL { get; set; }
        public int IdProfile { get; set; }
    }
}
