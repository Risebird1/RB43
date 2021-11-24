using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class AutoInterviewcancellation
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFService _irfService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly IMeetingService _meetingService;

        public AutoInterviewcancellation(IIRFService irfService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, ICandidateService candidateService, IIRFRepository irfRepository, IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _irfService = irfService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _meetingService = meetingService;
        }

        [FunctionName("AutoInterviewcancellation")]
        public async void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function(AutoInterviewcancellation) executed at: {DateTime.Now}");
            try
            {
                var _unassignedmeetinglist = await _meetingService.FetchAllUnassignedInterviewes();
                foreach (var item in _unassignedmeetinglist)
                {
                    try
                    {
                        if (_meetingService.PublicCancelInterview(item.Interviewid))
                        {
                            var setvalue = _meetingService.UpdateCancelInterviewDetails(item.Interviewid);
                            var candidateId = await _irfRepository.GetCandidateIdByEmail(item.CandidateEmail);
                            var result1 = await _irfService.AddCandidateStatus("System", candidateId, item.Irfid, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled);
                            var result = await _candidateService.FetchChannelOwnerandInterviewer(item.Interviewid);
                            var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(item.Interviewid);
                            List<string> interviewers = new List<string>();
                            if (!String.IsNullOrEmpty(result.interviewer))
                            {
                                interviewers.Add(result.interviewer);
                            }
                            if (!String.IsNullOrEmpty(result.EmailId))
                            {
                                interviewers.Add(result.EmailId);
                            }
                            var emailresult = _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList());
                            var emailresult1 = _notificationHelper.SendInterviewCancelledMailClient(result.interviewDate.ToString(), result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList());
                            var emailresult2 = _notificationHelper.SendInterviewCancelledMailCandidateWithSchedulingLink(result.candidateEmail, result.interviewDate.ToString(), result.CandidateName, result.CalendlyLink);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
