using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;
using Risebird.Entities.Data;
using System.Net;

namespace Risebird.CommonHelper.Tests.HttpSuites
{
    /// <summary>
    /// <c>OpenAI:CostOptimization</c> in <c>OpenAIResponsesHelper</c>. The property that matters most is
    /// the first one: with nothing configured, every request is byte-for-byte what it was before.
    /// After that, an optimisation that goes wrong must cost one retry with the old settings, never a
    /// missing answer. The response cache is process-wide, so these tests run in one class (xUnit
    /// runs a class's tests sequentially) and clear it before each test.
    /// </summary>
    public sealed class OpenAIResponsesCostOptimizationTests
    {
        private const string DefaultModel = "gpt-4.1";
        private const string Operation = "TranscriptToNotes";

        private readonly StubHttpMessageHandler _http = new();
        private readonly Mock<IOpenAIApiLogger> _apiLogger = new(MockBehavior.Loose);

        public OpenAIResponsesCostOptimizationTests()
        {
            OpenAIResponseCache.Clear();
            OpenAIResponseCache.UseSharedStore(null);
        }

        private OpenAIResponsesHelper Sut(Dictionary<string, string> costOptimization = null)
        {
            var settings = new Dictionary<string, string>
            {
                ["OpenAI:ApiKey"] = "test-key",
                ["OpenAI:BaseUrl"] = "https://api.openai.test/v1",
                ["OpenAI:ResponsesModel"] = DefaultModel
            };
            foreach (var pair in costOptimization ?? new Dictionary<string, string>())
                settings[$"OpenAI:CostOptimization:{pair.Key}"] = pair.Value;

            return new OpenAIResponsesHelper(
                new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
                new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object,
                _apiLogger.Object,
                new StubHttpClientFactory(_http));
        }

        private static string Success(string outputText, string model = DefaultModel, string id = "resp_1") =>
            new JObject
            {
                ["id"] = id,
                ["model"] = model,
                ["status"] = "completed",
                ["output_text"] = outputText,
                ["usage"] = new JObject { ["input_tokens"] = 100, ["output_tokens"] = 20 }
            }.ToString();

        private static JObject Body(RecordedHttpRequest request) => JObject.Parse(request.Body);

        private static string OutputOf(Risebird.Entities.Data.OpenAIModels.RunResult result) =>
            result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

        [Fact]
        public async Task With_nothing_configured_the_request_is_unchanged()
        {
            _http.RespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut().CreateJsonResponseAsync("input", Operation, "pmpt_1");

            var body = Body(_http.TheRequest);
            Assert.Equal(DefaultModel, (string)body["model"]);
            Assert.Null(body["prompt_cache_key"]);
        }

        [Fact]
        public async Task A_configured_override_sends_the_cheaper_model_for_that_operation_only()
        {
            var config = new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini"
            };
            _http.RespondWith(HttpStatusCode.OK, Success("{\"a\":1}", "gpt-4.1-mini"))
                 .RespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(config).CreateJsonResponseAsync("input", Operation, "pmpt_1");
            await Sut(config).CreateJsonResponseAsync("input", "ReportQC", "pmpt_1");

            Assert.Equal("gpt-4.1-mini", (string)Body(_http.Requests[0])["model"]);
            Assert.Equal(DefaultModel, (string)Body(_http.Requests[1])["model"]);
        }

        [Fact]
        public async Task The_master_switch_off_ignores_every_override()
        {
            _http.RespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "false",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini",
                ["UsePromptCacheKey"] = "true"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1");

            var body = Body(_http.TheRequest);
            Assert.Equal(DefaultModel, (string)body["model"]);
            Assert.Null(body["prompt_cache_key"]);
        }

        [Fact]
        public async Task A_call_pinned_to_its_stored_prompt_model_is_never_overridden()
        {
            _http.RespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1", useStoredPromptModel: true);

            Assert.Null(Body(_http.TheRequest)["model"]);
        }

        [Fact]
        public async Task A_failed_override_is_repeated_on_the_default_model()
        {
            _http.RespondWith(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"unsupported\"}}")
                 .RespondWith(HttpStatusCode.OK, Success("{\"ok\":true}"));

            var result = await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1");

            Assert.Equal(2, _http.Requests.Count);
            Assert.Equal("gpt-4.1-mini", (string)Body(_http.Requests[0])["model"]);
            Assert.Equal(DefaultModel, (string)Body(_http.Requests[1])["model"]);
            Assert.Equal("{\"ok\":true}", OutputOf(result));
        }

        [Fact]
        public async Task An_override_that_returns_unparseable_json_is_repeated_on_the_default_model()
        {
            _http.RespondWith(HttpStatusCode.OK, Success("this is not json", "gpt-4.1-mini"))
                 .RespondWith(HttpStatusCode.OK, Success("{\"ok\":true}"));

            var result = await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1");

            Assert.Equal(2, _http.Requests.Count);
            Assert.Equal("{\"ok\":true}", OutputOf(result));
        }

        [Fact]
        public async Task The_fallback_retry_drops_the_prompt_cache_key_too()
        {
            _http.RespondWith(HttpStatusCode.BadRequest, "{}")
                 .RespondWith(HttpStatusCode.OK, Success("{\"ok\":true}"));

            await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                ["UsePromptCacheKey"] = "true"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1");

            Assert.NotNull(Body(_http.Requests[0])["prompt_cache_key"]);
            Assert.Null(Body(_http.Requests[1])["prompt_cache_key"]);
        }

        [Fact]
        public async Task With_fallback_disabled_a_failed_override_is_not_retried()
        {
            _http.RespondWith(HttpStatusCode.BadRequest, "{}");

            var result = await Sut(new Dictionary<string, string>
            {
                ["Enabled"] = "true",
                ["FallbackToDefaultModelOnFailure"] = "false",
                [$"ModelOverrides:{Operation}"] = "gpt-4.1-mini"
            }).CreateJsonResponseAsync("input", Operation, "pmpt_1");

            Assert.Single(_http.Requests);
            Assert.Null(result);
        }

        [Fact]
        public async Task A_baseline_failure_is_not_retried()
        {
            _http.RespondWith(HttpStatusCode.InternalServerError, "{}");

            var result = await Sut().CreateJsonResponseAsync("input", Operation, "pmpt_1");

            Assert.Single(_http.Requests);
            Assert.Null(result);
        }

        [Fact]
        public async Task The_prompt_cache_key_is_stable_per_operation_and_prompt()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));
            var config = new Dictionary<string, string> { ["Enabled"] = "true", ["UsePromptCacheKey"] = "true" };

            await Sut(config).CreateJsonResponseAsync("first", Operation, "pmpt_1");
            await Sut(config).CreateJsonResponseAsync("second", Operation, "pmpt_1");
            await Sut(config).CreateJsonResponseAsync("third", Operation, "pmpt_2");

            var keys = _http.Requests.Select(r => (string)Body(r)["prompt_cache_key"]).ToList();
            Assert.Equal(keys[0], keys[1]);
            Assert.NotEqual(keys[0], keys[2]);
            Assert.True(keys[0].Length <= 64);
        }

        private static Dictionary<string, string> Cached(string operation, int ttlMinutes = 60) => new()
        {
            ["Enabled"] = "true",
            ["ResponseCache:Enabled"] = "true",
            [$"ResponseCache:Operations:{operation}"] = ttlMinutes.ToString()
        };

        [Fact]
        public async Task An_identical_request_for_a_cached_operation_is_answered_without_calling_openai()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            var first = await Sut(Cached("CacheOpA")).CreateJsonResponseAsync("same input", "CacheOpA", "pmpt_1");
            var second = await Sut(Cached("CacheOpA")).CreateJsonResponseAsync("same input", "CacheOpA", "pmpt_1");

            Assert.Single(_http.Requests);
            Assert.Equal(OutputOf(first), OutputOf(second));
            _apiLogger.Verify(l => l.LogApiCallAsync(
                It.IsAny<string>(), OpenAIResponsesHelper.CacheHitApiMethod, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OpenAIApiLogContext>()), Times.Once);
        }

        [Fact]
        public async Task A_different_input_is_a_cache_miss()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(Cached("CacheOpB")).CreateJsonResponseAsync("input one", "CacheOpB", "pmpt_1");
            await Sut(Cached("CacheOpB")).CreateJsonResponseAsync("input two", "CacheOpB", "pmpt_1");

            Assert.Equal(2, _http.Requests.Count);
        }

        [Fact]
        public async Task An_operation_not_listed_for_caching_always_calls_openai()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(Cached("CacheOpC")).CreateJsonResponseAsync("same", "SomethingElse", "pmpt_1");
            await Sut(Cached("CacheOpC")).CreateJsonResponseAsync("same", "SomethingElse", "pmpt_1");

            Assert.Equal(2, _http.Requests.Count);
        }

        [Fact]
        public async Task A_chained_request_is_never_served_from_the_cache()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(Cached("CacheOpD")).CreateJsonResponseWithIdAsync("same", "CacheOpD", "pmpt_1", previousResponseId: "resp_0");
            await Sut(Cached("CacheOpD")).CreateJsonResponseWithIdAsync("same", "CacheOpD", "pmpt_1", previousResponseId: "resp_0");

            Assert.Equal(2, _http.Requests.Count);
        }

        [Fact]
        public async Task A_failed_answer_is_not_cached()
        {
            _http.RespondWith(HttpStatusCode.InternalServerError, "{}")
                 .RespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            var first = await Sut(Cached("CacheOpE")).CreateJsonResponseAsync("same", "CacheOpE", "pmpt_1");
            var second = await Sut(Cached("CacheOpE")).CreateJsonResponseAsync("same", "CacheOpE", "pmpt_1");

            Assert.Null(first);
            Assert.NotNull(second);
            Assert.Equal(2, _http.Requests.Count);
        }

        [Fact]
        public async Task Text_responses_are_cached_too()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("plain notes"));

            await Sut(Cached("CacheOpF")).CreateTextResponseAsync("same", "CacheOpF", "pmpt_1");
            var second = await Sut(Cached("CacheOpF")).CreateTextResponseAsync("same", "CacheOpF", "pmpt_1");

            Assert.Single(_http.Requests);
            Assert.Equal("plain notes", OutputOf(second));
        }

        [Fact]
        public async Task Inline_instruction_requests_honour_the_override_and_the_cache()
        {
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}", "gpt-4.1-mini"));
            var config = Cached("CacheOpG");
            config["ModelOverrides:CacheOpG"] = "gpt-4.1-mini";

            var first = await Sut(config).CreateJsonWithInstructionsAsync("be brief", "same", "CacheOpG");
            var second = await Sut(config).CreateJsonWithInstructionsAsync("be brief", "same", "CacheOpG");

            Assert.Single(_http.Requests);
            Assert.Equal("gpt-4.1-mini", (string)Body(_http.TheRequest)["model"]);
            Assert.Equal(first, second);
        }

        // ---- Redis (shared store) second level ----
        // Redis may only hold answers to requests that were actually repeated, and must let go of an
        // answer that has not been read for an hour. Clearing the in-process cache between calls
        // stands in for the next request landing on a different web instance.

        private sealed class FakeSharedStore : IOpenAIResponseSharedStore
        {
            public sealed record Write(string Key, TimeSpan AbsoluteExpiry, TimeSpan IdleExpiry);

            public readonly Dictionary<string, string> Entries = new();
            public readonly List<Write> Writes = new();
            public Exception Fail;
            public TimeSpan Delay;

            public IEnumerable<string> AnswerKeys => Entries.Keys.Where(k => !k.EndsWith(":seen"));
            public IEnumerable<string> MarkerKeys => Entries.Keys.Where(k => k.EndsWith(":seen"));

            public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
            {
                if (Delay > TimeSpan.Zero) await Task.Delay(Delay);
                if (Fail != null) throw Fail;
                return Entries.TryGetValue(key, out var value) ? value : null;
            }

            public async Task SetAsync(string key, string value, TimeSpan absoluteExpiry, TimeSpan idleExpiry,
                CancellationToken cancellationToken)
            {
                if (Delay > TimeSpan.Zero) await Task.Delay(Delay);
                if (Fail != null) throw Fail;
                Entries[key] = value;
                Writes.Add(new Write(key, absoluteExpiry, idleExpiry));
            }
        }

        private static Dictionary<string, string> CachedInRedis(string operation, int ttlMinutes = 1440)
        {
            var config = Cached(operation, ttlMinutes);
            config["ResponseCache:UseRedis"] = "true";
            return config;
        }

        private void VerifyCacheHitLogged(string source, Func<Times> times)
        {
            _apiLogger.Verify(l => l.LogApiCallAsync(
                It.IsAny<string>(), OpenAIResponsesHelper.CacheHitApiMethod,
                It.Is<string>(payload => (string)JObject.Parse(payload)["source"] == source),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OpenAIApiLogContext>()), times);
        }

        [Fact]
        public async Task A_one_off_request_leaves_only_a_small_seen_marker_in_redis()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            var result = await Sut(CachedInRedis("RedisOpA")).CreateJsonResponseAsync("once", "RedisOpA", "pmpt_1");

            Assert.Equal("{\"a\":1}", OutputOf(result));
            Assert.Empty(redis.AnswerKeys);
            Assert.Equal("1", redis.Entries[Assert.Single(redis.MarkerKeys)]);
        }

        [Fact]
        public async Task A_repeat_on_another_instance_puts_the_answer_in_redis_for_everyone_after()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(CachedInRedis("RedisOpB")).CreateJsonResponseAsync("same", "RedisOpB", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(CachedInRedis("RedisOpB")).CreateJsonResponseAsync("same", "RedisOpB", "pmpt_1");
            Assert.Single(redis.AnswerKeys);

            OpenAIResponseCache.Clear();
            var third = await Sut(CachedInRedis("RedisOpB")).CreateJsonResponseAsync("same", "RedisOpB", "pmpt_1");

            Assert.Equal(2, _http.Requests.Count); // the first two instances had nothing to reuse yet
            Assert.Equal("{\"a\":1}", OutputOf(third));
            VerifyCacheHitLogged("redis", Times.Once);
        }

        [Fact]
        public async Task A_repeat_on_the_same_instance_copies_the_answer_to_redis_once()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(CachedInRedis("RedisOpC")).CreateJsonResponseAsync("same", "RedisOpC", "pmpt_1");
            await Sut(CachedInRedis("RedisOpC")).CreateJsonResponseAsync("same", "RedisOpC", "pmpt_1");
            await Sut(CachedInRedis("RedisOpC")).CreateJsonResponseAsync("same", "RedisOpC", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(CachedInRedis("RedisOpC")).CreateJsonResponseAsync("same", "RedisOpC", "pmpt_1");

            Assert.Single(_http.Requests);
            Assert.Single(redis.Writes, w => !w.Key.EndsWith(":seen"));
            VerifyCacheHitLogged("memory", () => Times.Exactly(2));
            VerifyCacheHitLogged("redis", Times.Once);
        }

        [Fact]
        public async Task Redis_entries_are_dropped_after_an_hour_without_use()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(CachedInRedis("RedisOpD", ttlMinutes: 1440)).CreateJsonResponseAsync("same", "RedisOpD", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(CachedInRedis("RedisOpD", ttlMinutes: 1440)).CreateJsonResponseAsync("same", "RedisOpD", "pmpt_1");

            var marker = Assert.Single(redis.Writes, w => w.Key.EndsWith(":seen"));
            Assert.Equal(TimeSpan.FromHours(1), marker.IdleExpiry);
            Assert.Equal(TimeSpan.FromHours(1), marker.AbsoluteExpiry);
            var answer = Assert.Single(redis.Writes, w => !w.Key.EndsWith(":seen"));
            Assert.Equal(TimeSpan.FromHours(1), answer.IdleExpiry);
            Assert.InRange(answer.AbsoluteExpiry, TimeSpan.FromMinutes(1439), TimeSpan.FromMinutes(1440));
        }

        [Fact]
        public async Task The_idle_window_is_configurable_and_never_outlives_the_operation_ttl()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));
            var shortTtl = CachedInRedis("RedisOpE", ttlMinutes: 20);
            shortTtl["ResponseCache:RedisIdleMinutes"] = "45";

            await Sut(shortTtl).CreateJsonResponseAsync("same", "RedisOpE", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(shortTtl).CreateJsonResponseAsync("same", "RedisOpE", "pmpt_1");

            Assert.Equal(TimeSpan.FromMinutes(45), Assert.Single(redis.Writes, w => w.Key.EndsWith(":seen")).IdleExpiry);
            var answer = Assert.Single(redis.Writes, w => !w.Key.EndsWith(":seen"));
            Assert.InRange(answer.AbsoluteExpiry, TimeSpan.FromMinutes(19), TimeSpan.FromMinutes(20));
            Assert.Equal(answer.AbsoluteExpiry, answer.IdleExpiry);
        }

        [Fact]
        public async Task With_UseRedis_off_nothing_goes_to_redis()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            await Sut(Cached("RedisOpF")).CreateJsonResponseAsync("same", "RedisOpF", "pmpt_1");
            await Sut(Cached("RedisOpF")).CreateJsonResponseAsync("same", "RedisOpF", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(Cached("RedisOpF")).CreateJsonResponseAsync("same", "RedisOpF", "pmpt_1");

            Assert.Empty(redis.Entries);
            Assert.Equal(2, _http.Requests.Count);
        }

        [Fact]
        public async Task With_caching_off_redis_is_never_touched_even_if_UseRedis_is_on()
        {
            var redis = new FakeSharedStore { Fail = new InvalidOperationException("must not be called") };
            OpenAIResponseCache.UseSharedStore(redis);
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));
            var config = CachedInRedis("RedisOpG");
            config["ResponseCache:Enabled"] = "false";

            await Sut(config).CreateJsonResponseAsync("same", "RedisOpG", "pmpt_1");
            await Sut(config).CreateJsonResponseAsync("same", "RedisOpG", "pmpt_1");

            Assert.Equal(2, _http.Requests.Count);
            Assert.Empty(redis.Writes);
        }

        [Fact]
        public async Task A_failing_redis_never_costs_an_answer()
        {
            OpenAIResponseCache.UseSharedStore(new FakeSharedStore { Fail = new TimeoutException("redis down") });
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));

            var first = await Sut(CachedInRedis("RedisOpH")).CreateJsonResponseAsync("same", "RedisOpH", "pmpt_1");
            var second = await Sut(CachedInRedis("RedisOpH")).CreateJsonResponseAsync("same", "RedisOpH", "pmpt_1");
            OpenAIResponseCache.Clear();
            var third = await Sut(CachedInRedis("RedisOpH")).CreateJsonResponseAsync("same", "RedisOpH", "pmpt_1");

            Assert.Equal("{\"a\":1}", OutputOf(first));
            Assert.Equal("{\"a\":1}", OutputOf(second));
            Assert.Equal("{\"a\":1}", OutputOf(third));
            Assert.Equal(2, _http.Requests.Count); // the in-process level still works
        }

        [Fact]
        public async Task A_slow_redis_is_abandoned_after_the_timeout()
        {
            OpenAIResponseCache.UseSharedStore(new FakeSharedStore { Delay = TimeSpan.FromSeconds(10) });
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success("{\"a\":1}"));
            var config = CachedInRedis("RedisOpI");
            config["ResponseCache:RedisTimeoutMilliseconds"] = "50";

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = await Sut(config).CreateJsonResponseAsync("same", "RedisOpI", "pmpt_1");
            watch.Stop();

            Assert.Equal("{\"a\":1}", OutputOf(result));
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"took {watch.Elapsed}");
        }

        [Fact]
        public async Task An_answer_over_the_size_limit_stays_in_process()
        {
            var redis = new FakeSharedStore();
            OpenAIResponseCache.UseSharedStore(redis);
            var big = "{\"text\":\"" + new string('x', 4000) + "\"}";
            _http.AlwaysRespondWith(HttpStatusCode.OK, Success(big));
            var config = CachedInRedis("RedisOpJ");
            config["ResponseCache:RedisMaxEntryKilobytes"] = "1";

            await Sut(config).CreateJsonResponseAsync("same", "RedisOpJ", "pmpt_1");
            await Sut(config).CreateJsonResponseAsync("same", "RedisOpJ", "pmpt_1");
            OpenAIResponseCache.Clear();
            await Sut(config).CreateJsonResponseAsync("same", "RedisOpJ", "pmpt_1");

            Assert.Empty(redis.AnswerKeys);
            Assert.Equal(2, _http.Requests.Count);
        }
    }
}
