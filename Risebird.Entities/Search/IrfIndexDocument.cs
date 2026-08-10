using System;
using System.Collections.Generic;

namespace Risebird.Entities.Search
{
    /// <summary>
    /// Denormalized IRF (skill channel) document stored in the Azure AI Search index <c>irfindex</c>.
    /// <para>
    /// This is the in-memory shape used between SQL and Azure Search. It mirrors one row from
    /// <c>usp_GetIrfIndexBatch</c> and maps 1:1 to the list-view card on <c>/IRF/Index</c>.
    /// </para>
    /// <para><b>Data flow</b></para>
    /// <list type="number">
    ///   <item>SQL builds the row via <c>usp_GetIrfIndexBatch</c>.</item>
    ///   <item><see cref="Risebird.Repository.Indexing.IrfIndexDocumentMapper"/> converts row → this type → Azure payload.</item>
    ///   <item><see cref="Risebird.Repository.IrfListingSearchRepository"/> maps search hits back to the list-view IRF model.</item>
    /// </list>
    /// <para><b>When to extend</b></para>
    /// Add a property here only when the list view or filter panel needs it. Update
    /// <see cref="Risebird.Repository.Indexing.IrfIndexDefinition"/>,
    /// <c>Risebird.DB/Scripts/Create-IrfIndex.ps1</c>, and the stored procedure together so all layers stay aligned.
    /// </summary>
    public class IrfIndexDocument
    {
        /// <summary>SQL primary key (<c>IRF.id</c>). Azure Search document key (<c>irfIntId</c>).</summary>
        public int IrfIntId { get; set; }

        /// <summary>Business skill-channel code shown in the card header (filter: Skill Channel).</summary>
        public string IRFId { get; set; }

        /// <summary>Full JD title; primary skill is parsed from the segment before '|'.</summary>
        public string JDTiltle { get; set; }

        /// <summary>Primary skill extracted from <see cref="JDTiltle"/> (filter + search).</summary>
        public string PrimarySkill { get; set; }

        /// <summary>Client company / project name (filter: Project Name).</summary>
        public string ClientCompName { get; set; }

        /// <summary>End-client name (filter: Client Name).</summary>
        public string ClientName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string JDLink { get; set; }

        public int RisebirdHr { get; set; }

        /// <summary>Channel owner email shown on the list card.</summary>
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

        /// <summary>Card label: Total Candidates.</summary>
        public int? TotalMeetings { get; set; }

        /// <summary>Card label: Selected Candidates.</summary>
        public int? SelectedMeetings { get; set; }

        /// <summary>Card label: Completed Interviews.</summary>
        public int? ScheduledMeetings { get; set; }

        public int FlagCount { get; set; }
        public int? CreatedFromTemplateId { get; set; }
        public string AssessmentGroupId { get; set; }
        public bool IsAIInterview { get; set; }
        public int AIInterviewType { get; set; }
        public int InterviewerBillingCycle { get; set; }
        public DateTime CreatedOn { get; set; }

        /// <summary>When true, internal profiles (Admin, Finance, etc.) can see this IRF without ACL lookup.</summary>
        public bool AllowAllInternal { get; set; } = true;

        /// <summary>Emails allowed to view this IRF (client users, recruiters, HR, etc.).</summary>
        public IList<string> AllowedViewerEmails { get; set; } = new List<string>();

        /// <summary>When true, visible to CuratedUser profile.</summary>
        public bool CuratedLink { get; set; }

        /// <summary>Affinda "Search And Match" embed URL shown on the list card.</summary>
        public string AffindaEmbedUrl { get; set; }

        /// <summary>When the Affinda embed URL was created; used to compute <c>urlCreationTimeElapsed</c> on read.</summary>
        public DateTime? AffindaEmbedUrlCreatedAt { get; set; }
    }
}
