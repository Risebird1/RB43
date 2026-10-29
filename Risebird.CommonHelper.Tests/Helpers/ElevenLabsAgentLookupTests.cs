using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Risebird.CommonHelper.Tests.Infrastructure;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// <c>ElevenLabsAgentService.FindCampaignAgentIdByNameAsync</c> decides whether campaign agent
    /// setup updates an existing ElevenLabs agent or creates a new one - a wrong "not found" makes
    /// duplicates. It must match the exact name only, prefer the newest duplicate, call the campaign
    /// account with its own key, and treat any failure as "not found" without throwing.
    /// </summary>
    public sealed class ElevenLabsAgentLookupTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public HttpRequestMessage Request { get; private set; }

            public StubHandler(HttpStatusCode status, string body) { _status = status; _body = body; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Request = request;
                return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
            }
        }

        private static (ElevenLabsAgentService Sut, StubHandler Handler) Sut(string body, HttpStatusCode status = HttpStatusCode.OK,
            string campaignKey = "sk_campaign_residency_in", string mainKey = "sk_main")
        {
            var handler = new StubHandler(status, body);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
            var config = TestConfiguration.From(("ElevenLabs:ApiKey", mainKey), ("ElevenLabs:Campaigns:ApiKey", campaignKey));
            return (new ElevenLabsAgentService(config, factory.Object), handler);
        }

        private const string Agents = @"{ ""agents"": [
            { ""agent_id"": ""agent_other"",  ""name"": ""Risebird Campaign - Schedule v2"",  ""created_at_unix_secs"": 300 },
            { ""agent_id"": ""agent_old"",    ""name"": ""Risebird Campaign - Schedule"",     ""created_at_unix_secs"": 100 },
            { ""agent_id"": ""agent_newest"", ""name"": ""risebird campaign - schedule"",     ""created_at_unix_secs"": 200 }
        ] }";

        [Fact]
        public async Task The_newest_agent_with_exactly_that_name_is_found()
        {
            var (sut, _) = Sut(Agents);
            Assert.Equal("agent_newest", await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Schedule"));
        }

        [Fact]
        public async Task A_name_that_only_appears_inside_another_name_is_not_a_match()
        {
            var (sut, _) = Sut(Agents);
            Assert.Null(await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Reschedule"));
        }

        [Fact]
        public async Task It_asks_the_campaign_account_with_the_campaign_key_and_searches_by_name()
        {
            var (sut, handler) = Sut(Agents);
            await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Schedule");

            // A *_residency_in key selects the India API on its own.
            Assert.StartsWith("https://api.in.residency.elevenlabs.io/v1/convai/agents?", handler.Request.RequestUri.ToString());
            Assert.Contains("search=Risebird%20Campaign%20-%20Schedule", handler.Request.RequestUri.AbsoluteUri);
            Assert.Equal("sk_campaign_residency_in", string.Join("", handler.Request.Headers.GetValues("xi-api-key")));
        }

        [Fact]
        public async Task Without_a_campaign_key_the_main_account_is_used()
        {
            var (sut, handler) = Sut(Agents, campaignKey: "");
            await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Schedule");

            Assert.StartsWith("https://api.elevenlabs.io/", handler.Request.RequestUri.ToString());
            Assert.Equal("sk_main", string.Join("", handler.Request.Headers.GetValues("xi-api-key")));
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized, "{\"detail\":\"invalid_api_key\"}")]
        [InlineData(HttpStatusCode.OK, "not json")]
        [InlineData(HttpStatusCode.OK, "{ \"agents\": [] }")]
        [InlineData(HttpStatusCode.OK, "{ }")]
        public async Task Any_failure_reads_as_not_found(HttpStatusCode status, string body)
        {
            var (sut, _) = Sut(body, status);
            Assert.Null(await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Schedule"));
        }

        [Fact]
        public async Task No_key_or_no_name_does_not_call_out()
        {
            var (sut, handler) = Sut(Agents, campaignKey: "", mainKey: "");
            Assert.Null(await sut.FindCampaignAgentIdByNameAsync("Risebird Campaign - Schedule"));
            Assert.Null(handler.Request);

            var (sut2, handler2) = Sut(Agents);
            Assert.Null(await sut2.FindCampaignAgentIdByNameAsync("  "));
            Assert.Null(handler2.Request);
        }
    }
}
