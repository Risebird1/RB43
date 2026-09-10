using System.Net.Http;

namespace Risebird.CommonHelper.Tests.Infrastructure
{
    /// <summary>
    /// An <see cref="IHttpClientFactory"/> over a <see cref="StubHttpMessageHandler"/>, for the
    /// helpers that take the factory rather than a client — <c>PlivoService</c> today, and every
    /// helper that gets the same treatment later.
    ///
    /// Each <c>CreateClient</c> call returns a new <see cref="HttpClient"/> sharing the one
    /// handler, matching what the real factory does: the client is cheap and per-use, the handler
    /// is pooled. That shared handler is what makes the requests visible to the test whichever
    /// client the service happened to create, and it also means a service that sets
    /// <c>DefaultRequestHeaders</c> on its client (as PlivoService does for Basic auth) is
    /// observed through the requests it sends, not through the client object.
    /// </summary>
    public sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string _baseAddress;

        public StubHttpClientFactory(StubHttpMessageHandler handler = null, string baseAddress = null)
        {
            Handler = handler ?? new StubHttpMessageHandler();
            _baseAddress = baseAddress;
        }

        public StubHttpMessageHandler Handler { get; }

        /// <summary>Names the factory was asked for, in order — a client name typo is otherwise invisible.</summary>
        public List<string> RequestedNames { get; } = new();

        public HttpClient CreateClient(string name)
        {
            RequestedNames.Add(name);
            return Handler.CreateClient(_baseAddress);
        }
    }
}
