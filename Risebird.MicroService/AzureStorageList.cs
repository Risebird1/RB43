using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;

namespace Risebird.MicroService
{
    public class AzureStorageList
    {
        private readonly IConfiguration _configuration;
        private readonly string accountName;
        private readonly string accountKey;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageService _azureStorageService;
        public AzureStorageList(IConfiguration configuration, IAzureStorageService azureStorageService, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureStorageService = azureStorageService;
            _applicationErrorLogger = applicationErrorLogger;
            accountName = _configuration["StorageSettings:StorageName"];
            accountKey = _configuration["StorageSettings:SecretKey"];
        }
        [FunctionName("AzureStorageList")]
        public async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                StorageCredentials creds = new StorageCredentials(accountName, accountKey);
                CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);

                // Blob Storage
                CloudBlobClient blobClient = account.CreateCloudBlobClient();
                List<StorageItem> containerNames = await GetBlobContainers(blobClient);

                var storageInfo = new AzureStorageInfoModelVM
                {
                    BlobContainers = containerNames
                };
                bool saveSuccess = await _azureStorageService.SaveAzurStorageDetails(storageInfo);
                log.LogInformation($"C# Timer trigger function completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

        }

        private async Task<List<StorageItem>> GetBlobContainers(CloudBlobClient blobClient)
        {
            List<StorageItem> containerInfoList = new List<StorageItem>();
            BlobContinuationToken continuationToken = null;

            do
            {
                var resultSegment = await blobClient.ListContainersSegmentedAsync(continuationToken);
                continuationToken = resultSegment.ContinuationToken;

                foreach (var container in resultSegment.Results)
                {

                    long containerSize = await GetContainerSize(container);

                    // Placeholder for price calculation (replace with actual logic based on your requirements)
                    // decimal containerPrice = CalculateContainerPrice(containerSize);

                    var containerInfo = new StorageItem
                    {
                        Name = container.Name,
                        Size = containerSize,
                    };

                    // Add the container info to the list
                    containerInfoList.Add(containerInfo);
                }
            } while (continuationToken != null);

            return containerInfoList;
        }
        private async Task<long> GetContainerSize(CloudBlobContainer container)
        {
            BlobContinuationToken continuationToken = null;
            long sizeInKB = 0;
            do
            {
                var resultSegment = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = resultSegment.ContinuationToken;
                foreach (var blobItem in resultSegment.Results)
                {
                    if (blobItem is CloudBlockBlob blockBlob)
                    {
                        long sizeInBytes = blockBlob.Properties.Length;
                        //long sizeInKB = (sizeInBytes / 1024);
                        sizeInKB = (sizeInBytes / 1024);
                        // size += sizeInKB;
                    }
                    // Handle other types of blobs if needed
                }


            } while (continuationToken != null);
            return sizeInKB;
        }

    }
}
