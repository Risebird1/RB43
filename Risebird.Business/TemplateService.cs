using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TemplateService : ITemplateService
    {

        private readonly ITemplateRepository _templateRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        

        public TemplateService(ITemplateRepository templateRespository, IApplicationErrorLogger applicationErrorLogger)
        {
            _templateRespository = templateRespository;
            _applicationErrorLogger = applicationErrorLogger;
            
        }

        public async Task<IEnumerable<SkillChannelTemplateVM>> GetAllTemplates(string emailId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _templateRespository.GetAllTemplates(emailId, startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddTemplate(SkillChannelTemplateVM templateDetails, string createdByEmailId)
        {
            try
            {
                return await _templateRespository.AddTemplate(templateDetails, createdByEmailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> IsTemplateNameInUse(string templateName)
        {
            try
            {
                return await _templateRespository.IsTemplateNameInUse(templateName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<EditSkillChannelTemplateVM> GetTemplateById(int id)
        {
            try
            {
                EditSkillChannelTemplateVM model = new EditSkillChannelTemplateVM();
                model = await _templateRespository.GetTemplateById(id);
                model.TechSkillCollection = (await _templateRespository.GetTemplateTechSkills(id)).ToList();
                model.InterviewerCollection = (await _templateRespository.GetTemplateInterviewers(id)).ToList();
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateTemplate(EditSkillChannelTemplateVM templateDetails, string modifiedByEmailId)
        {
            try
            {
                return await _templateRespository.UpdateTemplate(templateDetails, modifiedByEmailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> DeleteTemplate(int id)
        {
            try
            {
                return await _templateRespository.DeleteTemplate(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
