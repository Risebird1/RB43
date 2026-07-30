using System;

namespace Risebird.Repository.Indexing
{
    /// <summary>Dapper row shape from <c>usp_GetCallCatalogueIndexBatch</c>.</summary>
    internal sealed class CallCatalogueIndexDocumentRow
    {
        public string DocumentKey { get; set; }
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
        public string AllowedViewerEmails { get; set; }
    }
}
