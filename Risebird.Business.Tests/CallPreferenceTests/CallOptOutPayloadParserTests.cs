using Risebird.Business.CallPreference;
using Xunit;

namespace Risebird.Business.Tests.CallPreferenceTests
{
    /// <summary>
    /// api/CallPreference/*/OptOut (CallOptOutPayload): the opt-out signal and our ids must be found in whatever
    /// shape the provider posts — ElevenLabs server tool, ElevenLabs post-call webhook, Plivo IVR / portal.
    /// </summary>
    public class CallOptOutPayloadParserTests
    {
        private const string ElevenLabsPostCall = @"{
            ""type"": ""post_call_transcription"",
            ""data"": {
                ""agent_id"": ""a1"",
                ""conversation_id"": ""conv_9"",
                ""user_id"": null,
                ""metadata"": { ""phone_call"": { ""external_number"": ""+91 98765-43210"", ""agent_number"": ""+911140000000"" } },
                ""conversation_initiation_client_data"": {
                    ""dynamic_variables"": { ""candidate_id"": ""1234"", ""irf_id"": 77 }
                },
                ""analysis"": {
                    ""data_collection_results"": {
                        ""unsubscribe"": { ""value"": true, ""rationale"": ""asked us to stop calling"" },
                        ""opt_out_reason"": { ""value"": ""please stop calling me"" }
                    }
                }
            }
        }";

        [Fact]
        public void ElevenLabsPostCall_ReadsFlagIdsPhoneAndSource()
        {
            var p = CallOptOutPayload.Parse(ElevenLabsPostCall);

            Assert.True(p.IsOptOut);
            Assert.Equal(1234, p.CandidateId);
            Assert.Equal(77, p.IRFId);
            Assert.Equal("+91 98765-43210", p.Phone);
            Assert.Equal("conv_9", p.CallId);
            Assert.Equal("please stop calling me", p.Reason);
            Assert.Equal("ElevenLabs", p.DetectSource());
        }

        [Fact]
        public void PlivoInterviewer_EventValue_IsOptOut()
        {
            var p = CallOptOutPayload.Parse(@"{ ""CallUUID"": ""38fed99b"", ""To"": ""+918412999109"", ""event"": ""not_interested"", ""userId"": 5521 }");

            Assert.True(p.IsOptOut);
            Assert.Equal(5521, p.UserId);
            Assert.Equal("+918412999109", p.Phone);
            Assert.Equal("38fed99b", p.CallId);
            Assert.Equal("Plivo", p.DetectSource());
        }

        [Fact]
        public void ServerToolBody_UnsubstitutedVariable_IsNotAnId()
        {
            var p = CallOptOutPayload.Parse(@"{ ""unsubscribe"": true, ""user_id"": ""{{user_id}}"", ""phone"": ""9876543210"", ""reason"": ""stop"" }");

            Assert.True(p.IsOptOut);
            Assert.Null(p.UserId);
            Assert.Equal("9876543210", p.Phone);
            Assert.Equal("Unknown", p.DetectSource());
        }

        [Fact]
        public void ValueFalse_IsNotOptOut()
        {
            var p = CallOptOutPayload.Parse(@"{ ""analysis"": { ""data_collection_results"": { ""unsubscribe"": { ""value"": false } } }, ""event"": ""stop"" }");

            Assert.False(p.IsOptOut);
        }

        [Theory]
        [InlineData(@"{ ""opt-out"": ""yes"" }")]
        [InlineData(@"{ ""DoNotCall"": 1 }")]
        [InlineData(@"{ ""disposition"": ""DNC"" }")]
        [InlineData(@"{ ""outcome"": ""Opt_Out"" }")]
        public void FlagAndActionNameVariants_AreOptOut(string json)
        {
            Assert.True(CallOptOutPayload.Parse(json).IsOptOut);
        }

        [Theory]
        [InlineData(@"{ ""event"": ""call_me_later"" }")]
        [InlineData(@"{ ""unsubscribe"": ""no"" }")]
        [InlineData(@"{ ""phone"": ""9876543210"" }")]
        public void NoSignal_IsNotOptOut(string json)
        {
            Assert.False(CallOptOutPayload.Parse(json).IsOptOut);
        }

        [Fact]
        public void ShallowestMatchWins()
        {
            var p = CallOptOutPayload.Parse(@"{ ""unsubscribe"": true, ""userId"": 10, ""nested"": { ""userId"": 20 } }");

            Assert.Equal(10, p.UserId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("[1,2]")]
        public void NonObjectBody_ReturnsNull(string body)
        {
            Assert.Null(CallOptOutPayload.Parse(body));
        }
    }
}
