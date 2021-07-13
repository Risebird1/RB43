using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface ISASUrlHelper
    {
        string CreateSasUrl(string containerName);
    }
}
