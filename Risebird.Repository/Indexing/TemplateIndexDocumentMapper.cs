using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Search;
using System;
using System.Globalization;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Converts template (ClonableSkillChannel) index data between SQL, domain documents, Azure payloads, and list VMs.
    /// </summary>
    internal static class TemplateIndexDocumentMapper
    {
        public static TemplateIndexDocument FromDatabaseRow(TemplateIndexDocumentRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            return new TemplateIndexDocument
            {
                ClonableSkillChannelId = row.ClonableSkillChannelId,
                Name = row.Name,
                CreatedFromIRFId = row.CreatedFromIRFId,
                MeetingPlatform = row.MeetingPlatform,
                CompanyTypeId = row.CompanyTypeId,
                SeniorityLevelId = row.SeniorityLevelId,
                CountryType = row.CountryType.GetValueOrDefault(),
                AssessmentGroupName = row.AssessmentGroupName,
                AssessmentGroupId = row.AssessmentGroupId,
                DifficultyLevel = row.DifficultyLevel,
                EmploymentType = row.EmploymentType,
                PerHourRate = row.PerHourRate,
                CategoryName = row.CategoryName,
                SkillChannelCounts = row.SkillChannelCounts,
                PrimarySkills = row.PrimarySkills,
                CreatedOn = row.CreatedOn,
                TaggedClients = row.TaggedClients,
                IsDeleted = row.IsDeleted,
                SkillChannelName = row.SkillChannelName,
                ChannelOwner = row.ChannelOwner
            };
        }

        public static SearchDocument ToAzureDocument(TemplateIndexDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var azureDoc = new SearchDocument
            {
                [TemplateIndexFields.Key] = document.ClonableSkillChannelId.ToString(CultureInfo.InvariantCulture),
                [TemplateIndexFields.Name] = document.Name ?? string.Empty,
                [TemplateIndexFields.CreatedFromIRFId] = document.CreatedFromIRFId,
                [TemplateIndexFields.MeetingPlatform] = document.MeetingPlatform ?? string.Empty,
                [TemplateIndexFields.CompanyTypeId] = document.CompanyTypeId.GetValueOrDefault(),
                [TemplateIndexFields.SeniorityLevelId] = document.SeniorityLevelId ?? string.Empty,
                [TemplateIndexFields.CountryType] = document.CountryType,
                [TemplateIndexFields.AssessmentGroupName] = document.AssessmentGroupName ?? string.Empty,
                [TemplateIndexFields.AssessmentGroupId] = document.AssessmentGroupId.GetValueOrDefault(),
                [TemplateIndexFields.DifficultyLevel] = document.DifficultyLevel ?? string.Empty,
                [TemplateIndexFields.EmploymentType] = document.EmploymentType ?? string.Empty,
                [TemplateIndexFields.PerHourRate] = document.PerHourRate.GetValueOrDefault(),
                [TemplateIndexFields.CategoryName] = document.CategoryName ?? string.Empty,
                [TemplateIndexFields.SkillChannelCounts] = document.SkillChannelCounts,
                [TemplateIndexFields.PrimarySkills] = document.PrimarySkills ?? string.Empty,
                [TemplateIndexFields.CreatedOn] = ToIndexDate(document.CreatedOn),
                [TemplateIndexFields.TaggedClients] = document.TaggedClients ?? string.Empty,
                [TemplateIndexFields.IsDeleted] = document.IsDeleted,
                [TemplateIndexFields.SkillChannelName] = document.SkillChannelName ?? string.Empty,
                [TemplateIndexFields.ChannelOwner] = document.ChannelOwner ?? string.Empty
            };

            return azureDoc;
        }

        public static ClonableSkillChannelVM ToListViewModel(SearchDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var assessmentGroupId = AzureSearchFieldReader.GetNullableInt(document, TemplateIndexFields.AssessmentGroupId);

            return new ClonableSkillChannelVM
            {
                ClonableSkillChannelId = AzureSearchFieldReader.GetInt(document, TemplateIndexFields.Key),
                Name = AzureSearchFieldReader.GetString(document, TemplateIndexFields.Name),
                CreatedFromIRFId = AzureSearchFieldReader.GetInt(document, TemplateIndexFields.CreatedFromIRFId),
                MeetingPlatform = AzureSearchFieldReader.GetString(document, TemplateIndexFields.MeetingPlatform),
                CompanyTypeId = AzureSearchFieldReader.GetNullableInt(document, TemplateIndexFields.CompanyTypeId),
                SeniorityLevelId = AzureSearchFieldReader.GetString(document, TemplateIndexFields.SeniorityLevelId),
                CountryType = AzureSearchFieldReader.GetNullableInt(document, TemplateIndexFields.CountryType),
                AssessmentGroupName = AzureSearchFieldReader.GetString(document, TemplateIndexFields.AssessmentGroupName),
                AssessmentGroupId = assessmentGroupId.HasValue ? assessmentGroupId.Value.ToString(CultureInfo.InvariantCulture) : null,
                DifficultyLevel = AzureSearchFieldReader.GetString(document, TemplateIndexFields.DifficultyLevel),
                EmploymentType = AzureSearchFieldReader.GetString(document, TemplateIndexFields.EmploymentType),
                PerHourRate = AzureSearchFieldReader.GetNullableInt(document, TemplateIndexFields.PerHourRate),
                CategoryName = AzureSearchFieldReader.GetString(document, TemplateIndexFields.CategoryName),
                SkillChannelCounts = AzureSearchFieldReader.GetInt(document, TemplateIndexFields.SkillChannelCounts),
                PrimarySkills = AzureSearchFieldReader.GetString(document, TemplateIndexFields.PrimarySkills),
                CreatedOn = AzureSearchFieldReader.GetDateTime(document, TemplateIndexFields.CreatedOn),
                TaggedClients = AzureSearchFieldReader.GetString(document, TemplateIndexFields.TaggedClients),
                IsDeleted = AzureSearchFieldReader.GetBool(document, TemplateIndexFields.IsDeleted),
                SkillChannelName = AzureSearchFieldReader.GetString(document, TemplateIndexFields.SkillChannelName),
                ChannelOwner = AzureSearchFieldReader.GetString(document, TemplateIndexFields.ChannelOwner)
            };
        }

        private static DateTimeOffset ToIndexDate(DateTime value)
        {
            if (value == default)
                return DateTimeOffset.MinValue;

            if (value.Kind == DateTimeKind.Unspecified)
                return new DateTimeOffset(value, TimeSpan.Zero);

            return new DateTimeOffset(value.ToUniversalTime());
        }
    }
}
