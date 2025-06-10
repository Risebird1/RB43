using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class OpenInterviewsNotification
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;
        private readonly ILogger log;

        public OpenInterviewsNotification(ILoggerFactory loggerFactory, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
            log = loggerFactory.CreateLogger<OpenInterviewsNotification>();
        }
        [Function("OpenInterviewsNotification")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer) //after every 30 minutes
        {
            try
            {
                log.LogInformation($"C# OpenInterviewsNotification started executing at: {DateTime.Now}");

                //get all the unassigned meetings scheduled three hours from now

                var unassignedInterviewMeetings = await _meetingRespository.GetUnassignedOpenInterviews();

                if (unassignedInterviewMeetings != null)
                {
                    var distinctIRF = unassignedInterviewMeetings.Select(x => x.irfIntId).Distinct();
                    foreach (var irf in distinctIRF)
                    {
                        var irfName = unassignedInterviewMeetings.Where(x => x.irfIntId == irf)?.FirstOrDefault().IRFId;
                        //var irfInterviewers = await _meetingRespository.GetIRFActiveInterviewers(irfName);
                        var unassignedMeetingsByIRF = unassignedInterviewMeetings?.Where(x => x.irfIntId == irf);

                        foreach (var meeting in unassignedMeetingsByIRF)
                        {

                            //get all the active interviewers for the IRF
                            var irfInterviewers = await _meetingRespository.GetEmergencyInterviewersForIRF(meeting.irfIntId);

                            //var irfInterviewers = await _meetingRespository.GetIRFActiveInterviewers(meeting.IRFId);
                            foreach (var item in irfInterviewers)
                            {
                                //await _notificationHelper.SendIRFBasedInterviewerMail(irfInterviewers.ToList(), meeting.interviewDate.ToString(), meeting.uniqueId, meeting.irfIntId, meeting.imid);
                                await _notificationHelper.EmergencyInterviewerMail(item.EmailId, meeting.interviewDate.ToString(), item.uniqueid, meeting.irfIntId, meeting.imid);

                            }

                            //if (irfInterviewers != null && irfInterviewers.Count() > 0)
                            //{
                            //    //send mails to the active irf interviewers
                            //    await _notificationHelper.SendIRFBasedInterviewerMail(irfInterviewers.ToList(), meeting.interviewDate.ToString(),meeting.uniqueId, meeting.irfIntId, meeting.imid);
                            //}

                        }

                    }
                }

                log.LogInformation($"C# OpenInterviewsNotification executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"C# OpenInterviewsNotification error, msg : {ex.Message}, time : {DateTime.Now}");

            }
        }


    }
}
