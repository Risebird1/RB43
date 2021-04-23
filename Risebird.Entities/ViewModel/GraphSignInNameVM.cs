using Newtonsoft.Json;

namespace Risebird.Entities.ViewModel
{
    public class GraphSignInNameVM
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
