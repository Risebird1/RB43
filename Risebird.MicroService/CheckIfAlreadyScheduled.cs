using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Repository.Interface;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CheckIfAlreadyScheduled
    {
        private readonly IMeetingRespository _meetingRespository;
        private readonly ILogger log;
        public CheckIfAlreadyScheduled(ILoggerFactory loggerFactory, IMeetingRespository meetingRespository)
        {
            _meetingRespository = meetingRespository;
            log = loggerFactory.CreateLogger<CheckIfAlreadyScheduled>();
        }

        [Function("CheckIfAlreadyScheduled")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            string email = req.Query["email"];

            var result = await _meetingRespository.CheckCompletedInterviews(email);

            return new OkObjectResult(result);
        }
    }
}
