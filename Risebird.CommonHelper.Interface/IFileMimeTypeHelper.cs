using Microsoft.AspNetCore.Http;

namespace Risebird.CommonHelper.Interface
{
    public interface IFileMimeTypeHelper
    {
        string GetFileMimeType(IFormFile file);
    }
}
