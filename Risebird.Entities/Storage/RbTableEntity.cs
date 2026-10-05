using System;
using Azure;
using Azure.Data.Tables;

namespace Risebird.Entities.Storage
{
    /// <summary>
    /// Base class for Azure Table Storage POCOs, replacing the retired
    /// <c>Microsoft.WindowsAzure.Storage.Table.TableEntity</c>. Implements the current
    /// <see cref="ITableEntity"/> contract; the Azure.Data.Tables client serializes the
    /// derived type's public properties automatically.
    /// </summary>
    public abstract class RbTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
