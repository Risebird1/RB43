using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search index schema for <c>templateindex</c>.
    /// Production indexes are created with <c>Risebird.DB/Scripts/Create-TemplateIndex.ps1</c>.
    /// </summary>
    internal static class TemplateIndexDefinition
    {
        public const string DefaultIndexName = "templateindex";
        public const int DefaultBatchSize = 500;
        public const int DefaultPageSize = 50;

        public static SearchIndex CreateIndex(string indexName)
        {
            return new SearchIndex(indexName)
            {
                Fields =
                {
                    KeyField(TemplateIndexFields.Key),
                    SearchFilterSort(TemplateIndexFields.Name),
                    IntFilter(TemplateIndexFields.CreatedFromIRFId),
                    SearchFilter(TemplateIndexFields.MeetingPlatform),
                    IntFilter(TemplateIndexFields.CompanyTypeId),
                    SearchFilterSort(TemplateIndexFields.SeniorityLevelId),
                    IntFilter(TemplateIndexFields.CountryType),
                    SearchFilterFacet(TemplateIndexFields.AssessmentGroupName),
                    IntFilter(TemplateIndexFields.AssessmentGroupId),
                    SearchFilterFacet(TemplateIndexFields.DifficultyLevel),
                    SearchFilterFacet(TemplateIndexFields.EmploymentType),
                    IntFilter(TemplateIndexFields.PerHourRate),
                    SearchFilterFacet(TemplateIndexFields.CategoryName),
                    IntSort(TemplateIndexFields.SkillChannelCounts),
                    SearchOnly(TemplateIndexFields.PrimarySkills),
                    DateField(TemplateIndexFields.CreatedOn),
                    SearchOnly(TemplateIndexFields.TaggedClients),
                    BoolFilter(TemplateIndexFields.IsDeleted),
                    SearchFilterSort(TemplateIndexFields.SkillChannelName),
                    SearchFilterSort(TemplateIndexFields.ChannelOwner)
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

        private static SimpleField DateField(string name) =>
            new SimpleField(name, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true };

        private static SimpleField IntFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true };

        private static SimpleField IntSort(string name) =>
            new SimpleField(name, SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true };

        private static SimpleField BoolFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Boolean) { IsFilterable = true };
    }
}
