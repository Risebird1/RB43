using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Risebird.MicroService
{
    public static class ProctorBlobTrigger
    {
        [FunctionName("ProctorBlobTrigger")]
        public static void Run([BlobTrigger("reports/{name}", Connection = "proctoringreport")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
