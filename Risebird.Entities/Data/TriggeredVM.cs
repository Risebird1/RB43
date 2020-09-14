using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class TriggeredVM
    {
        public int Id { get; set; }
        public int VMPropertyId { get; set; }
        public string LastAction { get; set; }
        public string ExpectedStatus { get; set; }
        public string Name { get; set; }
        public string ResourceGroupName { get; set; }
        public int Status { get; set; }
        public string StatusDesc { get; set; }
    }
}
