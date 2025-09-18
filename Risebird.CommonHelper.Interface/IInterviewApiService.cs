using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IInterviewApiService
    {
        Task<string> SendInterviewMessage(Guid sessionId, string message);
        Task<InterviewStartResult> StartConversation(Guid sessionId, string prompt, string assistantId);
        Task<List<string>> SendReply(Guid sessionId, string message, string assistantId);
        Task<InterviewReportResult> GenerateReport(Guid sessionId, string prompt, int? aiInterviewType = null, bool? isExit = false);
    }
}
