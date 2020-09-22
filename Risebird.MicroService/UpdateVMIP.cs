using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class UpdateVMIP
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        //private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        //private readonly INotificationHelper _notificationHelper;
        //private readonly IProctorInterviewsService _proctorInterviewsService;
        private readonly string connectionString;

        public UpdateVMIP(IAzureHelper azureHelper, IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            //_notificationHelper = notificationHelper;
            //_proctorInterviewsService = proctorInterviewsService;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        [FunctionName("UpdateVMIP")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
