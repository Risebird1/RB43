using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Risebird.Repository.Indexing
{
    internal static class CallCatalogueIndexDefinition
    {
        public const string DefaultIndexName = "callcatalogueindex";
        public const int DefaultBatchSize = 500;
        public const int DefaultPageSize = 50;

        public static SearchIndex CreateIndex(string indexName)
        {
            return new SearchIndex(indexName)
            {
                Fields =
                {
                    KeyField(CallCatalogueIndexFields.Key),
                    SearchFilterFacet(CallCatalogueIndexFields.CatalogueType),
                    IntFilter(CallCatalogueIndexFields.IrfIntId),
                    SearchFilterSort(CallCatalogueIndexFields.IRFId),
                    SearchFilterSort(CallCatalogueIndexFields.CandidateEmail),
                    SearchFilterSort(CallCatalogueIndexFields.CandidateName),
                    SearchOnly(CallCatalogueIndexFields.JDTiltle),
                    SearchFilterFacet(CallCatalogueIndexFields.PrimarySkill),
                    SearchFilterFacet(CallCatalogueIndexFields.ClientName),
                    DateField(CallCatalogueIndexFields.CalledOn),
                    SearchFilterSort(CallCatalogueIndexFields.WhoCalled),
                    BoolFilter(CallCatalogueIndexFields.AllowAllInternal),
                    new SearchField(CallCatalogueIndexFields.AllowedViewerEmails, SearchFieldDataType.Collection(SearchFieldDataType.String))
                    {
                        IsFilterable = true
                    }
                }
            };
        }

        private static SimpleField KeyField(string name) =>
            new SimpleField(name, SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterSort(string name) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterFacet(string name) =>
            new SearchableField(name) { IsFilterable = true, IsFacetable = true, IsSortable = true };

        private static SearchableField SearchOnly(string name) => new SearchableField(name);

        private static SimpleField DateField(string name) =>
            new SimpleField(name, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true };

        private static SimpleField IntFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true };

        private static SimpleField BoolFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Boolean) { IsFilterable = true };
    }
}
