using System.ComponentModel;

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
