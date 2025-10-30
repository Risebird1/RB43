using Microsoft.AspNetCore.Http;
using Risebird.Entities.Data.RekognitionModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAWSRekognitionService
    {
        Task<string> UploadVideoAsync(string videoUrl);
        Task<VideoAnalysisResponseDto> AnalyzeVideoAsync(string videoKey);
        Task<string> UploadVideoAsync(IFormFile file);
    }
}
