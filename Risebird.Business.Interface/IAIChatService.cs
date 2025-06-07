using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAIChatService
    {
        Task<string> GetAIResponseAsync(string userMessage);
        Task<bool> SaveChatHistoryAsync(string userId, string userMessage, string aiResponse);
        Task<List<ChatMessage>> GetChatHistoryAsync(string userId);
    }

    public class ChatMessage
    {
        public string Message { get; set; }
        public string Role { get; set; }  // "user" or "assistant"
        public DateTime Timestamp { get; set; }
    }
} 