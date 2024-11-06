using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ComputeProbableNoShowCandidates
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly ILogger log;

        private readonly string ContainerName;
        //private readonly string SASConnectionString;

        public ComputeProbableNoShowCandidates(ILoggerFactory loggerFactory, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            log = loggerFactory.CreateLogger<ComputeProbableNoShowCandidates>();
        }

        [Function("ComputeProbableNoShowCandidates")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                _ = await _meetingRespository.UpdateProbableNoShowCandidates();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
