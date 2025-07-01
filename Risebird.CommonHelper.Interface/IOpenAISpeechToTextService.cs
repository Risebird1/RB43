using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAISpeechToTextService
    {
        Task<string> TranscribeAsync(Stream audioStream, string fileName);
    }
}
