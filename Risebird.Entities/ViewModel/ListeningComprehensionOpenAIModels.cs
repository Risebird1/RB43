using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    // Inputs sent to the listening comprehension scoring prompt. The prompt itself holds the
    // grading instructions, so only the raw material travels in the request body.
    public class ListeningComprehensionInputs
    {
        public string audio_transcription { get; set; }
        public string expected_answer { get; set; }
        public string candidate_summary { get; set; }
        public decimal time_taken_seconds { get; set; }
        public int audio_play_count { get; set; }
        public bool time_expired { get; set; }
    }

    // Expected shape of the scoring prompt's JSON response.
    public class ListeningComprehensionOpenAIResponse
    {
        public ListeningComprehensionScores scores { get; set; }
        public ListeningComprehensionCoverage coverage { get; set; }
        public ListeningComprehensionVerdict verdict { get; set; }
    }

    public class ListeningComprehensionScores
    {
        public decimal comprehension_score { get; set; }
        public decimal max_score { get; set; }
        public decimal content_accuracy_percent { get; set; }
        public decimal completeness_percent { get; set; }
        public decimal language_quality_percent { get; set; }
        public string overall_grade { get; set; }
    }

    public class ListeningComprehensionCoverage
    {
        public List<string> key_points_captured { get; set; }
        public List<string> key_points_missed { get; set; }
        public List<string> incorrect_statements { get; set; }
    }

    public class ListeningComprehensionVerdict
    {
        public bool recommended { get; set; }
        public string comprehension_comment { get; set; }
        public string language_comment { get; set; }
        public List<string> actionable_tips { get; set; }
        public ListeningComprehensionPassingCriteria passing_criteria { get; set; }
    }

    public class ListeningComprehensionPassingCriteria
    {
        public decimal min_comprehension_score { get; set; }
    }
}
