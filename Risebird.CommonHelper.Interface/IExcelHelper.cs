using System.Collections.Generic;

namespace Risebird.CommonHelper.Interface
{
    public interface IExcelHelper
    {
        byte[] CreateExcelFile(List<string> candidates);//, string irfName

    }
}
