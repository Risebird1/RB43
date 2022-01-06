using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IApplicationErrorLogger
    {
        Task LogMessage(LogLevelInfo logLevelInfo, Exception ex, string userEmailid = "");
        Task LogMessage(LogLevelInfo logLevelInfo, string message, string userEmailid = "");
    }
}
