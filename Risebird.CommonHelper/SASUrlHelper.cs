using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.CommonHelper
{
    public class SASUrlHelper : ISASUrlHelper
    {
        private readonly string SASConnectionString;

        private readonly IConfiguration _configuration;
        public SASUrlHelper(IConfiguration configuration)
        {
            _configuration = configuration;
             //ContainerName = _configuration["SASURLSettings:ContainerName"];
            SASConnectionString = _configuration["SASURLSettings:ConnectionString"];
        }

        public string CreateSasUrl(string containerName)
        {
            BlobContainerClient blobcontainerclient = new BlobContainerClient(SASConnectionString, containerName);
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
