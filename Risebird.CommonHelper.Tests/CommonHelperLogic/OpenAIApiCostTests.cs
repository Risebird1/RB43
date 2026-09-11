using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;

namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// <c>OpenAIApiLogger</c> is how the portal knows what it spends on OpenAI: every logged call
    /// carries a token count pulled out of the provider's response and a cost derived from it.
    /// Both halves are pure and both are money — an under-priced model or a silently-zero token
    /// count makes the cost dashboard confidently wrong, which is worse than having none.
    /// </summary>
    public sealed class OpenAIApiCostCalculationTests
    {
        private static OpenAIApiLogger Sut() => new(
            new Mock<IOpenAIApiLogRepository>(MockBehavior.Loose).Object,
            new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object);

        [Theory]
        [InlineData("gpt-4o-mini")]
        [InlineData("GPT-4O-MINI")]
        [InlineData("gpt-4o-mini-2024-07-18")]
        [InlineData("openai/gpt-4o-mini")]
        public void The_mini_tier_is_matched_before_the_full_gpt_4o_tier(string model)
        {
            // Ordering matters: "gpt-4o-mini" also contains "gpt-4o", so a reordered chain would
            // bill mini traffic at roughly sixteen times its real price.
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.00015m + 0.00060m, cost);
        }

        [Theory]
        [InlineData("gpt-4o")]
        [InlineData("GPT-4o")]
        [InlineData("gpt-4o-2024-08-06")]
        public void The_gpt_4o_tier_is_priced_at_its_own_rates(string model)
        {
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.0025m + 0.0100m, cost);
        }

        [Theory]
        [InlineData("gpt-4-turbo")]
        [InlineData("gpt-4-turbo-preview")]
        [InlineData("gpt-4-1106-preview")]
        [InlineData("gpt-4-0125-preview")]
        public void The_turbo_variants_are_matched_before_plain_gpt_4(string model)
        {
            // Same trap as mini: every turbo name also contains "gpt-4", and plain gpt-4 is
            // three times the price.
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.01m + 0.03m, cost);
        }

        [Theory]
        [InlineData("gpt-4")]
        [InlineData("gpt-4-0613")]
        [InlineData("gpt-4-32k")]
        public void Plain_gpt_4_is_priced_at_the_legacy_rates(string model)
        {
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.03m + 0.06m, cost);
        }

        [Theory]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("gpt-3.5-turbo-0125")]
        public void The_3_5_turbo_tier_is_priced_at_its_own_rates(string model)
        {
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.0015m + 0.002m, cost);
        }

        [Theory]
        [InlineData("o3-mini")]
        [InlineData("claude-something")]
        [InlineData("whisper-1")]
        [InlineData("text-embedding-3-large")]
        [InlineData("a-model-that-does-not-exist")]
        public void An_unknown_model_falls_back_to_gpt_4o_pricing(string model)
        {
            // Deliberately the expensive default, so a new model shows up as over-stated rather
            // than free. Worth pinning because it is the behaviour a reader would least expect.
            var cost = Sut().CalculateCost(model, 1000, 1000);

            Assert.Equal(0.0025m + 0.0100m, cost);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_blank_model_costs_nothing(string model)
        {
            Assert.Equal(0m, Sut().CalculateCost(model, 1000, 1000));
        }

        [Fact]
        public void A_whitespace_model_is_not_blank_so_it_gets_the_fallback_price()
        {
            // The guard is IsNullOrEmpty, so "   " is treated as an unknown model rather than as
            // a missing one.
            Assert.Equal(0.0025m + 0.0100m, Sut().CalculateCost("   ", 1000, 1000));
        }

        [Fact]
        public void A_call_that_used_no_tokens_costs_nothing()
        {
            Assert.Equal(0m, Sut().CalculateCost("gpt-4o", 0, 0));
        }

        [Fact]
        public void Prompt_tokens_alone_are_billed()
        {
            Assert.Equal(0.0025m, Sut().CalculateCost("gpt-4o", 1000, 0));
        }

        [Fact]
        public void Completion_tokens_alone_are_billed()
        {
            Assert.Equal(0.0100m, Sut().CalculateCost("gpt-4o", 0, 1000));
        }

        [Fact]
        public void Cost_scales_linearly_with_tokens()
        {
            var sut = Sut();

            Assert.Equal(sut.CalculateCost("gpt-4o", 2000, 2000), sut.CalculateCost("gpt-4o", 1000, 1000) * 2);
        }

        [Fact]
        public void A_single_token_is_priced_rather_than_rounded_away()
        {
            // 1 token of gpt-4o output is $0.00001, which survives six-decimal rounding. If the
            // rounding were tighter, per-token calls would all record as free.
            Assert.Equal(0.00001m, Sut().CalculateCost("gpt-4o", 0, 1));
        }

        [Fact]
        public void A_cost_below_the_rounding_floor_becomes_zero()
        {
            // 1 prompt token of gpt-4o-mini is $0.00000015, which rounds to zero at six
            // decimals. Pinned so the limit of the recorded precision is explicit.
            Assert.Equal(0m, Sut().CalculateCost("gpt-4o-mini", 1, 0));
        }

        [Fact]
        public void Cost_is_rounded_to_six_decimal_places()
        {
            var cost = Sut().CalculateCost("gpt-4o-mini", 1234, 5678);

            Assert.Equal(cost, Math.Round(cost, 6));
        }

        [Fact]
        public void A_large_token_count_is_billed_without_overflow()
        {
            var cost = Sut().CalculateCost("gpt-4o", 1_000_000, 1_000_000);

            Assert.Equal(2.5m + 10m, cost);
        }

        [Fact]
        public void Negative_token_counts_produce_a_negative_cost_rather_than_being_clamped()
        {
            // Not a scenario the API produces, but recorded: nothing rejects it, so a corrupted
            // usage block would subtract from the running total instead of being ignored.
            Assert.Equal(-0.0025m, Sut().CalculateCost("gpt-4o", -1000, 0));
        }

        [Fact]
        public void A_negative_prompt_can_cancel_a_positive_completion_out_to_zero()
        {
            // -4000 prompt tokens at $0.0025/1k is -$0.01, exactly offsetting 1000 completion
            // tokens at $0.0100/1k. A cost of zero on a call that used tokens is the visible
            // symptom of a corrupted usage block.
            Assert.Equal(0m, Sut().CalculateCost("gpt-4o", -4000, 1000));
        }
    }

    public sealed class OpenAIApiTokenExtractionTests
    {
        private static OpenAIApiLogger Sut() => new(
            new Mock<IOpenAIApiLogRepository>(MockBehavior.Loose).Object,
            new Mock<IApplicationErrorLogger>(MockBehavior.Loose).Object);

        [Fact]
        public void A_standard_chat_completion_usage_block_is_read()
        {
            var payload = "{\"usage\":{\"prompt_tokens\":120,\"completion_tokens\":45,\"total_tokens\":165}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(120, usage.promptTokens);
            Assert.Equal(45, usage.completionTokens);
            Assert.Equal(165, usage.totalTokens);
        }

        [Fact]
        public void A_missing_total_is_computed_from_the_two_halves()
        {
            var payload = "{\"usage\":{\"prompt_tokens\":120,\"completion_tokens\":45}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(165, usage.totalTokens);
        }

        [Fact]
        public void A_provider_supplied_total_is_trusted_even_when_it_disagrees_with_the_sum()
        {
            // OpenAI counts cached and reasoning tokens in the total, so the total can legally
            // exceed prompt + completion. Recomputing it would understate usage.
            var payload = "{\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":10,\"total_tokens\":999}}";

            Assert.Equal(999, Sut().ExtractTokenUsage(payload).totalTokens);
        }

        [Fact]
        public void A_camel_case_usage_block_is_read_as_a_fallback()
        {
            var payload = "{\"Usage\":{\"promptTokens\":10,\"completionTokens\":20,\"totalTokens\":30}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(10, usage.promptTokens);
            Assert.Equal(20, usage.completionTokens);
            Assert.Equal(30, usage.totalTokens);
        }

        [Fact]
        public void A_pascal_case_usage_block_is_read_too()
        {
            var payload = "{\"Usage\":{\"PromptTokens\":10,\"CompletionTokens\":20,\"TotalTokens\":30}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(10, usage.promptTokens);
            Assert.Equal(20, usage.completionTokens);
        }

        [Fact]
        public void Top_level_token_fields_are_read_for_assistant_runs()
        {
            var payload = "{\"prompt_tokens\":7,\"completion_tokens\":3}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(7, usage.promptTokens);
            Assert.Equal(3, usage.completionTokens);
            Assert.Equal(10, usage.totalTokens);
        }

        [Fact]
        public void Top_level_fields_are_read_even_when_only_one_is_present()
        {
            var usage = Sut().ExtractTokenUsage("{\"completion_tokens\":3}");

            Assert.Equal(0, usage.promptTokens);
            Assert.Equal(3, usage.completionTokens);
            Assert.Equal(3, usage.totalTokens);
        }

        [Fact]
        public void A_nested_usage_block_on_a_completed_thread_run_is_found()
        {
            var payload =
                "{\"object\":\"thread.run\",\"status\":\"completed\",\"step_details\":{\"usage\":" +
                "{\"prompt_tokens\":200,\"completion_tokens\":50,\"total_tokens\":250}}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(200, usage.promptTokens);
            Assert.Equal(50, usage.completionTokens);
            Assert.Equal(250, usage.totalTokens);
        }

        [Theory]
        [InlineData("in_progress")]
        [InlineData("queued")]
        [InlineData("failed")]
        [InlineData("cancelled")]
        public void A_thread_run_that_is_not_completed_is_not_deep_searched(string status)
        {
            // A run still in flight has no final usage, so charging for a partial figure would
            // double count once it completes.
            var payload =
                "{\"object\":\"thread.run\",\"status\":\"" + status + "\",\"step_details\":{\"usage\":" +
                "{\"prompt_tokens\":200,\"completion_tokens\":50}}}";

            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void Run_step_usage_is_summed_across_the_whole_list()
        {
            var payload =
                "{\"object\":\"list\",\"data\":[" +
                "{\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":10,\"total_tokens\":110}}," +
                "{\"usage\":{\"prompt_tokens\":200,\"completion_tokens\":20,\"total_tokens\":220}}]}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(300, usage.promptTokens);
            Assert.Equal(30, usage.completionTokens);
            Assert.Equal(330, usage.totalTokens);
        }

        [Fact]
        public void Run_steps_without_a_total_have_one_computed_from_the_sums()
        {
            var payload =
                "{\"object\":\"list\",\"data\":[" +
                "{\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":10}}," +
                "{\"usage\":{\"prompt_tokens\":200,\"completion_tokens\":20}}]}";

            Assert.Equal(330, Sut().ExtractTokenUsage(payload).totalTokens);
        }

        [Fact]
        public void Run_steps_with_no_usage_at_all_contribute_nothing()
        {
            var payload = "{\"object\":\"list\",\"data\":[{\"id\":\"step-1\"},{\"id\":\"step-2\"}]}";

            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void A_mixed_list_counts_only_the_steps_that_reported_usage()
        {
            var payload =
                "{\"object\":\"list\",\"data\":[" +
                "{\"id\":\"step-1\"}," +
                "{\"usage\":{\"prompt_tokens\":50,\"completion_tokens\":5,\"total_tokens\":55}}]}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(50, usage.promptTokens);
            Assert.Equal(55, usage.totalTokens);
        }

        [Fact]
        public void An_empty_data_list_reports_nothing()
        {
            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage("{\"object\":\"list\",\"data\":[]}"));
        }

        [Fact]
        public void A_zeroed_top_level_usage_block_shadows_a_real_nested_one()
        {
            // A real defect, pinned rather than hidden. The zeroed block correctly falls past
            // the first check, but the thread.run branch then takes SelectTokens("$..usage")
            // .FirstOrDefault() — which is document order, so it finds the zeroed top-level
            // block again and the nested figures are lost. The fix is to pick the first usage
            // that actually reports tokens rather than the first one that exists.
            var payload =
                "{\"usage\":{\"prompt_tokens\":0,\"completion_tokens\":0}," +
                "\"object\":\"thread.run\",\"status\":\"completed\"," +
                "\"step_details\":{\"usage\":{\"prompt_tokens\":11,\"completion_tokens\":2}}}";

            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void With_no_top_level_usage_the_nested_block_is_reached()
        {
            // The same payload without the zeroed decoy costs correctly, which is what makes the
            // test above a shadowing bug rather than intended behaviour.
            var payload =
                "{\"object\":\"thread.run\",\"status\":\"completed\"," +
                "\"step_details\":{\"usage\":{\"prompt_tokens\":11,\"completion_tokens\":2}}}";

            var usage = Sut().ExtractTokenUsage(payload);

            Assert.Equal(11, usage.promptTokens);
            Assert.Equal(2, usage.completionTokens);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_blank_payload_reports_nothing(string payload)
        {
            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("{ broken")]
        [InlineData("[1,2,3]")]
        [InlineData("\"a string\"")]
        [InlineData("null")]
        [InlineData("{}")]
        public void Unparseable_or_unrecognised_payloads_report_nothing_rather_than_throwing(string payload)
        {
            // This runs on the logging path, so throwing here would turn a cost-tracking miss
            // into a failed API call.
            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void A_usage_block_with_non_numeric_token_values_reports_nothing_rather_than_throwing()
        {
            var payload = "{\"usage\":{\"prompt_tokens\":\"lots\",\"completion_tokens\":\"some\"}}";

            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void A_usage_block_with_null_token_values_reports_nothing()
        {
            var payload = "{\"usage\":{\"prompt_tokens\":null,\"completion_tokens\":null}}";

            Assert.Equal((0, 0, 0), Sut().ExtractTokenUsage(payload));
        }

        [Fact]
        public void Extraction_and_costing_compose_into_the_figure_the_dashboard_shows()
        {
            var sut = Sut();
            var payload = "{\"usage\":{\"prompt_tokens\":10000,\"completion_tokens\":2000,\"total_tokens\":12000}}";

            var usage = sut.ExtractTokenUsage(payload);
            var cost = sut.CalculateCost("gpt-4o-mini", usage.promptTokens, usage.completionTokens);

            Assert.Equal(0.0015m + 0.0012m, cost);
        }

        [Fact]
        public void A_response_carrying_no_usage_costs_nothing_end_to_end()
        {
            var sut = Sut();

            var usage = sut.ExtractTokenUsage("{\"id\":\"resp-1\",\"object\":\"response\"}");
            var cost = sut.CalculateCost("gpt-4o", usage.promptTokens, usage.completionTokens);

            Assert.Equal(0m, cost);
        }
    }
}
