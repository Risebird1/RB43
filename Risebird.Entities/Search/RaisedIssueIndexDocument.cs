using System;

namespace Risebird.Entities.Search
{
    /// <summary>
    /// Denormalized raised-issue (support ticket) document for Azure AI Search index <c>raisedissueindex</c>.
    /// Mirrors <c>usp_GetRaisedIssueIndexBatch</c> / <c>/Support/RaisedRequests</c> list row shape.
    /// </summary>
    public class RaisedIssueIndexDocument
    {
        /// <summary>Azure Search document key (<c>RaisedIssues.id</c>).</summary>
        public int Id { get; set; }

        public string RequestType { get; set; }
        public string Comment { get; set; }
        public string AttachmentURL { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedOn { get; set; }
        public string Status { get; set; }
        public string StatusComment { get; set; }
        public string ClosedBy { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string Requester { get; set; }
        public int? InterviewId { get; set; }
        public string AIReviewResponse { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public int? IrfIntId { get; set; }
        public string CandidateEmail { get; set; }
        public string Interviewer { get; set; }

        /// <summary>True when the ticket has at least one 'Reopened' history entry.</summary>
        public bool IsReopened { get; set; }
    }
}
