using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search index schema for <c>irfindex</c>.
    /// <para>
    /// Production indexes are created with <c>Risebird.DB/Scripts/Create-IrfIndex.ps1</c>.
    /// Field names come from <see cref="IrfIndexFields"/>. The app calls <see cref="CreateIndex"/>
    /// only when <c>AzureSearch:IrfListing:ManageIndexSchema=true</c> (dev/local).
    /// </para>
    /// </summary>
    internal static class IrfIndexDefinition
    {
        public const string DefaultIndexName = "irfindex";
        public const int DefaultBatchSize = 500;

        /// <inheritdoc cref="IrfIndexFields.ListViewSearch"/>
        public static System.Collections.Generic.IReadOnlyList<string> ListViewSearchFields =>
            IrfIndexFields.ListViewSearch;

        /// <summary>Builds the Azure index model for CreateOrUpdateIndexAsync.</summary>
        public static SearchIndex CreateIndex(string indexName)
        {
            return new SearchIndex(indexName)
            {
                Fields =
                {
                    // Identity & list card
                    KeyField(IrfIndexFields.Key),
                    SearchFilterSort(IrfIndexFields.IrfId, facet: false),
                    SearchFilter(IrfIndexFields.JdTitle),
                    SearchFilterFacet(IrfIndexFields.PrimarySkill),
                    SearchFilterFacet(IrfIndexFields.ClientCompName),
                    SearchFilterFacet(IrfIndexFields.ClientName),
                    DateField(IrfIndexFields.StartDate),
                    DateField(IrfIndexFields.EndDate),
                    SearchFilter(IrfIndexFields.RisebirdHrEmail),

                    // Metrics shown on list cards
                    IntFilterSort(IrfIndexFields.TotalMeetings),
                    IntFilterSort(IrfIndexFields.ScheduledMeetings),
                    IntFilterSort(IrfIndexFields.SelectedMeetings),
                    IntFilterSort(IrfIndexFields.ConfirmedMeetings),
                    IntFilterSort(IrfIndexFields.FlagCount),

                    // Status & visibility (OData filters)
                    BoolFilter(IrfIndexFields.IsDeleted),
                    BoolFilter(IrfIndexFields.IsPaused),
                    BoolFilter(IrfIndexFields.AllowAllInternal),
                    BoolFilter(IrfIndexFields.CuratedLink),
                    new SearchField(IrfIndexFields.AllowedViewerEmails, SearchFieldDataType.Collection(SearchFieldDataType.String))
                    {
                        IsFilterable = true
                    },

                    // Additional IRF attributes (retrieved for detail mapping)
                    SearchOnly(IrfIndexFields.JdLink),
                    IntFilter(IrfIndexFields.RisebirdHr),
                    DoubleFilterSort(IrfIndexFields.PerMinuteRate),
                    BoolFilter(IrfIndexFields.IsClientUser),
                    IntFilterSort(IrfIndexFields.TotalInvites),
                    IntFilterSort(IrfIndexFields.TotalBudget),
                    DoubleFilterSort(IrfIndexFields.TotalBudgetConsumed),
                    SearchOnly(IrfIndexFields.CalendlyLink),
                    SearchOnly(IrfIndexFields.CompanyDetails),
                    DoubleFilter(IrfIndexFields.Discount),
                    BoolFilter(IrfIndexFields.IsBiometricEnabled),
                    IntFilterSort(IrfIndexFields.PriorityLevel),
                    IntFilter(IrfIndexFields.CreatedFromTemplateId),
                    SearchFilterNoSort(IrfIndexFields.AssessmentGroupId),
                    BoolFilter(IrfIndexFields.IsAiInterview),
                    IntFilter(IrfIndexFields.AiInterviewType),
                    IntFilter(IrfIndexFields.InterviewerBillingCycle),
                    DateField(IrfIndexFields.CreatedOn),
                    SearchOnly(IrfIndexFields.AffindaEmbedUrl),
                    DateField(IrfIndexFields.AffindaEmbedUrlCreatedAt)
                }
            };
        }

        private static SimpleField KeyField(string name) =>
            new SimpleField(name, SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterSort(string name, bool facet) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = true, IsFacetable = facet };

        private static SearchableField SearchFilter(string name) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = true };

        private static SearchableField SearchFilterNoSort(string name) =>
            new SearchableField(name) { IsFilterable = true, IsSortable = false };

        private static SearchableField SearchFilterFacet(string name) =>
            new SearchableField(name) { IsFilterable = true, IsFacetable = true, IsSortable = true };

        private static SearchableField SearchOnly(string name) => new SearchableField(name);

        private static SimpleField DateField(string name) =>
            new SimpleField(name, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true };

        private static SimpleField IntFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true };

        private static SimpleField IntFilterSort(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true };

        private static SimpleField DoubleFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Double) { IsFilterable = true };

        private static SimpleField DoubleFilterSort(string name) =>
            new SimpleField(name, SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true };

        private static SimpleField BoolFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Boolean) { IsFilterable = true };
    }
}
