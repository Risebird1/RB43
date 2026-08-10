using System;

namespace Risebird.Repository.Indexing
{
    /// <summary>Dapper row shape from <c>usp_GetTemplateIndexBatch</c>.</summary>
    internal sealed class TemplateIndexDocumentRow
    {
        public int ClonableSkillChannelId { get; set; }
        public string Name { get; set; }
        public int CreatedFromIRFId { get; set; }
        public string MeetingPlatform { get; set; }
        public int? CompanyTypeId { get; set; }
        public string SeniorityLevelId { get; set; }
        public int? CountryType { get; set; }
        public string AssessmentGroupName { get; set; }
        public int? AssessmentGroupId { get; set; }
        public string DifficultyLevel { get; set; }
        public string EmploymentType { get; set; }
        public int? PerHourRate { get; set; }
        public string CategoryName { get; set; }
        public int SkillChannelCounts { get; set; }
        public string PrimarySkills { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TaggedClients { get; set; }
        public bool IsDeleted { get; set; }
        public string SkillChannelName { get; set; }
        public string ChannelOwner { get; set; }
    }
}
