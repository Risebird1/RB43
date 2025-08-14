using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAIInterviewerAssistantHelper
    {
        Task<string> CreateThreadAsync();
        Task AddMessageAsync(string threadId, string role, string content);
        Task<string> CreateRunAsync(string threadId, string assistantId);
        Task<string> WaitForRunAsync(string threadId, string runId);
        Task<List<string>> GetMessagesAsync(string threadId);
        Task<List<TranscriptConv>> GetFullConversation(Guid sessionId, string threadId);
    }
}
