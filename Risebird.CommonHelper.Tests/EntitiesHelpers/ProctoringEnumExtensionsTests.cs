using Risebird.Entities.Enum;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class ProctoringEnumExtensionsToWireStringTests
    {
        [Theory]
        [InlineData(ProctoringSeverity.Info, "info")]
        [InlineData(ProctoringSeverity.Low, "low")]
        [InlineData(ProctoringSeverity.Medium, "medium")]
        [InlineData(ProctoringSeverity.High, "high")]
        public void Severity_serializes_to_camel_case(ProctoringSeverity value, string expected) =>
            Assert.Equal(expected, value.ToWireString());

        [Theory]
        [InlineData(ProctoringConfidence.Low, "low")]
        [InlineData(ProctoringConfidence.Medium, "medium")]
        [InlineData(ProctoringConfidence.High, "high")]
        public void Confidence_serializes_to_camel_case(ProctoringConfidence value, string expected) =>
            Assert.Equal(expected, value.ToWireString());

        [Theory]
        [InlineData(ProctoringSessionState.Idle, "idle")]
        [InlineData(ProctoringSessionState.Starting, "starting")]
        [InlineData(ProctoringSessionState.Running, "running")]
        [InlineData(ProctoringSessionState.Degraded, "degraded")]
        [InlineData(ProctoringSessionState.Faulted, "faulted")]
        [InlineData(ProctoringSessionState.Stopped, "stopped")]
        public void Session_state_serializes_to_camel_case(ProctoringSessionState value, string expected) =>
            Assert.Equal(expected, value.ToWireString());

        [Theory]
        [InlineData(ProctoringTrust.Unverified, "Unverified")]
        [InlineData(ProctoringTrust.Verified, "Verified")]
        public void Trust_serializes_to_the_raw_member_name(ProctoringTrust value, string expected) =>
            Assert.Equal(expected, value.ToWireString());

        [Fact]
        public void Every_declared_enum_member_has_a_wire_string()
        {
            foreach (ProctoringSeverity value in System.Enum.GetValues<ProctoringSeverity>())
                Assert.False(string.IsNullOrEmpty(value.ToWireString()));
            foreach (ProctoringConfidence value in System.Enum.GetValues<ProctoringConfidence>())
                Assert.False(string.IsNullOrEmpty(value.ToWireString()));
            foreach (ProctoringSessionState value in System.Enum.GetValues<ProctoringSessionState>())
                Assert.False(string.IsNullOrEmpty(value.ToWireString()));
            foreach (ProctoringTrust value in System.Enum.GetValues<ProctoringTrust>())
                Assert.False(string.IsNullOrEmpty(value.ToWireString()));
        }

        [Fact]
        public void An_undefined_severity_throws_rather_than_writing_a_bogus_value() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ProctoringSeverity)99).ToWireString());

        [Fact]
        public void An_undefined_confidence_throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ProctoringConfidence)99).ToWireString());

        [Fact]
        public void An_undefined_session_state_throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ProctoringSessionState)99).ToWireString());

        [Fact]
        public void An_undefined_trust_falls_back_to_the_numeric_string_because_it_uses_ToString() =>
            Assert.Equal("99", ((ProctoringTrust)99).ToWireString());
    }

    public class ProctoringEnumExtensionsParseTests
    {
        [Theory]
        [InlineData("idle", ProctoringSessionState.Idle)]
        [InlineData("IDLE", ProctoringSessionState.Idle)]
        [InlineData("  Idle  ", ProctoringSessionState.Idle)]
        [InlineData("starting", ProctoringSessionState.Starting)]
        [InlineData("running", ProctoringSessionState.Running)]
        [InlineData("degraded", ProctoringSessionState.Degraded)]
        [InlineData("faulted", ProctoringSessionState.Faulted)]
        [InlineData("stopped", ProctoringSessionState.Stopped)]
        public void Session_state_parses_case_insensitively(string wire, ProctoringSessionState expected) =>
            Assert.Equal(expected, ProctoringEnumExtensions.ParseSessionState(wire));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown")]
        [InlineData("idl")]
        [InlineData("idle ")]
        public void Session_state_parse_rejects_unknown_tokens(string wire)
        {
            // "idle " is not rejected — it is trimmed first; see the trimming theory above.
            if (wire == "idle ")
            {
                Assert.Equal(ProctoringSessionState.Idle, ProctoringEnumExtensions.ParseSessionState(wire));
                return;
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => ProctoringEnumExtensions.ParseSessionState(wire));
        }

        [Theory]
        [InlineData("Verified", ProctoringTrust.Verified)]
        [InlineData("Unverified", ProctoringTrust.Unverified)]
        [InlineData("  Verified  ", ProctoringTrust.Verified)]
        public void Trust_parses_the_exact_member_name(string wire, ProctoringTrust expected) =>
            Assert.Equal(expected, ProctoringEnumExtensions.ParseTrust(wire));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("verified")]     // case-sensitive by design
        [InlineData("VERIFIED")]
        [InlineData("unknown")]
        public void Trust_parse_rejects_anything_but_the_exact_member_name(string wire) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ProctoringEnumExtensions.ParseTrust(wire));

        [Fact]
        public void Session_state_round_trips_through_the_wire_format()
        {
            foreach (ProctoringSessionState value in System.Enum.GetValues<ProctoringSessionState>())
                Assert.Equal(value, ProctoringEnumExtensions.ParseSessionState(value.ToWireString()));
        }

        [Fact]
        public void Trust_round_trips_through_the_wire_format()
        {
            foreach (ProctoringTrust value in System.Enum.GetValues<ProctoringTrust>())
                Assert.Equal(value, ProctoringEnumExtensions.ParseTrust(value.ToWireString()));
        }
    }
}
