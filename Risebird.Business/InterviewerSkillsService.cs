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
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewerSkillsService : IInterviewerSkillsService
    {
        private IInterviewerSkillsRepository _interviewerSkillsRepository;
        private readonly INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IClientService _clientService;
        private readonly IConfiguration _configuration;


        public InterviewerSkillsService(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, ITwilioHelper twilioHelper, INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration)
        {
            _interviewerSkillsRepository = InterviewerSkillsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioHelper = twilioHelper;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _configuration = configuration;
        }
        public IEnumerable<InterviewerSkillsModel> GetAllInterviewerSkills(string profileName)
        {
            try
            {
                var InterviewerSkillsList = _interviewerSkillsRepository.GetAllInterviewerSkills(profileName).Result.ToList();
                return InterviewerSkillsList;

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
                return _interviewerSkillsRepository.GetGeneralSkillMatrix();

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
                return _interviewerSkillsRepository.GetSelectabilityMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<Users> GetRiseBirdHR()
        {
            try
            {

                //var call = _twilioHelper.MakePhoneCall("+919284487005");

                var hrList = new List<Users>();
                var users = _interviewerSkillsRepository.GetUsers();
                foreach (var item in users)
                {
                    if (item.idProfile == 2)
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
        public async Task<bool> AddInterviewerSkillsDetails(InterviewerSkillsVM InterviewerSkillsDetails)
        {
            try
            {
                return _interviewerSkillsRepository.AddInterviewerSkillsDetails(InterviewerSkillsDetails).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public string GetInterviewerSkills(int id)
        {
            try
            {
                var interviewers = _interviewerSkillsRepository.GetInterviewerSkills();
                string Name = string.Empty;
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
                //var interviewers = _irfRepository.GetInterviewerSkills().Where(x=> x.IsApproved==true);
                var interviewers = _interviewerSkillsRepository.GetInterviewerSkillofInterviewerSkills();
                var skillList = skills.Split(",");
                var skillNameList = new List<string>();
                foreach (var skill in skillList)
                {
                    if (skill == "")
                    {
                        continue;
                    }
                    var skillName = _interviewerSkillsRepository.GetSkillById(Convert.ToInt16(skill)).Name;
                    skillNameList.Add(skillName);
                }
                //skillList.RemoveAt()
                var list = new List<Interviewer>();


                foreach (var item in interviewers)
                {
                    foreach (var skillName in skillNameList)
                    {
                        //if (skill == "")
                        //    continue;
                        //var skillName = _irfRepository.GetSkills().Where(x=> x.id == Convert.ToInt16(skill)).FirstOrDefault().Name;
                        if (item.PrimarySkill.Contains(skillName))
                        {
                            list.Add(item);
                            break;
                        }
                    }
                }
                list = list.Distinct().ToList();

                return list;
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
                return _interviewerSkillsRepository.CheckUniqueness(value);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<InterviewerSkillsVM> GetEditInterviewerSkills(int id)
        {
            try
            {
                var model = new InterviewerSkillsVM();

                //get all values from database
                var InterviewerSkills = _interviewerSkillsRepository.EditInterviewerSkills(id);
                var transTechSkills = _interviewerSkillsRepository.GetSkillsbyInterviewerSkillId(id);
                var transSkillsGeneral = _interviewerSkillsRepository.GetSkillsGeneralbyInterviewerSkillId(id);
                var transInterviewer = _interviewerSkillsRepository.GetTransInterviewer(id);
                var transSelectability = _interviewerSkillsRepository.GetTransSelectability(id);

                //assign values retrieved to the model
                model.id = InterviewerSkills.id;
                model.InterviewerSkillId = InterviewerSkills.InterviewerSkillId;
                model.InterviewerCollection = (IList<TransInterviewerSkillsLeadInterviewer>)(transInterviewer);
                model.JDTiltle = InterviewerSkills.JDTiltle;
                model.PerMintuteRate = InterviewerSkills.PerMintuteRate;
                model.SkillCollection = (IList<TransInterviewerSkillsofSkills>)(transTechSkills);
                model.SkillGeneral = (IList<TransInterviewerSkillsofGeneralSkill>)(transSkillsGeneral);
                model.Selectability = (IList<TransInterviewerSkillsSelectability>)(transSelectability);
                model.PrimarySkill = _interviewerSkillsRepository.GetPrimarySkill(id);
                model.StartDate = InterviewerSkills.StartDate;
                model.EndDate = InterviewerSkills.EndDate;
                model.CalendlyLink = InterviewerSkills.CalendlyLink;
                var skillCount = transTechSkills.Count();
                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateInterviewerSkills(InterviewerSkillsVM model)
        {
            try
            {
                return _interviewerSkillsRepository.UpdateInterviewerSkillsDetails(model).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> DeleteInterviewerSkills(int id)
        {
            try
            {
                return _interviewerSkillsRepository.DeleteInterviewerSkills(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        //public async Task<bool> UnArchiveDeleteInterviewerSkills(int id)
        //{
        //    try
        //    {
        //        return _interviewerSkillsRepository.UnArchiveInterviewerSkills(id).Result;
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}
        public IEnumerable<InterviewerSkillsVM> GetInterviewerSkillsInterviewer(int id)
        {
            try
            {
                return _interviewerSkillsRepository.GetInterviewerSkillsInterviewer(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public int GetUserId(string emailId, string profile)
        {
            int result = 0;
            try
            {
                result = _interviewerSkillsRepository.GetUserId(emailId, profile);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }
        public bool AddInterviewer(InterviewerSkillsVM Model)
        {
            try
            {
                Model.InterviewerListsCollection = Model.InterviewerListsCollection.Where(x => x.IsAdded == true).ToList();
                return _interviewerSkillsRepository.AddInterviewer(Model);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool AddInterviewer(int id, bool IsChecked)
        {
            try
            {
                return _interviewerSkillsRepository.AddInterviewer(id, IsChecked);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<InterviewerSkillsVM> GetUnApprovedInterviewerList(int id )
        {
            try
            {
                var InterviewerSkillsList = _interviewerSkillsRepository.GetUnApprovedInterviewerList(id).Result.ToList();
                return InterviewerSkillsList;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool PauseInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {
               return _interviewerSkillsRepository.PauseInterviewerSkills(InterviewerSkillId, InterviewerSkillName);
                ////update meeting status
                //var updatedMeetings = _interviewerSkillsRepository.UpdateMeetingStatus(InterviewerSkillName).Result.ToList();

                ////send mails
                //if (updatedMeetings != null && updatedMeetings.Count() != 0)
                //{
                //    //get emails
                //    foreach (var item in updatedMeetings)
                //    {
                //        _notificationHelper.SendInterviewCancelledMail(item.interviewer, item.interviewDate.ToString("MMMM dd, yyyy"));
                //        //insert meeting in PausedIRF
                //        // _irfRepository.AddPausedMeetings(irfId, item.imid);


                //        _notificationHelper.SendInterviewCancelledMailtoCandidate(item.candidateEmail, item.ClientHrContact, item.interviewDate.ToString("MMMM dd, yyyy"));

                //    }
                //    //_notificationHelper.CancelCandidateInvite(ClientName, irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0], email);

                //}
                //return true;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public bool PlayInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {

                //update meeting status and IsPaused flag in IRF
                var updatedMeetings = _interviewerSkillsRepository.PlayInterviewerSkills(InterviewerSkillId, InterviewerSkillName);

                //if(updatedMeetings!=null && updatedMeetings.Count()>0)
                //{
                //    var meetingIds = string.Join(",", from item in updatedMeetings select item.imid);
                //    var result = _irfRepository.UpdatePausedMeetingstoPlay(meetingIds);
                //    _irfRepository.DeletePlayedMeetings(meetingIds);
                //    return result;
                //}
                //send mails

                return updatedMeetings;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> DeleteInterviewerList(int id)
        {
            try
            {
                return _interviewerSkillsRepository.DeleteInterviewerList(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

    }
}
