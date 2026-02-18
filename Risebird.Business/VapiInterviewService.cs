using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.VapiModels;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Business service for Vapi interview calling operations
    /// </summary>
    public class VapiInterviewService : IVapiInterviewService
    {
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;
        private readonly IApplicationErrorLogger _logger;
        private readonly string _assistantId;
        private readonly string _phoneNumberId;

        public VapiInterviewService(
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository,
            IApplicationErrorLogger logger,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
            _logger = logger;
            _assistantId = configuration["Vapi:InterviewBookingAssistantId"];
            _phoneNumberId = configuration["Vapi:PhoneNumberId"];
        }

        /// <summary>
        /// Initiates interview booking calls for multiple candidates
        /// </summary>
        public async Task<List<VapiCallLog>> InitiateBatchCallsAsync(List<CandidateCallInfo> candidates)
        {
            var callLogs = new List<VapiCallLog>();

            foreach (var candidate in candidates)
            {
                try
                {
                    // Add delay between calls to avoid rate limiting
                    if (callLogs.Count > 0)
                    {
                        await Task.Delay(2000); // 2 second delay between calls
                    }

                    var callRequest = new VapiCallRequest
                    {
                        PhoneNumberId = _phoneNumberId,
                        Customer = new VapiCustomer
                        {
                            Number = NormalizePhoneNumber(candidate.CandidatePhone),
                            Name = candidate.CandidateName,
                            Email = candidate.CandidateEmail
                        },
                        AssistantId = _assistantId
                    };

                    var response = await _vapiService.InitiateCallAsync(callRequest);

                    var callLog = new VapiCallLog
                    {
                        VapiCallId = response.Id,
                        InterviewId = candidate.InterviewId,
                        CandidateId = candidate.CandidateId,
                        CandidatePhone = candidate.CandidatePhone,
                        CandidateName = candidate.CandidateName,
                        CandidateEmail = candidate.CandidateEmail,
                        CallStatus = response.Status,
                        ScheduledDateTime = candidate.PreferredDateTime,
                        AssistantId = _assistantId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    await _vapiCallRepository.SaveCallLogAsync(callLog);
                    callLogs.Add(callLog);
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(Entities.Enum.LogLevelInfo.Exception, ex);
                    // Continue with next candidate even if one fails
                }
            }

            return callLogs;
        }

        /// <summary>
        /// Gets call statistics for reporting
        /// </summary>
        public async Task<VapiCallStatistics> GetCallStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var allCalls = await _vapiCallRepository.GetRecentCallLogsAsync(1000);

            // Filter by date range if provided
            if (startDate.HasValue)
            {
                allCalls = allCalls.Where(c => c.CreatedAt >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                allCalls = allCalls.Where(c => c.CreatedAt <= endDate.Value).ToList();
            }

            var totalCalls = allCalls.Count;
            var successfulCalls = allCalls.Count(c => c.CallStatus == "ended" && c.EndedReason != "no-answer");
            var failedCalls = allCalls.Count(c => c.CallStatus == "failed" || c.EndedReason == "no-answer");
            var bookingsCompleted = allCalls.Count(c => c.InterviewBooked == true);
            var pendingCallbacks = allCalls.Count(c => c.CallStatus == "ended" && c.InterviewBooked != true);

            var callsWithDuration = allCalls.Where(c => c.DurationSeconds.HasValue).ToList();
            var avgDuration = callsWithDuration.Any() ? callsWithDuration.Average(c => c.DurationSeconds.Value) : 0;

            var totalCost = allCalls.Where(c => c.CallCost.HasValue).Sum(c => (double)c.CallCost.Value);

            var conversionRate = totalCalls > 0 ? (bookingsCompleted * 100.0 / totalCalls) : 0;

            var callsByStatus = allCalls
                .GroupBy(c => c.CallStatus ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            return new VapiCallStatistics
            {
                TotalCalls = totalCalls,
                SuccessfulCalls = successfulCalls,
                FailedCalls = failedCalls,
                BookingsCompleted = bookingsCompleted,
                PendingCallbacks = pendingCallbacks,
                AverageDurationSeconds = avgDuration,
                TotalCost = totalCost,
                BookingConversionRate = conversionRate,
                CallsByStatus = callsByStatus
            };
        }

        /// <summary>
        /// Processes successful interview booking
        /// </summary>
        public async Task<bool> ProcessInterviewBookingAsync(string callId, DateTime interviewDateTime)
        {
            try
            {
                var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
                if (callLog == null) return false;

                // Update the call log with booking information
                await _vapiCallRepository.UpdateBookingStatusAsync(
                    callId,
                    true,
                    interviewDateTime,
                    $"Interview booked via voice AI call on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");

                // TODO: Integrate with your interview scheduling system
                // - Update interview table
                // - Send confirmation email
                // - Update candidate status
                // - Notify interviewer

                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(Entities.Enum.LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Sends follow-up after unsuccessful call
        /// </summary>
        public async Task<bool> SendFollowUpAsync(string callId)
        {
            try
            {
                var callLog = await _vapiCallRepository.GetCallLogByVapiCallIdAsync(callId);
                if (callLog == null) return false;

                // TODO: Integrate with your communication system
                // - Send email
                // - Send SMS
                // - Send WhatsApp message
                // - Schedule retry call

                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(Entities.Enum.LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets candidates who need follow-up calls
        /// </summary>
        public async Task<List<VapiCallLog>> GetPendingCallbacksAsync()
        {
            try
            {
                var recentCalls = await _vapiCallRepository.GetRecentCallLogsAsync(500);

                // Filter for calls that ended without booking
                var pendingCallbacks = recentCalls
                    .Where(c =>
                        c.CallStatus == "ended" &&
                        c.InterviewBooked != true &&
                        (c.EndedReason == "user-ended" || c.EndedReason == "assistant-ended") &&
                        c.CallEndedAt.HasValue &&
                        c.CallEndedAt.Value > DateTime.UtcNow.AddDays(-7)) // Within last 7 days
                    .ToList();

                return pendingCallbacks;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(Entities.Enum.LogLevelInfo.Exception, ex);
                return new List<VapiCallLog>();
            }
        }

        /// <summary>
        /// Normalizes phone number to E.164 format
        /// </summary>
        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            // Remove spaces, dashes, parentheses
            phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Add + if missing
            if (!phone.StartsWith("+"))
            {
                // Assume India country code if no country code
                if (phone.Length == 10)
                {
                    phone = "+91" + phone;
                }
                else
                {
                    phone = "+" + phone;
                }
            }

            return phone;
        }
    }
}
