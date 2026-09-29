using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Risebird.CommonHelper.Storage;

namespace Risebird.CommonHelper.Tests.Storage
{
    /// <summary>
    /// The blob adapters (<see cref="RbBlobContainer"/>/<see cref="RbBlockBlob"/>/<see cref="RbBlob"/>)
    /// exist so that ~30 controllers using the old <c>GetBlockBlobReference().Properties.ContentType = x;
    /// UploadFromStreamAsync(...)</c> idiom keep working unchanged on top of Azure.Storage.Blobs.
    /// The one piece of real behaviour they carry is translating that set-then-upload idiom into a
    /// single <see cref="BlobUploadOptions"/> with the right <see cref="BlobHttpHeaders.ContentType"/> —
    /// get that wrong and resumes/videos upload with the wrong content type and stop rendering in the
    /// browser. These tests drive a mocked SDK client and assert the delegation and that translation.
    /// </summary>
    public sealed class BlobAdapterTests
    {
        private static Response<T> Ok<T>(T value) => Response.FromValue(value, Mock.Of<Response>());

        /// <summary>A blob-client mock that captures the options/stream handed to UploadAsync.</summary>
        private static Mock<BlobClient> UploadCapturingBlob(
            Action<BlobUploadOptions> onOptions = null,
            Action<byte[]> onStreamBytes = null)
        {
            var blob = new Mock<BlobClient>(MockBehavior.Loose);

            blob.Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, BlobUploadOptions, CancellationToken>((stream, options, _) =>
                {
                    onOptions?.Invoke(options);
                    if (onStreamBytes != null)
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        onStreamBytes(ms.ToArray());
                    }
                })
                .ReturnsAsync(Ok<BlobContentInfo>(null));

            blob.Setup(b => b.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
                .Callback<BinaryData, BlobUploadOptions, CancellationToken>((data, options, _) =>
                {
                    onOptions?.Invoke(options);
                    onStreamBytes?.Invoke(data.ToArray());
                })
                .ReturnsAsync(Ok<BlobContentInfo>(null));

            return blob;
        }

        private static RbBlobContainer ContainerWrapping(BlobClient blob)
        {
            var container = new Mock<BlobContainerClient>(MockBehavior.Loose);
            container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob);
            return new RbBlobContainer(container.Object);
        }

        // ---- container delegation -------------------------------------------

        [Fact]
        public void GetBlockBlobReference_asks_the_container_for_a_client_by_name()
        {
            var container = new Mock<BlobContainerClient>(MockBehavior.Loose);
            container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(Mock.Of<BlobClient>());

            new RbBlobContainer(container.Object).GetBlockBlobReference("resume.pdf");

            container.Verify(c => c.GetBlobClient("resume.pdf"), Times.Once);
        }

        [Fact]
        public void GetBlobReference_asks_the_container_for_a_client_by_name()
        {
            var container = new Mock<BlobContainerClient>(MockBehavior.Loose);
            container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(Mock.Of<BlobClient>());

            new RbBlobContainer(container.Object).GetBlobReference("audio.mp3");

            container.Verify(c => c.GetBlobClient("audio.mp3"), Times.Once);
        }

        [Fact]
        public async Task Container_ExistsAsync_unwraps_the_response_value()
        {
            var container = new Mock<BlobContainerClient>(MockBehavior.Loose);
            container.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Ok(true));

            Assert.True(await new RbBlobContainer(container.Object).ExistsAsync());
        }

        // ---- the content-type translation (the behaviour that matters) ------

        [Fact]
        public async Task Upload_without_a_content_type_sends_no_http_headers()
        {
            BlobUploadOptions captured = null;
            var blob = UploadCapturingBlob(onOptions: o => captured = o);

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("x.bin");
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });
            await block.UploadFromStreamAsync(content);

            Assert.NotNull(captured);
            Assert.Null(captured.HttpHeaders);
        }

        [Fact]
        public async Task Setting_Properties_ContentType_then_uploading_puts_it_on_the_upload_headers()
        {
            BlobUploadOptions captured = null;
            var blob = UploadCapturingBlob(onOptions: o => captured = o);

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("resume.pdf");
            block.Properties.ContentType = "application/pdf";
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });
            await block.UploadFromStreamAsync(content);

            Assert.NotNull(captured?.HttpHeaders);
            Assert.Equal("application/pdf", captured.HttpHeaders.ContentType);
        }

        [Fact]
        public async Task UploadFromByteArray_uploads_exactly_the_requested_slice()
        {
            byte[] uploaded = null;
            var blob = UploadCapturingBlob(onStreamBytes: b => uploaded = b);

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("x.bin");
            var buffer = new byte[] { 0, 10, 20, 30, 40, 50 };
            await block.UploadFromByteArrayAsync(buffer, index: 1, count: 3);

            Assert.Equal(new byte[] { 10, 20, 30 }, uploaded);
        }

        [Fact]
        public async Task UploadText_writes_the_utf8_bytes_with_the_content_type()
        {
            BlobUploadOptions captured = null;
            byte[] uploaded = null;
            var blob = UploadCapturingBlob(onOptions: o => captured = o, onStreamBytes: b => uploaded = b);

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("note.txt");
            block.Properties.ContentType = "text/plain";
            await block.UploadTextAsync("hello");

            Assert.Equal(Encoding.UTF8.GetBytes("hello"), uploaded);
            Assert.Equal("text/plain", captured?.HttpHeaders?.ContentType);
        }

        [Fact]
        public async Task BlockBlob_ExistsAsync_unwraps_the_response_value()
        {
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Ok(false));

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("x.bin");

            Assert.False(await block.ExistsAsync());
        }

        [Fact]
        public async Task DeleteAsync_deletes_only_if_the_blob_exists()
        {
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.Setup(b => b.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Ok(true));

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("x.bin");
            await block.DeleteAsync();

            blob.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void BlockBlob_Uri_delegates_to_the_underlying_client()
        {
            var uri = new Uri("https://acct.blob.core.windows.net/resumes/resume.pdf");
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.SetupGet(b => b.Uri).Returns(uri);

            var block = ContainerWrapping(blob.Object).GetBlockBlobReference("resume.pdf");

            Assert.Equal(uri, block.Uri);
        }

        // ---- read-oriented blob adapter -------------------------------------

        [Fact]
        public async Task Blob_OpenReadAsync_returns_the_underlying_stream()
        {
            var payload = new MemoryStream(new byte[] { 7, 8, 9 });
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.Setup(b => b.OpenReadAsync(
                    It.IsAny<long>(), It.IsAny<int?>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(payload);

            var rbBlob = ContainerWrapping(blob.Object).GetBlobReference("x.bin");
            var stream = await rbBlob.OpenReadAsync();

            Assert.Same(payload, stream);
        }

        [Fact]
        public async Task Blob_DownloadToStreamAsync_forwards_the_destination_to_the_client()
        {
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.Setup(b => b.DownloadToAsync(It.IsAny<Stream>()))
                .Callback<Stream>(dest => dest.Write(new byte[] { 1, 2 }, 0, 2))
                .ReturnsAsync(Mock.Of<Response>());

            var rbBlob = ContainerWrapping(blob.Object).GetBlobReference("x.bin");
            using var target = new MemoryStream();
            await rbBlob.DownloadToStreamAsync(target);

            Assert.Equal(new byte[] { 1, 2 }, target.ToArray());
        }

        [Fact]
        public async Task Blob_ExistsAsync_unwraps_the_response_value()
        {
            var blob = new Mock<BlobClient>(MockBehavior.Loose);
            blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Ok(true));

            var rbBlob = ContainerWrapping(blob.Object).GetBlobReference("x.bin");

            Assert.True(await rbBlob.ExistsAsync());
        }
    }
}
