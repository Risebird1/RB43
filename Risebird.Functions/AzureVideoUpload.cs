using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace Risebird.Functions
{
    public static class AzureVideoUpload
    {
        [FunctionName("AzureVideoUpload")]
        public static void Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            //write to connect db and find zoom urls
            //write to fetch video from zoom
            //write to upload video on azure

        }
    }
}
