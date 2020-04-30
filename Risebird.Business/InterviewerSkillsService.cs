using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
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
    public class InterviewerSkillsService:IInterviewerSkillsService
    {
        private IInterviewerSkillsRepository _InterviewerSkillsRepository;
        private readonly INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IClientService _clientService;
        private readonly IConfiguration _configuration;


        public InterviewerSkillsService(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, ITwilioHelper twilioHelper, INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration)
        {
            _InterviewerSkillsRepository = InterviewerSkillsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioHelper = twilioHelper;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _configuration = configuration;
        }
        public IEnumerable<InterviewerSkillsModel> GetAllInterviewerSkills(string emailId, string profileName)
        {
            try
            {
                var InterviewerSkillsList = _InterviewerSkillsRepository.GetAllInterviewerSkills(emailId, profileName).Result.ToList();
                foreach (var irf in InterviewerSkillsList)
                {
                    if (irf.TotalBudget != 0)
                    {
                        var consumedBudget = (irf.TotalBudgetConsumed / irf.TotalBudget) * (100);
                        irf.BudgetConsumed = consumedBudget.ToString() + "%";
                    }
                    else
                    {
                        irf.BudgetConsumed = "Not Defined";
                    }
                }
                return InterviewerSkillsList;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
