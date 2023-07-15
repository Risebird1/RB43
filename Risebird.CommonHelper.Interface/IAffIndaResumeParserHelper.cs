using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAffIndaResumeParserHelper
    {
        Task<string> UploadAndParseResume(string fileUrl = null);
    }
}
