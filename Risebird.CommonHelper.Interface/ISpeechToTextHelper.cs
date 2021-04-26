using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;

namespace Risebird.CommonHelper.Interface
{
    public interface ISpeechToTextHelper
    {
        AudioStreamFormat readWaveHeader(BinaryReader reader);
    }
}
