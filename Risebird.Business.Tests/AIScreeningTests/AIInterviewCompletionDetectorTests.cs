using Risebird.Entities.ViewModel;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// An adaptive interview must end when the assistant does, not loop on its goodbye until someone
    /// clicks Exit - which is what testers hit when a prompt left the completion marker out.
    /// </summary>
    public class AIInterviewCompletionDetectorTests
    {
        [Fact]
        public void The_marker_signals_completion()
        {
            Assert.True(AIInterviewCompletionDetector.SignalsCompletion(
                "Thanks for your time. " + AIInterviewCompletionDetector.Marker));
        }

        [Theory]
        [InlineData("{\"message\":\"Thanks for your time.\",\"interview_complete\":true}")]
        [InlineData("{\"message\":\"Thanks for your time.\",\"isComplete\":\"true\"}")]
        [InlineData("{\"closing\":\"Thanks for your time.\",\"status\":\"completed\"}")]
        public void A_json_completion_flag_signals_completion(string reply)
        {
            Assert.True(AIInterviewCompletionDetector.SignalsCompletion(reply));
        }

        [Theory]
        [InlineData("{\"greeting\":\"Hello\",\"question\":\"Tell me about yourself?\"}")]
        [InlineData("{\"message\":\"Thanks.\",\"interview_complete\":false}")]
        [InlineData("Could you describe your last role?")]
        [InlineData(null)]
        public void Ordinary_turns_do_not_signal_completion(string reply)
        {
            Assert.False(AIInterviewCompletionDetector.SignalsCompletion(reply));
        }

        [Theory]
        [InlineData("Thanks for your time. We will get back to you.", "Thanks for your time. We will get back to you.")]
        [InlineData("Thanks for your time! We will get back to you", "Thanks for your time. We will get back to you.")]
        [InlineData("Thanks again for your time. We will get back to you.", "Thanks for your time. We will get back to you.")]
        public void A_repeated_closing_statement_is_the_goodbye_loop(string reply, string previous)
        {
            Assert.True(AIInterviewCompletionDetector.RepeatsClosingTurn(reply, previous));
        }

        [Fact]
        public void A_repeated_question_is_not_treated_as_the_end()
        {
            const string question = "Could you walk me through your most recent customer support experience?";

            Assert.False(AIInterviewCompletionDetector.RepeatsClosingTurn(question, question));
        }

        [Fact]
        public void A_new_statement_that_starts_like_the_last_one_is_not_the_end()
        {
            Assert.False(AIInterviewCompletionDetector.RepeatsClosingTurn(
                "Great. Now describe how you handle an escalation from an unhappy customer.",
                "Great."));
        }

        [Theory]
        [InlineData("Thank you, goodbye, take care.")]
        [InlineData("Thanks for your time. We will get back to you.")]
        [InlineData("That concludes the interview. Have a great day!")]
        public void A_sign_off_is_a_closing_statement(string reply)
        {
            Assert.True(AIInterviewCompletionDetector.IsClosingStatement(reply));
        }

        [Theory]
        [InlineData("Understood. Which CRM have you used, and how did you use it to track your team's leads?")]
        [InlineData("Thank you. Now tell me about a time you had to say goodbye to a difficult client.")]
        [InlineData("Take care to describe the process step by step.")]
        [InlineData("Great answer. Let's move on.")]
        [InlineData("")]
        public void A_question_or_prompt_is_not_a_closing_statement(string reply)
        {
            Assert.False(AIInterviewCompletionDetector.IsClosingStatement(reply));
        }

        [Fact]
        public void An_unwrapped_turn_keeps_its_completion_flag_as_the_marker()
        {
            const string reply = "{\"message\":\"Thanks for your time. We will get back to you.\",\"interview_complete\":true}";

            var spoken = AIInterviewReplySanitizer.StripOrFallback(reply, out _);

            Assert.Equal("Thanks for your time. We will get back to you. " + AIInterviewCompletionDetector.Marker, spoken);
        }

        [Fact]
        public void Status_fields_and_string_flags_are_not_spoken()
        {
            const string reply = "{\"message\":\"Thanks for your time.\",\"status\":\"completed\",\"done\":\"true\"}";

            var spoken = AIInterviewReplySanitizer.StripOrFallback(reply, out _);

            Assert.Equal("Thanks for your time. " + AIInterviewCompletionDetector.Marker, spoken);
        }

        [Theory]
        [InlineData("The interview has not started yet.")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("Thanks, that's helpful. Let's move on.")]
        public void A_turn_that_asks_nothing_needs_the_next_question(string reply)
        {
            Assert.True(AIInterviewCompletionDetector.NeedsNextQuestion(reply));
        }

        [Theory]
        [InlineData("Could you walk me through a recent deal?")]
        [InlineData("Walk me through a B2B institute subscription deal you worked on at PrepLadder.")]
        [InlineData("Tell me about a time a decision-maker objected to your price.")]
        [InlineData("Thank you, goodbye, take care.")]
        [InlineData("Thanks for your time. " + AIInterviewCompletionDetector.Marker)]
        public void A_question_or_a_sign_off_does_not(string reply)
        {
            Assert.False(AIInterviewCompletionDetector.NeedsNextQuestion(reply));
        }
    }
}
