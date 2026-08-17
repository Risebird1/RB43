namespace Risebird.Entities.Enum
{
    /// <summary>
    /// Identifies which field of the report a discrepancy row describes. Persisted by name so the
    /// stored audit trail stays readable and survives reordering of this enum.
    /// </summary>
    public enum AICodeInterviewDiscrepancyFieldTypeEnum
    {
        OverallVerdict,
        OverallComment,
        OverallCommunicationRating,
        OverallCommunicationComment,

        /// <summary>
        /// A report level rubric dimension. Tech skill scores are modelled as dimensions keyed by
        /// skill name, so they are compared and stored through this field type as well.
        /// </summary>
        OverallRubricRating,

        QuestionRating,
        QuestionVerdict,
        QuestionComment,
        QuestionCommunicationRating,
        QuestionCommunicationComment,
        QuestionRubricRating,

        /// <summary>
        /// The transcript shown beside each recorded answer. The AI copy holds the speech to text
        /// output, the expert copy holds their corrected text.
        /// </summary>
        TranscriptText
    }
}
