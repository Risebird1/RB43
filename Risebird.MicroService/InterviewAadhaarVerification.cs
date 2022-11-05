using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class InterviewAadhaarVerification
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;

        public InterviewAadhaarVerification(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }
        [FunctionName("InterviewAadhaarVerification")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, ILogger log)//Every hour
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                var _candidateDetails = await _meetingRespository.GetUpcomingInterviewsCandidates();
                var uniqueurl = string.Empty;
                foreach (var item in _candidateDetails)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.uniqueid))
                        {
                            if (item.JobCountry == 2)
                            {
                                uniqueurl = "https://portal.risebird.io/Aadhaar/CandidateVerification?uniqueid=" + item.uniqueid;
                            }
                            else
                            {
                                uniqueurl = "https://portal.risebird.io/Aadhaar/AadhaarVerification?uniqueid=" + item.uniqueid;
                            }
                        }
                        Mail _mail = new Mail
                        {
                            TO = item.EmailId,
                            Subject = item.ClientName + " : Candidate Verification"
                        };
                        var emailsend = await _notificationHelper.SendInterviewAadhaarVerificationNotification(_mail, uniqueurl, item.Name);
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
