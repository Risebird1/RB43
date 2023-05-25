using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.CommonHelper.Interface
{
    public interface IResumeParserHelper
    {
        Task<RchilliMapFields> ParseResume(MemoryStream fileStream, string fileName);
    }
}
