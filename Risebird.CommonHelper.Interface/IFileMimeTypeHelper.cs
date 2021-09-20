using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface IFileMimeTypeHelper
    {
        string GetFileMimeType(IFormFile file);
    }
}
