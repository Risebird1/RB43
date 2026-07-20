using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IClonableSkillChannelTemplateService
    {
        Task<ClonableSkillChannelVM> GetTemplateDataFromSourceSkillChannel(int irfId, int assessmentGroupId, bool isNewSkillChannel = false);
        Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser);
        Task<ClonableSkillChannelVM> PopulateDropDownsForTemplate(ClonableSkillChannelVM model, bool isEdit = false);
        Task<ClonableSkillChannelVM> GetClonableSkillChannel(int id);

        /// <summary>
        /// Gets all clonable skill channels (templates). When Azure Search (TemplateListing) is enabled,
        /// tries the search index first (with <paramref name="filter"/> applied via OData) and falls back
        /// to SQL when disabled or on error.
        /// </summary>
        Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels(ClonableTemplatesFilter filter = null);
        Task<bool> UpdateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser);
        Task<IRFVm> GetTemplateDataForPopulatingSkillChannel(int id, string clientEmailId = null);
        Task<IRFVm> PopulateDropDownsForSkillChannel(IRFVm model, string clientEmailId = null);
        IEnumerable<TransSkill> GetClonableTechSkillsbyId(int id);
        IEnumerable<TransInterviewer> GetClonableIRFTransInterviewer(int id);
        Task<ClonableSkillChannelVM> GetClonableSkillChannelMeetingPlatformById(int id);
        IEnumerable<ClonableSkillChannelVM> GetClonableTempStatsbyId(int id);
        Task<bool> CreateClonableSkillChannelFromMAG(int assessmentGroupId, string assessmentGroupName, string loggedInUser);
        Task<IList<SkillComment>> GetDeliveryHeadSampleQuestions(int id, string emailId = null);
        
        /// <summary>
        /// Gets all MasterAssessmentGroups that don't have a template.
        /// </summary>
        Task<IEnumerable<MAGWithoutTemplateVM>> GetMAGsWithoutTemplatesAsync();
        
        /// <summary>
        /// Creates templates for all MAGs that don't have one.
        /// </summary>
        Task<BulkTemplateCreationResult> CreateTemplatesForAllMAGsAsync(string loggedInUser);
    }
}
