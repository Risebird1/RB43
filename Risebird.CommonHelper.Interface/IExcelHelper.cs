using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IExcelHelper
    {
        byte[] CreateExcelFile(List<string> candidates);//, string irfName

    }
}
