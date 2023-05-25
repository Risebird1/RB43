using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAffIndaResumeParserHelper
    {
        Task<string> UploadAndParseResume(string fileUrl = null);
    }
}
