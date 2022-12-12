using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class HttpParameters
    {
        public string name { get; set; }
        public object value { get; set; }
        public ParameterType parameterType { get; set; }
    }
}
