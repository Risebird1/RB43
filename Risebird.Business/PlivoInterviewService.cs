using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Business service for Plivo interview calling operations
    /// </summary>
    public class PlivoInterviewService : IPlivoInterviewService
    {
        private readonly IPlivoService _plivoService;
        private readonly IPlivoCallRepository _plivoCallRepository;
        private readonly IApplicationErrorLogger _logger;
        private readonly IConfiguration _configuration;

        public PlivoInterviewService(
            IPlivoService plivoService,
            IPlivoCallRepository plivoCallRepository,
            IApplicationErrorLogger logger,
            IConfiguration configuration)
        {
            _plivoService = plivoService;
            _plivoCallRepository = plivoCallRepository;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Initiates interview booking calls for a batch of recipients
        /// </summary>
        public async Task<List<PlivoCallLog>> InitiateBatchCallsAsync(List<RecipientCallInfo> recipients)
        {
            var callLogs = new List<PlivoCallLog>();
            var baseUrl = _configuration["HostDetails:baseUrl"];
            var fromNumber = _configuration["Plivo:FromNumber"];

            foreach (var recipient in recipients)
            {
                try
                {
                    if (callLogs.Count > 0)
                        await Task.Delay(2000);

                    var phone = NormalizePhoneNumber(recipient.RecipientPhone);
                    if (string.IsNullOrEmpty(phone)) continue;

                    // Save call log first so we have the DB id for the answer_url
                    var callLog = new PlivoCallLog
                    {
                        RecipientPhone = phone,
                        RecipientName  = recipient.RecipientName,
                        RecipientEmail = recipient.RecipientEmail,
                        FromNumber     = fromNumber,
                        InterviewId    = recipient.InterviewId,
                        RecipientId    = recipient.RecipientId,
                        CallStatus     = "queued",
                        CreatedAt      = DateTime.UtcNow,
                        CreatedBy      = "PlivoInterviewService"
                    };
                    var logId = await _plivoCallRepository.SaveCallLogAsync(callLog);
                    callLog.Id = logId;

                    var answerUrl       = $"{baseUrl}/PlivoInterviewCall/AnswerXml/{logId}";
                    var hangupUrl       = $"{baseUrl}/PlivoInterviewCall/HangupCallback";
                    var ringUrl         = $"{baseUrl}/PlivoInterviewCall/RingCallback";

                    var callRequest = new PlivoOutboundCallRequest
                    {
                        To         = phone,
                        From       = fromNumber,
                        AnswerUrl  = answerUrl,
                        HangupUrl  = hangupUrl,
                        RingUrl    = ringUrl,
                        TimeLimit  = 300
                    };

                    var response = await _plivoService.InitiateCallAsync(callRequest);

                    callLog.PlivoRequestUUID = response.RequestUUID;
                    callLog.CallStatus       = "queued";
                    callLog.UpdatedAt        = DateTime.UtcNow;
                    await _plivoCallRepository.UpdateCallLogAsync(callLog);

                    callLogs.Add(callLog);
                }
                catch (Exception ex)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }

            return callLogs;
        }

        /// <summary>
        /// Marks a call log as having a confirmed interview booking
        /// </summary>
        public async Task<bool> ProcessInterviewBookingAsync(string callUUID, DateTime interviewDateTime)
        {
            try
            {
                return await _plivoCallRepository.UpdateBookingStatusAsync(
                    callUUID,
                    true,
                    interviewDateTime,
                    $"Interview booked via Plivo AI call on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Returns calls that ended without a booking in the last 7 days
        /// </summary>
        public async Task<List<PlivoCallLog>> GetPendingCallbacksAsync()
        {
            try
            {
                var recent = await _plivoCallRepository.GetRecentCallLogsAsync(500);
                return recent
                    .Where(c =>
                        (c.CallStatus == "completed" || c.CallStatus == "answered") &&
                        c.InterviewBooked != true &&
                        c.CallEndedAt.HasValue &&
                        c.CallEndedAt.Value > DateTime.UtcNow.AddDays(-7))
                    .ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCallLog>();
            }
        }

        private static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (!phone.StartsWith("+"))
                phone = phone.Length == 10 ? "+91" + phone : "+" + phone;
            return phone;
        }
    }
}
