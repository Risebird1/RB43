using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class UpdateIRFRedFlagsCounts
    {
        private readonly IConfiguration _configuration;
        private readonly IIRFService _irfService;
        private readonly ILogger log;
        public UpdateIRFRedFlagsCounts(ILoggerFactory loggerFactory, IConfiguration configuration, IIRFService irfService)
        {
            _configuration = configuration;
            _irfService = irfService;
            log = loggerFactory.CreateLogger<UpdateIRFRedFlagsCounts>();
        }
        [Function("UpdateIRFRedFlagsCounts")]
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await _irfService.UpdateIRFRedFlags();
        }
    }
}
