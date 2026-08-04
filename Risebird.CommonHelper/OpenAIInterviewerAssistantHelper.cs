using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// AI-interviewer conversation helper.
    ///
    /// Migrated from the deprecated Assistants API (threads / runs / messages) to the Responses API
    /// plus the Conversations API. The public contract is unchanged so all callers keep working:
    ///   - A "thread id" is now a Responses Conversation id (conv_...), persisted by callers as before.
    ///   - AddMessageAsync stages the user turn; CreateRunAsync sends it to /v1/responses with the
    ///     conversation attached (so prior turns are carried as context) and returns the response id.
    ///   - Responses are synchronous, so WaitForRunAsync no longer polls.
    /// </summary>
    public class OpenAIInterviewerAssistantHelper : IOpenAIInterviewerAssistantHelper
    {
        private readonly IConfiguration _config;
        private readonly IOpenAIApiLogger _openAIApiLogger;

        private readonly string _openAiKey;
        private readonly string _baseUrl;

        // Staged user input per conversation (set by AddMessageAsync, consumed by CreateRunAsync).
        private static readonly ConcurrentDictionary<string, string> _pendingInput =
            new ConcurrentDictionary<string, string>();
        // Last assistant reply per conversation, so GetMessagesAsync avoids an extra round trip.
        private static readonly ConcurrentDictionary<string, string> _lastReply =
            new ConcurrentDictionary<string, string>();

        public OpenAIInterviewerAssistantHelper(IConfiguration config, IOpenAIApiLogger openAIApiLogger)
        {
            _config = config;
            _openAIApiLogger = openAIApiLogger;
            _openAiKey = _config["OpenAI:ApiKey"];
            _baseUrl = string.IsNullOrWhiteSpace(_config["OpenAI:BaseUrl"]) ? "https://api.openai.com/v1" : _config["OpenAI:BaseUrl"];
        }

        public async Task<string> CreateThreadAsync()
        {
            var conversationId = await OpenAIResponsesGateway.CreateConversationAsync(
                _baseUrl, _openAiKey, _openAIApiLogger,
                new OpenAIApiLogContext { OperationType = "AIInterview-ThreadCreation" });

            if (string.IsNullOrEmpty(conversationId))
            {
                throw new Exception("Failed to create OpenAI conversation.");
            }

            return conversationId;
        }

        public Task AddMessageAsync(string threadId, string role, string content)
        {
            if (!string.IsNullOrEmpty(threadId))
            {
                // Only user turns drive a generation; assistant/system content is folded into the input.
                _pendingInput.AddOrUpdate(
                    threadId,
                    content ?? string.Empty,
                    (_, existing) => string.IsNullOrEmpty(existing) ? (content ?? string.Empty) : existing + "\n\n" + content);
            }

            return Task.CompletedTask;
        }

        public async Task<string> CreateRunAsync(string threadId, string assistantId)
        {
            var profile = await OpenAIResponsesGateway.ResolveAssistantAsync(_baseUrl, _openAiKey, assistantId, _openAIApiLogger);

            _pendingInput.TryRemove(threadId, out var input);

            var result = await OpenAIResponsesGateway.CreateResponseAsync(
                baseUrl: _baseUrl,
                apiKey: _openAiKey,
                model: profile.Model,
                instructions: profile.Instructions,
                input: input ?? string.Empty,
                jsonObjectFormat: true, // preserved from the legacy run (response_format: json_object)
                tools: profile.Tools,
                conversationId: threadId,
                logger: _openAIApiLogger,
                context: new OpenAIApiLogContext { OperationType = "AIInterview-CreateRun" },
                assistantId: assistantId);

            _lastReply[threadId] = result.OutputText ?? string.Empty;
            return result.Id;
        }

        /// <summary>
        /// Responses calls are synchronous (they return once complete), so there is nothing to poll.
        /// Retained for interface compatibility; always reports completion.
        /// </summary>
        public Task<string> WaitForRunAsync(string threadId, string runId)
        {
            return Task.FromResult("completed");
        }

        public async Task<List<string>> GetMessagesAsync(string threadId)
        {
            // Prefer the reply captured during the just-completed run.
            if (!string.IsNullOrEmpty(threadId) && _lastReply.TryGetValue(threadId, out var reply) && !string.IsNullOrEmpty(reply))
            {
                return new List<string> { reply };
            }

            // Fallback: reconstruct from the conversation (newest assistant messages first, mirroring
            // the legacy messages listing order).
            var items = await OpenAIResponsesGateway.ListConversationItemsAsync(
                _baseUrl, _openAiKey, threadId, _openAIApiLogger,
                new OpenAIApiLogContext { OperationType = "AIInterview-GetMessages" });

            return items
                .Where(i => string.Equals(i.Role, "assistant", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.Text))
                .Select(i => i.Text)
                .Reverse()
                .ToList();
        }

        public async Task<List<TranscriptConv>> GetFullConversation(Guid sessionId, string threadId)
        {
            var items = await OpenAIResponsesGateway.ListConversationItemsAsync(
                _baseUrl, _openAiKey, threadId, _openAIApiLogger,
                new OpenAIApiLogContext { SessionId = sessionId, OperationType = "AIInterview-GetFullConversation" });

            var now = DateTime.UtcNow;
            var messages = items
                .Where(i => !string.IsNullOrEmpty(i.Text))
                .Select((i, index) => new TranscriptConv
                {
                    SessionId = sessionId,
                    Role = i.Role,
                    Message = i.Text,
                    Timestamp = now.AddMilliseconds(index)
                })
                .ToList();

            return messages;
        }
    }
}
