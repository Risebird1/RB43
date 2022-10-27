using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Risebird.Repository.Interface;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CheckIfAlreadyScheduled
    {
        private readonly IMeetingRespository _meetingRespository;
        public CheckIfAlreadyScheduled(IMeetingRespository meetingRespository)
        {
            _meetingRespository = meetingRespository;
        }

        [FunctionName("CheckIfAlreadyScheduled")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string email = req.Query["email"];

            var result = await _meetingRespository.CheckCompletedInterviews(email);

            return new OkObjectResult(result);
        }
    }
}
