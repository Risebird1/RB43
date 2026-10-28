using Risebird.Business.PlivoCX;

namespace Risebird.Business.Tests.PlivoCXCampaignTests
{
    /// <summary>
    /// The agent's LLM writes candidate_confirmed / candidate_disposition, so the same answer
    /// arrives in many spellings. An exact "true" check recorded real confirmations as "not
    /// confirmed" - these pin the forms that must be read correctly.
    /// </summary>
    public class PlivoCXOutcomeParserTests
    {
        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("\"true\"")]
        [InlineData(" true. ")]
        [InlineData("Yes")]
        [InlineData("yes!")]
        [InlineData("1")]
        [InlineData("confirmed")]
        [InlineData("Haan ji")]
        public void A_yes_in_any_spelling_is_confirmed(string raw)
        {
            Assert.True(PlivoCXOutcomeParser.IsConfirmed(raw));
        }

        [Theory]
        [InlineData("false")]
        [InlineData("\"false\"")]
        [InlineData("No")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("not confirmed")]
        [InlineData("maybe")]
        public void Anything_else_is_not_confirmed(string raw)
        {
            Assert.False(PlivoCXOutcomeParser.IsConfirmed(raw));
        }

        [Theory]
        [InlineData("interested", "interested")]
        [InlineData("Interested.", "interested")]
        [InlineData("\"interested\"", "interested")]
        [InlineData("not interested", "uninterested")]
        [InlineData("Uninterested", "uninterested")]
        [InlineData("not_interested", "uninterested")]
        [InlineData("cant_say", "cant_say")]
        [InlineData("Can't say", "cant_say")]
        [InlineData("something else", "cant_say")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Dispositions_map_to_the_three_values(string raw, string expected)
        {
            Assert.Equal(expected, PlivoCXOutcomeParser.NormaliseDisposition(raw));
        }
    }
}
