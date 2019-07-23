using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class IRFService :IIRFService
    {
        private IIRFRepository _irfRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        #region IRF Functions
        public async Task<bool> AddIRFDetails(IRFVm irfDetails)
        {
            try
            {
                return  _irfRepository.AddIRFDetails(irfDetails).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<Users> GetRiseBirdHR()
        {
            try
            {
                var hrList = new List<Users>();
                var users = _irfRepository.GetUsers();
                foreach (var item in users)
                {
                    if(item.idProfile == 2)
                    {
                        hrList.Add(item);
                    }
                }
                return hrList;
                
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string GetInterviewerSkills(int id)
        {
            try
            {
                var interviewers = _irfRepository.GetInterviewerSkills();
                string Name = String.Empty;
                foreach (var item in interviewers)
                {
                    if (item.UserId == id)
                    {
                        Name = item.PrimarySkill;
                        break;
                    }

                }
                return Name;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<Skill> GetTechSkills(int id)
        {
            try
            {
                List<Skill> list = new List<Skill>();
                var skills = _irfRepository.GetSkillsbyIRFid(id);
                var result = _irfRepository.GetSkills();
                foreach (var item in skills)
                {

                   var obj= result.Where(x => x.id == item.SkillId).FirstOrDefault();
                    list.Add(obj);
                }
                
                return list;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddSkillComments(TechSkillComments skillComments)
        {
            try
            {
                return _irfRepository.AddSkillComments(skillComments);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion
    }
}
