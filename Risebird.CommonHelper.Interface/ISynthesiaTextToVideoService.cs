using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ISynthesiaTextToVideoService
    {
        Task<string> CreateVideoAsync(string text, string templateId);
        Task<string> GetVideoUrlAsync(string videoId);
    }
}
