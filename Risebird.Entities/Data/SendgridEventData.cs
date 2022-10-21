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
        public string smtpid { get; set; }
        public string ip { get; set; }
        public string category { get; set; }
        public string sg_event_id { get; set; }
        public string sg_message_id { get; set; }
        public string response { get; set; }
        public string url { get; set; }
        public string useragent { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
    }
}
