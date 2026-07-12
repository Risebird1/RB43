using System;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Dapper row shape returned by <c>usp_GetIrfIndexBatch</c>.
    /// Convert to <see cref="Entities.Search.IrfIndexDocument"/> via <see cref="IrfIndexDocumentMapper.FromDatabaseRow"/>.
    /// </summary>
    internal sealed class IrfIndexDocumentRow
    {
        public int IrfIntId { get; set; }
        public string IRFId { get; set; }
        public string JDTiltle { get; set; }
        public string PrimarySkill { get; set; }
        public string ClientCompName { get; set; }
        public string ClientName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string JDLink { get; set; }
        public int RisebirdHr { get; set; }
        public string RisebirdHREmail { get; set; }
        public double PerMintuteRate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPaused { get; set; }
        public bool IsClientUser { get; set; }
        public int TotalInvites { get; set; }
        public int TotalBudget { get; set; }
        public double TotalBudgetConsumed { get; set; }
        public string CalendlyLink { get; set; }
        public string CompanyDetails { get; set; }
        public double Discount { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public int? PriorityLevel { get; set; }
        public int? ConfirmedMeetings { get; set; }
        public int? TotalMeetings { get; set; }
        public int? SelectedMeetings { get; set; }
        public int? ScheduledMeetings { get; set; }
        public int FlagCount { get; set; }
        public int? CreatedFromTemplateId { get; set; }
        public string AssessmentGroupId { get; set; }
        public bool IsAIInterview { get; set; }
        public int AIInterviewType { get; set; }
        public int InterviewerBillingCycle { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool AllowAllInternal { get; set; }

        /// <summary>Pipe-delimited emails from SQL <c>STRING_AGG</c>; parsed in the mapper.</summary>
        public string AllowedViewerEmails { get; set; }

        public bool CuratedLink { get; set; }
        public string AffindaEmbedUrl { get; set; }
        public DateTime? AffindaEmbedUrlCreatedAt { get; set; }
    }
}
