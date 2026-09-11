using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Search;
using System;
using System.Globalization;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Converts raised-issue index data between SQL, domain documents, Azure payloads, and list VMs.
    /// </summary>
    internal static class RaisedIssueIndexDocumentMapper
    {
        public static RaisedIssueIndexDocument FromDatabaseRow(RaisedIssueIndexDocumentRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            return new RaisedIssueIndexDocument
            {
                Id = row.Id,
                RequestType = row.RequestType,
                Comment = row.Comment,
                AttachmentURL = row.AttachmentURL,
                RequestedBy = row.RequestedBy,
                RequestedOn = row.RequestedOn,
                Status = row.Status,
                StatusComment = row.StatusComment,
                ClosedBy = row.ClosedBy,
                ClosedOn = row.ClosedOn,
                Requester = row.Requester,
                InterviewId = row.InterviewId,
                AIReviewResponse = row.AIReviewResponse,
                MobileNumber = row.MobileNumber,
                Name = row.Name,
                IrfIntId = row.IrfIntId,
                CandidateEmail = row.CandidateEmail,
                Interviewer = row.Interviewer,
                IsReopened = row.IsReopened
            };
        }

        public static SearchDocument ToAzureDocument(RaisedIssueIndexDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var azureDoc = new SearchDocument
            {
                [RaisedIssueIndexFields.Key] = document.Id.ToString(CultureInfo.InvariantCulture),
                [RaisedIssueIndexFields.RequestType] = document.RequestType ?? string.Empty,
                [RaisedIssueIndexFields.Comment] = TruncateForIndex(document.Comment),
                [RaisedIssueIndexFields.AttachmentURL] = document.AttachmentURL ?? string.Empty,
                [RaisedIssueIndexFields.RequestedBy] = document.RequestedBy ?? string.Empty,
                [RaisedIssueIndexFields.RequestedOn] = ToIndexDate(document.RequestedOn),
                [RaisedIssueIndexFields.Status] = document.Status ?? string.Empty,
                [RaisedIssueIndexFields.StatusComment] = TruncateForIndex(document.StatusComment),
                [RaisedIssueIndexFields.ClosedBy] = document.ClosedBy ?? string.Empty,
                // Written even when the ticket is open: MergeOrUpload leaves an omitted field
                // untouched, so a reopened ticket would keep the closedOn from its last closure.
                [RaisedIssueIndexFields.ClosedOn] = document.ClosedOn.HasValue
                    ? ToIndexDate(document.ClosedOn.Value)
                    : (DateTimeOffset?)null,
                [RaisedIssueIndexFields.Requester] = document.Requester ?? string.Empty,
                [RaisedIssueIndexFields.InterviewId] = document.InterviewId.GetValueOrDefault(),
                [RaisedIssueIndexFields.AIReviewResponse] = TruncateForIndex(document.AIReviewResponse),
                [RaisedIssueIndexFields.MobileNumber] = document.MobileNumber ?? string.Empty,
                [RaisedIssueIndexFields.Name] = document.Name ?? string.Empty,
                [RaisedIssueIndexFields.IrfIntId] = document.IrfIntId.GetValueOrDefault(),
                [RaisedIssueIndexFields.CandidateEmail] = document.CandidateEmail ?? string.Empty,
                [RaisedIssueIndexFields.Interviewer] = document.Interviewer ?? string.Empty,
                [RaisedIssueIndexFields.IsReopened] = document.IsReopened
            };

            return azureDoc;
        }

        public static RaiseBugsVM ToListViewModel(SearchDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return new RaiseBugsVM
            {
                id = AzureSearchFieldReader.GetInt(document, RaisedIssueIndexFields.Key),
                RequestType = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.RequestType),
                Comment = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.Comment),
                AttachmentURL = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.AttachmentURL),
                RequestedBy = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.RequestedBy),
                RequestedOn = AzureSearchFieldReader.GetDateTime(document, RaisedIssueIndexFields.RequestedOn),
                Status = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.Status),
                StatusComment = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.StatusComment),
                ClosedBy = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.ClosedBy),
                ClosedOn = AzureSearchFieldReader.GetNullableDateTime(document, RaisedIssueIndexFields.ClosedOn),
                Requester = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.Requester),
                interviewId = AzureSearchFieldReader.GetInt(document, RaisedIssueIndexFields.InterviewId),
                MobileNumber = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.MobileNumber),
                Name = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.Name),
                irfIntId = AzureSearchFieldReader.GetInt(document, RaisedIssueIndexFields.IrfIntId),
                candidateEmail = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.CandidateEmail),
                Interviewer = AzureSearchFieldReader.GetString(document, RaisedIssueIndexFields.Interviewer),
                IsReopened = AzureSearchFieldReader.GetBool(document, RaisedIssueIndexFields.IsReopened)
            };
        }

        private static DateTimeOffset ToIndexDate(DateTime value)
        {
            if (value == default)
                return DateTimeOffset.MinValue;

            if (value.Kind == DateTimeKind.Unspecified)
                return new DateTimeOffset(value, TimeSpan.Zero);

            return new DateTimeOffset(value.ToUniversalTime());
        }

        /// <summary>Azure Search string fields are limited to 32 KB per value.</summary>
        private static string TruncateForIndex(string value, int maxChars = 32000)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
                return value ?? string.Empty;

            return value.Substring(0, maxChars);
        }
    }
}
