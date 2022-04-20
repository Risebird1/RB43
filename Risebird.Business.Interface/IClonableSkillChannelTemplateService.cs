using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IClonableSkillChannelTemplateService
    {
        Task<ClonableSkillChannelVM> GetTemplateDataFromSourceSkillChannel(int irfId, int assessmentGroupId);
        Task<bool> CreateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser);
        ClonableSkillChannelVM PopulateDropDownsForTemplate(ClonableSkillChannelVM model, bool isEdit = false);
        Task<ClonableSkillChannelVM> GetClonableSkillChannel(int id);
        Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels();
        Task<bool> UpdateClonableSkillChannel(ClonableSkillChannelVM model, string loggedInUser);
        Task<IRFVm> GetTemplateDataForPopulatingSkillChannel(int id);
        IRFVm PopulateDropDownsForSkillChannel(IRFVm model);
        IEnumerable<TransSkill> GetClonableTechSkillsbyId(int id);
    }
}
