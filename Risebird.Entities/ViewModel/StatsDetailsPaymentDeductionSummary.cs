namespace Risebird.Entities.ViewModel
{
    /// <summary>Batch row from PaymentSLADetails + NonAI reasoning for Completed Interviews column.</summary>
    public class StatsDetailsPaymentDeductionSummary
    {
        public int InterviewId { get; set; }
        public int? PaymentDeduction { get; set; }
        public int? PaymentComplianceCount { get; set; }
        public string NonAIreasoning { get; set; }
        public string Riskcategory { get; set; }
        public string Inaccuratedecisionmaking { get; set; }
        public string TimeManagement { get; set; }
        public string QuestionsRelevance { get; set; }
        public string ScoreRubricIgnorance { get; set; }
        public string PoorFeedbackComments { get; set; }
        public string CandidateInteractionQuality { get; set; }
        public string LimitedHandoncoding { get; set; }
        public string WrongStatusUpdates { get; set; }
        public string WeakInfrastructure { get; set; }
        public string ProctoringIgnorance { get; set; }
        public string UnethicalPractices { get; set; }
        public string PoorCommunicationSkills { get; set; }
        public string AttendanceJoinCompliance { get; set; }
        public string AIContradictionScore { get; set; }
        public string InterviewerVideo { get; set; }
    }
}
