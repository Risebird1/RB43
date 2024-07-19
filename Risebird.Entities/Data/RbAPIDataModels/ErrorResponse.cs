using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class ErrorResponse
    {
        public string Error { get; set; }
        public Dictionary<string, string> Details { get; set; }
    }

    public class ErrorResponseBasic
    {
        public string Error { get; set; }
        public string Details { get; set; }
    }

}
