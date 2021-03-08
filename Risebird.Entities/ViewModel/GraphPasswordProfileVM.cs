using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
   public class GraphPasswordProfileVM
    {
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("forceChangePasswordNextLogin", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ForceChangePasswordNextLogin { get; set; }
    }
}
