using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Immutable AI snapshot stored in SkillChannelSampleQuestions.AIDraftPayload.
    /// Compared with live fields using normalised text (trim + collapse whitespace).
    /// </summary>
    public class AIQuestionDraftSnapshot
    {
        public string title { get; set; }
        public string gist { get; set; }
        public string expectedOutput { get; set; }
        public string explanation { get; set; }
        public string safeImplementation { get; set; }
        public string semantic { get; set; }
        public string redFlags { get; set; }
        public int? max { get; set; }
        public int? time { get; set; }
        public string code { get; set; }
    }

    public class SMEQuestionSaveRequest
    {
        public int IrfId { get; set; }
        public int Id { get; set; }
        public string Comment { get; set; }
        public string QuestionGist { get; set; }
        public string ExpectedOutput { get; set; }
        public string AnswerExplanation { get; set; }
        public string SafeImplementation { get; set; }
        public string SemanticMatchCriteria { get; set; }
        public string MisconceptionRedFlags { get; set; }
        public int? MaxScore { get; set; }
        public int? TimeLimitMins { get; set; }
        public bool IsCodingQuestion { get; set; }
        public bool FlagForSentiment { get; set; }
        public string QuestionSlot { get; set; }
        public string QuestionVariant { get; set; }
        public string SkillName { get; set; }
    }

    public class SMEQuestionConfirmRequest
    {
        public int IrfId { get; set; }
        public int Id { get; set; }
        public SMEQuestionSaveRequest Question { get; set; }
    }

    public class SMEQuestionRejectRequest
    {
        public int IrfId { get; set; }
        public int Id { get; set; }
        public string Reason { get; set; }
    }

    public class SMEQuestionSetSaveRequest
    {
        public int IrfId { get; set; }
        public int? Id { get; set; }
        public string Name { get; set; }
        public int[] QuestionIds { get; set; }
    }

    public class SMEQuestionSetActionRequest
    {
        public int IrfId { get; set; }
        public int Id { get; set; }
    }

    public class SMEQuestionSetSwapRequest
    {
        public int IrfId { get; set; }
        public int SetId { get; set; }
        public int RemoveQuestionId { get; set; }
        public int AddQuestionId { get; set; }
    }

    public class ExpertInLoopConfirmRequest
    {
        public int IrfId { get; set; }
        public int? SEId { get; set; }
        public byte Scope { get; set; }
        public byte Status { get; set; }
        public string ClientComment { get; set; }
    }

    public class QuestionReviewLogRow
    {
        public int QuestionBankId { get; set; }
        public string FieldName { get; set; }
        public string AIValue { get; set; }
        public string SMEValue { get; set; }
        public byte Severity { get; set; }
        public string ChangedBy { get; set; }
    }

    public class AICodeInterviewQuestionSetVm
    {
        public int Id { get; set; }
        public int IRFId { get; set; }
        public string Name { get; set; }
        public bool IsAIProposed { get; set; }
        public byte ReviewStatus { get; set; }
        public bool IsDeleted { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int[] QuestionIds { get; set; }
    }

    public class ExpertInLoopClientConfirmationVm
    {
        public int Id { get; set; }
        public int IRFId { get; set; }
        public int? SEId { get; set; }
        public int? InterviewerProfileId { get; set; }
        public byte Scope { get; set; }
        public byte Status { get; set; }
        public string ClientUserId { get; set; }
        public DateTime? ConfirmedOn { get; set; }
        public string ClientComment { get; set; }
        public string ExpertCodedName { get; set; }
        public int? ExpertUserId { get; set; }
        public string ExpertEmail { get; set; }
        public string SkillChannel { get; set; }
        public int InterviewsConducted { get; set; }
        public decimal? Rating { get; set; }
        public int ConfirmedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CorrectedCount { get; set; }
        public string SignedOffOn { get; set; }
        public string ApprovedSetNames { get; set; }
        public int FieldsCorrected { get; set; }
        public string MaxSeverityLabel { get; set; }
        public string ReviewedOn { get; set; }
    }
}
