using System.Net;
using System.Text;

namespace Risebird.CommonHelper.Tests.Infrastructure
{
    /// <summary>
    /// A single request as the handler saw it, with the body already read. Reading the body at
    /// capture time matters: the production code disposes its <c>StringContent</c> when the
    /// request completes, so a test that tries to read it afterwards gets an
    /// <c>ObjectDisposedException</c> instead of the payload it wanted to assert on.
    /// </summary>
    public sealed class RecordedHttpRequest
    {
        public HttpMethod Method { get; init; }
        public Uri Uri { get; init; }
        public string Body { get; init; }
        public string ContentType { get; init; }
        public IReadOnlyDictionary<string, string[]> Headers { get; init; }
        public IReadOnlyDictionary<string, string[]> ContentHeaders { get; init; }

        public string Header(string name) =>
            Headers.TryGetValue(name, out var values) ? string.Join(",", values) : null;

        public bool HasHeader(string name) => Headers.ContainsKey(name);
    }

    /// <summary>
    /// An offline <see cref="HttpMessageHandler"/> for driving every helper that talks over
    /// <see cref="HttpClient"/>. Nothing here touches a socket, so the suites are deterministic
    /// and run in milliseconds.
    ///
    /// Two ways to script it:
    ///   <c>RespondWith(...)</c>  queues one response per call, in order — for a helper that
    ///                            makes several requests and must see different answers.
    ///   <c>AlwaysRespondWith</c> a standing answer for every request.
    ///
    /// With nothing scripted it answers <c>200 {}</c>, which keeps a service on its happy path
    /// rather than throwing on the first response check.
    /// </summary>
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _scripted = new();
        private readonly List<RecordedHttpRequest> _requests = new();
        private Func<HttpRequestMessage, HttpResponseMessage> _standing;

        /// <summary>Every request the handler was asked to send, in order.</summary>
        public IReadOnlyList<RecordedHttpRequest> Requests => _requests;

        public RecordedHttpRequest LastRequest =>
            _requests.Count == 0 ? null : _requests[_requests.Count - 1];

        /// <summary>The single request the handler saw, failing loudly if there was not exactly one.</summary>
        public RecordedHttpRequest TheRequest
        {
            get
            {
                if (_requests.Count != 1)
                    throw new InvalidOperationException(
                        $"Expected exactly one HTTP request, saw {_requests.Count}.");
                return _requests[0];
            }
        }

        /// <summary>Set when the handler was asked to send a request with an already-cancelled token.</summary>
        public bool SawCancellation { get; private set; }

        /// <summary>How many times the handler was invoked, including calls that threw.</summary>
        public int CallCount { get; private set; }

        public StubHttpMessageHandler RespondWith(HttpStatusCode status, string body = "{}",
            string contentType = "application/json")
        {
            _scripted.Enqueue(_ => Build(status, body, contentType));
            return this;
        }

        public StubHttpMessageHandler RespondWithJson(object payload, HttpStatusCode status = HttpStatusCode.OK)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            _scripted.Enqueue(_ => Build(status, json, "application/json"));
            return this;
        }

        /// <summary>Queues a response with no content at all, which is legal and easy to get wrong.</summary>
        public StubHttpMessageHandler RespondWithNoContent(HttpStatusCode status = HttpStatusCode.NoContent)
        {
            _scripted.Enqueue(_ => new HttpResponseMessage(status) { Content = null });
            return this;
        }

        /// <summary>Queues a transport-level failure, the shape of a DNS or TLS error.</summary>
        public StubHttpMessageHandler RespondWithTransportFailure(string message = "connection refused")
        {
            _scripted.Enqueue(_ => throw new HttpRequestException(message));
            return this;
        }

        public StubHttpMessageHandler AlwaysRespondWith(HttpStatusCode status, string body = "{}",
            string contentType = "application/json")
        {
            _standing = _ => Build(status, body, contentType);
            return this;
        }

        /// <summary>Full control, for the rare case that needs to inspect the request to answer it.</summary>
        public StubHttpMessageHandler AlwaysRespondWith(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _standing = responder;
            return this;
        }

        public HttpClient CreateClient(string baseAddress = null)
        {
            var client = new HttpClient(this, disposeHandler: false);
            if (!string.IsNullOrEmpty(baseAddress))
                client.BaseAddress = new Uri(baseAddress);
            return client;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (cancellationToken.IsCancellationRequested)
                SawCancellation = true;

            _requests.Add(await RecordAsync(request));

            cancellationToken.ThrowIfCancellationRequested();

            var responder = _scripted.Count > 0 ? _scripted.Dequeue() : _standing;
            var response = responder != null
                ? responder(request)
                : Build(HttpStatusCode.OK, "{}", "application/json");

            if (response != null)
                response.RequestMessage = request;
            return response;
        }

        private static async Task<RecordedHttpRequest> RecordAsync(HttpRequestMessage request)
        {
            string body = null;
            string contentType = null;
            var contentHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync();
                contentType = request.Content.Headers.ContentType?.ToString();
                foreach (var header in request.Content.Headers)
                    contentHeaders[header.Key] = header.Value.ToArray();
            }

            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
                headers[header.Key] = header.Value.ToArray();

            return new RecordedHttpRequest
            {
                Method = request.Method,
                Uri = request.RequestUri,
                Body = body,
                ContentType = contentType,
                Headers = headers,
                ContentHeaders = contentHeaders
            };
        }

        private static HttpResponseMessage Build(HttpStatusCode status, string body, string contentType) =>
            new(status)
            {
                Content = body == null
                    ? null
                    : new StringContent(body, Encoding.UTF8, contentType)
            };
    }
}
