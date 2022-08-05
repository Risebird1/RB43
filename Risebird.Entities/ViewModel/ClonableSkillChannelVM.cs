using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;

namespace Risebird.Entities.ViewModel
{
    public class ClonableSkillChannelVM : IRFVm
    {
        public int ClonableSkillChannelId { get; set; }
        public string Name { get; set; }
        public int CreatedFromIRFId { get; set; }
        public bool IsTemplateJoinSelectGenskillsRequired { get; set; }
        public SelectList SeniorityLevelList { get; set; }
        //public SelectList CompanyTypeList { get; set; }
        public SelectList CompetencyLevelList { get; set; }
        public SelectList Level { get; set; }
        //public SelectList LevelTech { get; set; }
        public SelectList Skills { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
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
        new public string MeetingPlatform { get; set; } = MeetingPlatforms.Zoom;

        public string[] MeetingPlatformOptions = new[] { MeetingPlatforms.Zoom, MeetingPlatforms.RisebirdMeet };
        public int SkillChannelCounts { get; set; }
        public int InterviewCounts { get; set; }
        public string PrimarySkills { get; set; }
        public string TaggedClients { get; set; }
    }
}

