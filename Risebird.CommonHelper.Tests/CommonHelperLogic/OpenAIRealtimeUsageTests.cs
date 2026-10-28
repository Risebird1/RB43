using Moq;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// Plivo AI phone interviews run on the OpenAI Realtime API, which reports usage per AI turn and
    /// bills audio and text tokens at different rates. These calls were never logged, so their cost
    /// was invisible to the AI cost reports and the Interview Profitability tab.
    /// </summary>
    public sealed class OpenAIRealtimeUsageTests
    {
        // One response.done usage block, in the Realtime API's own shape.
        private static JObject Turn(int textIn, int audioIn, int cachedText, int cachedAudio, int textOut, int audioOut) =>
            JObject.Parse($@"{{
                ""total_tokens"": {textIn + audioIn + textOut + audioOut},
                ""input_tokens"": {textIn + audioIn},
                ""output_tokens"": {textOut + audioOut},
                ""input_token_details"": {{
                    ""text_tokens"": {textIn}, ""audio_tokens"": {audioIn}, ""cached_tokens"": {cachedText + cachedAudio},
                    ""cached_tokens_details"": {{ ""text_tokens"": {cachedText}, ""audio_tokens"": {cachedAudio} }}
                }},
                ""output_token_details"": {{ ""text_tokens"": {textOut}, ""audio_tokens"": {audioOut} }}
            }}");

        [Fact]
        public void Every_turn_of_the_call_is_added_up()
        {
            var usage = new OpenAIRealtimeUsage();
            usage.Add(Turn(1000, 2000, 0, 0, 100, 500));
            usage.Add(Turn(1200, 2500, 800, 1500, 120, 600));

            Assert.Equal(2, usage.Responses);
            Assert.Equal(2200, usage.TextInputTokens);
            Assert.Equal(4500, usage.AudioInputTokens);
            Assert.Equal(800, usage.CachedTextInputTokens);
            Assert.Equal(1500, usage.CachedAudioInputTokens);
            Assert.Equal(220, usage.TextOutputTokens);
            Assert.Equal(1100, usage.AudioOutputTokens);
        }

        [Fact]
        public void Audio_text_and_cached_tokens_are_each_billed_at_their_own_rate()
        {
            var usage = new OpenAIRealtimeUsage();
            usage.Add(Turn(textIn: 1_000_000, audioIn: 1_000_000, cachedText: 0, cachedAudio: 0, textOut: 1_000_000, audioOut: 1_000_000));

            // gpt-realtime per 1M: text 4 in / 16 out, audio 32 in / 64 out.
            Assert.Equal(4m + 16m + 32m + 64m, usage.CostUSD("gpt-realtime"));
            // gpt-4o-realtime-preview per 1M: text 5 / 20, audio 40 / 80.
            Assert.Equal(5m + 20m + 40m + 80m, usage.CostUSD("gpt-4o-realtime-preview"));
            // mini per 1M: text 0.60 / 2.40, audio 10 / 20.
            Assert.Equal(0.60m + 2.40m + 10m + 20m, usage.CostUSD("gpt-4o-mini-realtime-preview"));
        }

        [Fact]
        public void Cached_input_is_billed_at_the_cached_rate_not_twice()
        {
            var usage = new OpenAIRealtimeUsage();
            usage.Add(Turn(textIn: 0, audioIn: 1_000_000, cachedText: 0, cachedAudio: 1_000_000, textOut: 0, audioOut: 0));

            Assert.Equal(0.40m, usage.CostUSD("gpt-realtime"));
        }

        [Fact]
        public void The_logged_payload_reads_back_to_the_same_usage()
        {
            var usage = new OpenAIRealtimeUsage();
            usage.Add(Turn(1000, 2000, 300, 400, 100, 500));
            usage.Add(Turn(1000, 2000, 300, 400, 100, 500));

            var copy = OpenAIRealtimeUsage.FromPayload(usage.ToPayload());

            Assert.Equal(usage.CostUSD("gpt-realtime"), copy.CostUSD("gpt-realtime"));
            Assert.Equal(2, copy.Responses);
        }

        [Fact]
        public void A_missing_or_broken_usage_block_adds_nothing()
        {
            var usage = new OpenAIRealtimeUsage();
            usage.Add(null);
            usage.Add(JValue.CreateNull());

            Assert.Equal(0, usage.Responses);
            Assert.Equal(0m, OpenAIRealtimeUsage.FromPayload("not json").CostUSD("gpt-realtime"));
        }

        [Fact]
        public async Task The_logger_prices_a_realtime_session_and_fills_its_token_columns()
        {
            var written = new List<OpenAIApiLog>();
            var repository = new Mock<IOpenAIApiLogRepository>(MockBehavior.Loose);
            repository.Setup(r => r.LogApiCallAsync(It.IsAny<OpenAIApiLog>()))
                .Callback<OpenAIApiLog>(written.Add)
                .ReturnsAsync(1L);
            var logger = new OpenAIApiLogger(repository.Object, new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object);

            var usage = new OpenAIRealtimeUsage();
            usage.Add(Turn(textIn: 2000, audioIn: 10000, cachedText: 0, cachedAudio: 0, textOut: 500, audioOut: 8000));

            await logger.LogApiCallAsync("wss://api.openai.com/v1/realtime", "REALTIME", "{}", usage.ToPayload(), 60000, 200, true,
                model: "gpt-realtime", context: new OpenAIApiLogContext { OperationType = "PlivoInterviewCall", MeetingId = 42 });

            var row = written.Single();
            Assert.Equal(usage.CostUSD("gpt-realtime"), row.EstimatedCostUSD);
            Assert.True(row.EstimatedCostUSD > 0.5m, "audio must not be priced as cheap text");
            Assert.Equal(12000, row.PromptTokens);
            Assert.Equal(8500, row.CompletionTokens);
            Assert.Equal(42, row.MeetingId);
        }
    }
}
