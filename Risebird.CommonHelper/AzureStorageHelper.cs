using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Storage;
using Risebird.Entities.ViewModel;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AzureStorageHelper : IAzureStorageHelper
    {
        private readonly string accountName;
        private readonly string accountKey;
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobService;
        private readonly TableServiceClient _tableService;

        private readonly IConfiguration _configuration;

        private readonly IHttpClientFactory _httpClientFactory;

        public AzureStorageHelper(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            accountName = _configuration["StorageSettings:StorageName"];
            accountKey = _configuration["StorageSettings:SecretKey"];
            _connectionString = BuildConnectionString(accountName, accountKey);
            _blobService = new BlobServiceClient(_connectionString);
            _tableService = new TableServiceClient(_connectionString);
        }

        private static string BuildConnectionString(string storageAccountName, string storageAccountKey) =>
            $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKey};EndpointSuffix=core.windows.net";

        // ---------------------------------------------------------------------
        // Table storage (Azure.Data.Tables)
        // ---------------------------------------------------------------------

        /// <summary>Returns a table client, creating the table if it does not exist.</summary>
        private TableClient GetTableClient(string tableName)
        {
            var table = _tableService.GetTableClient(tableName);
            table.CreateIfNotExists();
            return table;
        }

        public async Task<bool> CheckTableExistsOrNot(string tableName)
        {
            await foreach (var _ in _tableService.QueryAsync(filter: $"TableName eq '{tableName}'").ConfigureAwait(false))
                return true;
            return false;
        }

        public bool DeleteTable(string tableName)
        {
            try { _tableService.DeleteTable(tableName); }
            catch { /* table may not exist */ }
            return true;
        }

        /// <summary>INSERT OR REPLACE A SINGLE ROW.</summary>
        public async Task<bool> InsertEntity(string tableName, object obj)
        {
            var table = GetTableClient(tableName);
            await table.UpsertEntityAsync((ITableEntity)obj, TableUpdateMode.Replace).ConfigureAwait(false);
            return true;
        }

        /// <summary>DELETE A SINGLE ROW.</summary>
        public async Task<bool> DeleteEntity(string tableName, object obj)
        {
            try
            {
                var table = GetTableClient(tableName);
                var entity = (ITableEntity)obj;
                await table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<T> GetAllEntities<T>(string tableName) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            try
            {
                return table.Query<T>().ToList();
            }
            catch
            {
                return new List<T>();
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            var entities = new List<T>();
            try
            {
                await foreach (var entity in table.QueryAsync<T>().ConfigureAwait(false))
                    entities.Add(entity);
            }
            catch
            {
                return entities;
            }
            return entities;
        }

        public IEnumerable<T> GetLogEntities<T>(string tableName) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            try
            {
                var lowerlimit = new DateTimeOffset(DateTime.Today.AddDays(-2).ToUniversalTime());
                var filter = TableClient.CreateQueryFilter($"Timestamp ge {lowerlimit}");
                return table.Query<T>(filter).ToList();
            }
            catch
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// Fetches Risebirdlogs rows for a UTC date window.
        /// PartitionKey is reverse-ticks (DateTime.MaxValue.Ticks - UtcNow.Ticks), so date filters
        /// must use PartitionKey range — Timestamp filters force a full table scan and hang on large tables.
        /// </summary>
        public async Task<IList<T>> GetLogEntitiesByDateRangeAsync<T>(
            string tableName,
            DateTime startUtc,
            DateTime endUtcExclusive,
            int? logLevel = null,
            IList<string> selectColumns = null,
            int? maxResults = null)
            where T : class, ITableEntity, new()
        {
            var entities = new List<T>();
            try
            {
                var table = GetTableClient(tableName);
                var start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endUtcExclusive, DateTimeKind.Utc);
                if (end <= start)
                    return entities;

                // Newer logs have smaller reverse-tick PartitionKeys.
                // start <= T < end  ⇒  (MaxTicks-end) < PK <= (MaxTicks-start)
                var pkLowerExclusive = ToReverseTickPartitionKey(end);
                var pkUpperInclusive = ToReverseTickPartitionKey(start);

                var filter = BuildPartitionRangeFilter(pkLowerExclusive, pkUpperInclusive, logLevel);

                await foreach (var entity in table.QueryAsync<T>(filter: filter, select: selectColumns).ConfigureAwait(false))
                {
                    entities.Add(entity);
                    if (maxResults.HasValue && entities.Count >= maxResults.Value)
                        break;
                }
            }
            catch
            {
                return entities;
            }

            return entities;
        }

        /// <summary>
        /// Paged Risebirdlogs query by reverse-tick PartitionKey range. Pass continuationToken from a prior page for Next.
        /// </summary>
        public async Task<AzureTablePageResult<T>> GetLogEntitiesByDateRangePagedAsync<T>(
            string tableName,
            DateTime startUtc,
            DateTime endUtcExclusive,
            int pageSize,
            string continuationToken = null,
            int? logLevel = null,
            IList<string> selectColumns = null)
            where T : class, ITableEntity, new()
        {
            var result = new AzureTablePageResult<T>();
            if (pageSize <= 0)
                pageSize = 100;

            try
            {
                var table = GetTableClient(tableName);
                var start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endUtcExclusive, DateTimeKind.Utc);
                if (end <= start)
                    return result;

                var pkLowerExclusive = ToReverseTickPartitionKey(end);
                var pkUpperInclusive = ToReverseTickPartitionKey(start);

                var filter = BuildPartitionRangeFilter(pkLowerExclusive, pkUpperInclusive, logLevel);

                var pages = table.QueryAsync<T>(filter: filter, maxPerPage: pageSize, select: selectColumns)
                    .AsPages(string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken);

                await foreach (var page in pages.ConfigureAwait(false))
                {
                    result.Items = page.Values.ToList();
                    result.ContinuationToken = page.ContinuationToken;
                    break; // a single page per call
                }
            }
            catch
            {
                return result;
            }

            return result;
        }

        /// <summary>Fetches entities whose PartitionKey is within an inclusive string range (e.g. yyyy-MM-dd dates).</summary>
        public async Task<IList<T>> GetEntitiesByPartitionKeyRangeAsync<T>(
            string tableName,
            string partitionKeyFromInclusive,
            string partitionKeyToInclusive)
            where T : class, ITableEntity, new()
        {
            var entities = new List<T>();
            try
            {
                var table = GetTableClient(tableName);
                var filter = TableClient.CreateQueryFilter(
                    $"PartitionKey ge {partitionKeyFromInclusive} and PartitionKey le {partitionKeyToInclusive}");

                await foreach (var entity in table.QueryAsync<T>(filter: filter).ConfigureAwait(false))
                    entities.Add(entity);
            }
            catch
            {
                return entities;
            }

            return entities;
        }

        private static string BuildPartitionRangeFilter(string pkLowerExclusive, string pkUpperInclusive, int? logLevel)
        {
            var filter = TableClient.CreateQueryFilter(
                $"PartitionKey gt {pkLowerExclusive} and PartitionKey le {pkUpperInclusive}");
            if (logLevel.HasValue)
            {
                var levelFilter = TableClient.CreateQueryFilter($"LogeLevel eq {logLevel.Value}");
                filter = $"({filter}) and ({levelFilter})";
            }
            return filter;
        }

        /// <summary>Same encoding ApplicationErrorLogger uses for Risebirdlogs PartitionKey.</summary>
        private static string ToReverseTickPartitionKey(DateTime utc)
        {
            var ticks = DateTime.SpecifyKind(utc, DateTimeKind.Utc).Ticks;
            return (DateTime.MaxValue.Ticks - ticks).ToString("d19");
        }

        // ---------------------------------------------------------------------
        // Blob storage (Azure.Storage.Blobs, via Risebird.CommonHelper.Storage adapters)
        // ---------------------------------------------------------------------

        /// <summary>Returns a container adapter, creating the container (with public Container access) if missing.</summary>
        public RbBlobContainer GetCloudBlobContainer(string containerName)
        {
            var container = _blobService.GetBlobContainerClient(containerName);
            container.CreateIfNotExists(PublicAccessType.BlobContainer);
            return new RbBlobContainer(container);
        }

        public RbBlockBlob GetCloudBlockBlob(string containerName, string filename) =>
            GetCloudBlobContainer(containerName).GetBlockBlobReference(filename);

        public RbBlockBlob GetCloudBlockBlobClient(string containerName, string filename) =>
            GetCloudBlobContainer(containerName).GetBlockBlobReference(filename);

        public RbBlob GetBlobDataAsBlob(string containerName, string fileName)
        {
            try
            {
                return GetCloudBlobContainer(containerName).GetBlobReference(fileName);
            }
            catch
            {
                return null;
            }
        }

        public bool DeleteBlobContainerFile(string fileName, string containerName)
        {
            try
            {
                var container = _blobService.GetBlobContainerClient(containerName);
                container.CreateIfNotExists(PublicAccessType.BlobContainer);
                container.GetBlobClient(fileName).DeleteIfExists();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckBlobContainerFile(string fileName, string containerName)
        {
            var blob = GetCloudBlobContainer(containerName).GetBlobReference(fileName);
            return await blob.ExistsAsync().ConfigureAwait(false);
        }

        public async Task InsertBlob(string filename, string containerName, IFormFile blobFile)
        {
            var blob = GetCloudBlockBlobClient(containerName, filename);
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }
        }

        public async Task InsertResumeBlob(string filename, string containerName, IFormFile blobFile, string extension)
        {
            var blob = GetCloudBlockBlobClient(containerName, filename);
            string contentType;
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

        public async Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile = null, Stream blobFileStream = null, Stream transcriptStream = null, string contentType = null, bool? isOpenAiQc = false)
        {
            var blob = GetCloudBlockBlobClient(containerName, filename);
            if (blobFile != null)
            {
                if (isOpenAiQc.HasValue && isOpenAiQc.Value)
                {
                    await blob.UploadFromStreamAsync(blobFile).ConfigureAwait(false);
                }
                else
                {
                    await blob.UploadFromStreamAsync(blobFileStream).ConfigureAwait(false);
                }
            }
            else if (blobFileStream != null)
            {
                blob.Properties.ContentType = !string.IsNullOrEmpty(contentType) ? contentType : "video/mp4";
                await blob.UploadFromStreamAsync(blobFileStream).ConfigureAwait(false);
            }
            else if (transcriptStream != null)
            {
                blob.Properties.ContentType = "text/plain";
                await blob.UploadFromStreamAsync(transcriptStream).ConfigureAwait(false);
            }
            else
            {
                return null;
            }

            return blob.Uri.ToString();
        }

        public async Task<string> InsertBlobAIInterviewerAudio(string filename, string containerName, MemoryStream blobFile = null, string contentType = null)
        {
            var blob = GetCloudBlockBlobClient(containerName, filename);
            blob.Properties.ContentType = contentType;
            if (blobFile != null)
            {
                await blob.UploadFromStreamAsync(blobFile).ConfigureAwait(false);
            }

            return blob.Uri.ToString();
        }

        public async Task<string> InsertBlobWithCustomStorage(string filename, string containerName, string storageAccountName, string storageAccountKey, MemoryStream blobFile = null, Stream blobFileStream = null, string contentType = null)
        {
            try
            {
                var customService = new BlobServiceClient(BuildConnectionString(storageAccountName, storageAccountKey));
                var container = customService.GetBlobContainerClient(containerName);
                container.CreateIfNotExists(PublicAccessType.BlobContainer);

                var blob = new RbBlobContainer(container).GetBlockBlobReference(filename);
                blob.Properties.ContentType = !string.IsNullOrEmpty(contentType) ? contentType : "video/mp4";

                if (blobFile != null)
                {
                    await blob.UploadFromStreamAsync(blobFile).ConfigureAwait(false);
                }
                else if (blobFileStream != null)
                {
                    await blob.UploadFromStreamAsync(blobFileStream).ConfigureAwait(false);
                }
                else
                {
                    return null;
                }

                return blob.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading blob to custom storage account: {ex.Message}", ex);
            }
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
                var container = GetCloudBlobContainer(containerName);
                return container.GetBlobReference(fileName).ExistsAsync().Result ? fileName : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetBlobData(string containerName, string fileName)
        {
            try
            {
                var container = GetCloudBlobContainer(containerName);
                var blob = container.GetBlobReference(fileName);
                if (!blob.ExistsAsync().Result)
                    return "";
                using (var reader = new StreamReader(blob.OpenReadAsync().Result))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task InsertImageBlob(string filename, string containerName, IFormFile blobFile, string contentType)
        {
            var blob = GetCloudBlockBlobClient(containerName, filename);
            blob.Properties.ContentType = contentType;
            using (var fileStream = blobFile.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(fileStream).ConfigureAwait(false);
            }
        }

        public string InsertBlobVMFile(string filename, string containerName, string blobFile)
        {
            var blob = GetCloudBlockBlob(containerName, filename);
            blob.UploadTextAsync(blobFile);
            return blob.Uri.ToString();
        }

        // ---------------------------------------------------------------------
        // Resume format conversion helpers
        // ---------------------------------------------------------------------

        public async Task<bool> IsDocFileAsync(string url)
        {
            try
            {
                if (url.EndsWith(".doc"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking the resume format: {ex.Message}");
            }
            return false;
        }

        public async Task<string> DownloadAndConvertFromDocToPdfAsync(string url)
        {
            try
            {
                string localPath = string.Empty;
                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        localPath = Path.GetTempFileName();
                        using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }
                }

                if (string.IsNullOrEmpty(localPath))
                {
                    Console.WriteLine("Failed to download the resume.");
                    return null;
                }

                string pdfPath = Path.ChangeExtension(localPath, ".pdf");

                using (FileStream inputStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    WordDocument document = new WordDocument();
                    document.Open(inputStream, FormatType.Doc);

                    // Convert Word document to PDF
                    using (DocIORenderer renderer = new DocIORenderer())
                    {
                        renderer.Settings.AutoTag = true;
                        using (PdfDocument pdfDocument = renderer.ConvertToPDF(document))
                        {
                            using (FileStream outputFileStream = new FileStream(pdfPath, FileMode.Create, FileAccess.ReadWrite))
                            {
                                pdfDocument.Save(outputFileStream);
                            }
                        }
                    }
                }

                string uploadedFileUri = string.Empty;

                // Upload the converted .pdf back to Azure Blob Storage
                byte[] fileContent = File.ReadAllBytes(pdfPath);
                using (Stream memoryStream = new MemoryStream(fileContent))
                {
                    uploadedFileUri = await InsertBlobCloudCompare(url.Split("/").Last().Split(".").First() + ".pdf", "resumes", blobFileStream: memoryStream, contentType: "application/pdf");
                }

                // Cleanup temporary files
                File.Delete(localPath);
                File.Delete(pdfPath);
                return uploadedFileUri;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading the resume: {ex.Message}");
            }
            return null;
        }
    }
}
