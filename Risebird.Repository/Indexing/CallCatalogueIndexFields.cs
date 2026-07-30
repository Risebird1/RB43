using System.Collections.Generic;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search field names for the call catalogue index (<c>callcatalogueindex</c>).
    /// </summary>
    internal static class CallCatalogueIndexFields
    {
        public const string Key = "id";
        public const string CatalogueType = "catalogueType";
        public const string IrfIntId = "irfIntId";
        public const string IRFId = "irfId";
        public const string CandidateEmail = "candidateEmail";
        public const string CandidateName = "candidateName";
        public const string JDTiltle = "jdTitle";
        public const string PrimarySkill = "primarySkill";
        public const string ClientName = "clientName";
        public const string CalledOn = "calledOn";
        public const string WhoCalled = "whoCalled";
        public const string AllowAllInternal = "allowAllInternal";
        public const string AllowedViewerEmails = "allowedViewerEmails";

        public static readonly IReadOnlyList<string> ListViewSearch = new[]
        {
            IRFId,
            CandidateEmail,
            CandidateName,
            JDTiltle,
            PrimarySkill,
            ClientName,
            WhoCalled
        };

        public static readonly IReadOnlyList<string> ListViewSelect = new[]
        {
            Key,
            CatalogueType,
            IrfIntId,
            IRFId,
            CandidateEmail,
            CandidateName,
            JDTiltle,
            PrimarySkill,
            ClientName,
            CalledOn,
            WhoCalled
        };
    }
}
