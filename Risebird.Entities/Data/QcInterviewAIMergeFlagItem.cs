using Newtonsoft.Json;

namespace Risebird.Entities.Data
{
    /// <summary>Single flag entry from the QC interview AI merge assistant (<c>flags[]</c>): category, flag, penalty only.</summary>
    public class QcInterviewAIMergeFlagItem
    {
        public string category { get; set; }
        public string flag { get; set; }
        [JsonProperty("penalty")]
        public int? penalty { get; set; }
    }
}
