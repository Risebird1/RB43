using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITemplateRepository
    {
        Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates(string emailId/*, DateTime startDate, DateTime endDate*/);
        Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId);
        Task<bool> IsTemplateNameInUse(string templateName);
        Task<EditSkillChannelTemplateVM> GetTemplateById(int id);
        Task<IEnumerable<TransTemplateInterviewer>> GetTemplateInterviewers(int templateId);
        Task<IEnumerable<TransTemplateTechSkill>> GetTemplateTechSkills(int templateId);
        Task<bool> UpdateTemplate(EditSkillChannelTemplateVM templateDetails, string modifiedByEmailId);
        Task<bool> DeleteTemplate(int id);
        Task<bool> RestoreTemplate(int id);
    }
}
