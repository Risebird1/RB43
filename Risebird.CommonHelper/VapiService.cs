using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.VapiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Service implementation for Vapi.ai voice agent operations
    /// </summary>
    public class VapiService : IVapiService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _vapiApiKey;
        private readonly string _vapiBaseUrl;
        private readonly string _phoneNumberId;

        public VapiService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _vapiApiKey = _configuration["Vapi:ApiKey"];
            _vapiBaseUrl = _configuration["Vapi:BaseUrl"] ?? "https://api.vapi.ai";
            _phoneNumberId = _configuration["Vapi:PhoneNumberId"];

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_vapiApiKey}");
        }

        /// <summary>
        /// Creates an assistant configured for interview booking conversations
        /// </summary>
        public async Task<string> CreateInterviewBookingAssistantAsync(string assistantName, string webhookUrl)
        {
            var assistant = new
            {
                name = assistantName,
                model = new
                {
                    provider = "openai",
                    model = "gpt-4o-mini",
                    temperature = 0.7,
                    maxTokens = 250,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = @"You are a professional recruitment assistant from Risebird calling to schedule an interview.

Your objectives:
1. Politely introduce yourself and confirm you're speaking with the correct candidate
2. Inform them about the interview opportunity
3. Check their availability for the interview
4. Book a suitable time slot
5. Confirm the interview details

Guidelines:
- Be professional, friendly, and concise
- Listen carefully to the candidate's availability
- If they're busy, offer to call back at a better time
- If they decline, politely thank them and end the call
- Confirm all details before ending the call

You have access to tools to:
- Check available interview slots
- Book the interview
- Send confirmation details"
                        }
                    }
                },
                voice = new
                {
                    provider = "11labs",
                    voiceId = "sarah", // Professional female voice
                    stability = 0.5,
                    similarityBoost = 0.75
                },
                firstMessage = "Hello! This is Sarah from Risebird. May I speak with {{candidateName}}?",
                transcriber = new
                {
                    provider = "deepgram",
                    model = "nova-2",
                    language = "en"
                },
                tools = new object[]
                {
                    new
                    {
                        type = "function",
                        function = new
                        {
                            name = "check_availability",
                            description = "Check available interview time slots",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    preferredDate = new
                                    {
                                        type = "string",
                                        description = "Preferred date mentioned by candidate (YYYY-MM-DD)"
                                    },
                                    preferredTime = new
                                    {
                                        type = "string",
                                        description = "Preferred time mentioned by candidate (HH:MM)"
                                    }
                                },
                                required = new[] { "preferredDate" }
                            }
                        },
                        server = new
                        {
                            url = webhookUrl,
                            timeoutSeconds = 20
                        }
                    },
                    new
                    {
                        type = "function",
                        function = new
                        {
                            name = "book_interview",
                            description = "Book the interview at the confirmed time",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    interviewDateTime = new
                                    {
                                        type = "string",
                                        description = "Confirmed interview date and time (YYYY-MM-DD HH:MM)"
                                    },
                                    candidateConfirmed = new
                                    {
                                        type = "boolean",
                                        description = "Whether candidate confirmed the time"
                                    }
                                },
                                required = new[] { "interviewDateTime", "candidateConfirmed" }
                            }
                        },
                        server = new
                        {
                            url = webhookUrl,
                            timeoutSeconds = 20
                        }
                    },
                    new
                    {
                        type = "endCall",
                        messages = new[]
                        {
                            new
                            {
                                type = "request-complete",
                                content = "Thank you for your time. You'll receive a confirmation email shortly. Have a great day!"
                            }
                        }
                    }
                },
                endCallPhrases = new[] { "goodbye", "bye", "thank you bye" },
                maxDurationSeconds = 300, // 5 minutes max
                backgroundSound = "office",
                server = new
                {
                    url = webhookUrl
                }
            };

            var json = JsonConvert.SerializeObject(assistant);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_vapiBaseUrl}/assistant", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create assistant: {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
            return result.id;
        }

        /// <summary>
        /// Initiates an outbound call to a candidate
        /// </summary>
        public async Task<VapiCallResponse> InitiateCallAsync(VapiCallRequest request)
        {
            var payload = new
            {
                phoneNumberId = request.PhoneNumberId,
                customer = request.Customer,
                assistantId = request.AssistantId,
                assistant = request.Assistant,
                assistantOverrides = request.AssistantOverrides
            };

            var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_vapiBaseUrl}/call/phone", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to initiate call: {responseContent}");
            }

            return JsonConvert.DeserializeObject<VapiCallResponse>(responseContent);
        }

        /// <summary>
        /// Gets call details by call ID
        /// </summary>
        public async Task<VapiCallResponse> GetCallDetailsAsync(string callId)
        {
            var response = await _httpClient.GetAsync($"{_vapiBaseUrl}/call/{callId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get call details: {responseContent}");
            }

            return JsonConvert.DeserializeObject<VapiCallResponse>(responseContent);
        }

        /// <summary>
        /// Lists all assistants
        /// </summary>
        public async Task<string> ListAssistantsAsync()
        {
            var response = await _httpClient.GetAsync($"{_vapiBaseUrl}/assistant");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to list assistants: {responseContent}");
            }

            return responseContent;
        }

        /// <summary>
        /// Deletes a call (for compliance/privacy)
        /// </summary>
        public async Task<bool> DeleteCallAsync(string callId)
        {
            var response = await _httpClient.DeleteAsync($"{_vapiBaseUrl}/call/{callId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Processes tool call webhooks from Vapi
        /// This handles function calls like check_availability and book_interview
        /// </summary>
        public async Task<VapiToolCallResult> ProcessToolCallAsync(VapiWebhookMessage webhookMessage)
        {
            var results = new List<VapiToolCallResultItem>();

            if (webhookMessage?.Message?.ToolCalls != null && webhookMessage.Message.ToolCalls.Any())
            {
                foreach (var toolCall in webhookMessage.Message.ToolCalls)
                {
                    var result = ProcessIndividualToolCall(toolCall);
                    results.Add(new VapiToolCallResultItem
                    {
                        ToolCallId = toolCall.Id,
                        Result = result
                    });
                }
            }

            return new VapiToolCallResult { Results = results };
        }

        /// <summary>
        /// Processes individual tool call based on function name
        /// </summary>
        private string ProcessIndividualToolCall(VapiToolCall toolCall)
        {
            try
            {
                switch (toolCall.Function.Name)
                {
                    case "check_availability":
                        return CheckAvailability(toolCall.Function.Arguments);

                    case "book_interview":
                        return BookInterview(toolCall.Function.Arguments);

                    case "submitInterviewSlot":
                        return SubmitInterviewSlot(toolCall.Function.Arguments);

                    default:
                        return JsonConvert.SerializeObject(new { success = false, message = "Unknown function" });
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Checks available interview slots
        /// This is a placeholder - integrate with your actual scheduling system
        /// </summary>
        private string CheckAvailability(Dictionary<string, object> arguments)
        {
            // TODO: Integrate with your actual calendar/scheduling system
            // This is mock data for demonstration

            var preferredDate = arguments.ContainsKey("preferredDate") ? arguments["preferredDate"].ToString() : null;

            var availableSlots = new
            {
                success = true,
                availableSlots = new[]
                {
                    new { date = preferredDate ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), time = "10:00 AM" },
                    new { date = preferredDate ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), time = "02:00 PM" },
                    new { date = preferredDate ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), time = "04:00 PM" }
                },
                message = "Here are the available time slots. Which one works best for you?"
            };

            return JsonConvert.SerializeObject(availableSlots);
        }

        /// <summary>
        /// Books the interview
        /// This is a placeholder - integrate with your actual booking system
        /// </summary>
        private string BookInterview(Dictionary<string, object> arguments)
        {
            // TODO: Integrate with your actual interview booking system
            // Save to database, send confirmations, etc.

            var interviewDateTime = arguments.ContainsKey("interviewDateTime") ? arguments["interviewDateTime"].ToString() : null;
            var confirmed = arguments.ContainsKey("candidateConfirmed") && Convert.ToBoolean(arguments["candidateConfirmed"]);

            if (!confirmed)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = "Candidate did not confirm the interview time"
                });
            }

            var bookingResult = new
            {
                success = true,
                bookingId = Guid.NewGuid().ToString(),
                interviewDateTime = interviewDateTime,
                message = $"Interview has been successfully booked for {interviewDateTime}. A confirmation email will be sent shortly."
            };

            return JsonConvert.SerializeObject(bookingResult);
        }

        /// <summary>
        /// Handles interview slot submission from the scheduling assistant
        /// This is called when the assistant collects a confirmed time slot
        /// </summary>
        private string SubmitInterviewSlot(Dictionary<string, object> arguments)
        {
            try
            {
                // Extract arguments
                var candidateName = arguments.ContainsKey("candidateName") ? arguments["candidateName"]?.ToString() : null;
                var interviewDate = arguments.ContainsKey("interviewDate") ? arguments["interviewDate"]?.ToString() : null;
                var interviewTime = arguments.ContainsKey("interviewTime") ? arguments["interviewTime"]?.ToString() : null;
                var timezone = arguments.ContainsKey("timezone") ? arguments["timezone"]?.ToString() : "IST";
                var candidateConfirmed = arguments.ContainsKey("candidateConfirmed") && Convert.ToBoolean(arguments["candidateConfirmed"]);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(candidateName))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        message = "Please confirm the candidate's name before proceeding."
                    });
                }

                if (string.IsNullOrWhiteSpace(interviewDate) || string.IsNullOrWhiteSpace(interviewTime))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        message = "Please ensure both date and time are provided."
                    });
                }

                if (!candidateConfirmed)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        message = "Candidate has not confirmed the time slot. Please get explicit confirmation before submitting."
                    });
                }

                // Parse and validate date/time
                var dateTimeString = $"{interviewDate} {interviewTime}";
                DateTime parsedDateTime;
                string[] formats = { 
                    "yyyy-MM-dd hh:mm tt",
                    "yyyy-MM-dd h:mm tt",
                    "yyyy-MM-dd HH:mm",
                    "yyyy-MM-dd H:mm"
                };

                bool parsed = false;
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateTimeString, format, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out parsedDateTime))
                    {
                        parsed = true;
                        
                        // Validate it's within next 2 days
                        var maxDate = DateTime.Now.AddDays(2).Date.AddDays(1);
                        if (parsedDateTime > maxDate)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                message = "The interview must be scheduled within the next 1-2 days only. Please ask for an earlier slot."
                            });
                        }

                        if (parsedDateTime < DateTime.Now)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                message = "The time slot is in the past. Please ask for a valid future time."
                            });
                        }

                        // Success - This will be logged in the database via webhook
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = $"Perfect! I've confirmed your interview for {interviewDate} at {interviewTime} {timezone}. You'll receive a confirmation email shortly.",
                            interviewDateTime = parsedDateTime.ToString("yyyy-MM-dd HH:mm"),
                            candidateName = candidateName,
                            timezone = timezone
                        });
                    }
                }

                if (!parsed)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        message = "I couldn't understand the time format. Please ask the candidate to provide the time clearly with AM or PM."
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = "An error occurred while processing the interview slot."
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }
    }
}
