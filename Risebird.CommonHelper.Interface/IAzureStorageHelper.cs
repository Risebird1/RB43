using Microsoft.AspNetCore.Http;
using Azure.Data.Tables;
using Risebird.CommonHelper.Storage;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Risebird.CommonHelper.Interface
{
    public interface IAzureStorageHelper
    {
        Task<bool> CheckTableExistsOrNot(string tableName);
        bool DeleteTable(string tableName);
        Task<bool> InsertEntity(string tableName, object obj);
        Task<bool> DeleteEntity(string tableName, object obj);
        RbBlockBlob GetCloudBlockBlob(string containerName, string filename);
        bool DeleteBlob(string fileName, string containerName);
        RbBlobContainer GetCloudBlobContainer(string containerName);
        IEnumerable<T> GetAllEntities<T>(string tableName) where T : class, ITableEntity, new();
        Task InsertBlob(string filename, string containerName, IFormFile blobFile);
        Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile = null, Stream blobFIleStream = null, Stream transcriptStream = null, string contentType = null, bool? isOpenAiQc = false);
        string CheckIfFileExists(string containerName, string fileName);
        bool DeleteBlobContainerFile(string vhdName, string containerName);
        Task<bool> CheckBlobContainerFile(string vhdName, string containerName);
        string GetBlobData(string containerName, string fileName);
        RbBlob GetBlobDataAsBlob(string containerName, string fileName);
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();

        Task InsertResumeBlob(string filename, string containerName, IFormFile blobFile, string extension);
        Task InsertImageBlob(string filename, string containerName, IFormFile blobFile, string contentType);
        string InsertBlobVMFile(string filename, string containerName, string blobFile);
        Task<bool> IsDocFileAsync(string url);
        Task<string> DownloadAndConvertFromDocToPdfAsync(string url);
        Task<string> InsertBlobAIInterviewerAudio(string filename, string containerName, MemoryStream blobFile = null, string contentType = null);
        Task<string> InsertBlobWithCustomStorage(string filename, string containerName, string storageAccountName, string storageAccountKey, MemoryStream blobFile = null, Stream blobFileStream = null, string contentType = null);
        Task<IList<T>> GetLogEntitiesByDateRangeAsync<T>(string tableName, DateTime startUtc, DateTime endUtcExclusive, int? logLevel = null, IList<string> selectColumns = null, int? maxResults = null) where T : class, ITableEntity, new();
        Task<AzureTablePageResult<T>> GetLogEntitiesByDateRangePagedAsync<T>(string tableName, DateTime startUtc, DateTime endUtcExclusive, int pageSize, string continuationToken = null, int? logLevel = null, IList<string> selectColumns = null) where T : class, ITableEntity, new();
        Task<IList<T>> GetEntitiesByPartitionKeyRangeAsync<T>(string tableName, string partitionKeyFromInclusive, string partitionKeyToInclusive) where T : class, ITableEntity, new();
    }
}
