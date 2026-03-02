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
        /// <summary>
        /// Sends an internal/system-level prompt to OpenAI without logging it to TransCriptsConv.
        /// Only the assistant reply is logged. Use this for recovery/context prompts that should
        /// not appear in the candidate-facing transcript.
        /// </summary>
        Task<List<string>> SendSystemPrompt(Guid sessionId, string systemPrompt, string assistantId);
        Task<InterviewReportResult> GenerateReport(Guid sessionId, string prompt, int? aiInterviewType = null, bool? isExit = false);
    }
}
