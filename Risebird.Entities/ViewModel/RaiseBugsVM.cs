using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class RaiseBugsVM
    {
        public int id { get; set; }
        public string RequestType { get; set; }
        public string Comment { get; set; }
        public IFormFile Attachment { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedOn { get; set; }
        public string AttachmentURL { get; set; }
        public string Status { get; set; }
        public string StatusComment { get; set; }
        public string ClosedBy { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string Requester { get; set; }
        public string MobileNumber { get; set; }
        public string Name { get; set; }
        public int interviewId { get; set; }
        public int irfIntId { get; set; }
        public string candidateEmail { get; set; }
        public string Interviewer { get; set; }

        /// <summary>True when the ticket has at least one 'Reopened' history entry.</summary>
        public bool IsReopened { get; set; }

        /// <summary>Populated on the detail/My-Ticket page to show full audit trail.</summary>
        public List<RaisedIssueHistoryVM> History { get; set; }
    }
}
