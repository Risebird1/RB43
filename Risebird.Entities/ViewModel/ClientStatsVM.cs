using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class ClientStatsVM : ClientStatsBase
    {
        public int id { get; set; }
        public string ClientName { get; set; }
        public SelectList ClientList { get; set; }
    }
}
