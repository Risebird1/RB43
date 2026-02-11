using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class TypingAssessmentOpenAIRequest
    {
        public string task { get; set; } = "typing_accuracy_analysis";
        public TypingInputs inputs { get; set; }
        public TypingProcessingRules processing_rules { get; set; }
        public TypingGradingRules grading_rules { get; set; }
    }

    public class TypingInputs
    {
        public string reference_paragraph { get; set; }
        public string user_typed_text { get; set; }
        public decimal words_per_minute { get; set; }
        public decimal total_minutes { get; set; }
    }

    public class TypingProcessingRules
    {
        public TypingAllowedWordCount allowed_word_count { get; set; }
        public TypingReferenceRestriction reference_restriction { get; set; }
        public TypingExtraWordsHandling extra_words_handling { get; set; }
        public List<string> error_categories_to_track { get; set; }
    }

    public class TypingAllowedWordCount
    {
        public string formula { get; set; } = "words_per_minute * total_minutes";
        public string rounding { get; set; } = "use integer value; if decimal, round down";
    }

    public class TypingReferenceRestriction
    {
        public string method { get; set; } = "compare only the first N words of the reference paragraph";
        public string N { get; set; } = "allowed_word_count";
    }

    public class TypingExtraWordsHandling
    {
        public string rule { get; set; } = "If user types more than allowed_word_count, treat as positive performance. Do not penalize extra words.";
        public string comparison_scope { get; set; } = "accuracy comparison still only uses the first allowed_word_count words of the reference paragraph";
    }

    public class TypingGradingRules
    {
        public List<TypingGradeMapping> overall_grade_mapping { get; set; }
    }

    public class TypingGradeMapping
    {
        public int min_accuracy_percent { get; set; }
        public string grade { get; set; }
    }

    // OpenAI Response Models
    public class TypingAssessmentOpenAIResponse
    {
        public TypingMetaData meta { get; set; }
        public TypingScores scores { get; set; }
        public TypingCounts counts { get; set; }
        public TypingHighlights highlights { get; set; }
        public TypingComparisonPreview comparison_preview { get; set; }
        public TypingVerdict verdict { get; set; }
    }

    public class TypingMetaData
    {
        public decimal words_per_minute { get; set; }
        public decimal total_minutes { get; set; }
        public int allowed_word_count { get; set; }
        public int reference_total_words { get; set; }
        public int reference_words_used_for_comparison { get; set; }
        public int user_total_words_typed { get; set; }
        public bool user_exceeded_allowed_words { get; set; }
        public int exceeded_by_words { get; set; }
    }

    public class TypingScores
    {
        public decimal word_level_accuracy_percent { get; set; }
        public decimal character_level_accuracy_percent { get; set; }
        public string overall_grade { get; set; }
    }

    public class TypingCounts
    {
        public int correct_words { get; set; }
        public int incorrect_words { get; set; }
        public int missing_words { get; set; }
        public int extra_words_within_limit { get; set; }
        public int extra_words_beyond_limit { get; set; }
        public int spelling_errors { get; set; }
        public int capitalization_errors { get; set; }
        public int punctuation_errors { get; set; }
    }

    public class TypingHighlights
    {
        public List<TypingTopErrorType> top_error_types { get; set; }
        public List<TypingMissingWordExample> missing_word_examples { get; set; }
        public List<TypingExtraWordExample> extra_word_examples { get; set; }
    }

    public class TypingTopErrorType
    {
        public string type { get; set; }
        public int count { get; set; }
        public List<TypingErrorExample> examples { get; set; }
    }

    public class TypingErrorExample
    {
        public string user_text { get; set; }
        public string expected_text { get; set; }
        public int position_word_index { get; set; }
    }

    public class TypingMissingWordExample
    {
        public string expected_text { get; set; }
        public int position_word_index { get; set; }
    }

    public class TypingExtraWordExample
    {
        public string user_text { get; set; }
        public int position_word_index { get; set; }
        public bool within_comparison_limit { get; set; }
    }

    public class TypingComparisonPreview
    {
        public string reference_used_excerpt { get; set; }
        public string user_used_excerpt { get; set; }
        public List<string> notes { get; set; }
    }

    public class TypingVerdict
    {
        public bool met_expected_output_size { get; set; }
        public string speed_comment { get; set; }
        public string accuracy_comment { get; set; }
        public List<string> actionable_tips { get; set; }
    }
}
