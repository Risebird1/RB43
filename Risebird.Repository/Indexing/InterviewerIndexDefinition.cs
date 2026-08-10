using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search index schema for <c>interviewerindex</c>.
    /// Production indexes are created with <c>Risebird.DB/Scripts/Create-InterviewerIndex.ps1</c>.
    /// </summary>
    internal static class InterviewerIndexDefinition
    {
        public const string DefaultIndexName = "interviewerindex";
        public const int DefaultBatchSize = 500;
        public const int DefaultPageSize = 10;

        public static SearchIndex CreateIndex(string indexName)
        {
            return new SearchIndex(indexName)
            {
                Fields =
                {
                    KeyField(InterviewerIndexFields.Key),
                    IntFilter(InterviewerIndexFields.UserId),
                    SearchFilterSort(InterviewerIndexFields.FirstName),
                    SearchFilterSort(InterviewerIndexFields.LastName),
                    SearchFilterSort(InterviewerIndexFields.NickName),
                    SearchFilterSort(InterviewerIndexFields.EmailId),
                    SearchFilter(InterviewerIndexFields.MobileNumber),
                    SearchFilterFacet(InterviewerIndexFields.PrimarySkill),
                    CollectionFilter(InterviewerIndexFields.Skills),
                    SearchFilter(InterviewerIndexFields.AssessmentGroupId),
                    CollectionFilter(InterviewerIndexFields.AssessmentGroupNames),
                    SearchFilterFacet(InterviewerIndexFields.Senioritylevel),
                    SearchFilterFacet(InterviewerIndexFields.PerHourRateIndia),
                    SearchFilterFacet(InterviewerIndexFields.PerHourRateOutside),
                    SearchFilterFacet(InterviewerIndexFields.CompentencyLevel),
                    SearchFilterFacet(InterviewerIndexFields.ProfileType),
                    SearchFilterFacet(InterviewerIndexFields.CompanyType),
                    SearchFilterFacet(InterviewerIndexFields.Country),
                    BoolFilter(InterviewerIndexFields.IsLeadInterviewer),
                    BoolFilter(InterviewerIndexFields.IsActive),
                    BoolFilter(InterviewerIndexFields.IsInterviewerAccess),
                    BoolFilter(InterviewerIndexFields.IsApproved),
                    BoolFilter(InterviewerIndexFields.IsInterested),
                    BoolFilter(InterviewerIndexFields.IsDeleted),
                    IntFilter(InterviewerIndexFields.Isavailable),
                    SearchFilter(InterviewerIndexFields.AvailabilityStatusValue),
                    IntFilter(InterviewerIndexFields.VideoConfirmation),
                    SearchOnly(InterviewerIndexFields.ResumeLink),
                    SearchOnly(InterviewerIndexFields.CodedResumeLink),
                    SearchFilter(InterviewerIndexFields.City),
                    SearchFilter(InterviewerIndexFields.CurrentRole),
                    DoubleFilter(InterviewerIndexFields.Experience),
                    SearchFilter(InterviewerIndexFields.PreferredDomain),
                    SearchOnly(InterviewerIndexFields.UniqueId),
                    DateField(InterviewerIndexFields.AvailabilityResponseDate),
                    BoolFilter(InterviewerIndexFields.IsHoldPayment)
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

        private static SimpleField DoubleFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true };

        private static SimpleField BoolFilter(string name) =>
            new SimpleField(name, SearchFieldDataType.Boolean) { IsFilterable = true };

        private static SearchField CollectionFilter(string name) =>
            new SearchField(name, SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFilterable = true,
                IsSearchable = true
            };
    }
}
