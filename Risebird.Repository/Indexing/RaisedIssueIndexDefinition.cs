using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search index schema for <c>raisedissueindex</c>.
    /// Production indexes are created with <c>Risebird.DB/Scripts/Create-RaisedIssueIndex.ps1</c>.
    /// </summary>
    internal static class RaisedIssueIndexDefinition
    {
        public const string DefaultIndexName = "raisedissueindex";
        public const int DefaultBatchSize = 500;
        public const int DefaultPageSize = 50;

        public static SearchIndex CreateIndex(string indexName)
        {
            return new SearchIndex(indexName)
            {
                Fields =
                {
                    KeyField(RaisedIssueIndexFields.Key),
                    SearchFilter(RaisedIssueIndexFields.RequestType),
                    SearchOnly(RaisedIssueIndexFields.Comment),
                    RetrievableOnly(RaisedIssueIndexFields.AttachmentURL),
                    SearchFilterSort(RaisedIssueIndexFields.RequestedBy),
                    DateField(RaisedIssueIndexFields.RequestedOn),
                    SearchFilterFacet(RaisedIssueIndexFields.Status),
                    RetrievableOnly(RaisedIssueIndexFields.StatusComment),
                    SearchFilterSort(RaisedIssueIndexFields.ClosedBy),
                    DateField(RaisedIssueIndexFields.ClosedOn),
                    SearchFilterFacet(RaisedIssueIndexFields.Requester),
                    IntFilter(RaisedIssueIndexFields.InterviewId),
                    RetrievableOnly(RaisedIssueIndexFields.AIReviewResponse),
                    SearchFilter(RaisedIssueIndexFields.MobileNumber),
                    SearchFilterSort(RaisedIssueIndexFields.Name),
                    IntFilter(RaisedIssueIndexFields.IrfIntId),
                    SearchFilterSort(RaisedIssueIndexFields.CandidateEmail),
                    SearchFilterSort(RaisedIssueIndexFields.Interviewer),
                    BoolFilter(RaisedIssueIndexFields.IsReopened)
                }
            };
        }

        private static SimpleField KeyField(string name) =>
            new SimpleField(name, SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterSort(string name) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilter(string name) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterFacet(string name) =>
            new SearchableField(name) { IsFilterable = true, IsFacetable = true, IsSortable = true };

        private static SearchableField SearchOnly(string name) => new SearchableField(name);

        private static SimpleField RetrievableOnly(string name) =>
            new SimpleField(name, SearchFieldDataType.String);

        private static SimpleField DateField(string name) =>
            new SimpleField(name, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true };

        private static SimpleField IntFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true };

        private static SimpleField BoolFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Boolean) { IsFilterable = true };
    }
}
