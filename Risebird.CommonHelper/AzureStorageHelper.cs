using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AzureStorageHelper: IAzureStorageHelper
    {
        public CloudTableClient TableClient;
        private readonly string accountName;
        private readonly string accountKey;
        private readonly IConfiguration _configuration;
        public AzureStorageHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            accountName = _configuration["StorageSettings:StorageName"];
            accountKey = _configuration["StorageSettings:SecretKey"];
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
            catch (Exception ex)
            {
                throw ex;
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
        /// INSERT NEW SINGLE ROW
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<bool> InsertEntity(string tableName, object obj)
        {
            bool insertResult = false;
            try
            {
                var table = GetTable(tableName);
                TableOperation insertOperation = TableOperation.InsertOrReplace(obj as TableEntity);

                TableResult result = await table.ExecuteAsync(insertOperation);

                insertResult = true;

                return insertResult;
            }
            catch (Exception ex)
            {
                insertResult = false;
                throw ex;
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
        public string InsertBlob(string filename, string containerName, object blobFile)
        {
            CloudBlockBlob blob;
            blob = GetCloudBlockBlobClient(containerName, filename);
            blob.UploadTextAsync(JsonConvert.SerializeObject(blobFile));
            return blob.Uri.ToString();
        }

        public async Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile)
        {
            CloudBlockBlob blob;
           
                blob = GetCloudBlockBlobClient(containerName, filename);
            if (blobFile != null)
            {
                await blob.UploadFromStreamAsync(blobFile);
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        
        
    }
}
