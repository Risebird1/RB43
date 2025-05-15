using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System.Web;

namespace Risebird.AzureTimerFunctions
{
    public class SendTimelyMAGApplicationLink
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFRepository _irfRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly ILogger log;

        public SendTimelyMAGApplicationLink(ILoggerFactory loggerFactory, 
            IApplicationErrorLogger applicationErrorLogger,
            IIRFRepository irfRepository,
            IEncryptionHelper encryptionHelper, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _irfRepository = irfRepository;
            log = loggerFactory.CreateLogger<SendTimelyMAGApplicationLink>();
            _encryptionHelper = encryptionHelper;
            _notificationHelper = notificationHelper;
        }

        [Function("SendTimelyMAGApplicationLink")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"SendTimelyMAGApplicationLink : C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                //get all mags that have unassigned interviews an hour or later from the current tiime.
                var unassignedMags = await _irfRepository.GetUnassignedInterviewsMAG();
                //for each mag, first get all the interviewers mapped to it, if the list is not null, create a mag link and send them all the link


                if (unassignedMags != null && unassignedMags.Count() > 0)
                {
                    foreach (var item in unassignedMags)
                    {
                        try
                        {
                            var interviewers = await _irfRepository.GetAssessmentGroupInterviewerMembers(item.id, true); //item.id here is the id of the assessment group and not a mag table primary id

                            if (interviewers != null && interviewers.Count() > 0)
                            {
                                //create mag link
                                var encryptedId = await _encryptionHelper.EncryptStringValue(item.id.ToString());
                                var magLink = "https://portal.risebird.io/Interviewer/MAGApply?id=" + HttpUtility.UrlEncode(encryptedId);

                                //send the mag link to all the interviewers, will have to create a template
                                foreach (var interviewer in interviewers)
                                {
                                    await _notificationHelper.SendMAGApplicationLinkToInterviewers(interviewer.EmailId, interviewer.uniqueid, magLink);

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"SendTimelyMAGApplicationLink : C# Timer trigger {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"SendTimelyMAGApplicationLink : C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
