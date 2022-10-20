using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class SendgridEventData
    {
        public string email { get; set; }

        //[JsonProperty("event")]
        public string @event { get; set; }
        public string timestamp { get; set; }
    }
}
