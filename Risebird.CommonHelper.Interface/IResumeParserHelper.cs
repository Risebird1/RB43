using System.IO;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.CommonHelper.Interface
{
    public interface IResumeParserHelper
    {
        Task<RchilliMapFields> ParseResume(MemoryStream fileStream, string fileName);
    }
}
