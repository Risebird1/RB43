using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum CommunicationMedium
    {
        [Description("Email")]
        Email = 1,

        [Description("Whatsapp")]
        Whatsapp = 2,

        [Description("VoiceCall")]
        VoiceCall = 3
    }
}
