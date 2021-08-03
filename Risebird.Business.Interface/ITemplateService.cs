using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITemplateService
    {
        Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates(string emailId, DateTime startDate, DateTime endDate);
        Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId);
        Task<bool> IsTemplateNameInUse(string templateName);
        Task<EditSkillChannelTemplateVM> GetTemplateById(int id);
        Task<bool> UpdateTemplate(EditSkillChannelTemplateVM templateDetails, string modifiedByEmailId);
        Task<bool> DeleteTemplate(int id);
    }
}
