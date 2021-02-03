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
        private readonly IInterviewerSkillsRepository _interviewerSkillsRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;



        public InterviewerSkillsService(IInterviewerSkillsRepository InterviewerSkillsRepository, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _interviewerSkillsRepository = InterviewerSkillsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
        }
        public IEnumerable<InterviewerSkillsChannelModel> GetAllInterviewerSkillsChannel(string profileName, string emailId)
        {
            try
            {
                var InterviewerSkillsList = _interviewerSkillsRepository.GetAllInterviewerSkillsChannel(profileName, emailId).Result.ToList();
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
                return _interviewerSkillsRepository.GetInterviewerSkills(id).FirstOrDefault().PrimarySkill;
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
                var transInterviewer = _interviewerSkillsRepository.GetTransInterviewer(id);

                //assign values retrieved to the model
                model.id = InterviewerSkills.id;
                model.InterviewerSkillId = InterviewerSkills.InterviewerSkillId;
                model.InterviewerCollection = (IList<TransInterviewerSkillsLeadInterviewer>)(transInterviewer);
                model.JDTiltle = InterviewerSkills.JDTiltle;
                model.PerMintuteRate = InterviewerSkills.PerMintuteRate;
                model.SkillCollection = (IList<TransInterviewerSkillsofSkills>)(transTechSkills);
                model.StartDate = InterviewerSkills.StartDate;
                model.EndDate = InterviewerSkills.EndDate;
                model.CalendlyLink = InterviewerSkills.CalendlyLink;
                model.RisebirdHr = InterviewerSkills.RisebirdHr;
                model.PrimarySkill = InterviewerSkills.PrimarySkill;
                //var skillCount = transTechSkills.Count();
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
        public async Task<bool> DeleteInterviewerSkillsChannel(int id)
        {
            try
            {
                return _interviewerSkillsRepository.DeleteInterviewerSkillsChannel(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UnArchiveISC(int id)
        {
            try
            {
                return _interviewerSkillsRepository.UnArchiveISC(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<ISCInterviewer> GetISCInterviewers(int id)
        {
            try
            {
                return _interviewerSkillsRepository.GetISCInterviewers(id);
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
        public bool AddUnApprovedInterviewer(IEnumerable<UnApprovedInterviewer> model, int iscId)
        {
            try
            {
                return _interviewerSkillsRepository.AddUnApprovedInterviewer(model.Where(x => x.IsAdded), iscId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<UnApprovedInterviewer> GetUnApprovedInterviewerList(int id)
        {
            try
            {
                return _interviewerSkillsRepository.GetUnApprovedInterviewerList(id).Result.ToList();
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


                return updatedMeetings;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> DeleteISCInterviewer(int id)
        {
            try
            {
                return _interviewerSkillsRepository.DeleteISCInterviewer(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool SendInvite(int iscId, string email, int interviewerProfileId)
        {
            try
            {
                var isc = _interviewerSkillsRepository.GetISCbyId(iscId);

                if (email != null && isc != null)
                {
                    var result = _notificationHelper.SendInviteToInterviewer(isc.InterviewerSkillId, isc.JDTiltle, isc.SkillName, isc.CalendlyLink, email, isc.HREmailId);
                    if (result.Contains("successfully"))
                    {
                        //update total invites count
                        _interviewerSkillsRepository.UpdateInterviewerInvites(iscId, interviewerProfileId);

                    }
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

        public bool CancelInvite(int iscId, string email)
        {
            try
            {
                var isc = _interviewerSkillsRepository.GetISCbyId(iscId);

                if (email != null)
                {

                    var result = _notificationHelper.CancelInterviewerInvite(isc.InterviewerSkillId, isc.SkillName, email, isc.HREmailId);
                    if (result.ToLower().Contains("success"))
                    {
                        return true;
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<MeetingVM>> GetInterviewerRiseTubeMeetingsPaged(int Page)
        {
            try
            {
                var result = await _interviewerSkillsRepository.GetInterviewerRiseTubeMeetingsPaged(Page);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<int> GetInterviewerRiseTubeMeetingsCount()
        {
            try
            {
                return await _interviewerSkillsRepository.GetInterviewerRiseTubeMeetingsCount(); ;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<MeetingVM> GetMeetingbyid(int id, string Email)
        {
            try
            {
                var meeting = await _interviewerSkillsRepository.GetMeetingbyid(id, Email);
                if (meeting == null)
                    return null;
                else
                    return meeting;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetInterviewerRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern)
        {
            try
            {
                var result = await _interviewerSkillsRepository.GetInterviewerRiseTubeMeetingsByJD(clientId, Emailid, JDpattern);
                return result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<InterviewReport> GetInterviewerInteviewReport(int interviewId, int irfId)
        {
            try
            {
                var reportModel = new InterviewReport();
                var irfDetails = _interviewerSkillsRepository.GetInterviewerIRFDetails(irfId);
                //check whether an IRF exists for the given IRFId
                var check = irfDetails != null ? true : false;
                var interviewDetails = _interviewerSkillsRepository.GetInterviewMeetingById(interviewId).Result;

                if (!check)
                {
                    return null;
                }
                // check whether a feedback for the interviewId has already been submitted
                var interviewReport = _interviewerSkillsRepository.GetInterviewerInterviewReportById(interviewId, irfId);
                if (interviewReport != null && interviewReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport.FirstOrDefault();
                    await EditInterviewReport(reportModel);
                }
                else
                {
                    var model = interviewReport.Any() ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;

                    //get all values from database
                    var levelTech = _interviewerSkillsRepository.GetLevelTech();
                    var skills = _interviewerSkillsRepository.GetSkills();
                    var techSkillsDetails = await _interviewerSkillsRepository.GetSkillsbyIRFid(irfId);

                    var techSkillList = new List<TechSkillReport>();
                    //assign values to model
                    foreach (var item in techSkillsDetails ?? new List<TransSkill>())
                    {
                        TechSkillReport obj = new TechSkillReport
                        {
                            InterviewId = interviewId,
                            RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id,
                            SkillName = skills.FirstOrDefault(x => x.id == item.SkillId)?.Name,
                            SkillId = skills.FirstOrDefault(x => x.id == item.SkillId).id,
                            WeightageRequired = item.Weightage
                        };

                        techSkillList.Add(obj);
                    }
                    model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;


                    model.InterviewId = interviewId;
                    model.CandidateName = interviewDetails.candidateName;
                    model.interviewDate = interviewDetails.interviewDate.ToString();
                    model.resumeLink = interviewDetails.resumeLink;
                    model.encodedresumeLink = interviewDetails.encodedresumeLink;
                    model.IRFId = irfId;

                    model.JobLevelName = Enum.GetName(typeof(JobLevels), interviewDetails.JobLevel);
                    model.currentEmployerName = interviewDetails.currentEmployerName;
                    model.expectedPerHourRate = interviewDetails.expectedPerHourRate;
                    model.PreferredLocation = interviewDetails.preferredLocation;
                    model.IsEdited = false;
                    model.IsApproved = interviewDetails.IsApproved;
                    model.IsSelected = interviewDetails.IsSelected;
                    model.FinalComments = interviewDetails.FinalComments;
                    model.ApprovalComments = interviewDetails.ApprovalComments;
                    model.IsBiometricEnabled = interviewDetails.IsBiometricEnabled;
                    model.JoiningBiometryScore = interviewDetails.JoiningBiometryScore;
                    model.InterviewBiometryScore = interviewDetails.InterviewBiometryScore;
                    reportModel = model;
                }
                // joinability and Selectability

                //get candidateId
                reportModel.CandidateId = _interviewerSkillsRepository.GetCandidateId(interviewId, irfDetails.IRFId);
                reportModel.CandidateName = interviewDetails.candidateName;
                reportModel.TotalExperience = interviewDetails.TotalExperience + " Years";
                reportModel.CandidateEmail = interviewDetails.candidateEmail;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;

                reportModel.IRFName = irfDetails.IRFId;
                reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                reportModel.joiningTime = interviewDetails.joiningTime;
                return reportModel;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        private async Task<InterviewReport> EditInterviewReport(InterviewReport model)
        {
            var techReportCollection = (IList<TechSkillReport>)(_interviewerSkillsRepository.GetTechSkillReport(model.InterviewId));
            if (!techReportCollection.Any())
            {
                var techSkillsDetails = await _interviewerSkillsRepository.GetSkillsbyIRFid(model.IRFId);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = _interviewerSkillsRepository.GetLevelTech();
                var skills = _interviewerSkillsRepository.GetSkills();

                //assign values to model
                foreach (var item in techSkillsDetails ?? new List<TransSkill>())
                {
                    TechSkillReport obj = new TechSkillReport
                    {
                        InterviewId = model.InterviewId,
                        RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                        RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id,
                        SkillName = skills.FirstOrDefault(x => x.id == item.SkillId)?.Name,
                        SkillId = skills.FirstOrDefault(x => x.id == item.SkillId).id,
                        WeightageRequired = item.Weightage
                    };

                    techSkillList.Add(obj);
                }
                model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;
            }
            else
            {
                model.TechSkillReportCollection = techReportCollection;
            }


            model.QuestionsCollection = (IList<QuestionsDiscussed>)(_interviewerSkillsRepository.GetQuestionsDiscussed(model.InterviewId));
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _interviewerSkillsRepository.GetInterviewMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.encodedresumeLink = interviewMeetingsDetails.encodedresumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            model.OldInterviewStatus = model.InterviewStatus;
            model.OldMeetingStatus = model.MeetingStatus;

            return model;
        }
    }

}
