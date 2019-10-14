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
        private readonly INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IClientService _clientService;

        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger, ITwilioHelper twilioHelper, INotificationHelper notificationHelper, IClientService clientService)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioHelper = twilioHelper;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
        }

        #region IRF Functions
        public async Task<bool> AddIRFDetails(IRFVm irfDetails)
        {
            try
            {
                irfDetails.CreatedOn = DateTime.UtcNow;
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

                //var call = _twilioHelper.MakePhoneCall("+919284487005");

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
        public IEnumerable<Interviewer> GetInterviewers(string skills)
        {
            try
            {
                var interviewers = _irfRepository.GetInterviewerSkills().Where(x=> x.IsApproved==true);
                var skillList = skills.Split(",");
                //skillList.RemoveAt()
                var list = new List<Interviewer>();
                foreach (var item in interviewers)
                {
                    foreach (var skill in skillList)
                    {
                        if (skill == "")
                            continue;
                        var skillName = _irfRepository.GetSkills().Where(x=> x.id == Convert.ToInt16(skill)).FirstOrDefault().Name;
                        if(item.PrimarySkill.Contains(skillName))
                        {
                            list.Add(item);
                            break;
                        }
                    }
                }
                list.Distinct();
                
                return list;
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

        public string GetClientUserRole(int id)
        {
            try
            {
                return _irfRepository.GetClientUserRole(id);
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
        public async Task<bool> DeleteCandidate(int id)
        {
            try
            {
                return _irfRepository.DeleteCandidate(id).Result;
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
                var result= _irfRepository.GetIRFInterviewMeetings(id).OrderBy(x => x.interviewDate).ToList();
                
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
                model.CalendlyLink = IRF.CalendlyLink;
                model.TotalInvites = IRF.TotalInvites;
                model.TotalBudget = IRF.TotalBudget;

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
        public IEnumerable<Users> GetClientUser(int id)
        {
            try
            {

                return _irfRepository.GetClientUser(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            
        }
        public InterviewPlanner GetInterviewPlanner(int id)
        {
            try
            {
                return _irfRepository.GetInterviewPlanner(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public IEnumerable<Candidate> GetIRFCandidates(int id)
        {
            try
            {
                return _irfRepository.GetIRFCandidates(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public async Task<string> AddCandidate(Candidate obj)
        {
            try
            {
                //check if the candidate email exists for the IRFId
                var duplicate = _irfRepository.GetIRFCandidates(obj.IRFId).Where(x=> x.EmailId==obj.EmailId);
                if(duplicate != null && duplicate.Count()>0)
                {
                    return "Sorry! Candidate already exists for the IRF.";
                }
                return _irfRepository.AddCandidate(obj).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not add candidate.";
            }
        }
        public async Task<string> AddMultipleCandidates(List<Candidate> candidateList)
        {
            try
            {
                var candidates = _irfRepository.GetIRFCandidates(candidateList.FirstOrDefault().IRFId);
                foreach (var candidate in candidateList)
                {
                    //check if the candidate email exists for the IRFId
                    var duplicate = candidates.Where(x => x.EmailId == candidate.EmailId);
                    if (duplicate != null && duplicate.Count() > 0)
                    {
                        return "Sorry! One or more candidates already exist for the IRF.";
                    }
                }
                return _irfRepository.AddMultipleCandidates(candidateList).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not import candidates.";
            }
        }
        public Candidate GetCandidate(int id, int irfId)
        {
            try
            {
                var candidate = _irfRepository.GetIRFCandidates(irfId).Where(x => x.id == id).FirstOrDefault();                
                return candidate;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateCandidate(Candidate obj)
        {
            try
            {
                return _irfRepository.UpdateCandidate(obj);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool SendInvite(int irfId, string email)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var candidate = _irfRepository.GetIRFCandidates(irfId).Where(x => x.EmailId == email).FirstOrDefault();
                //var candidateInviteCount = candidate.TotalInvites;
                if (email != null && calendlyLink!=null)
                {
                    var jdLink = irf.JDLink;
                    var ClientName = _clientService.GetClient(irf.ClientId)?.Name;
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x=>x.ClientRoleId==1 || x.ClientRoleId == 4);
                    _notificationHelper.SendCandidateInvite(ClientName, jdLink, irfClientUsers.ToList(),irf.JDTiltle.Split("|")[0],calendlyLink,email);
                    if(candidate.TotalInvites==0)
                    {
                        _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                    }
                    _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites +1));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model)
        {
            try
            {
                return _irfRepository.AddUpdateInterviewPlanner(model).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


    }
}
