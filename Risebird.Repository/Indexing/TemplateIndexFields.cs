using System.Collections.Generic;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search field names for the clonable skill channel (template) index (<c>templateindex</c>).
    /// </summary>
    internal static class TemplateIndexFields
    {
        public const string Key = "clonableSkillChannelId";
        public const string Name = "name";
        public const string CreatedFromIRFId = "createdFromIRFId";
        public const string MeetingPlatform = "meetingPlatform";
        public const string CompanyTypeId = "companyTypeId";
        public const string SeniorityLevelId = "seniorityLevelId";
        public const string CountryType = "countryType";
        public const string AssessmentGroupName = "assessmentGroupName";
        public const string AssessmentGroupId = "assessmentGroupId";
        public const string DifficultyLevel = "difficultyLevel";
        public const string EmploymentType = "employmentType";
        public const string PerHourRate = "perHourRate";
        public const string CategoryName = "categoryName";
        public const string SkillChannelCounts = "skillChannelCounts";
        public const string PrimarySkills = "primarySkills";
        public const string CreatedOn = "createdOn";
        public const string TaggedClients = "taggedClients";
        public const string IsDeleted = "isDeleted";
        public const string SkillChannelName = "skillChannelName";
        public const string ChannelOwner = "channelOwner";

        public static readonly IReadOnlyList<string> ListViewSearch = new[]
        {
            Name,
            AssessmentGroupName,
            CategoryName,
            PrimarySkills,
            SkillChannelName,
            ChannelOwner,
            TaggedClients
        };

        /// <summary>Fields retrieved for the Templates list views (keep in sync with ToListViewModel).</summary>
        public static readonly IReadOnlyList<string> ListViewSelect = new[]
        {
            Key,
            Name,
            CreatedFromIRFId,
            MeetingPlatform,
            CompanyTypeId,
            SeniorityLevelId,
            CountryType,
            AssessmentGroupName,
            AssessmentGroupId,
            DifficultyLevel,
            EmploymentType,
            PerHourRate,
            CategoryName,
            SkillChannelCounts,
            PrimarySkills,
            CreatedOn,
            TaggedClients,
            IsDeleted,
            SkillChannelName,
            ChannelOwner
        };
    }
}
