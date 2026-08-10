using System;

namespace Risebird.Repository.Indexing
{
    /// <summary>Dapper row shape from <c>usp_GetRaisedIssueIndexBatch</c>.</summary>
    internal sealed class RaisedIssueIndexDocumentRow
    {
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
        public bool IsReopened { get; set; }
    }
}
