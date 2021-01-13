using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Risebird.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoUploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string accountName;
        private readonly string accountKey;
        private readonly string userName;
        private readonly string Password;
        private readonly string ContainerName;
        private readonly string ConnectionString;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<VideoUploadController> _logger;

        public VideoUploadController(ILogger<VideoUploadController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            accountName = _configuration["StorageSettings:StorageName"];
            accountKey = _configuration["StorageSettings:SecretKey"];
            userName = _configuration["StorageSettings:UserName"];
            Password = _configuration["StorageSettings:Password"];
            ContainerName = _configuration["StorageSettings:AccountName"];
            ConnectionString = _configuration["StorageSettings:ConnectionString"];
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        public async Task InsertBlob(string filename, string containerName, IFormFile blobFile)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            blob.Properties.ContentType = "video/mp4";
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }
        }
        public CloudBlockBlob GetCloudBlockBlobClient(string containerName, string filename)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.ExistsAsync().Result)
            {
                blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissionsAsync(permission);
            }
            CloudBlockBlob blob = blobClient.GetContainerReference(containerName).GetBlockBlobReference(filename);
            return blob;
        }
        public CloudBlobContainer GetCloudBlobContainer(string containerName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.ExistsAsync().Result)
            {
                blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissionsAsync(permission);
            }
            return blobContainer;
        }
        public CloudStorageAccount GetStorageAccount()
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            return account;
        }
        public async Task<string> CreateSasUrl()
        {
            BlobContainerClient blobcontainerclient = new BlobContainerClient(ConnectionString, ContainerName);
            var uri = GetServiceSasUriForContainer(blobcontainerclient);
            return uri.AbsoluteUri.ToString().Split("?")[1].ToString();
        }
        public Uri GetServiceSasUriForContainer(BlobContainerClient containerClient, string storedPolicyName = null)
        {
            if (containerClient.CanGenerateSasUri)
            {
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = containerClient.Name,
                    Resource = "c",
                };
                if (storedPolicyName == null)
                {

                    sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
                    sasBuilder.StartsOn = DateTimeOffset.UtcNow;
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMonths(7);
                    sasBuilder.Protocol = SasProtocol.Https;

                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }
                Uri sasUri = containerClient.GenerateSasUri(sasBuilder);
                return sasUri;
            }
            else
            {
                return null;
            }
        }
    }
}
