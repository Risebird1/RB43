using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Risebird.Repository.Interface;

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
