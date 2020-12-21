using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface ISpeechToTextHelper
    {
        AudioStreamFormat readWaveHeader(BinaryReader reader);
    }
}
