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
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class GetUserProfile
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public GetUserProfile(IConfiguration configuration, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [FunctionName("GetUserProfile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var profileName =  _userRepository.GetUserProfileBasedOnEmailId(requestBody);

            
            return new OkObjectResult(profileName);
        }
    }
}
