using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class DeleteorShutDownVM
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IVmHelper _vmhelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public DeleteorShutDownVM(IConfiguration configuration, IAzureHelper azureHelper, IProctorInterviewsRepository proctorInterviewsRepository, IVmHelper vmhelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _vmhelper = vmhelper;
            _applicationErrorLogger = applicationErrorLogger;
        }
        [FunctionName("DeleteorShutDownVM")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _listvm = await _proctorInterviewsRepository.GetDeleteorShutdownVMList();
            foreach (var item in _listvm)
            {
                try
                {
                    if (item.methodtype == "Delete")
                    {
                        await _vmhelper.DeleteVM(item.ResourceGroupName, item.Name);
                        await _vmhelper.DeleteResourcegroup(item.ResourceGroupName);
                    }
                    else if (item.methodtype == "Shutdown")
                    {
                        await _vmhelper.DeallocateVM(item.ResourceGroupName, item.Name);
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
