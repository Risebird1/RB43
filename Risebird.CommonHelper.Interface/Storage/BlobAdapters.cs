using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Risebird.CommonHelper.Storage
{
    /// <summary>
    /// Thin adapters over <see cref="Azure.Storage.Blobs"/> that preserve the small member surface
    /// the codebase used from the retired <c>Microsoft.WindowsAzure.Storage</c> SDK
    /// (GetBlockBlobReference / GetBlobReference / Properties.ContentType / UploadFrom* / Uri / ExistsAsync
    /// / OpenReadAsync / DownloadToStreamAsync). This keeps the ~30 call sites that consume
    /// <see cref="IAzureStorageHelper"/> unchanged while the transport moves to the current SDK.
    /// </summary>
    public sealed class RbBlobContainer
    {
        private readonly BlobContainerClient _container;

        public RbBlobContainer(BlobContainerClient container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>The underlying SDK container client, for callers that need the current API directly.</summary>
        public BlobContainerClient Client => _container;

        public RbBlockBlob GetBlockBlobReference(string blobName) => new RbBlockBlob(_container.GetBlobClient(blobName));

        public RbBlob GetBlobReference(string blobName) => new RbBlob(_container.GetBlobClient(blobName));

        public async Task<bool> ExistsAsync() => (await _container.ExistsAsync().ConfigureAwait(false)).Value;
    }

    /// <summary>Pending blob properties applied at upload time (mirrors the old set-then-upload idiom).</summary>
    public sealed class RbBlobProperties
    {
        public string ContentType { get; set; }
    }

    /// <summary>Adapter for a block blob: set <see cref="Properties"/>.ContentType, then UploadFrom*.</summary>
    public sealed class RbBlockBlob
    {
        private readonly BlobClient _blob;

        internal RbBlockBlob(BlobClient blob)
        {
            _blob = blob ?? throw new ArgumentNullException(nameof(blob));
        }

        public BlobClient Client => _blob;

        public Uri Uri => _blob.Uri;

        public RbBlobProperties Properties { get; } = new RbBlobProperties();

        private BlobUploadOptions BuildOptions()
        {
            var options = new BlobUploadOptions();
            if (!string.IsNullOrEmpty(Properties.ContentType))
            {
                options.HttpHeaders = new BlobHttpHeaders { ContentType = Properties.ContentType };
            }
            return options;
        }

        public Task UploadFromStreamAsync(Stream source) => _blob.UploadAsync(source, BuildOptions());

        public async Task UploadFromByteArrayAsync(byte[] buffer, int index, int count)
        {
            using var ms = new MemoryStream(buffer, index, count);
            await _blob.UploadAsync(ms, BuildOptions()).ConfigureAwait(false);
        }

        public Task UploadTextAsync(string content) =>
            _blob.UploadAsync(BinaryData.FromString(content ?? string.Empty), BuildOptions());

        public async Task<bool> ExistsAsync() => (await _blob.ExistsAsync().ConfigureAwait(false)).Value;

        public Task DeleteAsync() => _blob.DeleteIfExistsAsync();
    }

    /// <summary>Adapter for a read-oriented blob reference.</summary>
    public sealed class RbBlob
    {
        private readonly BlobClient _blob;

        internal RbBlob(BlobClient blob)
        {
            _blob = blob ?? throw new ArgumentNullException(nameof(blob));
        }

        public BlobClient Client => _blob;

        public Uri Uri => _blob.Uri;

        public async Task<bool> ExistsAsync() => (await _blob.ExistsAsync().ConfigureAwait(false)).Value;

        public Task<Stream> OpenReadAsync() => _blob.OpenReadAsync();

        public Task DownloadToStreamAsync(Stream target) => _blob.DownloadToAsync(target);
    }
}
