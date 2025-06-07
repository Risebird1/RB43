using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAIChatRepository
    {
        Task<bool> SaveChatMessageAsync(string userId, string userMessage, string aiResponse, DateTime timestamp);
        Task<List<(string Message, string Role, DateTime Timestamp)>> GetChatHistoryAsync(string userId);
    }
} 