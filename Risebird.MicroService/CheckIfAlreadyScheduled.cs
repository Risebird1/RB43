using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Risebird.Repository.Interface;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;

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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestData req)
        {

            NameValueCollection queryParameters = HttpUtility.ParseQueryString(req.Url.Query);
            // Get the "email" query parameter value
            string email = queryParameters["email"];

            //string email = req.Query["email"];

            var result = await _meetingRespository.CheckCompletedInterviews(email);

            return new OkObjectResult(result);
        }
    }
}
