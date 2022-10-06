using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ProctorBlobTrigger
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IConfiguration _configuration;

        public ProctorBlobTrigger(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
        }
        [FunctionName("ProctorBlobTrigger")]
        public async Task Run([BlobTrigger("reports/{name}", Connection = "proctoringreport")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            try
            {
                int interviewid = 0;
                if (!string.IsNullOrEmpty(name))
                {
                    var id = name.Split("_")[0];
                    interviewid = Convert.ToInt32(id);
                }
                var reportUrl = "https://proctoringreport.blob.core.windows.net/reports/" + name;
                var result = await _meetingRespository.SaveProctorReport(interviewid, reportUrl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
