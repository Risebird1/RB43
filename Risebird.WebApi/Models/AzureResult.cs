using System;
using System.IO;
using System.Net;
using System.Web;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;

namespace WebApplication1.Models
{

    public class AzureResult
    {
        private static readonly bool useAESRestriction = false;

        private static readonly string mediaServiceStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=risebirdtest;AccountKey=7jVJYb07V6vJwoQakqUieolrzecu+G/ovFGFLg8MivrwR3OsWL3553rsl5GMrqx37ONG5fPWMR9riZz+LaIl7Q==;EndpointSuffix=core.windows.net";
        private static readonly string mediaServiceStorageContainerReference = "temporary-video-storage";

        //// DEPRECATED method
        //private static readonly string mediaServiceAccountName = ConfigurationManager.AppSettings["MediaServiceAccountName"];
        //private static readonly string mediaServiceAccountKey = ConfigurationManager.AppSettings["MediaServiceAccountKey"];
        //private static readonly CloudMediaContext context = new CloudMediaContext(new MediaServicesCredentials(mediaServiceAccountName, mediaServiceAccountKey));

        //// Azure Active Directory (AAD)
        private static readonly string mediaServiceADTenantDomain = "talentrisebird.onmicrosoft.com";
        private static readonly string mediaServiceADRestApiEndpoint = "https://risebirdtest.restv2.southindia.media.azure.net/api/";
        private static readonly string mediaServiceADApplicationId = "13ca1deb-ccb6-4c8e-9c98-dd475e7c794b";
        private static readonly string mediaServiceADApplicationSecret = "0bYjljv3w6.eAa7rWzuObXyfJa]edM?]";
        private static readonly AzureAdTokenCredentials azureAdTokenCredentials = new AzureAdTokenCredentials(mediaServiceADTenantDomain, new AzureAdClientSymmetricKey(mediaServiceADApplicationId, mediaServiceADApplicationSecret), AzureEnvironments.AzureCloudEnvironment);
        private static readonly AzureAdTokenProvider azureAdTokenProvider = new AzureAdTokenProvider(azureAdTokenCredentials);
        private static readonly CloudMediaContext context = new CloudMediaContext(new Uri(mediaServiceADRestApiEndpoint), azureAdTokenProvider);
        public string SetMetadata(byte[] chunk, string filename, long filesize)
        {

            string fileName = filename;
            long fileSize = filesize;
            //var assets = context.Assets;
            //foreach (var a in assets)
            //{
            //    Console.WriteLine(a.Name);
            //}
            int blocksCount = 1;
            var container = CloudStorageAccount.Parse(mediaServiceStorageConnectionString).CreateCloudBlobClient().GetContainerReference(mediaServiceStorageContainerReference);

            container.CreateIfNotExists();

            var fileToUpload = new CloudFile()
            {
                BlockCount = blocksCount,
                FileName = fileName,
                Size = fileSize,
                BlockBlob = container.GetBlockBlobReference(fileName),
                StartTime = DateTime.Now,
                IsUploadCompleted = false,
                UploadStatusMessage = string.Empty
            };
            //Session.Add("CurrentFile", fileToUpload);
            var urluri = UploadChunk(1, chunk, fileToUpload);
            return urluri;
        }
        public string UploadChunk(int id, byte[] chunk, CloudFile fileToUpload)
        {
            //byte[] chunk = new byte[request.Length];
            //MemoryStream stream;
            //using (stream = new MemoryStream())
            //{
            //    request.CopyTo(stream);
            //    chunk = stream.ToArray();
            //}
            string returnData = null;
            string fileSession = "CurrentFile";

            //CloudFile model = (CloudFile)Session[fileSession];
            CloudFile model = fileToUpload;
            returnData = UploadCurrentChunk(model, chunk, id);
            if (returnData != null)
            {
                return returnData;
            }
            if (id == model.BlockCount)
            {
                return CommitAllChunks(model, fileToUpload);
            }
            return "";
        }

        private string UploadCurrentChunk(CloudFile model, byte[] chunk, int id)
        {
            using (var chunkStream = new MemoryStream(chunk))
            {
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, "{0:D4}", id)));
                try
                {
                    model.BlockBlob.PutBlock(
                        blockId,
                        chunkStream, null, null,
                        new BlobRequestOptions()
                        {
                            RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(10), 3)
                        },
                        null);
                    return null;
                }
                catch (StorageException e)
                {
                    model.IsUploadCompleted = true;
                    model.UploadStatusMessage = "Failed to Upload file. Exception - " + e.Message;
                    return model.UploadStatusMessage;
                }
            }
        }

        private string CommitAllChunks(CloudFile model, CloudFile fileToUpload)
        {
            model.IsUploadCompleted = true;
            bool errorInOperation = false;
            try
            {
                var blockList = Enumerable.Range(1, (int)model.BlockCount).ToList<int>().ConvertAll(rangeElement => Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "{0:D4}", rangeElement))));
                model.BlockBlob.PutBlockList(blockList);
                var duration = DateTime.Now - model.StartTime;
                float fileSizeInKb = model.Size / 1024;
                string fileSizeMessage = fileSizeInKb > 1024 ? string.Concat((fileSizeInKb / 1024).ToString(CultureInfo.CurrentCulture), " MB") : string.Concat(fileSizeInKb.ToString(CultureInfo.CurrentCulture), " KB");
                model.UploadStatusMessage = string.Format(CultureInfo.CurrentCulture, "File of size {0} took {1} seconds to upload.", fileSizeMessage, duration.TotalSeconds);

                IAsset mediaServiceAsset = CreateMediaAsset(model);
                model.AssetId = mediaServiceAsset.Id;
            }
            catch (StorageException e)
            {
                model.UploadStatusMessage = "Failed to upload file. Exception - " + e.Message;
                errorInOperation = true;
            }
            var utlui = EncodeToAdaptiveBitrateMP4s(model.AssetId, fileToUpload);
            return utlui;
        }

        private IAsset CreateMediaAsset(CloudFile model)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(mediaServiceStorageConnectionString);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer mediaBlobContainer = cloudBlobClient.GetContainerReference(mediaServiceStorageContainerReference);

            mediaBlobContainer.CreateIfNotExists();
            // Create a new asset.
            IAsset asset = context.Assets.Create("UploadedVideo-" + Guid.NewGuid().ToString().ToLower(), AssetCreationOptions.None);
            IAccessPolicy writePolicy = context.AccessPolicies.Create("writePolicy", TimeSpan.FromMinutes(120), AccessPermissions.Write);
            ILocator destinationLocator = context.Locators.CreateLocator(LocatorType.Sas, asset, writePolicy);

            // Get the asset container URI and copy blobs from mediaContainer to assetContainer.
            Uri uploadUri = new Uri(destinationLocator.Path);
            string assetContainerName = uploadUri.Segments[1];
            CloudBlobContainer assetContainer = cloudBlobClient.GetContainerReference(assetContainerName);
            string fileName = HttpUtility.UrlDecode(Path.GetFileName(model.BlockBlob.Uri.AbsoluteUri));

            var sourceCloudBlob = mediaBlobContainer.GetBlockBlobReference(fileName);
            sourceCloudBlob.FetchAttributes();

            if (sourceCloudBlob.Properties.Length > 0)
            {
                IAssetFile assetFile = (IAssetFile)asset.AssetFiles.Create(fileName);
                var destinationBlob = assetContainer.GetBlockBlobReference(fileName);

                destinationBlob.DeleteIfExists();
                destinationBlob.StartCopy(sourceCloudBlob);
                destinationBlob.FetchAttributes();
                if (sourceCloudBlob.Properties.Length != destinationBlob.Properties.Length)
                    model.UploadStatusMessage += "Failed to copy as Media Asset!";
            }
            destinationLocator.Delete();
            writePolicy.Delete();
            sourceCloudBlob.Delete();  //delete temp blob

            // Refresh the asset.
            asset = context.Assets.Where(a => a.Id == asset.Id).FirstOrDefault();

            var ismAssetFiles = asset.AssetFiles.FirstOrDefault();
            ismAssetFiles.IsPrimary = true;
            ismAssetFiles.Update();

            model.UploadStatusMessage += " Media file uploaded successfully by id: " + asset.Id;
            model.AssetId = asset.Id;



            return asset;
        }


        public string EncodeToAdaptiveBitrateMP4s(string assetId, CloudFile fileToUpload)
        {

            IAsset inputAsset = GetAssetById(assetId);
            string token = string.Empty;
            string uploadFileOriginalName = string.Empty;

            IJob job = context.Jobs.Create(inputAsset.Name);
            IMediaProcessor processor = GetLatestMediaProcessorByName("Media Encoder Standard");
            string configuration = File.ReadAllText(HttpContext.Current.Server.MapPath("~/MediaServicesCustomPreset.xml"));
            ITask task = job.Tasks.AddNew(inputAsset.Name + "- encoding task", processor, configuration, TaskOptions.None);
            task.InputAssets.Add(inputAsset);
            task.OutputAssets.AddNew(inputAsset.Name + "-Adaptive-Bitrate-MP4", AssetCreationOptions.None);
            job.Submit();
            IAsset encodedAsset = job.OutputMediaAssets[0];

            // process policy & encryption
            ProcessPolicyAndEncryption(encodedAsset);

            // Get file name
            string fileSession = "CurrentFile";

            CloudFile model = fileToUpload;
            uploadFileOriginalName = model.FileName;
            // Generate Streaming URL
            string smoothStreamingUri = GetStreamingOriginLocator(encodedAsset, uploadFileOriginalName);
            return smoothStreamingUri;

        }

        public void ProcessPolicyAndEncryption(IAsset asset)
        {
            IContentKey key = CreateEnvelopeTypeContentKey(asset);

            if (useAESRestriction)
            {
                // AES restriction
                AddTokenRestrictedAuthorizationPolicy(key);
            }
            else
            {
                // No restriction
                AddOpenAuthorizationPolicy(key);
            }

            // Set asset delivery policy
            CreateAssetDeliveryPolicy(asset, key);
        }

        static public void AddOpenAuthorizationPolicy(IContentKey contentKey)
        {
            // Create ContentKeyAuthorizationPolicy with Open restrictions and create authorization policy             
            IContentKeyAuthorizationPolicy policy = context.ContentKeyAuthorizationPolicies.CreateAsync("Open Authorization Policy").Result;

            List<ContentKeyAuthorizationPolicyRestriction> restrictions = new List<ContentKeyAuthorizationPolicyRestriction>();
            ContentKeyAuthorizationPolicyRestriction restriction =
                new ContentKeyAuthorizationPolicyRestriction
                {
                    Name = "HLS Open Authorization Policy",
                    KeyRestrictionType = (int)ContentKeyRestrictionType.Open,
                    Requirements = null // no requirements needed for HLS
                };

            restrictions.Add(restriction);

            IContentKeyAuthorizationPolicyOption policyOption =
                context.ContentKeyAuthorizationPolicyOptions.Create(
                "policy",
                ContentKeyDeliveryType.BaselineHttp,
                restrictions,
                "");

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;
        }

        static public IContentKey CreateEnvelopeTypeContentKey(IAsset asset)
        {
            // Create envelope encryption content key
            Guid keyId = Guid.NewGuid();
            byte[] contentKey = GetRandomBuffer(16);

            IContentKey key = context.ContentKeys.Create(
                                    keyId,
                                    contentKey,
                                    "ContentKey",
                                    ContentKeyType.EnvelopeEncryption);

            // Associate the key with the asset.
            asset.ContentKeys.Add(key);

            return key;
        }

        static public string GetStreamingOriginLocator(IAsset asset, string filename)
        {
            // Get a reference to the streaming manifest file from the collection of files in the asset. 
            //var assetFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();

            // A locator expiry can be set to maximum 100 years, using 99 years below.
            TimeSpan daysForWhichStreamingUrlIsActive = DateTime.Now.AddYears(99) - DateTime.Now;
            IAccessPolicy policy = context.AccessPolicies.Create("Streaming policy", daysForWhichStreamingUrlIsActive, AccessPermissions.Read);

            // Create a locator to the streaming content on an origin.
            ILocator originLocator = context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, policy, DateTime.UtcNow.AddMinutes(-5));

            // Create a URL to the manifest file.

            return GetWithoutHttp(originLocator.Path + Path.GetFileNameWithoutExtension(filename) + ".ism/manifest");
        }

        static public void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key)
        {
            Uri keyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);

            string envelopeEncryptionIV = Convert.ToBase64String(GetRandomBuffer(16));

            // The following policy configuration specifies: key url that will have KID=<Guid> appended to the envelope and
            // the Initialization Vector (IV) to use for the envelope encryption.
            Dictionary<AssetDeliveryPolicyConfigurationKey, string> assetDeliveryPolicyConfiguration =
                new Dictionary<AssetDeliveryPolicyConfigurationKey, string>
            {
                        {AssetDeliveryPolicyConfigurationKey.EnvelopeKeyAcquisitionUrl, keyAcquisitionUri.ToString()}
            };

            IAssetDeliveryPolicy assetDeliveryPolicy =
                context.AssetDeliveryPolicies.Create(
                            "AssetDeliveryPolicy",
                            AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                            AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.Dash,
                            assetDeliveryPolicyConfiguration);

            // Add AssetDelivery Policy to the asset
            asset.DeliveryPolicies.Add(assetDeliveryPolicy);
        }

        public static string AddTokenRestrictedAuthorizationPolicy(IContentKey contentKey)
        {
            string tokenTemplateString = GenerateTokenRequirements();

            IContentKeyAuthorizationPolicy policy = context.
                                    ContentKeyAuthorizationPolicies.
                                    CreateAsync("HLS token restricted authorization policy").Result;

            List<ContentKeyAuthorizationPolicyRestriction> restrictions = new List<ContentKeyAuthorizationPolicyRestriction>();

            ContentKeyAuthorizationPolicyRestriction restriction =
                    new ContentKeyAuthorizationPolicyRestriction
                    {
                        Name = "Token Authorization Policy",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.TokenRestricted,
                        Requirements = tokenTemplateString
                    };

            restrictions.Add(restriction);

            //You could have multiple options 
            IContentKeyAuthorizationPolicyOption policyOption =
                context.ContentKeyAuthorizationPolicyOptions.Create(
                    "Token option for HLS",
                    ContentKeyDeliveryType.BaselineHttp,
                    restrictions,
                    null  // no key delivery data is needed for HLS
                    );

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;

            return tokenTemplateString;
        }

        static private string GenerateTokenRequirements()
        {
            TokenRestrictionTemplate template = new TokenRestrictionTemplate(TokenType.SWT);

            template.PrimaryVerificationKey = new SymmetricVerificationKey();
            template.AlternateVerificationKeys.Add(new SymmetricVerificationKey());
            template.Audience = "urn:testwebsite";
            template.Issuer = "http://test.com";

            template.RequiredClaims.Add(TokenClaim.ContentKeyIdentifierClaim);

            return TokenRestrictionTemplateSerializer.Serialize(template);
        }

        static private byte[] GetRandomBuffer(int size)
        {
            byte[] randomBytes = new byte[size];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
            ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        private static string GetWithoutHttp(string smoothStreamingUri)
        {
            if (smoothStreamingUri.StartsWith("http:"))
            {
                smoothStreamingUri = smoothStreamingUri.Substring("http:".Length);
            }

            if (smoothStreamingUri.StartsWith("https:"))
            {
                smoothStreamingUri = smoothStreamingUri.Substring("https:".Length);
            }

            return smoothStreamingUri;
        }

        // Create locator and get urls
        static public string PublishAssetGetURLs(IAsset asset)
        {
            // A locator expiry can be set to maximum 100 years, using 99 years below.
            TimeSpan daysForWhichStreamingUrlIsActive = DateTime.Now.AddYears(99) - DateTime.Now;
            IAccessPolicy videoWatchPolicy = context.AccessPolicies.Create("videoWatchPolicy", daysForWhichStreamingUrlIsActive, AccessPermissions.Read);
            ILocator destinationLocator = context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, videoWatchPolicy);

            // Get the asset container URI
            Uri uploadUri = new Uri(destinationLocator.Path);

            // Note: You need atleast 1 reserve streaming unit for dynamic packaging of encoded media. If you don't have that, you can't see video file playing
            return uploadUri.ToString() + asset.Name + ".ism/manifest";
        }

        internal static IAsset GetAssetById(string assetId)
        {
            IAsset theAsset = (from a in context.Assets
                               where a.Id == assetId
                               select a).FirstOrDefault();
            return theAsset;
        }
    }

    public class CloudFile
    {
        public string FileName { get; set; }
        public string URL { get; set; }
        public long Size { get; set; }
        public long BlockCount { get; set; }
        public CloudBlockBlob BlockBlob { get; set; }
        public DateTime StartTime { get; set; }
        public string UploadStatusMessage { get; set; }
        public bool IsUploadCompleted { get; set; }
        public string AssetId { get; set; }
        public static CloudFile CreateFromIListBlobItem(IListBlobItem item)
        {
            if (item is CloudBlockBlob)
            {
                var blob = (CloudBlockBlob)item;
                return new CloudFile
                {
                    FileName = blob.Name,
                    URL = blob.Uri.ToString(),
                    Size = blob.Properties.Length
                };
            }
            return null;
        }
    }
    public class Reservation
    {
        public byte[] chunk { get; set; }
        public string filename { get; set; }
        public long fileSize { get; set; }

    }
}