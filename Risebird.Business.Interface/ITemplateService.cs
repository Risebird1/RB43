using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITemplateService
    {
        Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates(string emailId);
        Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId);
        Task<bool> IsTemplateNameInUse(string templateName);
        Task<EditSkillChannelTemplateVM> GetTemplateById(int id);
        Task<bool> UpdateTemplate(EditSkillChannelTemplateVM templateDetails, string modifiedByEmailId);
        Task<bool> DeleteTemplate(int id);
        Task<bool> RestoreTemplate(int id);
    }
}
