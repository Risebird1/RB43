using Azure;
using Microsoft.AspNetCore.Http;
//using Microsoft.Azure.Management.Media;
//using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
namespace Risebird.CommonHelper
{
    public class AzureStorageHelper : IAzureStorageHelper
    {
        public CloudTableClient TableClient;
        private readonly string accountName;
        private readonly string accountKey;
        private readonly string _AADTenantDomain;
        private readonly string _AADTenantId;
        private readonly string _RESTAPIEndpoint;
        private readonly string _AMSClientId;
        private readonly string _AMSClientSecret;
        private readonly string _ARMEndpoint;
        private readonly string _AMSSubscriptionId;
        private readonly string _AMSResourceGroup;
        private readonly string _AMSAccountName;

        //private static CloudMediaContext _context = null;
        //private const string AdaptiveStreamingTransformName = "MyTransformWithAdaptiveStreamingPreset";
        //private const string OutputFolderName = @"Output";

        private readonly IConfiguration _configuration;
        public AzureStorageHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            accountName = _configuration["StorageSettings:StorageName"];
            accountKey = _configuration["StorageSettings:SecretKey"];
            _AADTenantDomain = _configuration["AzureMediaServices:AMSAADTenantDomain"];
            _RESTAPIEndpoint = _configuration["AzureMediaServices:AMSRESTAPIEndpoint"];
            _AMSClientId = _configuration["AzureMediaServices:AMSClientId"];
            _AMSClientSecret = _configuration["AzureMediaServices:AMSClientSecret"];
            _AADTenantId = _configuration["AzureMediaServices:AADTenantId"];
            _ARMEndpoint = _configuration["AzureMediaServices:ARMEndpoint"];
            _AMSSubscriptionId = _configuration["AzureMediaServices:AMSSubscriptionId"];
            _AMSResourceGroup = _configuration["AzureMediaServices:AMSResourceGroup"];
            _AMSAccountName = _configuration["AzureMediaServices:AMSAccountName"];
        }

        /// <summary>
        /// Risebird storage account details
        /// </summary>
        /// <returns></returns>
        public CloudStorageAccount GetStorageAccount()
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            return account;
        }

        /// <summary>
        /// GET THE REFERENCE OF TABLE OBJECT
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public CloudTable GetTable(string tableName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudTableClient client = account.CreateCloudTableClient();

            CloudTable table = client.GetTableReference(tableName);
            table.CreateIfNotExistsAsync();

            return table;
        }

        /// <summary>
        /// From client storage account delete blob container file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public bool DeleteBlobContainerFile(string fileName, string containerName)
        {
            try
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

                CloudBlockBlob blob = blobClient.GetContainerReference(containerName).GetBlockBlobReference(fileName);

                blob.DeleteAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// From client storage account check if blob container file exist or not
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<bool> CheckBlobContainerFile(string fileName, string containerName)
        {
            try
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
                var blob = blobContainer.GetBlobReference(fileName);

                return blob.ExistsAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task<bool> CheckTableExistsOrNot(string tableName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table.ExistsAsync();
        }


        public CloudTable GetTableWithAccessKey(string tableName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }

        /// <summary>
        /// DELETE THE TABLE OBJECT BY TABLE NAME
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DeleteTable(string tableName)
        {

            CloudStorageAccount account = GetStorageAccount();
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            table.DeleteIfExistsAsync();

            return true;
        }


        /// <summary>
        /// Get all the entities from Table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IEnumerable<T> GetAllEntities<T>(string tableName) where T : TableEntity, new()
        {
            var table = GetTable(tableName);
            var entities = new List<T>();
            try
            {
                TableContinuationToken token = null;
                do
                {
                    var queryResult = table.ExecuteQuerySegmentedAsync(new TableQuery<T>(), token);
                    entities.AddRange(queryResult.Result);
                    token = queryResult.Result.ContinuationToken;
                } while (token != null);
            }
            catch (StorageException)
            {
                return entities;
            }
            return entities;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<CloudTable> GetTableAsync(string tableName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudTableClient client = account.CreateCloudTableClient();

            CloudTable table = client.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            return table;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : TableEntity, new()
        {
            var table = await GetTableAsync(tableName);
            var entities = new List<T>();
            try
            {
                TableContinuationToken token = null;
                do
                {
                    var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<T>(), token);
                    entities.AddRange(queryResult);
                    token = queryResult.ContinuationToken;
                } while (token != null);
            }
            catch (StorageException)
            {
                return entities;
            }
            return entities;
        }

        /// <summary>
        /// INSERT NEW SINGLE ROW
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> InsertEntity(string tableName, object obj)
        {
            bool insertResult;
            try
            {
                var table = GetTable(tableName);
                TableOperation insertOperation = TableOperation.InsertOrReplace(obj as TableEntity);

                TableResult result = await table.ExecuteAsync(insertOperation);

                insertResult = true;

                return insertResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DELETE SINGLE ROW
        /// </summary>
        /// <param name="table"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> DeleteEntity(string tableName, object obj)
        {
            try
            {
                var table = GetTable(tableName);

                TableOperation deleteOperation = TableOperation.Delete(obj as TableEntity);

                TableResult result = await table.ExecuteAsync(deleteOperation);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns Blob container from client storage account
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public CloudBlockBlob GetCloudBlockBlob(string containerName, string filename)
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
            CloudBlockBlob blob = blobClient.GetContainerReference(containerName).GetBlockBlobReference(filename);

            return blob;
        }
        /// <summary>
        /// Reads the file content using the container name and file name
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
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

        /// <summary>
        /// method to store file in blob
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="containerName"></param>
        /// <param name="blobFile"></param>
        /// <param name="connectionString"></param>
        /// <param name="isClient"></param>
        /// <returns></returns>
        public async Task InsertBlob(string filename, string containerName, IFormFile blobFile)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }


        }

        public async Task InsertResumeBlob(string filename, string containerName, IFormFile blobFile, string extension)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            string contentType = string.Empty;

            switch (extension)
            {
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".txt":
                    contentType = "text/plain";
                    break;
                case ".doc":
                    contentType = "application/msword";
                    break;
                case ".docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".dotx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
                    break;
                default:
                    contentType = blobFile.ContentType;
                    break;
            }

            blob.Properties.ContentType = contentType;
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }


        }

        public async Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile = null, Stream blobFileStream = null, Stream transcriptStream  = null)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            if (blobFile != null)
            {
                //await blob.UploadFromStreamAsync(blobFile);
                await blob.UploadFromStreamAsync(blobFileStream);
            }
            else if (blobFileStream != null)
            {
                blob.Properties.ContentType = "video/mp4";
                await blob.UploadFromStreamAsync(blobFileStream);

            }
            else if(transcriptStream != null)
            {
                blob.Properties.ContentType = "text/plain";
                await blob.UploadFromStreamAsync(transcriptStream);
            }
            else
            {
                return null;
            }

            return blob.Uri.ToString();
        }

        public bool DeleteBlob(string fileName, string containerName)
        {
            try
            {
                var blob = GetCloudBlockBlob(containerName, fileName);
                blob.DeleteAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string CheckIfFileExists(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
                if (blobContainer.GetBlobReference(fileName).ExistsAsync().Result)
                {
                    return fileName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get Blob data based container name and file name of the blob
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetBlobData(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
                CloudBlob blob = blobContainer.GetBlobReference(fileName);
                string contents; /* content to append */
                if (!blob.ExistsAsync().Result)
                {
                    contents = "";
                }
                else
                {
                    using (StreamReader reader = new StreamReader(blob.OpenReadAsync().Result))
                    {
                        contents = reader.ReadToEnd();
                    }
                }
                return contents;
            }
            catch (Exception)
            {
                return null;
            }
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

        public TableResult RetriveByPartitionAndRowKey(string tableName, string partitionKey, string rowKey)
        {
            var table = GetTable(tableName);
            var retrieveOperation = TableOperation.Retrieve(partitionKey, rowKey);
            var retrieveResult = table.ExecuteAsync(retrieveOperation);
            return retrieveResult.Result;
        }

        public async Task<TableResult> RetriveByPartitionAndRowKeyAsync(string tableName, string partitionKey, string rowKey)
        {
            var table = GetTable(tableName);
            var retrieveOperation = TableOperation.Retrieve(partitionKey, rowKey);
            var retrieveResult = await table.ExecuteAsync(retrieveOperation).ConfigureAwait(false);
            return retrieveResult;
        }

        public void InsertBatchInTable(string tableName, TableBatchOperation tableBatchOperation)
        {
            try
            {
                var table = GetTable(tableName);
                table.ExecuteBatchAsync(tableBatchOperation);
            }
            catch (Exception)
            {
                throw;
            }
        }

        ///// <summary>
        ///// upload video using Azure's media services
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <param name="containerName"></param>
        ///// <param name="blobFile"></param>
        ///// <returns></returns>
        //public string UploadVideo(string filename)
        //{
        //    var result = CreateAssetAndUploadSingleFile(filename).Uri.ToString();
        //    return result;
        //}

        //public IAsset CreateAssetAndUploadSingleFile(string singleFilePath)
        //{
        //    //if (!File.Exists(singleFilePath))
        //    //{
        //    //    Console.WriteLine("File does not exist.");
        //    //    return null;
        //    //}
        //    try
        //    {
        //        var tokenCredentials = new AzureAdTokenCredentials(_AADTenantDomain,
        //                                new AzureAdClientSymmetricKey(_AMSClientId, _AMSClientSecret),
        //                                AzureEnvironments.AzureCloudEnvironment);

        //        var tokenProvider = new AzureAdTokenProvider(tokenCredentials);
        //        CloudMediaContext _context = new CloudMediaContext(new Uri(_RESTAPIEndpoint), tokenProvider);

        //        AssetCreationOptions assetCreationOptions = AssetCreationOptions.None;
        //        var assetName = Path.GetFileNameWithoutExtension(singleFilePath);
        //        IAsset inputAsset = _context.Assets.Create(assetName, assetCreationOptions);

        //        var assetFile = inputAsset.AssetFiles.Create(Path.GetFileName(singleFilePath));

        //        //Console.WriteLine("Upload {0}", assetFile.Name);

        //        assetFile.Upload(singleFilePath);

        //        //Console.WriteLine("Done uploading {0}", assetFile.Name);

        //        return inputAsset;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //        //throw;
        //    }

        //}

        //#region Attempt 2
        ///// <summary>
        ///// attempt 2
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <returns></returns
        //public async Task<string> UploadVideoAsset(string filePath)
        //{

        //    try
        //    {
        //        // Creating a unique suffix so that we don't have name collisions if you run the sample
        //        // multiple times without cleaning up.
        //        string uniqueness = Guid.NewGuid().ToString("N");
        //        string jobName = $"job-{uniqueness}";
        //        string locatorName = $"locator-{uniqueness}";
        //        string outputAssetName = $"output-{uniqueness}";
        //        string outputUrl = string.Empty;
        //        IAzureMediaServicesClient client = await CreateMediaServicesClientAsync();
        //        var fileName = Path.GetFileName(filePath);
        //        var inputAssetName = Path.GetFileNameWithoutExtension(filePath);

        //        Transform transform = await GetOrCreateTransformAsync(client, _AMSResourceGroup, _AMSAccountName, AdaptiveStreamingTransformName);


        //        // Create a new input Asset and upload the specified local video file into it.
        //        await CreateInputAssetAsync(client, _AMSResourceGroup, _AMSAccountName, inputAssetName, fileName, filePath);

        //        // Use the name of the created input asset to create the job input.
        //        JobInput jobInput = new JobInputAsset(assetName: inputAssetName);


        //        // Output from the encoding Job must be written to an Asset, so let's create one
        //        Asset outputAsset = await CreateOutputAssetAsync(client, _AMSResourceGroup, _AMSAccountName, outputAssetName);

        //        Job job = await SubmitJobAsync(client, _AMSResourceGroup, _AMSAccountName, AdaptiveStreamingTransformName, jobName, inputAssetName, outputAsset.Name);

        //        job = await WaitForJobToFinishAsync(client, _AMSResourceGroup, _AMSAccountName, AdaptiveStreamingTransformName, jobName);

        //        if (job.State == JobState.Finished)
        //        {
        //            //Console.WriteLine("Job finished.");
        //            //if (!Directory.Exists(OutputFolderName))
        //            //    Directory.CreateDirectory(OutputFolderName);

        //            //await DownloadOutputAssetAsync(client, _AMSResourceGroup, _AMSAccountName, outputAsset.Name, OutputFolderName);

        //            StreamingLocator locator = await CreateStreamingLocatorAsync(client, _AMSResourceGroup, _AMSAccountName, outputAsset.Name, locatorName);

        //            IList<string> urls = await GetStreamingUrlsAsync(client, _AMSResourceGroup, _AMSAccountName, locator.Name);
        //            outputUrl = urls.LastOrDefault();
        //            //foreach (var url in urls)
        //            //{
        //            //    Console.WriteLine(url);
        //            //}
        //        }


        //        return outputUrl;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    //var result = CreateAssetAndUploadSingleFile(filename).Uri.ToString();
        //    //return result;
        //}

        ///// <summary>
        ///// Creates the AzureMediaServicesClient object based on the credentials
        ///// supplied in local configuration file.
        ///// </summary>
        ///// <param name="config">The parm is of type ConfigWrapper. This class reads values from local configuration file.</param>
        ///// <returns></returns>
        //// <CreateMediaServicesClient>
        //private async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync()
        //{
        //    var credentials = await GetCredentialsAsync();
        //    //_ARMEndpoint,
        //    return new AzureMediaServicesClient(new Uri(_ARMEndpoint), credentials)
        //    {
        //        SubscriptionId = _AMSSubscriptionId,
        //    };
        //}

        ///// <summary>
        ///// Create the ServiceClientCredentials object based on the credentials
        ///// supplied in local configuration file.
        ///// </summary>
        ///// <param name="config">The parm is of type ConfigWrapper. This class reads values from local configuration file.</param>
        ///// <returns></returns>
        //// <GetCredentialsAsync>
        //private async Task<ServiceClientCredentials> GetCredentialsAsync()
        //{
        //    // Use ApplicationTokenProvider.LoginSilentWithCertificateAsync or UserTokenProvider.LoginSilentAsync to get a token using service principal with certificate
        //    //// ClientAssertionCertificate
        //    //// ApplicationTokenProvider.LoginSilentWithCertificateAsync

        //    // Use ApplicationTokenProvider.LoginSilentAsync to get a token using a service principal with symetric key
        //    ClientCredential clientCredential = new ClientCredential(_AMSClientId, _AMSClientSecret);
        //    return await ApplicationTokenProvider.LoginSilentAsync(_AADTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
        //}

        ///// <summary>
        ///// Creates a new input Asset and uploads the specified local video file into it.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="assetName">The asset name.</param>
        ///// <param name="fileToUpload">The file you want to upload into the asset.</param>
        ///// <returns></returns>
        ///// 
        //private static async Task<Asset> CreateInputAssetAsync(
        //    IAzureMediaServicesClient client,
        //    string resourceGroupName,
        //    string accountName,
        //    string assetName,
        //    string fileToUpload,
        //    string filePath)
        //{
        //    try
        //    {
        //        // In this example, we are assuming that the asset name is unique.
        //        //
        //        // If you already have an asset with the desired name, use the Assets.Get method
        //        // to get the existing asset. In Media Services v3, the Get method on entities returns null 
        //        // if the entity doesn't exist (a case-insensitive check on the name).

        //        // Call Media Services API to create an Asset.
        //        // This method creates a container in storage for the Asset.
        //        // The files (blobs) associated with the asset will be stored in this container.
        //        Microsoft.Azure.Management.Media.Models.Asset asset = await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, assetName, new Microsoft.Azure.Management.Media.Models.Asset());

        //        // Use Media Services API to get back a response that contains
        //        // SAS URL for the Asset container into which to upload blobs.
        //        // That is where you would specify read-write permissions 
        //        // and the exparation time for the SAS URL.
        //        var response = await client.Assets.ListContainerSasAsync(
        //            resourceGroupName,
        //            accountName,
        //            assetName,
        //            permissions: AssetContainerPermission.ReadWrite,
        //            expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());

        //        var sasUri = new Uri(response.AssetContainerSasUrls.First());

        //        // Use Storage API to get a reference to the Asset container
        //        // that was created by calling Asset's CreateOrUpdate method.  
        //        CloudBlobContainer container = new CloudBlobContainer(sasUri);
        //        //var blob = container.GetBlockBlobReference(Path.GetFileName(fileToUpload));
        //        var blob = container.GetBlockBlobReference(fileToUpload);
        //        // Use Strorage API to upload the file into the container in storage.

        //        //await blob.UploadFromFileAsync(fileToUpload);
        //        await blob.UploadFromFileAsync(filePath);
        //        //return sasUri.ToString();
        //        return asset;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }


        //}



        ///// <summary>
        ///// If the specified transform exists, get that transform.
        ///// If the it does not exist, creates a new transform with the specified output. 
        ///// In this case, the output is set to encode a video using one of the built-in encoding presets.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="transformName">The name of the transform.</param>
        ///// <returns></returns>
        //// <EnsureTransformExists>
        //private static async Task<Transform> GetOrCreateTransformAsync(
        //    IAzureMediaServicesClient client,
        //    string resourceGroupName,
        //    string accountName,
        //    string transformName)
        //{
        //    // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
        //    // also uses the same recipe or Preset for processing content.
        //    Transform transform = await client.Transforms.GetAsync(resourceGroupName, accountName, transformName);

        //    if (transform == null)
        //    {
        //        // You need to specify what you want it to produce as an output
        //        TransformOutput[] output = new TransformOutput[]
        //        {
        //            new TransformOutput
        //            {
        //                // The preset for the Transform is set to one of Media Services built-in sample presets.
        //                // You can  customize the encoding settings by changing this to use "StandardEncoderPreset" class.
        //                Preset = new BuiltInStandardEncoderPreset()
        //                {
        //                    // This sample uses the built-in encoding preset for Adaptive Bitrate Streaming.
        //                    PresetName = EncoderNamedPreset.AdaptiveStreaming
        //                }
        //            }
        //        };

        //        // Create the Transform with the output defined above
        //        transform = await client.Transforms.CreateOrUpdateAsync(resourceGroupName, accountName, transformName, output);
        //    }

        //    return transform;
        //}
        //// </EnsureTransformExists>


        ///// <summary>
        ///// Creates an ouput asset. The output from the encoding Job must be written to an Asset.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="assetName">The output asset name.</param>
        ///// <returns></returns>
        //// <CreateOutputAsset>
        //private static async Task<Asset> CreateOutputAssetAsync(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName)
        //{
        //    // Check if an Asset already exists
        //    Asset outputAsset = await client.Assets.GetAsync(resourceGroupName, accountName, assetName);
        //    Asset asset = new Asset();
        //    string outputAssetName = assetName;

        //    if (outputAsset != null)
        //    {
        //        // Name collision! In order to get the sample to work, let's just go ahead and create a unique asset name
        //        // Note that the returned Asset can have a different name than the one specified as an input parameter.
        //        // You may want to update this part to throw an Exception instead, and handle name collisions differently.
        //        string uniqueness = $"-{Guid.NewGuid().ToString("N")}";
        //        outputAssetName += uniqueness;

        //        //Console.WriteLine("Warning – found an existing Asset with name = " + assetName);
        //        //Console.WriteLine("Creating an Asset with this name instead: " + outputAssetName);
        //    }

        //    return await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, outputAssetName, asset);
        //}
        //// </CreateOutputAsset>

        ///// <summary>
        ///// Submits a request to Media Services to apply the specified Transform to a given input video.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="transformName">The name of the transform.</param>
        ///// <param name="jobName">The (unique) name of the job.</param>
        ///// <param name="inputAssetName">The name of the input asset.</param>
        ///// <param name="outputAssetName">The (unique) name of the  output asset that will store the result of the encoding job. </param>
        //// <SubmitJob>
        //private static async Task<Job> SubmitJobAsync(IAzureMediaServicesClient client,
        //    string resourceGroupName,
        //    string accountName,
        //    string transformName,
        //    string jobName,
        //    string inputAssetName,
        //    string outputAssetName)
        //{
        //    // Use the name of the created input asset to create the job input.
        //    JobInput jobInput = new JobInputAsset(assetName: inputAssetName);

        //    JobOutput[] jobOutputs =
        //    {
        //        new JobOutputAsset(outputAssetName),
        //    };

        //    // In this example, we are assuming that the job name is unique.
        //    //
        //    // If you already have a job with the desired name, use the Jobs.Get method
        //    // to get the existing job. In Media Services v3, the Get method on entities returns null 
        //    // if the entity doesn't exist (a case-insensitive check on the name).
        //    Job job = await client.Jobs.CreateAsync(
        //        resourceGroupName,
        //        accountName,
        //        transformName,
        //        jobName,
        //        new Job
        //        {
        //            Input = jobInput,
        //            Outputs = jobOutputs,
        //        });

        //    return job;
        //}
        //// </SubmitJob>

        ///// <summary>
        ///// Polls Media Services for the status of the Job.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="transformName">The name of the transform.</param>
        ///// <param name="jobName">The name of the job you submitted.</param>
        ///// <returns></returns>
        //// <WaitForJobToFinish>
        //private static async Task<Job> WaitForJobToFinishAsync(IAzureMediaServicesClient client,
        //    string resourceGroupName,
        //    string accountName,
        //    string transformName,
        //    string jobName)
        //{
        //    const int SleepIntervalMs = 60 * 1000;

        //    Job job = null;

        //    do
        //    {
        //        job = await client.Jobs.GetAsync(resourceGroupName, accountName, transformName, jobName);

        //        //Console.WriteLine($"Job is '{job.State}'.");
        //        for (int i = 0; i < job.Outputs.Count; i++)
        //        {
        //            JobOutput output = job.Outputs[i];
        //            //Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
        //            if (output.State == JobState.Processing)
        //            {
        //                //Console.Write($"  Progress: '{output.Progress}'.");
        //            }

        //            //Console.WriteLine();
        //        }

        //        if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
        //        {
        //            await Task.Delay(SleepIntervalMs);
        //        }
        //    }
        //    while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

        //    return job;
        //}
        //// </WaitForJobToFinish>

        ///// <summary>
        /////  Downloads the results from the specified output asset, so you can see what you got.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="assetName">The output asset.</param>
        ///// <param name="outputFolderName">The name of the folder into which to download the results.</param>
        //// <DownloadResults>
        //private static async Task DownloadOutputAssetAsync(
        //    IAzureMediaServicesClient client,
        //    string resourceGroup,
        //    string accountName,
        //    string assetName,
        //    string outputFolderName)
        //{
        //    if (!Directory.Exists(outputFolderName))
        //    {
        //        Directory.CreateDirectory(outputFolderName);
        //    }

        //    AssetContainerSas assetContainerSas = await client.Assets.ListContainerSasAsync(
        //        resourceGroup,
        //        accountName,
        //        assetName,
        //        permissions: AssetContainerPermission.Read,
        //        expiryTime: DateTime.UtcNow.AddHours(1).ToUniversalTime());

        //    Uri containerSasUrl = new Uri(assetContainerSas.AssetContainerSasUrls.FirstOrDefault());
        //    CloudBlobContainer container = new CloudBlobContainer(containerSasUrl);

        //    string directory = Path.Combine(outputFolderName, assetName);
        //    Directory.CreateDirectory(directory);

        //    //Console.WriteLine($"Downloading output results to '{directory}'...");

        //    BlobContinuationToken continuationToken = null;
        //    IList<Task> downloadTasks = new List<Task>();

        //    do
        //    {
        //        // A non-negative integer value that indicates the maximum number of results to be returned at a time,
        //        // up to the per-operation limit of 5000. If this value is null, the maximum possible number of results
        //        // will be returned, up to 5000.
        //        int? ListBlobsSegmentMaxResult = null;

        //        BlobResultSegment segment = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, ListBlobsSegmentMaxResult, continuationToken, null, null);

        //        foreach (IListBlobItem blobItem in segment.Results)
        //        {
        //            CloudBlockBlob blob = blobItem as CloudBlockBlob;
        //            if (blob != null)
        //            {
        //                string path = Path.Combine(directory, blob.Name);

        //                downloadTasks.Add(blob.DownloadToFileAsync(path, FileMode.Create));
        //            }
        //        }

        //        continuationToken = segment.ContinuationToken;
        //    }
        //    while (continuationToken != null);

        //    await Task.WhenAll(downloadTasks);

        //    //Console.WriteLine("Download complete.");
        //}
        //// </DownloadResults>

        ///// <summary>
        ///// Creates a StreamingLocator for the specified asset and with the specified streaming policy name.
        ///// Once the StreamingLocator is created the output asset is available to clients for playback.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="assetName">The name of the output asset.</param>
        ///// <param name="locatorName">The StreamingLocator name (unique in this case).</param>
        ///// <returns></returns>
        //// <CreateStreamingLocator>
        //private static async Task<StreamingLocator> CreateStreamingLocatorAsync(
        //    IAzureMediaServicesClient client,
        //    string resourceGroup,
        //    string accountName,
        //    string assetName,
        //    string locatorName)
        //{
        //    StreamingLocator locator = await client.StreamingLocators.CreateAsync(
        //        resourceGroup,
        //        accountName,
        //        locatorName,
        //        new StreamingLocator
        //        {
        //            AssetName = assetName,
        //            StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly
        //        });

        //    return locator;
        //}
        //// </CreateStreamingLocator>


        ///// <summary>
        ///// Checks if the "default" streaming endpoint is in the running state,
        ///// if not, starts it.
        ///// Then, builds the streaming URLs.
        ///// </summary>
        ///// <param name="client">The Media Services client.</param>
        ///// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        ///// <param name="accountName"> The Media Services account name.</param>
        ///// <param name="locatorName">The name of the StreamingLocator that was created.</param>
        ///// <returns></returns>
        //// <GetStreamingURLs>
        //private static async Task<IList<string>> GetStreamingUrlsAsync(
        //    IAzureMediaServicesClient client,
        //    string resourceGroupName,
        //    string accountName,
        //    string locatorName)
        //{
        //    const string DefaultStreamingEndpointName = "default";

        //    IList<string> streamingUrls = new List<string>();

        //    StreamingEndpoint streamingEndpoint = await client.StreamingEndpoints.GetAsync(resourceGroupName, accountName, DefaultStreamingEndpointName);

        //    if (streamingEndpoint != null)
        //    {
        //        if (streamingEndpoint.ResourceState != StreamingEndpointResourceState.Running)
        //        {
        //            await client.StreamingEndpoints.StartAsync(resourceGroupName, accountName, DefaultStreamingEndpointName);
        //        }
        //    }

        //    ListPathsResponse paths = await client.StreamingLocators.ListPathsAsync(resourceGroupName, accountName, locatorName);

        //    foreach (StreamingPath path in paths.StreamingPaths)
        //    {
        //        UriBuilder uriBuilder = new UriBuilder();
        //        uriBuilder.Scheme = "https";
        //        uriBuilder.Host = streamingEndpoint.HostName;

        //        uriBuilder.Path = path.Paths[0];
        //        streamingUrls.Add(uriBuilder.ToString());
        //    }

        //    return streamingUrls;
        //}
        //// </GetStreamingURLs>

        //#endregion

        //#region  Attempt 3

        //public string UploadAndPublishVideo(string filePath)
        //{
        //    try
        //    {
        //        AzureAdTokenCredentials tokenCredentials =
        //        new AzureAdTokenCredentials(_AADTenantDomain,
        //            new AzureAdClientSymmetricKey(_AMSClientId, _AMSClientSecret),
        //            AzureEnvironments.AzureCloudEnvironment);

        //        //AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_AADTenantDomain, AzureEnvironments.AzureCloudEnvironment);
        //        AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);

        //       // _context = new CloudMediaContext(new Uri(_RESTAPIEndpoint), tokenProvider);
        //        _context = new CloudMediaContext(new Uri(_RESTAPIEndpoint), tokenProvider);
        //        IAsset inputAsset =
        //                UploadFile(filePath, AssetCreationOptions.None);

        //        IAsset encodedAsset =
        //                EncodeToAdaptiveBitrateMP4s(inputAsset, AssetCreationOptions.None);

        //        PublishAssetGetURLs(encodedAsset);
        //        return "";
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}

        //static public IAsset UploadFile(string fileName, AssetCreationOptions options)
        //{
        //    IAsset inputAsset = _context.Assets.CreateFromFile(
        //        fileName,
        //        options
        //        );

        //    //Console.WriteLine("Asset {0} created.", inputAsset.Id);

        //    return inputAsset;
        //}

        //static public IAsset EncodeToAdaptiveBitrateMP4s(IAsset asset, AssetCreationOptions options)
        //{

        //    // Prepare a job with a single task to transcode the specified asset
        //    // into a multi-bitrate asset.

        //    IJob job = _context.Jobs.CreateWithSingleTask(
        //        "Media Encoder Standard",
        //        "Adaptive Streaming",
        //        asset,
        //        "Adaptive Bitrate MP4",
        //        options);

        //    //Console.WriteLine("Submitting transcoding job...");


        //    // Submit the job and wait until it is completed.
        //    job.Submit();

        //    job = job.StartExecutionProgressTask(
        //        j =>
        //        {
        //            //Console.WriteLine("Job state: {0}", j.State);
        //            //Console.WriteLine("Job progress: {0:0.##}%", j.GetOverallProgress());
        //        },
        //        CancellationToken.None).Result;

        //    //Console.WriteLine("Transcoding job finished.");

        //    IAsset outputAsset = job.OutputMediaAssets[0];

        //    return outputAsset;
        //}


        //static public void PublishAssetGetURLs(IAsset asset, bool onDemaindURL = true, string fileExt = "")
        //{
        //    // Publish the output asset by creating an Origin locator for adaptive streaming,
        //    // and a SAS locator for progressive download.

        //    if (onDemaindURL)
        //    {
        //        _context.Locators.Create(
        //            LocatorType.OnDemandOrigin,
        //            asset,
        //            AccessPermissions.Read,
        //            TimeSpan.FromDays(30));

        //        // Get the Smooth Streaming, HLS and MPEG-DASH URLs for adaptive streaming,
        //        // and the Progressive Download URL.
        //        Uri smoothStreamingUri = asset.GetSmoothStreamingUri();
        //        Uri hlsUri = asset.GetHlsUri();
        //        Uri mpegDashUri = asset.GetMpegDashUri();

        //        // Display  the streaming URLs.
        //        //Console.WriteLine("Use the following URLs for adaptive streaming: ");
        //        //Console.WriteLine(smoothStreamingUri);
        //        //Console.WriteLine(hlsUri);
        //        //Console.WriteLine(mpegDashUri);
        //        //Console.WriteLine();
        //    }
        //    else
        //    {
        //        _context.Locators.Create(
        //            LocatorType.Sas,
        //            asset,
        //            AccessPermissions.Read,
        //            TimeSpan.FromDays(30));

        //        IEnumerable<IAssetFile> assetFiles = asset
        //            .AssetFiles
        //            .ToList()
        //            .Where(af => af.Name.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase));


        //        // Get the URls for progressive download for each specified file that was generated as a result
        //        // of encoding.

        //        List<Uri> sasUris = assetFiles.Select(af => af.GetSasUri()).ToList();

        //        // Display the URLs for progressive download.
        //        //Console.WriteLine("Use the following URLs for progressive download.");
        //        //sasUris.ForEach(uri => Console.WriteLine(uri + "\n"));
        //        //Console.WriteLine();
        //    }
        //}

        //#endregion

        public CloudBlob GetBlobDataAsBlob(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
                CloudBlob blob = blobContainer.GetBlobReference(fileName);

                return blob;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task InsertImageBlob(string filename, string containerName, IFormFile blobFile, string contentType)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            blob.Properties.ContentType = contentType;
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }
        }
        public string InsertBlobVMFile(string filename, string containerName, string blobFile)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlob(containerName, filename);
            blob.UploadTextAsync(blobFile);
            return blob.Uri.ToString();
        }
        public IEnumerable<T> GetLogEntities<T>(string tableName) where T : TableEntity, new()
        {
            var table = GetTable(tableName);
            var entities = new List<T>();
            try
            {
                TableContinuationToken token = null;
                var lowerlimit = DateTime.Today.AddDays(-2);
                string dateRangeFilter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, lowerlimit);
                do
                {
                    var queryResult = table.ExecuteQuerySegmentedAsync(new TableQuery<T>().Where(dateRangeFilter), token);
                    entities.AddRange(queryResult.Result);
                    token = queryResult.Result.ContinuationToken;
                } while (token != null);
            }
            catch (StorageException)
            {
                return entities;
            }
            return entities;
        }
    }

}
