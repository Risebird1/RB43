using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper.Interface
{
    public interface IAzureFaceApi
    {
        string FaceDetectionResult(string imageFilePath);
    }
}
