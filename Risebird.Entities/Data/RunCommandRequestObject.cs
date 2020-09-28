using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class RunCommandRequestObject
    {
        public string commandId { get; set; }
        public string[] script { get; set; }

    }
}
