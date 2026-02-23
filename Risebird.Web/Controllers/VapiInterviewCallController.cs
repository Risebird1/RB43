using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.VapiModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Controller for managing Vapi voice AI interview booking calls
    /// </summary>
    [Authorize]
    public class VapiInterviewCallController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public VapiInterviewCallController(
            IConfiguration configuration,
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        /// <summary>
        /// Dashboard view for managing Vapi calls
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var recentCalls = await _vapiCallRepository.GetRecentCallLogsAsync(100);
                return View(recentCalls);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new List<VapiCallLog>());
            }
        }

        /// <summary>
        /// Initiates a call to a candidate for interview booking
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> InitiateInterviewCall(
            int interviewId,
            int candidateId,
            string candidateName,
            string candidateEmail,
            string candidatePhone,
            string jobTitle,
            DateTime? preferredDateTime = null)
        {
            try
            {
                // Validate phone number format (E.164)
                if (string.IsNullOrWhiteSpace(candidatePhone))
                {
                    return Json(new { success = false, message = "Phone number is required" });
                }

                // Ensure phone number is in E.164 format
                if (!candidatePhone.StartsWith("+"))
                {
                    candidatePhone = "+91" + candidatePhone; // Default to India country code
                }

                // Get webhook URL from configuration
                var baseUrl = _configuration["HostDetails:baseUrl"];
                var webhookUrl = $"{baseUrl}/VapiInterviewCall/Webhook";

                // Get or create assistant ID
                var assistantId = _configuration["Vapi:InterviewBookingAssistantId"];
                if (string.IsNullOrEmpty(assistantId))
                {
                    // Create assistant if not exists
                    assistantId = await _vapiService.CreateInterviewBookingAssistantAsync(
                        "Interview Booking Assistant",
                        webhookUrl);

                    // TODO: Save this assistant ID to configuration or database
                }

                // Inject today's date dynamically into system prompt
                var systemPromptWithDate = GetSystemPromptWithCurrentDate(candidateName, jobTitle);

                // Create call request with customer info and date injection
                var callRequest = new VapiCallRequest
                {
                    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                    Customer = new VapiCustomer
                    {
                        Number = candidatePhone,
                        Name = candidateName,
                        Email = candidateEmail
                    },
                    AssistantId = assistantId,
                    AssistantOverrides = new VapiAssistantOverrides
                    {
                        Model = new VapiModelOverride
                        {
                            Messages = new List<VapiMessage>
                            {
                                new VapiMessage
                                {
                                    Role = "system",
                                    Content = systemPromptWithDate
                                }
                            }
                        }
                    }
                };

                // Initiate the call
                var callResponse = await _vapiService.InitiateCallAsync(callRequest);

                // Save call log to database
                var callLog = new VapiCallLog
                {
                    VapiCallId = callResponse.Id,
                    InterviewId = interviewId,
                    RecipientId = candidateId,
                    RecipientPhone = candidatePhone,
                    RecipientName = candidateName,
                    RecipientEmail = candidateEmail,
                    CallStatus = callResponse.Status,
                    ScheduledDateTime = preferredDateTime,
                    CallStartedAt = callResponse.StartedAt,
                    AssistantId = assistantId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity.Name
                };

                var logId = await _vapiCallRepository.SaveCallLogAsync(callLog);

                return Json(new
                {
                    success = true,
                    callId = callResponse.Id,
                    logId = logId,
                    status = callResponse.Status,
                    message = "Call initiated successfully"
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Webhook endpoint for Vapi callbacks
        /// This handles all webhook events from Vapi
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            VapiWebhookMessage message = null;
            string rawJson = null;
            
            try
            {
                // Enable buffering so we can read the body multiple times
                Request.EnableBuffering();
                
                // Read raw body first for debugging
                using (var reader = new System.IO.StreamReader(
                    Request.Body, 
                    System.Text.Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    rawJson = await reader.ReadToEndAsync();
                    
                    // Reset the stream position so it can be read again
                    Request.Body.Position = 0;
                }
                
                // Log that we received the webhook
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    await LogWebhookDebug("EMPTY BODY", "error", "Request body is empty", null);
                    return BadRequest("Empty request body");
                }
                
                await LogWebhookDebug(rawJson, "received", null, null);
                
                // Try to deserialize
                message = JsonConvert.DeserializeObject<VapiWebhookMessage>(rawJson);
                
                if (message?.Message == null)
                {
                    await LogWebhookDebug(rawJson, "error", 
                        $"Invalid webhook message - message is null. Deserialized type: {message?.GetType().Name ?? "null"}", 
                        null);
                    return BadRequest("Invalid webhook message");
                }

                var messageType = message.Message.Type;
                var callId = message.Message.CallId ?? message.Message.Call?.Id;

                await LogWebhookDebug(rawJson, "processing", null, messageType, callId);

                switch (messageType)
                {
                    case "status-update":
                        await HandleStatusUpdate(message);
                        break;

                    case "transcript":
                        await HandleTranscript(message);
                        break;

                    case "tool-calls":
                        await LogWebhookDebug(rawJson, "tool-call-received", null, "tool-calls", callId);
                        await LogCheckpoint("WEBHOOK-START", $"Processing tool-calls - Tool count: {message.Message.ToolCalls?.Count ?? 0}", callId);
                        
                        var result = await _vapiService.ProcessToolCallAsync(message);
                        
                        await LogCheckpoint("WEBHOOK-BEFORE-HANDLER", "About to call HandleInterviewSlotSubmission", callId);
                        
                        // Also handle interview slot submissions
                        await HandleInterviewSlotSubmission(message);
                        
                        await LogCheckpoint("WEBHOOK-AFTER-HANDLER", "HandleInterviewSlotSubmission completed", callId);
                        
                        return Json(result);

                    case "end-of-call-report":
                        await HandleEndOfCallReport(message);
                        break;

                    default:
                        // Log unhandled message type
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                            new Exception($"Unhandled Vapi webhook message type: {messageType}"));
                        await LogWebhookDebug(rawJson, "unhandled", $"Unknown type: {messageType}", messageType, callId);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await LogWebhookDebug(rawJson ?? "null", "error", $"Exception: {ex.Message}\nStack: {ex.StackTrace}", null);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Logs webhook debug information to database for troubleshooting
        /// </summary>
        private async Task LogWebhookDebug(string rawPayload, string status, string errorMsg, string webhookType = null, string callId = null)
        {
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(_configuration["ConnectionString:DBConnectionString"]))
                {
                    var sql = @"INSERT INTO VapiWebhookDebugLogs 
                        (RequestTimestamp, WebhookType, CallId, RawPayload, ProcessingStatus, ErrorMessage)
                        VALUES (@Timestamp, @Type, @CallId, @Payload, @Status, @Error)";
                    
                    await conn.ExecuteAsync(sql, new
                    {
                        Timestamp = DateTime.UtcNow,
                        Type = webhookType,
                        CallId = callId,
                        Payload = rawPayload?.Length > 10000 ? rawPayload.Substring(0, 10000) : rawPayload,
                        Status = status,
                        Error = errorMsg
                    });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, 
                    new Exception($"Failed to log webhook debug: {ex.Message}"));
            }
        }

        /// <summary>
        /// Logs checkpoint information to database for debugging webhook flow
        /// </summary>
        private async Task LogCheckpoint(string checkpointName, string message, string callId = null, string toolCallName = null, string additionalData = null)
        {
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(_configuration["ConnectionString:DBConnectionString"]))
                {
                    var sql = @"INSERT INTO VapiWebhookCheckpoints 
                        (Timestamp, CheckpointName, Message, CallId, ToolCallName, AdditionalData)
                        VALUES (@Timestamp, @CheckpointName, @Message, @CallId, @ToolCallName, @AdditionalData)";
                    
                    await conn.ExecuteAsync(sql, new
                    {
                        Timestamp = DateTime.UtcNow,
                        CheckpointName = checkpointName,
                        Message = message,
                        CallId = callId,
                        ToolCallName = toolCallName,
                        AdditionalData = additionalData?.Length > 5000 ? additionalData.Substring(0, 5000) : additionalData
                    });
                }
            }
            catch
            {
                // Silently fail - don't want logging to break the webhook
            }
        }

        /// <summary>
        /// Handles status update webhooks
        /// </summary>
        private async Task HandleStatusUpdate(VapiWebhookMessage message)
        {
            var call = message.Message.Call;
            if (call == null) return;

            var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(call.Id);
            if (callLog != null)
            {
                callLog.CallStatus = call.Status;
                callLog.CallStartedAt = call.StartedAt;
                callLog.CallEndedAt = call.EndedAt;
                callLog.EndedReason = call.EndedReason;
                callLog.UpdatedAt = DateTime.UtcNow;

                await _vapiCallRepository.UpdateCallLogAsync(callLog);
            }
        }

        /// <summary>
        /// Handles transcript webhooks
        /// </summary>
        private async Task HandleTranscript(VapiWebhookMessage message)
        {
            var transcript = message.Message.Transcript;
            var callId = message.Message.CallId;

            if (transcript == null || string.IsNullOrWhiteSpace(callId)) return;

            // Log transcript for monitoring/debugging
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                new Exception($"Vapi Transcript - CallId: {callId}, Role: {transcript.Role}, Text: {transcript.Transcript}"));
        }

        /// <summary>
        /// Handles interview slot submission from tool calls
        /// </summary>
        private async Task HandleInterviewSlotSubmission(VapiWebhookMessage message)
        {
            // Extract CallId early with fallback pattern
            string callId = message?.Message?.CallId ?? message?.Message?.Call?.Id;
            
            try
            {
                await LogCheckpoint("CHECKPOINT-1", $"HandleInterviewSlotSubmission method entry - CallId: {callId ?? "NULL"}", callId);

                if (message?.Message?.ToolCalls == null || !message.Message.ToolCalls.Any())
                {
                    await LogCheckpoint("CHECKPOINT-1a", $"ToolCalls null or empty - Message null: {message == null}, Message.Message null: {message?.Message == null}, ToolCalls null: {message?.Message?.ToolCalls == null}, ToolCalls count: {message?.Message?.ToolCalls?.Count ?? 0}", callId);
                    return;
                }

                var toolNames = string.Join(", ", message.Message.ToolCalls.Select(tc => tc.Function?.Name ?? "null"));
                await LogCheckpoint("CHECKPOINT-2", $"ToolCalls found - Count: {message.Message.ToolCalls.Count}", additionalData: $"Tool names: {toolNames}");

                foreach (var toolCall in message.Message.ToolCalls)
                {
                    await LogCheckpoint("CHECKPOINT-3", $"Examining tool call - Name: {toolCall.Function?.Name ?? "null"}, Function null: {toolCall.Function == null}");

                    if (toolCall.Function?.Name == "submitInterviewSlot")
                    {
                        var args = toolCall.Function.Arguments;
                        // CallId already extracted at method level with fallback

                        var argsJson = args != null ? JsonConvert.SerializeObject(args) : "NULL";
                        await LogCheckpoint("CHECKPOINT-4", "Found submitInterviewSlot tool call", callId, "submitInterviewSlot");
                        await LogCheckpoint("CHECKPOINT-5", $"Arguments - Args null: {args == null}, Args count: {args?.Count ?? 0}", callId, "submitInterviewSlot", argsJson);

                        // Validate CallId
                        if (string.IsNullOrWhiteSpace(callId))
                        {
                            await LogCheckpoint("CHECKPOINT-6-ERROR", "CallId is null or empty");
                            await UpdateWebhookDebugLog(callId, "error", "CallId is null or empty", null, null, false);
                            return;
                        }

                        await LogCheckpoint("CHECKPOINT-6", $"CallId validated: {callId}", callId);

                        // Extract interview slot data
                        var candidateName = args.ContainsKey("candidateName") ? args["candidateName"]?.ToString() : null;
                        var interviewDate = args.ContainsKey("interviewDate") ? args["interviewDate"]?.ToString() : null;
                        var interviewTime = args.ContainsKey("interviewTime") ? args["interviewTime"]?.ToString() : null;
                        var timezone = args.ContainsKey("timezone") ? args["timezone"]?.ToString() : "IST";
                        var candidateConfirmed = args.ContainsKey("candidateConfirmed") && Convert.ToBoolean(args["candidateConfirmed"]);

                        await LogCheckpoint("CHECKPOINT-7", $"Data extracted - Name: '{candidateName}', Date: '{interviewDate}', Time: '{interviewTime}', TZ: '{timezone}', Confirmed: {candidateConfirmed}", callId);
                        
                        // Check for old dates (likely from dashboard-initiated calls)
                        if (!string.IsNullOrEmpty(interviewDate) && DateTime.TryParse(interviewDate, out DateTime parsedDate))
                        {
                            if (parsedDate.Year < DateTime.Now.Year - 1)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                    new Exception($"⚠️ WARNING: Old date detected ({interviewDate}). This call was likely initiated from Vapi dashboard, not through the application. Dynamic date injection only works when calling through /InitiateInterviewCall endpoint."));
                                await UpdateWebhookDebugLog(callId, "warning", 
                                    $"Old date detected: {interviewDate}. Call was likely made from Vapi dashboard. Use application endpoint for current dates.", 
                                    null, null, false);
                            }
                        }

                        // Parse the interview date/time
                        var dateTimeString = $"{interviewDate} {interviewTime}";
                        DateTime? parsedDateTime = null;

                        string[] formats = { 
                            "yyyy-MM-dd hh:mm tt",
                            "yyyy-MM-dd h:mm tt",
                            "yyyy-MM-dd HH:mm",
                            "yyyy-MM-dd H:mm"
                        };

                        foreach (var format in formats)
                        {
                            if (DateTime.TryParseExact(dateTimeString, format, 
                                System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None, out DateTime result))
                            {
                                parsedDateTime = result;
                                break;
                            }
                        }

                    if (!parsedDateTime.HasValue)
                    {
                        await LogCheckpoint("CHECKPOINT-8-SKIPPED", $"Failed to parse date/time - Date: {interviewDate}, Time: {interviewTime}", callId);
                        await UpdateWebhookDebugLog(callId, "skipped", $"Failed to parse date/time: {interviewDate} {interviewTime}", null, null, false);
                        return;
                    }

                        await LogCheckpoint("CHECKPOINT-8", $"DateTime parsed successfully - {parsedDateTime.Value}", callId);

                        if (!candidateConfirmed)
                        {
                            await LogCheckpoint("CHECKPOINT-9-SKIPPED", "Candidate not confirmed", callId);
                            await UpdateWebhookDebugLog(callId, "skipped", "Candidate not confirmed", null, null, false);
                            return;
                        }

                        await LogCheckpoint("CHECKPOINT-9", "Candidate confirmed - proceeding to find/create call log", callId);

                        await LogCheckpoint("CHECKPOINT-10", "Looking for existing call log", callId);

                        // Try to find existing call log
                        var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
                        
                        await LogCheckpoint("CHECKPOINT-11", $"Call log lookup result - Found: {callLog != null}, Will create new: {callLog == null}", callId);

                        bool isNewCallLog = false;
                        if (callLog == null)
                        {
                            await LogCheckpoint("CHECKPOINT-12", "Call log not found - likely initiated from Vapi dashboard, not through application", callId);
                            
                            // Try to get call details from Vapi API
                            VapiCallResponse callDetails = null;
                            try
                            {
                                callDetails = await _vapiService.GetCallDetailsAsync(callId);
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                                    new Exception($"✅ Retrieved call details from Vapi API for CallId: {callId}"));
                            }
                            catch (Exception apiEx)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error,
                                    new Exception($"❌ Failed to retrieve call details from Vapi API for CallId: {callId}: {apiEx.Message}"));
                            }
                            
                            isNewCallLog = true;
                            callLog = new VapiCallLog
                            {
                                VapiCallId = callId,
                                RecipientPhone = callDetails?.Customer?.Number 
                                    ?? message.Message.Call?.Customer?.Number 
                                    ?? "unknown",
                                RecipientName = candidateName,
                                RecipientEmail = callDetails?.Customer?.Email 
                                    ?? message.Message.Call?.Customer?.Email,
                                CallStatus = callDetails?.Status 
                                    ?? message.Message.Call?.Status 
                                    ?? "ended",
                                AssistantId = callDetails?.AssistantId 
                                    ?? message.Message.Call?.AssistantId,
                                CallStartedAt = callDetails?.StartedAt,
                                CallEndedAt = callDetails?.EndedAt,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "Webhook"
                            };
                        }
                        //Call API with values : IRFId,ClientHr,CandidateResume,CandidateEmail,Interviewdate
                        // Update with booking information
                        callLog.InterviewBooked = true;
                        callLog.BookedInterviewDateTime = parsedDateTime.Value;
                        callLog.BookingConfirmationDetails = JsonConvert.SerializeObject(new
                        {
                            candidateName = candidateName,
                            interviewDate = interviewDate,
                            interviewTime = interviewTime,
                            timezone = timezone,
                            confirmedAt = DateTime.UtcNow,
                            toolCallId = toolCall.Id
                        });
                        callLog.UpdatedAt = DateTime.UtcNow;

                        await LogCheckpoint("CHECKPOINT-13", $"About to save to database - IsNew: {isNewCallLog}, RecipientPhone: {callLog.RecipientPhone}, RecipientName: {callLog.RecipientName}", callId);

                        // Save or update
                        try
                        {
                            int savedId = 0;
                            if (isNewCallLog)
                            {
                                await LogCheckpoint("CHECKPOINT-14", "Calling SaveCallLogAsync for new call log", callId);

                                savedId = await _vapiCallRepository.SaveCallLogAsync(callLog);

                                await LogCheckpoint("CHECKPOINT-15-SUCCESS", $"NEW call log created - ID: {savedId}, DateTime: {parsedDateTime.Value}, Candidate: {candidateName}", callId);
                                
                                // Update debug log
                                await UpdateWebhookDebugLog(callId, "success", null, true, savedId, true);
                            }
                            else
                            {
                                await LogCheckpoint("CHECKPOINT-16", $"Calling UpdateCallLogAsync for existing call log - ID: {callLog.Id}", callId);

                                await _vapiCallRepository.UpdateCallLogAsync(callLog);

                                await LogCheckpoint("CHECKPOINT-17-SUCCESS", $"Interview slot updated - DateTime: {parsedDateTime.Value}, Candidate: {candidateName}", callId);
                                
                                // Update debug log
                                await UpdateWebhookDebugLog(callId, "success", null, true, callLog.Id, true);
                            }
                        }
                        catch (Exception saveEx)
                        {
                            await LogCheckpoint("CHECKPOINT-18-ERROR", $"Database save FAILED - Error: {saveEx.Message}, InnerException: {saveEx.InnerException?.Message}", callId, additionalData: saveEx.StackTrace);
                            
                            // Update debug log with error
                            await UpdateWebhookDebugLog(callId, "error", 
                                $"Database save failed: {saveEx.Message}. InnerException: {saveEx.InnerException?.Message}. StackTrace: {saveEx.StackTrace}", 
                                null, null, false);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await LogCheckpoint("CHECKPOINT-19-EXCEPTION", $"HandleInterviewSlotSubmission method-level exception: {ex.Message}", callId ?? "unknown", additionalData: $"InnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
                
                // Try to update debug log even if we failed
                try
                {
                    await UpdateWebhookDebugLog(callId, "error", 
                        $"HandleInterviewSlotSubmission exception: {ex.Message}. InnerException: {ex.InnerException?.Message}. StackTrace: {ex.StackTrace}", 
                        null, null, false);
                }
                catch { }
            }
        }

        /// <summary>
        /// Handles end of call report
        /// </summary>
        private async Task HandleEndOfCallReport(VapiWebhookMessage message)
        {
            var call = message.Message.Call;
            if (call == null) return;

            var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(call.Id);
            if (callLog != null)
            {
                callLog.CallStatus = call.Status;
                callLog.CallEndedAt = call.EndedAt;
                callLog.EndedReason = call.EndedReason;

                if (call.StartedAt.HasValue && call.EndedAt.HasValue)
                {
                    callLog.DurationSeconds = (int)(call.EndedAt.Value - call.StartedAt.Value).TotalSeconds;
                }

                if (call.Artifact != null)
                {
                    callLog.Transcript = call.Artifact.Transcript;
                    callLog.RecordingUrl = call.Artifact.RecordingUrl;
                }

                if (call.Analysis != null)
                {
                    callLog.Summary = call.Analysis.Summary;
                    callLog.StructuredData = JsonConvert.SerializeObject(call.Analysis.StructuredData);
                }

                callLog.UpdatedAt = DateTime.UtcNow;

                await _vapiCallRepository.UpdateCallLogAsync(callLog);
            }
        }

        /// <summary>
        /// Gets call details and status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCallStatus(string callId)
        {
            try
            {
                var callDetails = await _vapiService.GetCallDetailsAsync(callId);
                var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);

                return Json(new
                {
                    success = true,
                    vapiData = callDetails,
                    localData = callLog
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lists all call logs for an interview
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInterviewCalls(int interviewId)
        {
            try
            {
                var calls = await _vapiCallRepository.GetCallLogsByInterviewIdAsync(interviewId);
                return Json(new { success = true, calls });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Manually updates booking status
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(
            string callId,
            bool isBooked,
            DateTime? bookedDateTime,
            string confirmationDetails)
        {
            try
            {
                var result = await _vapiCallRepository.UpdateBookingStatusAsync(
                    callId, isBooked, bookedDateTime, confirmationDetails);

                return Json(new
                {
                    success = result,
                    message = result ? "Booking status updated" : "Failed to update booking status"
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Generates system prompt with dynamically injected current date
        /// </summary>
        private string GetSystemPromptWithCurrentDate(string candidateName, string jobTitle)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var dayAfterTomorrow = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
            var dayOfWeek = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
            
            return $@"You are an outbound interview scheduling assistant from Risebird Interviews calling on behalf of SONATA for urgent hiring.

IDENTITY & PURPOSE:
- You represent Risebird Interviews on behalf of SONATA
- This is URGENT hiring - interviews MUST be within next 1-2 days ONLY
- Your goal: Get confirmed interview slot and collect it in structured format

CURRENT DATE INFORMATION (CRITICAL):
- Today is: {dayOfWeek} ({today})
- Tomorrow is: {tomorrow}
- Day after tomorrow is: {dayAfterTomorrow}
- When candidate says ""tomorrow"", use date: {tomorrow}
- When candidate says ""day after tomorrow"", use date: {dayAfterTomorrow}
- ONLY these dates are allowed: {tomorrow} OR {dayAfterTomorrow}

CRITICAL RULES:
- Interviews ONLY within next 1-2 days from today
- Do NOT allow scheduling beyond 2 days
- If candidate requests later dates: ""This is urgent hiring. Only slots within next 1-2 days are available.""
- If candidate insists on dates beyond 2 days: Politely end call
- ALL times must be confirmed in IST (Indian Standard Time)
- Date format in submitInterviewSlot tool: YYYY-MM-DD (e.g., {tomorrow})

CALL FLOW:
1. Greet professionally
   - ""Hello, this is Risebird Interviews calling. Is this a good time to speak?""
   - Wait for confirmation

2. State purpose and ask for name
   - ""I'm calling from Risebird on behalf of SONATA regarding an interview opportunity for {jobTitle}.""
   - ""May I have your full name please?""
   - Wait for candidate to provide their name
   - IMPORTANT: Use the EXACT name the candidate provides, NOT any pre-filled information

3. Explain urgency
   - ""This is urgent hiring and we need to schedule your interview within the next 1-2 days.""
   - ""Would you be available tomorrow or day after tomorrow?""
   - Wait for response

4. Get specific time
   - ""What time would be convenient for you?"" 
   - If they say just ""morning"" or ""afternoon"", ask: ""What specific time works best? Please specify with AM or PM.""
   - Wait for specific time (e.g., ""10:00 AM"", ""02:30 PM"")

5. Confirm details explicitly
   - Repeat EXACT name, date and time back to candidate
   - ""Let me confirm - [CANDIDATE_NAME], your interview with SONATA is scheduled for [DAY] at [TIME] IST. Is that correct?""
   - Wait for explicit confirmation (""yes"", ""correct"", ""that's right"")

6. Submit booking (ONLY after explicit confirmation)
   - Call submitInterviewSlot function with:
     * candidateName: The EXACT full name the candidate provided in step 2 (NOT the email or pre-filled value)
     * interviewDate: Date in YYYY-MM-DD format (either {tomorrow} or {dayAfterTomorrow})
     * interviewTime: Time in HH:MM AM/PM format (e.g., ""10:00 AM"", ""02:30 PM"")
     * timezone: ""IST""
     * candidateConfirmed: true (ONLY if candidate said yes/confirmed/correct)

7. End professionally
   - ""Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. Good luck!""

OBJECTION HANDLING:
- If busy now: ""I understand. This will just take 2 minutes to schedule.""
- If asks to call back: ""I have you on the line now and this is time-sensitive. Can we quickly find a slot?""
- If uncertain: ""I can see you applied for this position. The company is moving fast. Shall we lock in a time?""
- If wants different date: ""Unfortunately only {tomorrow} or {dayAfterTomorrow} are available due to urgent hiring needs. Which works better?""

VALIDATION RULES:
- Candidate name MUST be their actual full name (first and last name), NOT an email address or phone number
- Date MUST be either {tomorrow} or {dayAfterTomorrow}
- Time MUST include AM or PM (e.g., ""10:00 AM"", not just ""10:00"")
- Candidate MUST explicitly confirm with ""yes"", ""correct"", ""confirmed"", ""that's right"", or similar
- If candidate says ""maybe"", ""I think so"", ""probably"" - ask for explicit confirmation
- Do NOT submit booking without explicit confirmation

NEVER:
- Use email addresses or phone numbers as the candidate name
- Accept dates beyond 2 days from today
- Submit booking without explicit confirmation
- Use 24-hour time format (always use AM/PM)
- Forget to specify timezone as IST
- Use dates in any format other than YYYY-MM-DD in the tool call
- Use pre-filled or template values for candidateName - always use what the candidate tells you";
        }

        /// <summary>
        /// Updates the webhook debug log with processing results
        /// </summary>
        private async Task UpdateWebhookDebugLog(string callId, string status, string error, bool? callLogFound, int? callLogId, bool? dataSaved)
        {
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(_configuration["ConnectionString:DBConnectionString"]))
                {
                    var sql = @"UPDATE VapiWebhookDebugLogs 
                        SET ProcessingStatus = @Status, 
                            ErrorMessage = @Error, 
                            CallLogFound = @Found,
                            CallLogId = @LogId,
                            DataSaved = @Saved
                        WHERE CallId = @CallId 
                        AND Id = (SELECT MAX(Id) FROM VapiWebhookDebugLogs WHERE CallId = @CallId)";
                    
                    await conn.ExecuteAsync(sql, new
                    {
                        Status = status,
                        Error = error,
                        Found = callLogFound,
                        LogId = callLogId,
                        Saved = dataSaved,
                        CallId = callId
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Test endpoint to verify webhook is accessible
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult WebhookTest()
        {
            return Json(new
            {
                status = "ok",
                message = "Vapi webhook endpoint is accessible",
                timestamp = DateTime.UtcNow,
                url = $"{Request.Scheme}://{Request.Host}/VapiInterviewCall/Webhook"
            });
        }

        /// <summary>
        /// Gets recent webhook debug logs for troubleshooting
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WebhookDebugLogs(int count = 20)
        {
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(_configuration["ConnectionString:DBConnectionString"]))
                {
                    var sql = @"SELECT TOP (@Count) * 
                        FROM VapiWebhookDebugLogs 
                        ORDER BY RequestTimestamp DESC";
                    
                    var logs = await conn.QueryAsync<VapiWebhookDebugLog>(sql, new { Count = count });
                    return Json(new { success = true, logs });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Creates or updates the interview booking assistant
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SetupAssistant()
        {
            try
            {
                var baseUrl = _configuration["HostDetails:baseUrl"];
                var webhookUrl = $"{baseUrl}/VapiInterviewCall/Webhook";

                var assistantId = await _vapiService.CreateInterviewBookingAssistantAsync(
                    "Risebird Interview Booking Assistant",
                    webhookUrl);

                return Json(new
                {
                    success = true,
                    assistantId = assistantId,
                    message = "Assistant created successfully. Please save this ID in configuration: Vapi:InterviewBookingAssistantId"
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
