using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Business.Interface;
using Risebird.Repository.Interface;
using Microsoft.Extensions.Configuration;

namespace Risebird.Business.Services
{
    public class AIChatService : IAIChatService
    {
        private readonly IConfiguration _configuration;
        private readonly IAIChatRepository _chatRepository;
        
        public AIChatService(IConfiguration configuration, IAIChatRepository chatRepository)
        {
            _configuration = configuration;
            _chatRepository = chatRepository;
        }

        public async Task<string> GetAIResponseAsync(string userMessage)
        {
            // TODO: Implement AI integration (e.g., OpenAI, Azure OpenAI)
            // This is where you'll integrate with your chosen AI provider
            return await Task.FromResult("This is a placeholder response. Implement AI integration here.");
        }

        public async Task<bool> SaveChatHistoryAsync(string userId, string userMessage, string aiResponse)
        {
            return await _chatRepository.SaveChatMessageAsync(userId, userMessage, aiResponse, DateTime.UtcNow);
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string userId)
        {
            var history = await _chatRepository.GetChatHistoryAsync(userId);
            var messages = new List<ChatMessage>();
            
            foreach (var (Message, Role, Timestamp) in history)
            {
                messages.Add(new ChatMessage
                {
                    Message = Message,
                    Role = Role,
                    Timestamp = Timestamp
                });
            }
            
            return messages;
        }
    }
} 