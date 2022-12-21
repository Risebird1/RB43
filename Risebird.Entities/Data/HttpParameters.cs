using RestSharp;

namespace Risebird.Entities.Data
{
    public class HttpParameters
    {
        public string name { get; set; }
        public object value { get; set; }
        public ParameterType parameterType { get; set; }
    }
}
