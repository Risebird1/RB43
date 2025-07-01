using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAISpeechToTextService // irrespective of the name, this service also contains the other function (text to speech)

    {
        Task<string> TranscribeAsync(Stream audioStream, string fileName);
        Task<byte[]> ConvertTextToSpeechAsync(string text);
    }
}
