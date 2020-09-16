using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class VMProperties
    {
        public int id { get; set; }
        public string vmId { get; set; }
        public string Name { get; set; }
        public string ResourceGroupName { get; set; }
        public string IpAddress { get; set; }
        public int? Status { get; set; }
        public bool IsAdded { get; set; }
        public bool IsRemoved { get; set; }
        public bool IsDeleted { get; set; }
        public string StatusDesc { get; set; }
        public string ImageUrl { get; set; }
        public string LastAction { get; set; }

    }
}
