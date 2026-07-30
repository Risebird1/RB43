using System;
using System.Collections.Generic;

namespace Risebird.Entities.Search
{
    /// <summary>
    /// Denormalized call catalogue document for Azure AI Search index <c>callcatalogueindex</c>.
    /// One row per candidate IRF+email or per interviewer email (latest call).
    /// </summary>
    public class CallCatalogueIndexDocument
    {
        /// <summary>Composite Azure Search key, e.g. <c>Candidate:123:a@b.com</c>.</summary>
        public string Id { get; set; }

        /// <summary><c>Candidate</c> or <c>Interviewer</c>.</summary>
        public string CatalogueType { get; set; }

        public int IrfIntId { get; set; }
        public string IRFId { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public string JDTiltle { get; set; }
        public string PrimarySkill { get; set; }
        public string ClientName { get; set; }
        public DateTime CalledOn { get; set; }
        public string WhoCalled { get; set; }
        public bool AllowAllInternal { get; set; }
        public IList<string> AllowedViewerEmails { get; set; }
    }
}
