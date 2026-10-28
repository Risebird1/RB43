using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// The model tiers and the Responses API usage shape that the cost dashboard and the Interview
    /// Profitability grid depend on. Before these existed, every gpt-4.1 call was billed at legacy
    /// GPT-4 rates and every Responses API call logged zero tokens.
    /// </summary>
    public sealed class OpenAIModelTierPricingTests
    {
        private static OpenAIApiLogger Sut() => new(
            new Mock<IOpenAIApiLogRepository>(MockBehavior.Loose).Object,
            new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object);

        [Theory]
        [InlineData("gpt-4.1", 0.0020, 0.0080)]
        [InlineData("gpt-4.1-2025-04-14", 0.0020, 0.0080)]
        [InlineData("GPT-4.1", 0.0020, 0.0080)]
        [InlineData("gpt-4.1-mini", 0.0004, 0.0016)]
        [InlineData("gpt-4.1-mini-2025-04-14", 0.0004, 0.0016)]
        [InlineData("gpt-4.1-nano", 0.0001, 0.0004)]
        [InlineData("gpt-5", 0.00125, 0.0100)]
        [InlineData("gpt-5-mini", 0.00025, 0.0020)]
        [InlineData("gpt-5-nano", 0.00005, 0.0004)]
        public void Each_current_tier_is_billed_at_its_own_rates(string model, double inputPer1K, double outputPer1K)
        {
            Assert.Equal((decimal)inputPer1K + (decimal)outputPer1K, Sut().CalculateCost(model, 1000, 1000));
        }

        [Fact]
        public void Gpt_4_1_is_no_longer_billed_as_legacy_gpt_4()
        {
            Assert.True(Sut().CalculateCost("gpt-4.1", 1000, 1000) < Sut().CalculateCost("gpt-4", 1000, 1000) / 5);
        }

        [Fact]
        public void Cached_prompt_tokens_are_billed_at_the_cached_rate()
        {
            // 200 uncached at $2.00/1M plus 800 cached at $0.50/1M.
            Assert.Equal(0.0004m + 0.0004m, Sut().CalculateCost("gpt-4.1", 1000, 0, 800));
        }

        [Fact]
        public void A_cached_count_larger_than_the_prompt_is_clamped_to_the_prompt()
        {
            Assert.Equal(Sut().CalculateCost("gpt-4.1", 1000, 0, 1000), Sut().CalculateCost("gpt-4.1", 1000, 0, 5000));
        }

        [Fact]
        public void A_model_without_a_cached_rate_bills_cached_tokens_at_the_input_rate()
        {
            Assert.Equal(Sut().CalculateCost("gpt-4", 1000, 0), Sut().CalculateCost("gpt-4", 1000, 0, 900));
        }

        [Fact]
        public void The_three_argument_overload_is_unchanged_by_the_cache_discount()
        {
            Assert.Equal(Sut().CalculateCost("gpt-4o", 1234, 567, 0), Sut().CalculateCost("gpt-4o", 1234, 567));
        }

        [Fact]
        public void A_responses_api_usage_block_is_read()
        {
            var usage = Sut().ExtractTokenUsage(
                "{\"id\":\"resp_1\",\"usage\":{\"input_tokens\":120,\"output_tokens\":30,\"total_tokens\":150}}");

            Assert.Equal((120, 30, 150), usage);
        }

        [Fact]
        public void A_responses_api_usage_block_without_a_total_is_summed()
        {
            Assert.Equal((10, 5, 15), Sut().ExtractTokenUsage("{\"usage\":{\"input_tokens\":10,\"output_tokens\":5}}"));
        }

        [Theory]
        [InlineData("{\"usage\":{\"input_tokens\":100,\"input_tokens_details\":{\"cached_tokens\":64}}}", 64)]
        [InlineData("{\"usage\":{\"prompt_tokens\":100,\"prompt_tokens_details\":{\"cached_tokens\":32}}}", 32)]
        [InlineData("{\"usage\":{\"input_tokens\":100}}", 0)]
        [InlineData("{}", 0)]
        [InlineData("not json", 0)]
        [InlineData(null, 0)]
        public void Cached_input_tokens_are_read_from_either_api_shape(string payload, int expected)
        {
            Assert.Equal(expected, Sut().ExtractCachedInputTokens(payload));
        }
    }

    public sealed class OpenAIApiLogScopeTests
    {
        private static (OpenAIApiLogger Sut, List<OpenAIApiLog> Written) LoggerCapturingWrites()
        {
            var written = new List<OpenAIApiLog>();
            var repository = new Mock<IOpenAIApiLogRepository>(MockBehavior.Loose);
            repository.Setup(r => r.LogApiCallAsync(It.IsAny<OpenAIApiLog>()))
                .Callback<OpenAIApiLog>(written.Add)
                .ReturnsAsync(1L);
            return (new OpenAIApiLogger(repository.Object, new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object), written);
        }

        [Fact]
        public async Task A_call_logged_inside_a_scope_is_attributed_to_its_interview()
        {
            var (sut, written) = LoggerCapturingWrites();

            using (OpenAIApiLogScope.Begin(meetingId: 42, irfId: 7))
            {
                await sut.LogApiCallAsync("https://x/responses", "POST", "{}", "{}", 1, 200, true,
                    context: new OpenAIApiLogContext { OperationType = "TranscriptAnalysis" });
            }

            Assert.Equal(42, written.Single().MeetingId);
            Assert.Equal(7, written.Single().IRFId);
            Assert.Equal("TranscriptAnalysis", written.Single().OperationType);
        }

        [Fact]
        public async Task An_id_the_call_site_set_itself_wins_over_the_scope()
        {
            var (sut, written) = LoggerCapturingWrites();

            using (OpenAIApiLogScope.Begin(meetingId: 42))
            {
                await sut.LogApiCallAsync("u", "POST", "{}", "{}", 1, 200, true,
                    context: new OpenAIApiLogContext { MeetingId = 99 });
            }

            Assert.Equal(99, written.Single().MeetingId);
        }

        [Fact]
        public async Task A_call_outside_any_scope_is_unattributed()
        {
            var (sut, written) = LoggerCapturingWrites();

            await sut.LogApiCallAsync("u", "POST", "{}", "{}", 1, 200, true);

            Assert.Null(written.Single().MeetingId);
        }

        [Fact]
        public void Disposing_an_inner_scope_restores_the_outer_one()
        {
            using (OpenAIApiLogScope.Begin(meetingId: 1))
            {
                using (OpenAIApiLogScope.Begin(meetingId: 2))
                    Assert.Equal(2, OpenAIApiLogScope.Ambient.MeetingId);

                Assert.Equal(1, OpenAIApiLogScope.Ambient.MeetingId);
            }

            Assert.Null(OpenAIApiLogScope.Ambient);
        }

        [Fact]
        public void An_inner_scope_inherits_ids_it_does_not_set()
        {
            using (OpenAIApiLogScope.Begin(meetingId: 1, irfId: 5))
            using (OpenAIApiLogScope.Begin(candidateId: 9))
            {
                Assert.Equal(1, OpenAIApiLogScope.Ambient.MeetingId);
                Assert.Equal(5, OpenAIApiLogScope.Ambient.IRFId);
                Assert.Equal(9, OpenAIApiLogScope.Ambient.CandidateId);
            }
        }

        [Fact]
        public void Zero_and_empty_ids_mean_no_id()
        {
            using (OpenAIApiLogScope.Begin(meetingId: 0, sessionId: Guid.Empty))
            {
                Assert.Null(OpenAIApiLogScope.Ambient.MeetingId);
                Assert.Null(OpenAIApiLogScope.Ambient.SessionId);
            }
        }

        [Fact]
        public async Task The_scope_flows_across_awaits()
        {
            using (OpenAIApiLogScope.Begin(meetingId: 3))
            {
                await Task.Yield();
                await Task.Delay(1);
                Assert.Equal(3, OpenAIApiLogScope.Ambient.MeetingId);
            }
        }
    }
}
