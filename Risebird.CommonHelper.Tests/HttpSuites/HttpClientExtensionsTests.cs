using System.Net;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.HttpSuites
{
    /// <summary>
    /// <c>HttpClientExtensions.SendRequestAsync</c> is the single call every micro-service request
    /// in the portal goes through: it serialises the body, stamps the function key and the acting
    /// account, and sends. A missing header here is a 401 from the Azure Function, and a wrongly
    /// serialised body is a 400 — both of which surface far from this method, so it is worth
    /// pinning precisely.
    /// </summary>
    public sealed class HttpClientExtensionsTests
    {
        private static StubHttpMessageHandler Handler() => new();

        [Fact]
        public async Task The_request_goes_to_the_path_under_the_client_base_address()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1 }, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("https://functions.risebird.test/api/notify", handler.TheRequest.Uri.ToString());
        }

        [Fact]
        public async Task An_absolute_path_replaces_the_base_address_path()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/v1/");

            await client.SendRequestAsync(HttpMethod.Get, "/other", null, "key", "a@b.test", CancellationToken.None);

            Assert.Equal("https://functions.risebird.test/other", handler.TheRequest.Uri.ToString());
        }

        [Fact]
        public async Task A_fully_qualified_path_is_used_as_is()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "https://elsewhere.test/x", null, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("https://elsewhere.test/x", handler.TheRequest.Uri.ToString());
        }

        [Fact]
        public async Task A_query_string_on_the_path_survives()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify?id=7&mode=full", null, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("?id=7&mode=full", handler.TheRequest.Uri.Query);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public async Task Every_verb_is_passed_through_unchanged(string verb)
        {
            var method = new HttpMethod(verb);
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(method, "api/notify", null, "key", "a@b.test", CancellationToken.None);

            Assert.Equal(method, handler.TheRequest.Method);
        }

        [Fact]
        public async Task A_body_is_serialised_as_compact_JSON()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify",
                new { interviewId = 42, note = "hello" }, "key", "a@b.test", CancellationToken.None);

            var body = handler.TheRequest.Body;
            Assert.Equal("{\"interviewId\":42,\"note\":\"hello\"}", body);
            Assert.DoesNotContain("\n", body);
        }

        [Fact]
        public async Task A_body_is_sent_as_UTF8_JSON_content()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1 }, "key", "a@b.test",
                CancellationToken.None);

            var contentType = handler.TheRequest.ContentType;
            Assert.Contains("application/json", contentType);
            Assert.Contains("utf-8", contentType.ToLowerInvariant());
        }

        [Fact]
        public async Task Non_ASCII_body_text_survives_the_round_trip()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify",
                new { name = "Ramanujan \u0930\u093E\u092E" }, "key", "a@b.test", CancellationToken.None);

            Assert.Contains("\u0930\u093E\u092E", handler.TheRequest.Body);
        }

        [Fact]
        public async Task A_null_body_sends_no_content_at_all()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", "a@b.test",
                CancellationToken.None);

            Assert.Null(handler.TheRequest.Body);
            Assert.Null(handler.TheRequest.ContentType);
        }

        [Fact]
        public async Task A_string_body_is_serialised_as_a_JSON_string_not_as_raw_text()
        {
            // Easy to get wrong at a call site: passing pre-serialised JSON as the body double
            // encodes it. Pinned so the behaviour is discoverable rather than surprising.
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", "{\"already\":\"json\"}", "key",
                "a@b.test", CancellationToken.None);

            Assert.Equal("\"{\\\"already\\\":\\\"json\\\"}\"", handler.TheRequest.Body);
        }

        [Fact]
        public async Task A_collection_body_is_serialised_as_a_JSON_array()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", new[] { 1, 2, 3 }, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("[1,2,3]", handler.TheRequest.Body);
        }

        [Fact]
        public async Task Null_valued_properties_are_kept_rather_than_dropped()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1, note = (string)null },
                "key", "a@b.test", CancellationToken.None);

            var parsed = JObject.Parse(handler.TheRequest.Body);
            Assert.True(parsed.ContainsKey("note"));
            Assert.Equal(JTokenType.Null, parsed["note"].Type);
        }

        [Fact]
        public async Task The_function_key_and_the_account_email_are_both_sent_as_headers()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1 },
                "the-function-key", "dm@risebird.io", CancellationToken.None);

            Assert.Equal("the-function-key", handler.TheRequest.Header("x-functions-key"));
            Assert.Equal("dm@risebird.io", handler.TheRequest.Header("accountEmail"));
        }

        [Fact]
        public async Task The_accept_header_asks_for_JSON()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", "a@b.test",
                CancellationToken.None);

            Assert.Contains("application/json", handler.TheRequest.Header("Accept"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_blank_function_key_is_still_sent_rather_than_omitted(string functionKey)
        {
            // The Function would reject the call anyway, but sending an empty header makes the
            // cause visible in the request log instead of looking like a missing header.
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify", null, functionKey, "a@b.test",
                CancellationToken.None);

            Assert.True(handler.TheRequest.HasHeader("x-functions-key"));
            Assert.Equal(string.Empty, handler.TheRequest.Header("x-functions-key"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_blank_account_email_is_still_sent(string accountEmail)
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", accountEmail,
                CancellationToken.None);

            Assert.True(handler.TheRequest.HasHeader("accountEmail"));
        }

        [Fact]
        public async Task A_header_value_containing_a_newline_is_rejected_rather_than_smuggled_through()
        {
            // Header injection guard: HttpClient itself refuses this, and the test records that
            // the extension does not sanitise or silently drop it.
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await Assert.ThrowsAnyAsync<FormatException>(() =>
                client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key\r\nX-Evil: 1", "a@b.test",
                    CancellationToken.None));

            Assert.Equal(0, handler.CallCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task The_response_is_returned_as_is_without_throwing_on_a_failure_status(HttpStatusCode status)
        {
            // This extension deliberately does not call EnsureSuccessStatusCode: the caller
            // decides what a 4xx means for its own endpoint.
            var handler = Handler();
            handler.RespondWith(status, "{\"ok\":false}");
            using var client = handler.CreateClient("https://functions.risebird.test/");

            var response = await client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1 }, "key",
                "a@b.test", CancellationToken.None);

            Assert.Equal(status, response.StatusCode);
        }

        [Fact]
        public async Task The_response_body_is_readable_by_the_caller()
        {
            var handler = Handler();
            handler.RespondWith(HttpStatusCode.OK, "{\"reply\":\"done\"}");
            using var client = handler.CreateClient("https://functions.risebird.test/");

            var response = await client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("{\"reply\":\"done\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_cancelled_token_stops_the_call()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.SendRequestAsync(HttpMethod.Post, "api/notify", new { id = 1 }, "key", "a@b.test",
                    cts.Token));
        }

        [Fact]
        public async Task A_transport_failure_surfaces_to_the_caller()
        {
            var handler = Handler();
            handler.RespondWithTransportFailure();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", "a@b.test",
                    CancellationToken.None));
        }

        [Fact]
        public async Task A_relative_path_with_no_base_address_is_rejected()
        {
            var handler = Handler();
            using var client = handler.CreateClient();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SendRequestAsync(HttpMethod.Get, "api/notify", null, "key", "a@b.test",
                    CancellationToken.None));
        }

        [Fact]
        public async Task Each_call_builds_a_fresh_request_so_two_calls_do_not_share_state()
        {
            // A single HttpRequestMessage cannot be sent twice; the extension building a new one
            // per call is what makes the client reusable across calls.
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Post, "api/one", new { n = 1 }, "k1", "a@b.test",
                CancellationToken.None);
            await client.SendRequestAsync(HttpMethod.Post, "api/two", new { n = 2 }, "k2", "c@d.test",
                CancellationToken.None);

            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("/api/one", handler.Requests[0].Uri.AbsolutePath);
            Assert.EndsWith("/api/two", handler.Requests[1].Uri.AbsolutePath);
            Assert.Equal("k1", handler.Requests[0].Header("x-functions-key"));
            Assert.Equal("k2", handler.Requests[1].Header("x-functions-key"));
        }

        [Fact]
        public async Task A_GET_with_a_body_still_sends_the_body_because_nothing_forbids_it()
        {
            var handler = Handler();
            using var client = handler.CreateClient("https://functions.risebird.test/");

            await client.SendRequestAsync(HttpMethod.Get, "api/notify", new { id = 1 }, "key", "a@b.test",
                CancellationToken.None);

            Assert.Equal("{\"id\":1}", handler.TheRequest.Body);
        }
    }
}
