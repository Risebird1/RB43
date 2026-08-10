using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Converts IRF index data between SQL, domain documents, Azure Search payloads, and list-view models.
    /// <para><b>Write path:</b> <see cref="FromDatabaseRow"/> → <see cref="ToAzureDocument"/> → MergeOrUpload</para>
    /// <para><b>Read path:</b> Search hit → <see cref="ToListViewModel"/></para>
    /// </summary>
    internal static class IrfIndexDocumentMapper
    {
        /// <summary>SQL row from <c>usp_GetIrfIndexBatch</c> → in-memory index document.</summary>
        public static IrfIndexDocument FromDatabaseRow(IrfIndexDocumentRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            return new IrfIndexDocument
            {
                IrfIntId = row.IrfIntId,
                IRFId = row.IRFId,
                JDTiltle = row.JDTiltle,
                PrimarySkill = row.PrimarySkill,
                ClientCompName = row.ClientCompName,
                ClientName = row.ClientName,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                JDLink = row.JDLink,
                RisebirdHr = row.RisebirdHr,
                RisebirdHREmail = row.RisebirdHREmail,
                PerMintuteRate = row.PerMintuteRate,
                IsDeleted = row.IsDeleted,
                IsPaused = row.IsPaused,
                IsClientUser = row.IsClientUser,
                TotalInvites = row.TotalInvites,
                TotalBudget = row.TotalBudget,
                TotalBudgetConsumed = row.TotalBudgetConsumed,
                CalendlyLink = row.CalendlyLink,
                CompanyDetails = row.CompanyDetails,
                Discount = row.Discount,
                IsBiometricEnabled = row.IsBiometricEnabled,
                PriorityLevel = row.PriorityLevel,
                ConfirmedMeetings = row.ConfirmedMeetings,
                TotalMeetings = row.TotalMeetings,
                SelectedMeetings = row.SelectedMeetings,
                ScheduledMeetings = row.ScheduledMeetings,
                FlagCount = row.FlagCount,
                CreatedFromTemplateId = row.CreatedFromTemplateId,
                AssessmentGroupId = row.AssessmentGroupId,
                IsAIInterview = row.IsAIInterview,
                AIInterviewType = row.AIInterviewType,
                InterviewerBillingCycle = row.InterviewerBillingCycle,
                CreatedOn = row.CreatedOn,
                AllowAllInternal = row.AllowAllInternal,
                AllowedViewerEmails = ParseViewerEmails(row.AllowedViewerEmails),
                CuratedLink = row.CuratedLink,
                AffindaEmbedUrl = row.AffindaEmbedUrl,
                AffindaEmbedUrlCreatedAt = row.AffindaEmbedUrlCreatedAt
            };
        }

        /// <summary>Index document → JSON payload for Azure Search MergeOrUpload.</summary>
        public static SearchDocument ToAzureDocument(IrfIndexDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var azureDoc = new SearchDocument
            {
                [IrfIndexFields.Key] = document.IrfIntId.ToString(CultureInfo.InvariantCulture),
                [IrfIndexFields.IrfId] = document.IRFId ?? string.Empty,
                [IrfIndexFields.JdTitle] = document.JDTiltle ?? string.Empty,
                [IrfIndexFields.PrimarySkill] = document.PrimarySkill ?? string.Empty,
                [IrfIndexFields.ClientCompName] = document.ClientCompName ?? string.Empty,
                [IrfIndexFields.ClientName] = document.ClientName ?? string.Empty,
                [IrfIndexFields.StartDate] = ToIndexDate(document.StartDate),
                [IrfIndexFields.EndDate] = ToIndexDate(document.EndDate),
                [IrfIndexFields.JdLink] = document.JDLink ?? string.Empty,
                [IrfIndexFields.RisebirdHr] = document.RisebirdHr,
                [IrfIndexFields.RisebirdHrEmail] = document.RisebirdHREmail ?? string.Empty,
                [IrfIndexFields.PerMinuteRate] = document.PerMintuteRate,
                [IrfIndexFields.IsDeleted] = document.IsDeleted,
                [IrfIndexFields.IsPaused] = document.IsPaused,
                [IrfIndexFields.IsClientUser] = false,
                [IrfIndexFields.TotalInvites] = document.TotalInvites,
                [IrfIndexFields.TotalBudget] = document.TotalBudget,
                [IrfIndexFields.TotalBudgetConsumed] = document.TotalBudgetConsumed,
                [IrfIndexFields.CalendlyLink] = document.CalendlyLink ?? string.Empty,
                [IrfIndexFields.CompanyDetails] = document.CompanyDetails ?? string.Empty,
                [IrfIndexFields.Discount] = document.Discount,
                [IrfIndexFields.IsBiometricEnabled] = document.IsBiometricEnabled,
                [IrfIndexFields.PriorityLevel] = document.PriorityLevel ?? 0,
                [IrfIndexFields.ConfirmedMeetings] = document.ConfirmedMeetings ?? 0,
                [IrfIndexFields.TotalMeetings] = document.TotalMeetings ?? 0,
                [IrfIndexFields.SelectedMeetings] = document.SelectedMeetings ?? 0,
                [IrfIndexFields.ScheduledMeetings] = document.ScheduledMeetings ?? 0,
                [IrfIndexFields.FlagCount] = document.FlagCount,
                [IrfIndexFields.CreatedFromTemplateId] = document.CreatedFromTemplateId ?? 0,
                [IrfIndexFields.AssessmentGroupId] = document.AssessmentGroupId ?? string.Empty,
                [IrfIndexFields.IsAiInterview] = document.IsAIInterview,
                [IrfIndexFields.AiInterviewType] = document.AIInterviewType,
                [IrfIndexFields.InterviewerBillingCycle] = document.InterviewerBillingCycle,
                [IrfIndexFields.CreatedOn] = ToIndexDate(document.CreatedOn),
                [IrfIndexFields.AllowAllInternal] = document.AllowAllInternal,
                [IrfIndexFields.AllowedViewerEmails] = document.AllowedViewerEmails?.ToList() ?? new List<string>(),
                [IrfIndexFields.CuratedLink] = document.CuratedLink
            };

            // Omit Affinda fields when empty so MergeOrUpload still succeeds if the live
            // index was created before those fields existed (unknown fields fail the whole upsert).
            if (!string.IsNullOrWhiteSpace(document.AffindaEmbedUrl))
                azureDoc[IrfIndexFields.AffindaEmbedUrl] = document.AffindaEmbedUrl;

            if (document.AffindaEmbedUrlCreatedAt.HasValue && document.AffindaEmbedUrlCreatedAt.Value != default)
                azureDoc[IrfIndexFields.AffindaEmbedUrlCreatedAt] = ToIndexDate(document.AffindaEmbedUrlCreatedAt.Value);

            return azureDoc;
        }

        /// <summary>Azure Search hit → <c>/IRF/Index</c> list card model.</summary>
        public static IRF ToListViewModel(SearchDocument document, string profileName)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return new IRF
            {
                id = AzureSearchFieldReader.GetInt(document, IrfIndexFields.Key),
                IRFId = AzureSearchFieldReader.GetString(document, IrfIndexFields.IrfId),
                JDTiltle = AzureSearchFieldReader.GetString(document, IrfIndexFields.JdTitle),
                ClientName = AzureSearchFieldReader.GetString(document, IrfIndexFields.ClientName),
                ClientCompName = AzureSearchFieldReader.GetString(document, IrfIndexFields.ClientCompName),
                StartDate = AzureSearchFieldReader.GetDateTime(document, IrfIndexFields.StartDate),
                EndDate = AzureSearchFieldReader.GetDateTime(document, IrfIndexFields.EndDate),
                JDLink = AzureSearchFieldReader.GetString(document, IrfIndexFields.JdLink),
                RisebirdHr = AzureSearchFieldReader.GetInt(document, IrfIndexFields.RisebirdHr),
                RisebirdHREmail = AzureSearchFieldReader.GetString(document, IrfIndexFields.RisebirdHrEmail),
                PerMintuteRate = AzureSearchFieldReader.GetDouble(document, IrfIndexFields.PerMinuteRate),
                IsDeleted = AzureSearchFieldReader.GetBool(document, IrfIndexFields.IsDeleted),
                IsPaused = AzureSearchFieldReader.GetBool(document, IrfIndexFields.IsPaused),
                // IsClientUser is profile UI state, not an IRF attribute — never trust the indexed value.
                IsClientUser = string.Equals(profileName, "Client", StringComparison.OrdinalIgnoreCase),
                TotalInvites = AzureSearchFieldReader.GetInt(document, IrfIndexFields.TotalInvites),
                TotalBudget = AzureSearchFieldReader.GetInt(document, IrfIndexFields.TotalBudget),
                TotalBudgetConsumed = (float)AzureSearchFieldReader.GetDouble(document, IrfIndexFields.TotalBudgetConsumed),
                CalendlyLink = AzureSearchFieldReader.GetString(document, IrfIndexFields.CalendlyLink),
                CompanyDetails = AzureSearchFieldReader.GetString(document, IrfIndexFields.CompanyDetails),
                Discount = AzureSearchFieldReader.GetDouble(document, IrfIndexFields.Discount),
                IsBiometricEnabled = AzureSearchFieldReader.GetBool(document, IrfIndexFields.IsBiometricEnabled),
                PriorityLevel = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.PriorityLevel),
                ConfirmedMeetings = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.ConfirmedMeetings),
                TotalMeetings = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.TotalMeetings),
                SelectedMeetings = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.SelectedMeetings),
                ScheduledMeetings = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.ScheduledMeetings),
                flagcount = AzureSearchFieldReader.GetInt(document, IrfIndexFields.FlagCount),
                CreatedFromTemplateId = AzureSearchFieldReader.GetNullableInt(document, IrfIndexFields.CreatedFromTemplateId),
                AssessmentGroupId = AzureSearchFieldReader.GetString(document, IrfIndexFields.AssessmentGroupId),
                IsAIInterview = AzureSearchFieldReader.GetBool(document, IrfIndexFields.IsAiInterview),
                AIInterviewType = AzureSearchFieldReader.GetInt(document, IrfIndexFields.AiInterviewType),
                InterviewerBillingCycle = AzureSearchFieldReader.GetInt(document, IrfIndexFields.InterviewerBillingCycle),
                CreatedOn = AzureSearchFieldReader.GetDateTime(document, IrfIndexFields.CreatedOn),
                CuratedLink = AzureSearchFieldReader.GetBool(document, IrfIndexFields.CuratedLink),
                AffindaEmbedUrl = AzureSearchFieldReader.GetString(document, IrfIndexFields.AffindaEmbedUrl),
                AffindaEmbedUrlCreatedAt = AzureSearchFieldReader.GetNullableDateTime(document, IrfIndexFields.AffindaEmbedUrlCreatedAt),
                urlCreationTimeElapsed = ComputeUrlCreationTimeElapsed(
                    AzureSearchFieldReader.GetNullableDateTime(document, IrfIndexFields.AffindaEmbedUrlCreatedAt))
            };
        }

        /// <summary>Splits pipe-delimited viewer emails from SQL into a distinct list.</summary>
        public static IList<string> ParseViewerEmails(string pipeDelimitedEmails)
        {
            if (string.IsNullOrWhiteSpace(pipeDelimitedEmails))
                return new List<string>();

            return pipeDelimitedEmails
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DateTimeOffset ToIndexDate(DateTime value)
        {
            // Azure Search rejects DateTimeOffset.MinValue on some service versions.
            if (value == default || value < new DateTime(1900, 1, 1))
                return new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            if (value.Kind == DateTimeKind.Unspecified)
                return new DateTimeOffset(value, TimeSpan.Zero);

            return new DateTimeOffset(value.ToUniversalTime());
        }

        /// <summary>Matches <c>usp_GetAllIRF</c>: DATEDIFF(minute, AffindaEmbedUrlCreatedAt, now).</summary>
        private static int? ComputeUrlCreationTimeElapsed(DateTime? createdAt)
        {
            if (!createdAt.HasValue || createdAt.Value == default)
                return null;

            return (int)(DateTime.Now - createdAt.Value).TotalMinutes;
        }
    }
}
