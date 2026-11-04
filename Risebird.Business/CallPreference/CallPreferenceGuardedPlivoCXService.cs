using System;
using System.Threading.Tasks;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Repository.Interface;

namespace Risebird.Business.CallPreference
{
    /// <summary>
    /// Pre-dial check for every Plivo CX call a portal user starts (the Web app's call buttons): wraps
    /// <see cref="IPlivoCXService"/> and refuses the trigger, returning <c>Error</c> like a failed trigger, when the
    /// number opted out. Interviewer / prospect calls (availability, onboarding) check the interviewer opt-out;
    /// candidate calls (outbound, reschedule) check the cold / opt-out state in that skill channel. A candidate call
    /// whose IrfId isn't a numeric IRF.id can't be checked and goes through. The timer jobs check before writing
    /// their call logs, so they use the plain service.
    /// </summary>
    public class CallPreferenceGuardedPlivoCXService : IPlivoCXService
    {
        private readonly IPlivoCXService _inner;
        private readonly ICallPreferenceRepository _callPreferenceRepository;
        private readonly IApplicationErrorLogger _logger;

        public CallPreferenceGuardedPlivoCXService(IPlivoCXService inner, ICallPreferenceRepository callPreferenceRepository, IApplicationErrorLogger logger)
        {
            _inner = inner;
            _callPreferenceRepository = callPreferenceRepository;
            _logger = logger;
        }

        public async Task<PlivoCXTriggerResponse> TriggerOutboundCallAsync(PlivoCXTriggerRequest request) =>
            await BlockedCandidate(request) ?? await _inner.TriggerOutboundCallAsync(request);

        public async Task<PlivoCXTriggerResponse> TriggerRescheduleCallAsync(PlivoCXTriggerRequest request) =>
            await BlockedCandidate(request) ?? await _inner.TriggerRescheduleCallAsync(request);

        public async Task<PlivoCXTriggerResponse> TriggerOnboardingCallAsync(PlivoCXOnboardingTriggerRequest request) =>
            await BlockedInterviewer(request?.PhoneNumber) ?? await _inner.TriggerOnboardingCallAsync(request);

        public async Task<PlivoCXTriggerResponse> TriggerAvailabilityCallAsync(PlivoCXAvailabilityTriggerRequest request) =>
            await BlockedInterviewer(request?.PhoneNumber) ?? await _inner.TriggerAvailabilityCallAsync(request);

        public Task<PlivoCXTriggerResponse> TriggerByUrlAsync(string triggerUrl, object request) =>
            _inner.TriggerByUrlAsync(triggerUrl, request);

        public Task<PhoneScreenMediaResult> FetchCallMediaAsync(string callUuid, string transcriptionCallbackUrl = null) =>
            _inner.FetchCallMediaAsync(callUuid, transcriptionCallbackUrl);

        public Task<string> GetRecordingUrlAsync(string callUuid) => _inner.GetRecordingUrlAsync(callUuid);

        public Task<PhoneScreenMediaResult> GetRecordingInfoAsync(string callUuid) => _inner.GetRecordingInfoAsync(callUuid);

        private async Task<PlivoCXTriggerResponse> BlockedInterviewer(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            return await Check("Interviewer", phone, null);
        }

        private async Task<PlivoCXTriggerResponse> BlockedCandidate(PlivoCXTriggerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.PhoneNumber) || !int.TryParse(request.IrfId, out var irfId) || irfId <= 0)
                return null;
            return await Check("Candidate", request.PhoneNumber, irfId);
        }

        // A failed check (DB error) lets the call through, as before this check existed: a call button must not
        // start failing because the check could not run. The error is logged.
        private async Task<PlivoCXTriggerResponse> Check(string personType, string phone, int? irfId)
        {
            try
            {
                return Blocked(await _callPreferenceRepository.CanCallContact(personType, phone, irfId));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private static PlivoCXTriggerResponse Blocked(CanCallResult result) =>
            result == null || result.CanCall ? null : new PlivoCXTriggerResponse { Error = "Not called: " + result.Reason };
    }
}
