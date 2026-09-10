using System.Net;
using System.Net.Http;

namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Answers every request locally with a 200 and an empty JSON object, so services that
    /// hold an <see cref="HttpClient"/> can be exercised without touching the network.
    /// Records the requests it saw so tests can assert on them when they want to.
    /// </summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public StubHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string body = "{}")
        {
            _statusCode = statusCode;
            _body = body;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        }
    }
}
