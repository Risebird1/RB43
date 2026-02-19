// ============================================
// VAPI CUSTOM ASSISTANT - CODE EXAMPLES
// ============================================

using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.VapiModels;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Examples
{
    /// <summary>
    /// Examples of how to use your custom Vapi interview scheduling assistant
    /// </summary>
    public class VapiCustomAssistantExamples
    {
        private readonly IConfiguration _configuration;
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;

        public VapiCustomAssistantExamples(
            IConfiguration configuration,
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository)
        {
            _configuration = configuration;
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
        }

        // ============================================
        // EXAMPLE 1: Single Call - From Interview ID
        // ============================================
        public async Task<object> Example1_SingleCallFromInterview(int interviewId)
        {
            // Get interview and candidate details from your database
            // var interview = await _interviewRepository.GetByIdAsync(interviewId);
            // var candidate = await _candidateRepository.GetByIdAsync(interview.CandidateId);

            // For this example, using sample data:
            var candidatePhone = "+919876543210";
            var candidateName = "Rahul Kumar";
            var candidateEmail = "rahul@example.com";

            // Ensure phone is in E.164 format
            if (!candidatePhone.StartsWith("+"))
            {
                candidatePhone = "+91" + candidatePhone;
            }

            // Create call request with YOUR CUSTOM ASSISTANT
            var callRequest = new VapiCallRequest
            {
                PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                Customer = new VapiCustomer
                {
                    Number = candidatePhone,
                    Name = candidateName,
                    Email = candidateEmail
                },
                // This uses your custom assistant with the improved instructions
                AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
            };

            // Initiate the call
            var callResponse = await _vapiService.InitiateCallAsync(callRequest);

            // Save to database
            var callLog = new VapiCallLog
            {
                VapiCallId = callResponse.Id,
                InterviewId = interviewId,
                RecipientId = 123, // candidateId
                RecipientPhone = candidatePhone,
                RecipientName = candidateName,
                RecipientEmail = candidateEmail,
                CallStatus = callResponse.Status,
                AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _vapiCallRepository.SaveCallLogAsync(callLog);

            return new
            {
                success = true,
                callId = callResponse.Id,
                status = callResponse.Status,
                message = "Interview scheduling call initiated"
            };
        }

        // ============================================
        // EXAMPLE 2: Bulk Calls with Rate Limiting
        // ============================================
        public async Task<List<object>> Example2_BulkCallsWithRateLimiting(List<RecipientCallInfo> candidates)
        {
            var results = new List<object>();

            foreach (var candidate in candidates)
            {
                try
                {
                    // Add 2-second delay between calls to avoid rate limiting
                    if (results.Count > 0)
                    {
                        await Task.Delay(2000);
                    }

                    var phone = candidate.RecipientPhone.StartsWith("+")
                        ? candidate.RecipientPhone
                        : "+91" + candidate.RecipientPhone;

                    var callRequest = new VapiCallRequest
                    {
                        PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                        Customer = new VapiCustomer
                        {
                            Number = phone,
                            Name = candidate.RecipientName,
                            Email = candidate.RecipientEmail
                        },
                        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
                    };

                    var callResponse = await _vapiService.InitiateCallAsync(callRequest);

                    var callLog = new VapiCallLog
                    {
                        VapiCallId = callResponse.Id,
                        InterviewId = candidate.InterviewId,
                        RecipientId = candidate.RecipientId,
                        RecipientPhone = phone,
                        RecipientName = candidate.RecipientName,
                        RecipientEmail = candidate.RecipientEmail,
                        CallStatus = callResponse.Status,
                        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    await _vapiCallRepository.SaveCallLogAsync(callLog);

                    results.Add(new
                    {
                        success = true,
                        candidateName = candidate.RecipientName,
                        callId = callResponse.Id,
                        message = "Call initiated"
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        success = false,
                        candidateName = candidate.RecipientName,
                        error = ex.Message
                    });
                }
            }

            return results;
        }

        // ============================================
        // EXAMPLE 3: Call with Retry Logic
        // ============================================
        public async Task<object> Example3_CallWithRetry(
            int interviewId,
            string candidatePhone,
            string candidateName,
            string candidateEmail,
            int maxRetries = 3)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    if (!candidatePhone.StartsWith("+"))
                    {
                        candidatePhone = "+91" + candidatePhone;
                    }

                    var callRequest = new VapiCallRequest
                    {
                        PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                        Customer = new VapiCustomer
                        {
                            Number = candidatePhone,
                            Name = candidateName,
                            Email = candidateEmail
                        },
                        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
                    };

                    var callResponse = await _vapiService.InitiateCallAsync(callRequest);

                    var callLog = new VapiCallLog
                    {
                        VapiCallId = callResponse.Id,
                        InterviewId = interviewId,
                        RecipientPhone = candidatePhone,
                        RecipientName = candidateName,
                        RecipientEmail = candidateEmail,
                        CallStatus = callResponse.Status,
                        AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    await _vapiCallRepository.SaveCallLogAsync(callLog);

                    return new
                    {
                        success = true,
                        callId = callResponse.Id,
                        retriesUsed = retryCount,
                        message = "Call initiated successfully"
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;

                    if (retryCount < maxRetries)
                    {
                        // Wait before retrying (exponential backoff)
                        await Task.Delay(1000 * retryCount);
                    }
                }
            }

            return new
            {
                success = false,
                retriesUsed = retryCount,
                error = lastException?.Message ?? "Unknown error",
                message = $"Failed after {maxRetries} attempts"
            };
        }

        // ============================================
        // EXAMPLE 4: Check Booking Status After Call
        // ============================================
        public async Task<object> Example4_CheckBookingStatus(string vapiCallId)
        {
            // Get call log from database
            var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(vapiCallId);

            if (callLog == null)
            {
                return new { success = false, message = "Call not found" };
            }

            // Check if interview was booked
            if (callLog.InterviewBooked == true && callLog.BookedInterviewDateTime.HasValue)
            {
                // Parse the confirmation details
                var details = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(
                    callLog.BookingConfirmationDetails ?? "{}");

                return new
                {
                    success = true,
                    isBooked = true,
                    interviewDateTime = callLog.BookedInterviewDateTime.Value,
                    candidateName = (string)details.candidateName,
                    timezone = (string)details.timezone,
                    confirmedAt = (DateTime)details.confirmedAt,
                    callStatus = callLog.CallStatus,
                    duration = callLog.DurationSeconds,
                    message = "Interview successfully booked"
                };
            }
            else
            {
                return new
                {
                    success = true,
                    isBooked = false,
                    callStatus = callLog.CallStatus,
                    endedReason = callLog.EndedReason,
                    message = "Interview not booked - candidate may not have confirmed"
                };
            }
        }

        // ============================================
        // EXAMPLE 5: Get All Pending Bookings
        // ============================================
        public async Task<List<object>> Example5_GetPendingBookings()
        {
            // Get recent calls that haven't been booked yet
            var recentCalls = await _vapiCallRepository.GetRecentCallLogsAsync(100);

            var pendingBookings = new List<object>();

            foreach (var call in recentCalls)
            {
                // Only include calls that ended but didn't result in a booking
                if (call.CallStatus == "ended" &&
                    call.InterviewBooked != true &&
                    call.EndedReason != "no-answer" &&
                    call.EndedReason != "busy")
                {
                    pendingBookings.Add(new
                    {
                        callId = call.VapiCallId,
                        candidateName = call.RecipientName,
                        candidatePhone = call.RecipientPhone,
                        callDate = call.CreatedAt,
                        endedReason = call.EndedReason,
                        duration = call.DurationSeconds
                    });
                }
            }

            return pendingBookings;
        }

        // ============================================
        // EXAMPLE 6: Scheduled Calls (Call Tomorrow)
        // ============================================
        public async Task<object> Example6_ScheduleCallForLater(
            int interviewId,
            string candidatePhone,
            string candidateName,
            string candidateEmail,
            DateTime scheduledTime)
        {
            // Note: Vapi doesn't have native scheduled calls
            // You would implement this using your own job scheduler
            // This is a conceptual example

            if (!candidatePhone.StartsWith("+"))
            {
                candidatePhone = "+91" + candidatePhone;
            }

            // Save as scheduled in your database
            var callLog = new VapiCallLog
            {
                VapiCallId = "SCHEDULED_" + Guid.NewGuid().ToString(),
                InterviewId = interviewId,
                RecipientPhone = candidatePhone,
                RecipientName = candidateName,
                RecipientEmail = candidateEmail,
                CallStatus = "scheduled",
                ScheduledDateTime = scheduledTime,
                AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _vapiCallRepository.SaveCallLogAsync(callLog);

            // Then use a job scheduler (Hangfire, Azure Functions, etc.) to:
            // 1. Check for scheduled calls
            // 2. Initiate them at the scheduled time
            // 3. Update the VapiCallId with the real one from Vapi

            return new
            {
                success = true,
                scheduledFor = scheduledTime,
                message = "Call scheduled successfully"
            };
        }

        // ============================================
        // EXAMPLE 7: Get Call Statistics
        // ============================================
        public async Task<object> Example7_GetCallStatistics(DateTime? startDate = null)
        {
            startDate = startDate ?? DateTime.Now.AddDays(-7);
            var calls = await _vapiCallRepository.GetRecentCallLogsAsync(1000);

            var callsInRange = calls.Where(c => c.CreatedAt >= startDate).ToList();

            var stats = new
            {
                totalCalls = callsInRange.Count,
                successfulBookings = callsInRange.Count(c => c.InterviewBooked == true),
                pendingFollowups = callsInRange.Count(c =>
                    c.CallStatus == "ended" &&
                    c.InterviewBooked != true),
                noAnswers = callsInRange.Count(c => c.EndedReason == "no-answer"),
                avgDuration = callsInRange.Where(c => c.DurationSeconds.HasValue)
                    .Average(c => c.DurationSeconds.Value),
                totalCost = callsInRange.Where(c => c.CallCost.HasValue)
                    .Sum(c => c.CallCost.Value),
                conversionRate = callsInRange.Count > 0
                    ? (callsInRange.Count(c => c.InterviewBooked == true) * 100.0 / callsInRange.Count)
                    : 0
            };

            return stats;
        }
    }

    // ============================================
    // CONTROLLER EXAMPLE
    // ============================================
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewSchedulingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;

        public InterviewSchedulingController(
            IConfiguration configuration,
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository)
        {
            _configuration = configuration;
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
        }

        /// <summary>
        /// Initiates a single interview scheduling call
        /// POST: api/InterviewScheduling/call
        /// </summary>
        [HttpPost("call")]
        public async Task<IActionResult> InitiateCall([FromBody] CallRequest request)
        {
            try
            {
                var phone = request.Phone.StartsWith("+") ? request.Phone : "+91" + request.Phone;

                var callRequest = new VapiCallRequest
                {
                    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                    Customer = new VapiCustomer
                    {
                        Number = phone,
                        Name = request.Name,
                        Email = request.Email
                    },
                    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"]
                };

                var response = await _vapiService.InitiateCallAsync(callRequest);

                var callLog = new VapiCallLog
                {
                    VapiCallId = response.Id,
                    InterviewId = request.InterviewId,
                    RecipientPhone = phone,
                    RecipientName = request.Name,
                    RecipientEmail = request.Email,
                    CallStatus = response.Status,
                    AssistantId = _configuration["Vapi:InterviewBookingAssistantId"],
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "API"
                };

                await _vapiCallRepository.SaveCallLogAsync(callLog);

                return Ok(new { success = true, callId = response.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Checks the status of a call
        /// GET: api/InterviewScheduling/status/{callId}
        /// </summary>
        [HttpGet("status/{callId}")]
        public async Task<IActionResult> GetCallStatus(string callId)
        {
            try
            {
                var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);

                if (callLog == null)
                {
                    return NotFound(new { success = false, message = "Call not found" });
                }

                return Ok(new
                {
                    success = true,
                    callStatus = callLog.CallStatus,
                    isBooked = callLog.InterviewBooked,
                    interviewDateTime = callLog.BookedInterviewDateTime,
                    duration = callLog.DurationSeconds
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }

    // ============================================
    // REQUEST MODELS
    // ============================================
    public class CallRequest
    {
        public int? InterviewId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
