using Microsoft.Azure.WebJobs;
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


        public OpenInterviewsNotification(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }
        [FunctionName("OpenInterviewsNotification")]
        public async Task Run([TimerTrigger("0 0 */30 * * *")] TimerInfo myTimer, ILogger log) //after every 30 minutes
        {
            try
            {
                log.LogInformation($"C# OpenInterviewsNotification started executing at: {DateTime.Now}");

                //get all the unassigned meetings scheduled three hours from now

                var unassignedInterviewMeetings = await _meetingRespository.GetUnassignedOpenInterviews();

                foreach (var meeting in unassignedInterviewMeetings)
                {
                    //get all the active interviewers for the IRF
                    var irfInterviewers = await _meetingRespository.GetIRFActiveInterviewers(meeting.IRFId);


                    if (irfInterviewers != null && irfInterviewers.Count() > 0)
                    {
                        //send mails to the active irf interviewers
                        await _notificationHelper.SendIRFBasedInterviewerMail(irfInterviewers.ToList(), meeting.interviewDate.ToString(), meeting.irfIntId, meeting.imid);
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
