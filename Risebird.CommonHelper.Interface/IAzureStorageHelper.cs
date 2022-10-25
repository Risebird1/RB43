using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Risebird.CommonHelper.Interface
{
    public interface IAzureStorageHelper
    {
        CloudTable GetTable(string tableName);
        CloudTable GetTableWithAccessKey(string tableName);
        Task<bool> CheckTableExistsOrNot(string tableName);
        bool DeleteTable(string tableName);
        Task<bool> InsertEntity(string tableName, object obj);
        Task<bool> DeleteEntity(string tableName, object obj);
        CloudStorageAccount GetStorageAccount();
        CloudBlockBlob GetCloudBlockBlob(string containerName, string filename);
        bool DeleteBlob(string fileName, string containerName);
        CloudBlobContainer GetCloudBlobContainer(string containerName);
        TableResult RetriveByPartitionAndRowKey(string tableName, string partitionKey, string rowKey);
        Task<TableResult> RetriveByPartitionAndRowKeyAsync(string tableName, string partitionKey, string rowKey);
        IEnumerable<T> GetAllEntities<T>(string tableName) where T : TableEntity, new();
        Task InsertBlob(string filename, string containerName, IFormFile blobFile);
        Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile = null, Stream blobFIleStream = null);
        string CheckIfFileExists(string containerName, string fileName);
        bool DeleteBlobContainerFile(string vhdName, string containerName);
        Task<bool> CheckBlobContainerFile(string vhdName, string containerName);
        string GetBlobData(string containerName, string fileName);
        void InsertBatchInTable(string tableName, TableBatchOperation tableBatchOperation);
        CloudBlob GetBlobDataAsBlob(string containerName, string fileName);
        //string UploadVideo(string filename);
        //Task<string> UploadVideoAsset(string filePath);

        //string UploadAndPublishVideo(string filePath);
        Task<CloudTable> GetTableAsync(string tableName);
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : TableEntity, new();

        Task InsertResumeBlob(string filename, string containerName, IFormFile blobFile, string extension);
        Task InsertImageBlob(string filename, string containerName, IFormFile blobFile,string contentType);
    }
}
