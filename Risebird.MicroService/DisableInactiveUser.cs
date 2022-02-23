using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class DisableInactiveUser
    {

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IUserRepository _userRespository;


        public DisableInactiveUser(IApplicationErrorLogger applicationErrorLogger, IUserRepository userRespository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _userRespository = userRespository;
           
        }
        [FunctionName("DisableInactiveUser")]
        public async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)//0 0 0 * * *
        {
            log.LogInformation($"DisableInactiveUser Timer trigger function started executing at: {DateTime.Now}");

            try
            {
                var result = await _userRespository.DisableInactiveUser();

                if (result == (int)BooleanResultsEnum.True)
                {
                    log.LogInformation($"DisableInactiveUser Timer trigger function completed executing at: {DateTime.Now}");
                }
                if (result == (int)BooleanResultsEnum.Failed)
                {
                    log.LogInformation($"DisableInactiveUser Timer trigger function execution Failed. Time: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

        }
    }
}
