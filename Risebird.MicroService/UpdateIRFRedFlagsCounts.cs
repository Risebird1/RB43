using Microsoft.Azure.WebJobs;
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

        public UpdateIRFRedFlagsCounts(IConfiguration configuration, IIRFService irfService)
        {
            _configuration = configuration;
            _irfService = irfService;
        }
        [FunctionName("UpdateIRFRedFlagsCounts")]
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await _irfService.UpdateIRFRedFlags();
        }
    }
}
