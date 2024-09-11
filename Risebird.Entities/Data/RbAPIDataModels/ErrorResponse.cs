using System.Collections.Generic;

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
