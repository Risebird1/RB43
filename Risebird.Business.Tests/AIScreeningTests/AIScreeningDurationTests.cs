using Risebird.Business;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The recruiter states the interview length inside the instruction, spoken or typed, so it
    /// arrives as words as often as digits.
    /// </summary>
    public class AIScreeningDurationTests
    {
        [Theory]
        [InlineData("Be strict on compensation. Twenty minutes.", 20)]
        [InlineData("keep it to 30 minutes", 30)]
        [InlineData("15 min screen please", 15)]
        [InlineData("run a 45-minute conversation", 45)]
        [InlineData("thirty minutes, go heavy on Kafka", 30)]
        public void ParseDurationMinutes_reads_the_length_out_of_the_instruction(string instruction, int expected)
        {
            Assert.Equal(expected, AIScreeningIntakeService.ParseDurationMinutes(instruction));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("budget is sixteen LPA, Bangalore hybrid")]
        public void ParseDurationMinutes_falls_back_to_the_default_when_no_length_is_stated(string instruction)
        {
            Assert.Equal(20, AIScreeningIntakeService.ParseDurationMinutes(instruction));
        }
    }
}
