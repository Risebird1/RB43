using System.Collections.Generic;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search field names for the raised-issue support ticket index (<c>raisedissueindex</c>).
    /// </summary>
    internal static class RaisedIssueIndexFields
    {
        public const string Key = "id";
        public const string RequestType = "requestType";
        public const string Comment = "comment";
        public const string AttachmentURL = "attachmentURL";
        public const string RequestedBy = "requestedBy";
        public const string RequestedOn = "requestedOn";
        public const string Status = "status";
        public const string StatusComment = "statusComment";
        public const string ClosedBy = "closedBy";
        public const string ClosedOn = "closedOn";
        public const string Requester = "requester";
        public const string InterviewId = "interviewId";
        public const string AIReviewResponse = "aiReviewResponse";
        public const string MobileNumber = "mobileNumber";
        public const string Name = "name";
        public const string IrfIntId = "irfIntId";
        public const string CandidateEmail = "candidateEmail";
        public const string Interviewer = "interviewer";
        public const string IsReopened = "isReopened";

        public static readonly IReadOnlyList<string> ListViewSearch = new[]
        {
            RequestedBy,
            Name,
            Comment,
            CandidateEmail,
            Interviewer,
            RequestType
        };

        /// <summary>Fields retrieved for <c>/Support/RaisedRequests</c> (keep in sync with ToListViewModel).</summary>
        public static readonly IReadOnlyList<string> ListViewSelect = new[]
        {
            Key,
            RequestType,
            Comment,
            AttachmentURL,
            RequestedBy,
            RequestedOn,
            Status,
            StatusComment,
            ClosedBy,
            ClosedOn,
            Requester,
            InterviewId,
            AIReviewResponse,
            MobileNumber,
            Name,
            IrfIntId,
            CandidateEmail,
            Interviewer,
            IsReopened
        };
    }
}
