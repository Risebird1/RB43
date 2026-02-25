using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.VapiModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Azure Timer Function for automated Vapi interview scheduling calls
    /// Runs every 15 minutes between 9 AM and 9 PM to call eligible candidates
    /// </summary>
    public class VapiInterviewSchedulingJob
    {
        private readonly IConfiguration _configuration;
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public VapiInterviewSchedulingJob(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<VapiInterviewSchedulingJob>();
        }

        /// <summary>
        /// Timer trigger that runs every 15 minutes
        /// CRON: "0 */15 * * * *" = every 15 minutes
        /// </summary>
        [Function("VapiInterviewSchedulingJob")]
        public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
        {
            var currentTime = DateTime.Now;
            var currentHour = currentTime.Hour;

            _log.LogInformation($"Vapi Interview Scheduling Job triggered at: {currentTime}");

            try
            {
                // Only run between 9 AM (9) and 9 PM (21)
                if (currentHour < 9 || currentHour >= 21)
                {
                    _log.LogInformation($"Skipping execution - current time {currentTime:HH:mm} is outside business hours (9 AM - 9 PM)");
                    return;
                }

                _log.LogInformation($"Executing within business hours - Current time: {currentTime:HH:mm}");

                // Get eligible candidates for calls (max 10)
                var eligibleCandidates = await _vapiCallRepository.GetEligibleCandidatesForVapiCallsAsync(10);

                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    _log.LogInformation("No eligible candidates found for calling");
                    return;
                }

                _log.LogInformation($"Found {eligibleCandidates.Count} eligible candidates for calling");

                // Get configuration values
                var baseUrl = _configuration["HostDetails:baseUrl"];
                var assistantId = _configuration["Vapi:InterviewBookingAssistantId"];

                if (string.IsNullOrEmpty(assistantId))
                {
                    _log.LogError("Vapi Assistant ID is not configured. Please set Vapi:InterviewBookingAssistantId in configuration.");
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Error,
                        new Exception("Vapi Assistant ID is not configured for automated interview scheduling"));
                    return;
                }

                // Process each candidate
                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        _log.LogInformation($"Processing candidate: {candidate.CandidateName} (Email: {candidate.CandidateEmail}, IRFId: {candidate.IRFId}, Attempts: {candidate.CallAttempts})");

                        // Validate phone number
                        if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        {
                            _log.LogWarning($"Skipping candidate {candidate.CandidateEmail} - No phone number");
                            continue;
                        }

                        // Ensure phone number is in E.164 format
                        var phoneNumber = candidate.CandidatePhone.Trim();
                        if (!phoneNumber.StartsWith("+"))
                        {
                            phoneNumber = "+91" + phoneNumber; // Default to India country code
                        }

                        // Get webhook URL
                        var webhookUrl = $"{baseUrl}/VapiInterviewCall/Webhook";

                        // Inject today's date and client name dynamically into system prompt
                        var systemPromptWithDate = GetSystemPromptWithCurrentDate(
                            candidate.CandidateName, 
                            candidate.JobTitle ?? candidate.IRFName, 
                            candidate.IRFId.ToString(),
                            candidate.CandidateEmail,
                            candidate.ClientName);

                        // Create call request
                        var callRequest = new VapiCallRequest
                        {
                            PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                            Customer = new VapiCustomer
                            {
                                Number = phoneNumber,
                                Name = candidate.CandidateName,
                                Email = candidate.CandidateEmail
                            },
                            AssistantId = assistantId,
                            AssistantOverrides = new VapiAssistantOverrides
                            {
                                Model = new VapiModelOverride
                                {
                                    Provider = "openai",
                                    Model = "gpt-5-mini",
                                    Messages = new List<VapiMessage>
                                    {
                                        new VapiMessage
                                        {
                                            Role = "system",
                                            Content = systemPromptWithDate
                                        }
                                    }
                                }
                            },
                            VariableValues = new Dictionary<string, object>
                            {
                                { "irfId", candidate.IRFId.ToString() },
                                { "candidateEmail", candidate.CandidateEmail }
                            }
                        };

                        // Initiate the call
                        var callResponse = await _vapiService.InitiateCallAsync(callRequest);

                        _log.LogInformation($"Call initiated successfully for {candidate.CandidateEmail} - CallId: {callResponse.Id}, Status: {callResponse.Status}");

                        // Save call log to database
                        var callLog = new VapiCallLog
                        {
                            VapiCallId = callResponse.Id,
                            InterviewId = null, // Will be set later if interview is booked
                            IRFId = candidate.IRFId,
                            RecipientId = candidate.CandidateId,
                            RecipientPhone = phoneNumber,
                            RecipientName = candidate.CandidateName,
                            RecipientEmail = candidate.CandidateEmail,
                            CallStatus = callResponse.Status,
                            ScheduledDateTime = null,
                            CallStartedAt = callResponse.StartedAt,
                            AssistantId = assistantId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "VapiTimerFunction"
                        };

                        await _vapiCallRepository.SaveCallLogAsync(callLog);

                        successCount++;

                        // Add small delay between calls to avoid rate limiting
                        await Task.Delay(2000); // 2 second delay
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _log.LogError($"Failed to initiate call for candidate {candidate.CandidateEmail}: {ex.Message}");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, 
                            new Exception($"Vapi call failed for candidate {candidate.CandidateEmail} (IRFId: {candidate.IRFId})", ex));
                    }
                }

                _log.LogInformation($"Vapi Interview Scheduling Job completed - Success: {successCount}, Failed: {failureCount}, Total: {eligibleCandidates.Count}");
            }
            catch (Exception ex)
            {
                _log.LogError($"Vapi Interview Scheduling Job failed: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Generates system prompt with dynamically injected current date and client name
        /// This mirrors the method in VapiInterviewCallController but is self-contained for the timer function
        /// </summary>
        private string GetSystemPromptWithCurrentDate(string candidateName, string jobTitle, string irfId, string candidateEmail, string clientName)
        {
            var dayOfWeek = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            return $@"You are an outbound interview scheduling assistant from Risebird Interviews calling on behalf of {clientName} for urgent hiring.

IDENTITY & PURPOSE:
- You represent Risebird Interviews on behalf of {clientName}
- This is URGENT and time-sensitive hiring - interviews MUST be scheduled within the next TWO days ONLY
- Your goal: Politely convey urgency and secure a confirmed interview slot within the next two days

CURRENT DATE INFORMATION (CRITICAL):
- Today is: {dayOfWeek}
- You can ONLY offer: TOMORROW or DAY AFTER TOMORROW
- When speaking to candidate, use the words tomorrow or day after tomorrow ONLY
- NEVER read out specific dates like numbers
- IRF ID for this interview: {irfId}
- Candidate email for confirmation: {candidateEmail}

CRITICAL RULES:
- Interviews MUST be within the next TWO days from today (tomorrow or day after tomorrow ONLY)
- Do NOT allow scheduling beyond 2 days under any circumstances
- When speaking dates: Always say tomorrow or day after tomorrow - NEVER read out specific date numbers
- If candidate requests later dates: ""I apologize, but due to the urgent nature of this hiring process, we can only accommodate slots within the next two days. Would either tomorrow or the day after work for you?""
- If candidate insists on dates beyond 2 days: Politely explain this is not possible and end call gracefully
- ALL times must be confirmed in IST (Indian Standard Time)

CALL FLOW:
1. Greet and confirm identity
   - ""Am I speaking with {candidateName}?""
   - Wait for confirmation (yes/no)
   - If wrong person: ""I apologize for the confusion. Could you please let {candidateName} know we called? Thank you."" Then end call.

2. State purpose with urgency
   - ""I'm calling on behalf of {clientName} regarding an interview opportunity for the {jobTitle} position.""
   - ""This is time-sensitive and we're looking to schedule interviews within the next two days.""
   - ""Would you be available tomorrow or the day after tomorrow?""
   - Wait for response

3. Get specific time
   - ""What time would work best for you?"" 
   - If they say just ""morning"" or ""afternoon"", ask: ""What specific time would be most convenient? Please specify with AM or PM.""
   - Wait for specific time (e.g., ""10:00 AM"", ""02:30 PM"")

4. Confirm details explicitly
   - ""Thank you! Let me confirm the day and time - that's [tomorrow/day after tomorrow] at [TIME] IST. Is that correct?""
   - Wait for explicit confirmation (""yes"", ""correct"", ""that's right"")
   - IMPORTANT: When confirming, say tomorrow or day after tomorrow, do NOT read out date numbers

5. Submit booking (ONLY after explicit confirmation)
   - Call submitInterviewSlot function with:
     * candidateName: {candidateName}
     * candidateEmail: {candidateEmail}
     * interviewDate: Use tomorrow or day after tomorrow as the value
     * interviewTime: Time in HH:MM AM/PM format (e.g., ""10:00 AM"", ""02:30 PM"")
     * timezone: ""IST""
     * candidateConfirmed: true (ONLY if candidate said yes/confirmed/correct)
     * irfId: {irfId}

6. End professionally with gratitude and DISCONNECT
   - ""Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. We appreciate your time and look forward to speaking with you. Good luck!""
   - After you say this sentence, END THE CALL IMMEDIATELY and hang up. Do not say anything else. Do not keep the line open. Do not continue the conversation.

OBJECTION HANDLING (BE POLITE BUT PERSISTENT):
- If busy now: ""I completely understand. This will only take 2 minutes to find a suitable time. May we proceed?""
- If asks to call back: ""I appreciate that, and since I have you on the line and this is time-sensitive, could we quickly lock in a time that works for you? It will just take a moment.""
- If uncertain about job: ""I understand. You had applied for this position, and the company is moving quickly with qualified candidates. Would you like to secure an interview slot?""
- If wants different date beyond 2 days: ""I appreciate your preference, but unfortunately, due to the urgent hiring timeline, we can only offer slots within the next two days - either tomorrow or the day after. Would either of those work for you?""

VALIDATION RULES:
- Date MUST be either tomorrow or day after tomorrow - NO EXCEPTIONS
- When SPEAKING to candidate: Use words tomorrow or day after tomorrow ONLY
- When SUBMITTING to tool: Use tomorrow or day after tomorrow as the interviewDate value
- Time MUST include AM or PM (e.g., ""10:00 AM"", not just ""10:00"")
- Candidate MUST explicitly confirm the final details with ""yes"", ""correct"", ""confirmed"", ""that's right"", or similar
- If candidate says ""maybe"", ""I think so"", ""probably"" - ask for explicit confirmation: ""I want to make sure we have this right. Can you confirm this time works for you?""
- Do NOT submit booking without explicit confirmation
- ALWAYS include the IRF ID and candidate email when submitting the slot

DISCONNECT RULES:
- Once you have submitted the booking and spoken the final confirmation sentence, you MUST end the call immediately.
- Do NOT wait silently on the line, do NOT ask additional questions, and do NOT engage in further small talk.

NEVER:
- Accept dates beyond 2 days from today (be firm but polite about this)
- Submit booking without explicit confirmation
- Use 24-hour time format (always use AM/PM)
- Forget to specify timezone as IST
- Read out specific date numbers when speaking
- Forget to include irfId and candidate email in the tool call";
        }
    }
}
