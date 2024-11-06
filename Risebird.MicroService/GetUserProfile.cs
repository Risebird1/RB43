using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using System.IO;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class GetUserProfile
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger log;
        public GetUserProfile(ILoggerFactory loggerFactory, IConfiguration configuration, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            log = loggerFactory.CreateLogger<GetUserProfile>();
        }

        [Function("GetUserProfile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var profileName = await _userRepository.GetUserProfileBasedOnEmailId(requestBody);


            return new OkObjectResult(profileName);
        }
    }
}
