using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IClonableSkillChannelTemplateRepository
    {
        Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM templateDetails);
        Task<ClonableSkillChannelVM> GetClonableSkillChannelById(int id);
        Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels();
        Task<bool> UpdateClonableSkillChannel(ClonableSkillChannelVM templateDetails);
        Task<ClonableSkillChannelVM> GetClonableSkillChannelMeetingPlatformById(int id);
        Task<ClonableSkillChannelVM> GetClonableSkillChannelDetails(int id);
        Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannelsToBeRenewed();
        Task<bool> RenewTemplateRubrics(List<TransSkill> techRubrics, int templateId);
        Task<bool> CreateTemplateFromMAG(List<TransSkill> techRubrics, int assessmentGroupId, string loggedInUser);
        Task<IEnumerable<SkillComment>> GetDeliveryHeadSampleQuestions(int id, string emailId = null);
        
        /// <summary>
        /// Gets all MasterAssessmentGroups that don't have a template.
        /// </summary>
        Task<IEnumerable<MAGWithoutTemplateVM>> GetMAGsWithoutTemplatesAsync();
    }
}
