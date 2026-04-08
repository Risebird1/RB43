using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClonableSkillChannelVM : IRFVm
    {
        public int ClonableSkillChannelId { get; set; }
        public string Name { get; set; }
        public string NameWithoutHtml { get; set; }
        public int CreatedFromIRFId { get; set; }
        public bool IsTemplateJoinSelectGenskillsRequired { get; set; }
        public SelectList SeniorityLevelList { get; set; }
        //public SelectList CompanyTypeList { get; set; }
        public SelectList CompetencyLevelList { get; set; }
        public SelectList Level { get; set; }
        //public SelectList LevelTech { get; set; }
        public SelectList Skills { get; set; }
        public new string CreatedBy { get; set; }
        public new string ModifiedBy { get; set; }
        public string CompetencyLevelDesc { get; set; }
        public string SeniorityLevelDesc { get; set; }
        public string CountryTypeDesc { get; set; }
        public string CompanyTypeDesc { get; set; }
        public int? CompanyTypeId { get; set; }
        public int? CompetencyLevelId { get; set; }
        //public int? SeniorityLevelId { get; set; }
        public int? CountryType { get; set; }
        public string SeniorityLevelId { get; set; }
        public string DifficultyLevel { get; set; }
        public string EmploymentType { get; set; }
        public int? PerHourRate { get; set; }

        [BindProperty]
        new public string MeetingPlatform { get; set; } = MeetingPlatforms.Zoho;

        //public string[] MeetingPlatformOptions = new[] { MeetingPlatforms.Zoom, MeetingPlatforms.Zoho };
        public int SkillChannelCounts { get; set; }
        public string SkillChannelName { get; set; }
        public string ChannelOwner { get; set; }
        public int InterviewCounts { get; set; }
        public string PrimarySkills { get; set; }
        public string TaggedClients { get; set; }
        //public string Heuristicsscore { get; set; }
        //public string JobCountry { get; set; }
        //public string CompanyType { get; set; }
        new public bool IsDeleted { get; set; }

    }

    /// <summary>
    /// Represents a MasterAssessmentGroup that doesn't have a template.
    /// Used by the bulk template creation feature.
    /// </summary>
    public class MAGWithoutTemplateVM
    {
        public int MasterAssessmentGroupId { get; set; }
        public string MAGName { get; set; }
        public int AssessmentGroupId { get; set; }
        public string AssessmentGroupName { get; set; }
        public string PrimarySkills { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Result of bulk template creation operation.
    /// </summary>
    public class BulkTemplateCreationResult
    {
        public int TotalMAGs { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkTemplateCreationDetail> Details { get; set; } = new List<BulkTemplateCreationDetail>();
    }

    /// <summary>
    /// Detail of a single template creation attempt.
    /// </summary>
    public class BulkTemplateCreationDetail
    {
        public int AssessmentGroupId { get; set; }
        public string AssessmentGroupName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

