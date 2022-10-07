using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class StopVMNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        public StopVMNotifier(IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
        }
        [FunctionName("StopVMNotifier")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {

            // delete from transVMuser all candidates to whom the vm was shared 2 hours or before back
            try
            {
                //get emails of all the users who have started a VM 2 hours or before back along with the name of the VM

                var result = await _proctorInterviewsRepository.GetVMStarterEmailId();

                foreach (var item in result)
                {
                    //send email to each user
                    await _notificationHelper.SendVmStopReminder(item);
                }
                log.LogInformation($"C# UnShareVM Timer trigger function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }

        }
    }
}
