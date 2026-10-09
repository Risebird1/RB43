using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// The candidate confirms their transcript before it is submitted, and what speech-to-text
    /// actually heard is kept beside it. The divergence is what tells QC whether an edit fixed a
    /// mis-heard word or rewrote the answer.
    /// </summary>
    public class CandidateTranscriptReviewTests
    {
        #region No edit

        [Fact]
        public void An_untouched_transcript_records_no_edit()
        {
            const string spoken = "I have four years of experience with Spring Boot.";

            var review = CandidateTranscriptReview.Build(spoken, spoken);

            Assert.False(review.EditedByCandidate);
            Assert.Equal(0, review.DivergencePct);
            Assert.Equal(spoken, review.ConfirmedTranscript);
            Assert.Equal(spoken, review.RawTranscript);
        }

        [Fact]
        public void Casing_and_punctuation_changes_are_not_an_edit()
        {
            // Fixing a comma is not changing the answer.
            var review = CandidateTranscriptReview.Build(
                "i have four years of experience with spring boot",
                "I have four years of experience with Spring Boot.");

            Assert.False(review.EditedByCandidate);
            Assert.Equal(0, review.DivergencePct);
        }

        [Fact]
        public void Extra_whitespace_is_not_an_edit()
        {
            var review = CandidateTranscriptReview.Build(
                "I have four years",
                "  I  have   four\nyears  ");

            Assert.False(review.EditedByCandidate);
        }

        [Fact]
        public void A_blank_confirmation_falls_back_to_the_raw_transcript()
        {
            var review = CandidateTranscriptReview.Build("I led the migration.", "   ");

            Assert.Equal("I led the migration.", review.ConfirmedTranscript);
            Assert.False(review.EditedByCandidate);
        }

        #endregion

        #region A real correction

        [Fact]
        public void A_single_misheard_word_is_a_small_divergence()
        {
            // The case this feature exists for: "four years" heard as "fourteen years".
            var review = CandidateTranscriptReview.Build(
                "I have fourteen years of experience with Spring Boot",
                "I have four years of experience with Spring Boot");

            Assert.True(review.EditedByCandidate);
            Assert.InRange(review.DivergencePct, 1, 20);
            Assert.Equal("I have four years of experience with Spring Boot", review.ConfirmedTranscript);
        }

        [Fact]
        public void The_raw_transcript_is_never_replaced_by_the_correction()
        {
            var review = CandidateTranscriptReview.Build(
                "my name is Shreyash",
                "my name is Shreyas");

            // The machine's version survives alongside the candidate's, as the tiebreaker.
            Assert.Equal("my name is Shreyash", review.RawTranscript);
            Assert.Equal("my name is Shreyas", review.ConfirmedTranscript);
        }

        [Fact]
        public void A_rewritten_answer_is_a_large_divergence()
        {
            var review = CandidateTranscriptReview.Build(
                "I have not worked with Kafka at all",
                "I architected the entire event streaming platform using Kafka across twelve services");

            Assert.True(review.EditedByCandidate);
            Assert.InRange(review.DivergencePct, 60, 100);
        }

        [Fact]
        public void Answering_an_empty_transcript_from_scratch_is_a_full_divergence()
        {
            // What happens when the recording could not be made out and the candidate types instead.
            var review = CandidateTranscriptReview.Build("", "I spent three years on payments integrations.");

            Assert.True(review.EditedByCandidate);
            Assert.Equal(100, review.DivergencePct);
        }

        #endregion

        #region Divergence arithmetic

        [Fact]
        public void Divergence_is_measured_in_words_not_characters()
        {
            // One word of six changed: well under what the same edit looks like character by character.
            var pct = CandidateTranscriptReview.ComputeDivergencePct(
                "one two three four five six",
                "one two three four five seven");

            Assert.InRange(pct, 1, 20);
        }

        [Fact]
        public void Divergence_is_rounded_up_so_any_real_edit_shows()
        {
            // 1 word in 40 is 2.5%, which must not round down to zero and read as untouched.
            var raw = string.Join(" ", System.Linq.Enumerable.Range(1, 40).Select(i => "word" + i));
            var edited = raw.Replace("word40", "different");

            var pct = CandidateTranscriptReview.ComputeDivergencePct(raw, edited);

            Assert.Equal(3, pct);
        }

        [Fact]
        public void Two_empty_transcripts_do_not_diverge()
        {
            Assert.Equal(0, CandidateTranscriptReview.ComputeDivergencePct("", ""));
            Assert.Equal(0, CandidateTranscriptReview.ComputeDivergencePct(null, null));
        }

        [Fact]
        public void Divergence_never_leaves_the_zero_to_one_hundred_range()
        {
            Assert.Equal(100, CandidateTranscriptReview.ComputeDivergencePct("something", ""));
            Assert.Equal(100, CandidateTranscriptReview.ComputeDivergencePct("", "something"));
            Assert.InRange(CandidateTranscriptReview.ComputeDivergencePct("a b c", "x y z"), 0, 100);
        }

        #endregion

        #region Take count

        /// <summary>
        /// A take count below 1 still describes an answer with one recording behind it. The page
        /// posts this value, so it can arrive absent (an older page still open on the question) or
        /// hand-edited, and neither may make an answer look like it was never recorded.
        /// </summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(int.MinValue, 1)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(97, 97)]
        public void Take_count_is_never_below_one(int posted, int expected) =>
            Assert.Equal(expected, CandidateTranscriptReview.NormalizeTakeCount(posted));

        #endregion
    }
}
