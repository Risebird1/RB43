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
        Task<string> GetClonableSkillChannelMeetingPlatformById(int id);
    }
}
