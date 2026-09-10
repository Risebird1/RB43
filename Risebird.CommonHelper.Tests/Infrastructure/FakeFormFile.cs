using Microsoft.AspNetCore.Http;

namespace Risebird.CommonHelper.Tests.Infrastructure
{
    /// <summary>Minimal in-memory <see cref="IFormFile"/> for upload-validation tests.</summary>
    internal sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _content;

        public FakeFormFile(string fileName, byte[] content = null, string contentType = "application/octet-stream")
        {
            FileName = fileName;
            Name = "file";
            ContentType = contentType;
            _content = content ?? new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        }

        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => _content.LongLength;
        public string Name { get; }
        public string FileName { get; }

        public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            target.WriteAsync(_content, 0, _content.Length, cancellationToken);

        public Stream OpenReadStream() => new MemoryStream(_content, writable: false);

        /// <summary>A file that reports zero length (rejected before any sniffing).</summary>
        public static IFormFile Empty(string fileName) => new FakeFormFile(fileName, Array.Empty<byte>());
    }
}
