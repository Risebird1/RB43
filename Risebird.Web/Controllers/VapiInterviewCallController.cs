using Microsoft.AspNetCore.Authorization;
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
using System.Threading.Tasks;

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

                // Create call request with customer info
                var callRequest = new VapiCallRequest
                {
                    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                    Customer = new VapiCustomer
                    {
                        Number = candidatePhone,
                        Name = candidateName,
                        Email = candidateEmail
                    },
                    AssistantId = assistantId
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
        public async Task<IActionResult> Webhook([FromBody] VapiWebhookMessage message)
        {
            try
            {
                if (message?.Message == null)
                {
                    return BadRequest("Invalid webhook message");
                }

                var messageType = message.Message.Type;

                switch (messageType)
                {
                    case "status-update":
                        await HandleStatusUpdate(message);
                        break;

                    case "transcript":
                        await HandleTranscript(message);
                        break;

                    case "tool-calls":
                        var result = await _vapiService.ProcessToolCallAsync(message);
                        return Json(result);

                    case "end-of-call-report":
                        await HandleEndOfCallReport(message);
                        break;

                    default:
                        // Log unhandled message type
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                            new Exception($"Unhandled Vapi webhook message type: {messageType}"));
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = ex.Message });
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
