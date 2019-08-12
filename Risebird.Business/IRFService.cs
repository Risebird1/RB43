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

        public SelectList GetQualityController()
        {
            try
            {
                return new SelectList(_irfRepository.GetQualityController(), "id", "EmailId");
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

        public string GetQCSkills(int id)
        {
            try
            {
                return _irfRepository.GetQCSkills(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetQuestionSkills(string id)
        {
            try
            {
                return _irfRepository.GetQuestionSkills(id);
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

        public async Task<bool> DeleteIRF(int id)
        {
            try
            {
                return _irfRepository.DeleteIRF(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public List<MeetingVM> GetIRFInterviewMeetings(int id,string profileName, string email)
        {
            try
            {
                int role =0;
                var result= _irfRepository.GetIRFInterviewMeetings(id).OrderByDescending(x => x.dtCreatedOn).ToList();
                
                if(email != string.Empty && profileName == "Client")
                {
                    var clients = _irfRepository.GetTransClientUser(id);
                    foreach (var item in clients)
                    {
                        if (item.EmailId == email)
                        {
                            role = item.ClientRoleId;
                            break;
                        }
                    }
                    if(role ==0)
                    {
                        return new List<MeetingVM>();
                    }
                    if (role == 1)
                    {
                        result = result.Where(m => m.ClientHrContact == email).ToList();
                        return result;
                    }
                    else if (role == 4)
                    {
                        result = result.Where(m => m.staffingAgency == email).ToList();
                        return result;
                    }
                    else
                        return result;
                }
                else
                {
                    return result;
                }
                
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRFVm> GetEditIRF(int id)
        {
            try
            {
                var model = new IRFVm();

                //get all values from database
                var IRF = _irfRepository.EditIRF(id);
                var transTechSkills = _irfRepository.GetSkillsbyIRFid(id);
                var transSkillsGeneral = _irfRepository.GetSkillsGeneralbyIRFid(id);
                var transJoinability = _irfRepository.GetTransJoinability(id);
                var transClientUser = _irfRepository.GetTransClientUser(id);
                var transInterviewer = _irfRepository.GetTransInterviewer(id);
                var transQualityController = _irfRepository.GetTransQualityController(id);
                var transSelectability = _irfRepository.GetTransSelectability(id);


                //assign values retrieved to the model
                model.id = IRF.id;
                model.ClientCompName = IRF.ClientCompName;
                model.IRFId = IRF.IRFId;
                model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                model.JDLink = IRF.JDLink;
                model.JDTiltle = IRF.JDTiltle;
                model.JobCountry = IRF.JobCountry;
                model.Joinability = (IList<TransJoinability>)(transJoinability);
                model.NoOfIntterview = IRF.NoOfIntterview;
                model.NoOfPosition = IRF.NoOfPosition;
                model.PerMintuteRate = IRF.PerMintuteRate;
                model.QualityController = (IList<TransQualityController>)(transQualityController);
                model.RisebirdHr = IRF.RisebirdHr;
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                model.SkillGeneral = (IList<TransSkillGeneral>)(transSkillsGeneral);
                model.Selectability = (IList<TransSelectability>)(transSelectability);
                model.StartDate = IRF.StartDate;
                model.ClientCollection= (IList<TransClientUser>)(transClientUser);
                model.ClientId = IRF.ClientId;
                model.ClientName = IRF.ClientName;
                model.CompanyType = IRF.CompanyType;
                model.EndDate = IRF.EndDate;
                model.JobLevel = IRF.JobLevel;

                var skillCount = transTechSkills.Count();
                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateIRF(IRFVm model)
        {
            try
            {
                return _irfRepository.UpdateIRFDetails(model).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        #endregion
        public IList<SkillComment> GetTechSkillComments(int id)
        {
            try
            {
                var result = (IList<SkillComment>)_irfRepository.GetTechSkillComments(id);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public List<string> GetListOfInterviwerBasedOnIRFId(string irfId)
        {
            try
            {
                return _irfRepository.GetListOfInterviwerBasedOnIRFId(irfId).Result.ToList();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool CheckUniqueness(string value)
        {
            try
            {
                return _irfRepository.CheckUniqueness(value);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix()
        {
            try
            {
                return _irfRepository.GetJoinabilityMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix()
        {
            try
            {
                return _irfRepository.GetGeneralSkillMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix()
        {
            try
            {
                return _irfRepository.GetSelectabilityMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
