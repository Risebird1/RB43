using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Search;
using System;
using System.Globalization;
using System.Text;

namespace Risebird.Repository.Indexing
{
    internal static class CallCatalogueIndexDocumentMapper
    {
        public static CallCatalogueIndexDocument FromDatabaseRow(CallCatalogueIndexDocumentRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            var catalogueType = row.CatalogueType ?? string.Empty;
            var documentKey = string.Equals(catalogueType, "Interviewer", StringComparison.OrdinalIgnoreCase)
                ? BuildInterviewerDocumentKey(row.CandidateEmail)
                : BuildCandidateDocumentKey(row.IrfIntId, row.CandidateEmail);

            return new CallCatalogueIndexDocument
            {
                Id = documentKey,
                CatalogueType = catalogueType,
                IrfIntId = row.IrfIntId,
                IRFId = row.IRFId,
                CandidateEmail = row.CandidateEmail,
                CandidateName = row.CandidateName,
                JDTiltle = row.JDTiltle,
                PrimarySkill = row.PrimarySkill,
                ClientName = row.ClientName,
                CalledOn = row.CalledOn,
                WhoCalled = row.WhoCalled,
                AllowAllInternal = row.AllowAllInternal,
                AllowedViewerEmails = IrfIndexDocumentMapper.ParseViewerEmails(row.AllowedViewerEmails)
            };
        }

        public static string BuildCandidateDocumentKey(int irfIntId, string candidateEmail) =>
            $"c-{irfIntId.ToString(CultureInfo.InvariantCulture)}-{SanitizeKeyPart(candidateEmail)}";

        public static string BuildInterviewerDocumentKey(string interviewerEmail) =>
            $"i-{SanitizeKeyPart(interviewerEmail)}";

        /// <summary>
        /// Azure Search keys may only contain letters, digits, underscore, dash, or equals.
        /// </summary>
        private static string SanitizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "empty";

            var sb = new StringBuilder(value.Trim().Length * 2);
            foreach (var c in value.Trim().ToLowerInvariant())
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '=')
                    sb.Append(c);
                else if (c == '@')
                    sb.Append("_at_");
                else if (c == '.')
                    sb.Append('_');
                else
                    sb.Append('_');
            }

            return sb.Length == 0 ? "empty" : sb.ToString();
        }
        public static SearchDocument ToAzureDocument(CallCatalogueIndexDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return new SearchDocument
            {
                [CallCatalogueIndexFields.Key] = document.Id,
                [CallCatalogueIndexFields.CatalogueType] = document.CatalogueType ?? string.Empty,
                [CallCatalogueIndexFields.IrfIntId] = document.IrfIntId,
                [CallCatalogueIndexFields.IRFId] = document.IRFId ?? string.Empty,
                [CallCatalogueIndexFields.CandidateEmail] = document.CandidateEmail ?? string.Empty,
                [CallCatalogueIndexFields.CandidateName] = document.CandidateName ?? string.Empty,
                [CallCatalogueIndexFields.JDTiltle] = document.JDTiltle ?? string.Empty,
                [CallCatalogueIndexFields.PrimarySkill] = document.PrimarySkill ?? string.Empty,
                [CallCatalogueIndexFields.ClientName] = document.ClientName ?? string.Empty,
                [CallCatalogueIndexFields.CalledOn] = ToIndexDate(document.CalledOn),
                [CallCatalogueIndexFields.WhoCalled] = document.WhoCalled ?? string.Empty,
                [CallCatalogueIndexFields.AllowAllInternal] = document.AllowAllInternal,
                [CallCatalogueIndexFields.AllowedViewerEmails] = document.AllowedViewerEmails ?? Array.Empty<string>()
            };
        }

        public static CallCatalogueVM ToListViewModel(SearchDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return new CallCatalogueVM
            {
                id = AzureSearchFieldReader.GetInt(document, CallCatalogueIndexFields.IrfIntId),
                IRFId = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.IRFId),
                CandidateEmail = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.CandidateEmail),
                CandidateName = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.CandidateName),
                JDTiltle = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.JDTiltle),
                ClientName = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.ClientName),
                CalledOn = AzureSearchFieldReader.GetDateTime(document, CallCatalogueIndexFields.CalledOn),
                WhoCalled = AzureSearchFieldReader.GetString(document, CallCatalogueIndexFields.WhoCalled)
            };
        }

        private static DateTimeOffset ToIndexDate(DateTime value)
        {
            // Azure Search rejects DateTimeOffset.MinValue on some service versions.
            if (value == default || value < new DateTime(1900, 1, 1))
                return new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            if (value.Kind == DateTimeKind.Unspecified)
                return new DateTimeOffset(value, TimeSpan.Zero);

            return new DateTimeOffset(value.ToUniversalTime());
        }    }
}
