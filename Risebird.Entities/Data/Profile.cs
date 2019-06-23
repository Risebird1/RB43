using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class Profile
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Precedence { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
    }
}
