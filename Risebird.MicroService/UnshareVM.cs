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
    public class UnshareVM
    {
        private readonly IConfiguration _configuration;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;

        public UnshareVM(IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }

        [FunctionName("UnshareVM")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {

            // delete from transVMuser all candidates to whom the vm was shared 2 hours or before back
            try
            {
                await _proctorInterviewsRepository.RemoveCandidateVmAccess();
                log.LogInformation($"C# UnShareVM Timer trigger function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {

                await  _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }

        }
    }
}
