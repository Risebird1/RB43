using System.Collections.Generic;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search field names for the IRF listing index (<c>irfindex</c>).
    /// <para>
    /// Use these constants everywhere you build OData filters, upload JSON, or define the index schema.
    /// When adding a field, update this class, <see cref="IrfIndexDefinition"/>,
    /// <see cref="IrfIndexDocumentMapper"/>, <c>Create-IrfIndex.ps1</c>, and <c>usp_GetIrfIndexBatch</c>.
    /// </para>
    /// </summary>
    internal static class IrfIndexFields
    {
        public const string Key = "irfIntId";
        public const string IrfId = "irfId";
        public const string JdTitle = "jdTiltle";
        public const string PrimarySkill = "primarySkill";
        public const string ClientCompName = "clientCompName";
        public const string ClientName = "clientName";
        public const string StartDate = "startDate";
        public const string EndDate = "endDate";
        public const string CreatedOn = "createdOn";
        public const string JdLink = "jdLink";
        public const string RisebirdHr = "risebirdHr";
        public const string RisebirdHrEmail = "risebirdHrEmail";
        public const string PerMinuteRate = "perMintuteRate";
        public const string IsDeleted = "isDeleted";
        public const string IsPaused = "isPaused";
        public const string IsClientUser = "isClientUser";
        public const string TotalInvites = "totalInvites";
        public const string TotalBudget = "totalBudget";
        public const string TotalBudgetConsumed = "totalBudgetConsumed";
        public const string CalendlyLink = "calendlyLink";
        public const string CompanyDetails = "companyDetails";
        public const string Discount = "discount";
        public const string IsBiometricEnabled = "isBiometricEnabled";
        public const string PriorityLevel = "priorityLevel";
        public const string ConfirmedMeetings = "confirmedMeetings";
        public const string TotalMeetings = "totalMeetings";
        public const string SelectedMeetings = "selectedMeetings";
        public const string ScheduledMeetings = "scheduledMeetings";
        public const string FlagCount = "flagCount";
        public const string CreatedFromTemplateId = "createdFromTemplateId";
        public const string AssessmentGroupId = "assessmentGroupId";
        public const string IsAiInterview = "isAIInterview";
        public const string AiInterviewType = "aiInterviewType";
        public const string InterviewerBillingCycle = "interviewerBillingCycle";
        public const string AllowAllInternal = "allowAllInternal";
        public const string AllowedViewerEmails = "allowedViewerEmails";
        public const string CuratedLink = "curatedLink";
        public const string AffindaEmbedUrl = "affindaEmbedUrl";
        public const string AffindaEmbedUrlCreatedAt = "affindaEmbedUrlCreatedAt";

        /// <summary>Fields searched by the IRF list search box on <c>/IRF/Index</c>.</summary>
        public static readonly IReadOnlyList<string> ListViewSearch = new[]
        {
            IrfId,
            JdTitle,
            PrimarySkill,
            ClientCompName,
            ClientName,
            RisebirdHrEmail
        };
    }
}
