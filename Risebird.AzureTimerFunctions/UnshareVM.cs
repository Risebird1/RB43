using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class UnshareVM
    {
        private readonly IConfiguration _configuration;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;
        private readonly ILogger log;

        public UnshareVM(ILoggerFactory loggerFactory, IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            log = loggerFactory.CreateLogger<UnshareVM>();
        }

        [Function("UnshareVM")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {

            // delete from transVMuser all candidates to whom the vm was shared 2 hours or before back
            try
            {
                await _proctorInterviewsRepository.RemoveCandidateVmAccess();
                log.LogInformation($"C# UnShareVM Timer trigger function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }

        }
    }
}
