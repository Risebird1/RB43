using Azure.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.Business
{
    public class IRFService : IIRFService
    {
        private readonly IIRFRepository _irfRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IConfiguration _configuration;
        private readonly IActivityTrackerRepository _activityTrackerRepository;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ITwilioHelper _twilioHelper;
        private readonly ICalendlyHelper _calendlyHelper;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;
        //private readonly ISkillService _skillService;
        private readonly IClonableSkillChannelTemplateRepository _clonableSkillChannelTemplateRepository;
        //private readonly IClientRepository _clientRepository;
        private readonly IUserRepository _userRepository;

        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger,
            INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration,
            IActivityTrackerRepository activityTrackerRepository, IWhatsAppHelper whatsAppHelper,
            ITwilioHelper twilioHelper, ICalendlyHelper calendlyHelper, ICandidateRepository candidateRepository,
            IInterviewSchedulerRepository interviewSchedulerRepository,
            IClonableSkillChannelTemplateRepository clonableSkillChannelTemplateRepository, IUserRepository userRepository)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _configuration = configuration;
            _activityTrackerRepository = activityTrackerRepository;
            _whatsAppHelper = whatsAppHelper;
            _twilioHelper = twilioHelper;
            _calendlyHelper = calendlyHelper;
            _candidateRepository = candidateRepository;
            _interviewSchedulerRepository = interviewSchedulerRepository;
            //_skillService = skillService;
            _clonableSkillChannelTemplateRepository = clonableSkillChannelTemplateRepository;
            //_clientRepository = clientRepository;
            _userRepository = userRepository;
        }

        #region IRF Functions
        public async Task<bool> AddIRFDetails(IRFVm irfDetails, string loggedInUser, string domain)
        {
            try
            {
                irfDetails.IRFId = irfDetails.IRFId.Trim();
                irfDetails.CreatedOn = DateTime.Now; // DateTime.UtcNow;
                irfDetails.CreatedBy = loggedInUser;
                //irfDetails.IRFRecommendations.Status = irfDetails.IRFRecommendations.StatusRadioValue == "true" ? true : false;
                int irfId = 0;
                var result = _irfRepository.AddIRFDetails(irfDetails, out irfId);
                if (result)
                {
                    irfDetails.id = irfId;
                    //if(!string.IsNullOrEmpty(irfDetails.AssessmentGroupId))
                    //{
                    //    //List<string> interviewers = new List<string>();
                    //    var interviewers = await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(Convert.ToInt16(irfDetails.AssessmentGroupId));

                    //    //send notification to all interviewers of the assessment group to check for their availability and note down the time


                    //    StringBuilder sb = new StringBuilder();
                    //    sb.Append("<table style='width: 100%; text-align: center;border-collapse:collapse'>");
                    //    sb.Append("<tr><td style='border: 1px solid black'>Skills</td><td style='border: 1px solid black'>Weightage</td></tr>");
                    //    foreach (var item in irfDetails.SkillCollection)
                    //    {
                    //        sb.Append("<tr><td style='border: 1px solid black'>");
                    //        sb.Append(item.SkillName);
                    //        sb.Append("</td>");
                    //        sb.Append("<td style='border: 1px solid black'>");
                    //        sb.Append(item.Weightage);
                    //        sb.Append("</td></tr>");
                    //    }

                    //    foreach (var item in interviewers)
                    //    {
                    //        var encryptedDate = _encryptionHelper.EncryptDateTime(DateTime.Now);
                    //        if (!string.IsNullOrEmpty(item.EmailId))
                    //        {

                    //            string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                    //            string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + item.uniqueid + "&Irfid=" + irfId + "&dec=" + encryptedDate;
                    //            Mail _mail = new Mail
                    //            {
                    //                TO = item.EmailId,
                    //                Subject = "Risebird: Availbility check for Skill Channel " + irfDetails.IRFId + " Interviews"
                    //            };

                    //            var emailresult = _notificationHelper.SendCheckIntervieweravailbility(_mail, item.InterviewerName, yesbutton, nobutton, sb.ToString(), irfDetails.IRFId, irfDetails.JDLink, 0);
                    //        }

                    //    }
                        

                    //    //await _notificationHelper.SendEmailCheckingInterviewerAvailability(smeEmail, irfDetails.IRFId, client, irfDetails.JDTiltle, irfDetails.PrimarySkillInvite, channelOwner);

                    //}
                    if (irfDetails.SkillChannelSMEs != null && irfDetails.SkillChannelSMEs.Any(x => x.IsSelectedForIRF))
                    {
                        //send notification to the sme here
                        var smeEmail = irfDetails.SkillChannelSMEs.Where(x => x.IsSelectedForIRF).FirstOrDefault().EmailId;
                        string channelOwner = string.Empty;
                        var client = (await _clientService.GetClient(irfDetails.ClientId))?.Name;

                        if (irfDetails.RisebirdHr != 0)
                        {
                            channelOwner = (await _userRepository.GetUser(irfDetails.RisebirdHr)).EmailId;
                        }
                        await _notificationHelper.SendAssignedNotificationToSME(smeEmail, irfDetails.IRFId, client, irfDetails.JDTiltle, irfDetails.PrimarySkillInvite, channelOwner);
                    }

                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.AddSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Skill channel created";

                    if (irfId != 0)
                    {
                        activityTrackerModel.ResourceUrl = domain + $"/IRF/EditIRF?id={irfId}";
                    }


                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
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
                foreach (var item in users ?? new List<Users>())
                {
                    if (item?.idProfile == 2)
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

        public IEnumerable<Users> GetAllHRs()
        {
            try
            {
                return _irfRepository.GetAllHRs();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<Users> GetEligibleChannelOwners()
        {
            try
            {
                return _irfRepository.GetEligibleChannelOwners();
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
                string Name = string.Empty;
                foreach (var item in interviewers ?? new List<Interviewer>())
                {
                    if (item == null)
                        continue;
                    if (item?.UserId == id)
                    {
                        Name = item?.PrimarySkill;
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
        public Interviewer GetInterviewerSkillsAndResume(int id)
        {
            try
            {
                return _irfRepository.GetInterviewerSkillsAndResume(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetRecruiterDomain(int userId)
        {
            try
            {
                return _irfRepository.GetRecruiterDomain(userId);

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<Interviewer>> GetInterviewers(string assessmentgroupid)
        {
            try
            {
                var interviewers = await _irfRepository.GetIRFInterviewerSkills(assessmentgroupid);
                return interviewers;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        /// <summary>
        /// TODO Move this code in stored Procedure
        /// </summary>
        /// <param name="assessmentgroupid"></param>
        /// <returns></returns>
        public async Task<List<Interviewer>> GetInterviewersForIRF(string assessmentgroupid, int? irfId = null)
        {
            try
            {
                return await _irfRepository.GetIRFInterviewerSkills(assessmentgroupid, irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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

        public async Task<string> GetTechSkillofSkillChannelByQuestion(string question, int irfId)
        {
            try
            {
                return await _irfRepository.GetTechSkillofSkillChannelByQuestion(question, irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<IEnumerable<Skill>> GetTechSkills(int id)
        {
            try
            {
                List<Skill> list = new List<Skill>();
                var skills = await _irfRepository.GetSkillsbyIRFid(id);
                var result = await _irfRepository.GetSkills();
                foreach (var item in skills ?? new List<TransSkill>())
                {

                    var obj = result.FirstOrDefault(x => x.id == item.SkillId);
                    list.Add(obj);
                }

                return list;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddSkillComments(TechSkillComments skillComments)
        {
            try
            {
                return await _irfRepository.AddSkillComments(skillComments);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool AddCandidateCuratedScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore)
        {
            try
            {
                return _irfRepository.AddCandidateCuratedScore(IrfId, candidateCuratedScore);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> ArchiveIRF(int id)
        {
            try
            {
                return await _irfRepository.ArchiveIRF(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UnArchiveIRF(int id)
        {
            try
            {
                return await _irfRepository.UnArchiveIRF(id);
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
                return await _irfRepository.DeleteCandidate(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        public async Task<List<MeetingVM>> GetIRFInterviewMeetings(int id, string profileName, string email)
        {
            try
            {
                int role = 0;
                var result = (await _irfRepository.GetIRFInterviewMeetings(id, email)).OrderByDescending(x => x.interviewDate).ToList();

                if (email != string.Empty && profileName == "Client")
                {
                    var clients = _irfRepository.GetTransClientUser(id);
                    foreach (var item in clients ?? new List<TransClientUser>())
                    {
                        if (item.EmailId.ToLower() == email.ToLower())
                        {
                            role = item.ClientRoleId;
                            break;
                        }
                    }
                    if (role == 0)
                    {
                        return new List<MeetingVM>();
                    }
                    if (role == 1)
                    {
                        result = result.Where(m => m.ClientHrContact?.Trim().ToLower() == email.ToLower()).ToList();
                        return result;
                    }
                    else if (role == 4)
                    {
                        result = result.Where(m => m.staffingAgency?.Trim().ToLower() == email.ToLower()).ToList();
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
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<List<MeetingVM>> GetIRFInterviewMeetingsForClient(int id, string profileName, string email)
        {
            try
            {
                var result = (await _irfRepository.GetIRFInterviewMeetings(id, email)).OrderByDescending(x => x.interviewDate).ToList();
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRFVm> GetEditIRF(int id, int? templateId, bool isClone = false)
        {
            try
            {
                var model = new IRFVm();

                //get all values from database
                //var IRF = _irfRepository.EditIRF(id);
                model = await _irfRepository.GetSkillChannelDetailsForUpdate(id);
                if (model == null)
                    return null;
                //var IRFConfiguration = _irfRepository.EditIRFConfiguration(id);
                if (!model.EditIRFConfiguration)
                {
                    //var _clientdetails = await _clientRepository.GetClient(id);
                    //if (_clientdetails != null)
                    //{
                    //    model.IsCandidateavailability = _clientdetails.IsCandidateavailability;
                    //    model.IsMeetingAutoCancelled = _clientdetails.IsMeetingAutoCancelled;
                    //    model.IsMeetingNotificationSend = _clientdetails.IsMeetingNotificationSend;
                    //    model.InterviewDuration = _clientdetails.InterviewDuration;
                    //}
                    model.IsCandidateavailability = model.IsClientCandidateAvailablily;
                    model.IsMeetingAutoCancelled = model.IsClientMeetingAutoCancelled;
                    model.IsMeetingNotificationSend = model.IsClientMeetingNotificationSend;
                    model.InterviewDuration = model.ClientInterviewDuration;
                }
                //var transTechSkills = await _irfRepository.GetSkillsbyIRFid(id);
                //var transSkillsGeneral = _irfRepository.GetSkillsGeneralbyIRFid(id);
                //var transJoinability = _irfRepository.GetTransJoinability(id);
                //var transClientUser = _irfRepository.GetTransClientUser(id);

                //var transInterviewer = await _irfRepository.GetTransInterviewer(id);
                //var transQualityController = _irfRepository.GetTransQualityController(id);
                //var transSelectability = _irfRepository.GetTransSelectability(id);
                //var transEmergencyInterviewer = _irfRepository.GetTransEmergencyInterviewer(id);
                //var transResumeEvaluator = _irfRepository.GetTransResumeEvaluator(id);
                //var transHR = _irfRepository.GetTransHR(id);
                //var transRecruiter = _irfRepository.GetTransRecruiter(id);

                //model.MeetingPlatform= IRF.meeting


                //assign values retrieved to the model
                model.id = id;

                //model.IsLocked = IRF.IsLocked;
                //model.MeetingPlatform = IRF.MeetingPlatform;
                //model.ClientCompName = IRF.ClientCompName;
                //model.IRFId = IRF.IRFId;
                //model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer?.OrderByDescending(x => x.IsActive).ToList());
                //model.EmergencyInterviewerCollection = (IList<TransEmergencyInterviewer>)(transEmergencyInterviewer);
                //model.JDLink = IRF.JDLink;
                //model.JDTiltle = IRF.JDTiltle;
                //model.JobCountry = IRF.JobCountry;
                //model.Joinability = (IList<TransJoinability>)(transJoinability);
                //model.NoOfIntterview = IRF.NoOfIntterview;
                //model.NoOfPosition = IRF.NoOfPosition;
                //model.PerMintuteRate = IRF.PerMintuteRate;
                //model.QualityController = (IList<TransQualityController>)(transQualityController);
                //model.RisebirdHr = IRF.RisebirdHr;
                //model.CompanyDetails = IRF.CompanyDetails;
                //model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                //model.SkillGeneral = (IList<TransSkillGeneral>)(transSkillsGeneral);
                //model.Selectability = (IList<TransSelectability>)(transSelectability);
                //model.StartDate = IRF.StartDate;
                //if (!isClone)
                //{
                //    var transClientUser = _irfRepository.GetTransClientUser(id);
                //    model.ClientCollection = (IList<TransClientUser>)(transClientUser);
                //}
                //model.ResumeEvaluatorCollection = (IList<TransResumeEvaluator>)(transResumeEvaluator);
                //model.RecruiterCollection = (IList<TransRecruiter>)(transRecruiter);
                //model.HRCollection = (IList<TransHR>)(transHR);
                //model.ClientId = IRF.ClientId;
                //model.ClientName = IRF.ClientName;
                //model.CompanyType = IRF.CompanyType;
                //model.EndDate = IRF.EndDate;
                //model.JobLevel = IRF.JobLevel;
                //model.CalendlyLink = IRF.CalendlyLink;
                //model.TotalInvites = IRF.TotalInvites;
                //model.TotalBudget = IRF.TotalBudget;
                //model.IsBiometricEnabled = IRF.IsBiometricEnabled;
                //model.PriorityLevel = IRF.PriorityLevel;
                //model.Heuristicsscore = IRF.Heuristicsscore;
                //model.Type = IRF.BussinessType;
                model.Type = model.BussinessType;
                //model.PrimarySkillInvite = IRF.PrimarySkillInvite;
                //model.AssessmentGroupId = IRF.AssessmentGroupId;
                //model.BillingOwner = IRF.BillingOwner;
                //var skillCount = transTechSkills.Count();
                model.OldAssessmentGroupId = model.AssessmentGroupId;
                model.OldCalendlyLink = model.CalendlyLink;
                model.OldJDLink = model.JDLink;
                model.OldInterviewerCollection = model.InterviewerCollection;
                model.OldEmergencyInterviewerCollection = model.EmergencyInterviewerCollection;
                model.OldSkillCollection = model.SkillCollection;
                //model.IsQCRequired = IRF.IsQCRequired;
                //model.IsJoinabilitySelectabilityRequired = IRF.IsJoinabilitySelectabilityRequired;
                //model.IsGeneralSkillRequired = IRF.IsGeneralSkillRequired;
                //model.IsAadhaarVerificationRequired = IRF.IsAadhaarVerificationRequired;
                //model.IsCandidateavailability = IRF.IsCandidateavailability;
                //model.IsMeetingAutoCancelled = IRF.IsMeetingAutoCancelled;
                //model.IsMeetingNotificationSend = IRF.IsMeetingNotificationSend;
                //model.InterviewDuration = IRF.InterviewDuration;
                //model.IsFixedInterviewduration = IRF.IsFixedInterviewduration;
                //model.FixedAmount = IRF.FixedAmount;
                //model.IsNewSkillChannel = IRF.IsNewSkillChannel;
                //model.Surcharge = IRF.Surcharge;
                return model;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateIRF(IRFVm model, string loggedInUser)
        {
            try
            {
                model.ModifiedBy = loggedInUser;
                var interviewerChangesData = new List<InterviewerSuccessRateChangeData>();
                var removedInterviewers = new List<RemovedInterviewersFromSkillsChannelData>();
                var addedNewInterviewers = new List<AddedNewInterviewersInSkillsChannelData>();
                var removedTechSkills = new List<RemovedTechSkillsFromIRF>();
                if (model.OldInterviewerCollection != null && model.InterviewerCollection != null && model.OldInterviewerCollection != model.InterviewerCollection)
                {
                    foreach (var oldInterviewer in model.OldInterviewerCollection)
                    {
                        if (model.InterviewerCollection.Where(x => x.UserId == oldInterviewer.UserId && x.PerSuccessRate != oldInterviewer.PerSuccessRate).Count() == 1)
                        {
                            var interviewer = model.InterviewerCollection.FirstOrDefault(x => x.UserId == oldInterviewer.UserId);
                            float newRate = interviewer.PerSuccessRate;
                            float oldRate = oldInterviewer.PerSuccessRate;
                            float percentChanges = (newRate - oldRate) * 100 / oldRate;

                            if (percentChanges != 0)
                            {
                                interviewerChangesData.Add(new InterviewerSuccessRateChangeData
                                {
                                    Type = "Interviewer",
                                    IRFId = model.id,
                                    PercentChange = percentChanges,
                                    UserId = interviewer.UserId
                                });
                            }
                        }

                        if (!model.InterviewerCollection.Any(x => x.UserId == oldInterviewer.UserId))
                        {
                            removedInterviewers.Add(new RemovedInterviewersFromSkillsChannelData
                            {
                                IRFId = model.id,
                                UserId = oldInterviewer.UserId,
                                Type = "Interviewer"
                            });
                        }
                    }

                    foreach (var newInterviewer in model.InterviewerCollection)
                    {
                        if (!model.OldInterviewerCollection.Any(x => x.UserId == newInterviewer.UserId))
                        {
                            addedNewInterviewers.Add(new AddedNewInterviewersInSkillsChannelData
                            {
                                Type = "Interviewer",
                                IRFId = model.id,
                                PerSuccessRate = newInterviewer.PerSuccessRate,
                                UserId = newInterviewer.UserId
                            });
                        }

                    }
                }
                if (model.OldEmergencyInterviewerCollection != null && model.EmergencyInterviewerCollection != null && model.OldEmergencyInterviewerCollection != model.EmergencyInterviewerCollection)
                {
                    foreach (var oldEmergencyInterviewer in model.OldEmergencyInterviewerCollection)
                    {
                        if (model.EmergencyInterviewerCollection.Where(x => x.UserId == oldEmergencyInterviewer.UserId && x.PerSuccessRate != oldEmergencyInterviewer.PerSuccessRate).Count() == 1)
                        {
                            var emergencyInterviewer = model.EmergencyInterviewerCollection.FirstOrDefault(x => x.UserId == oldEmergencyInterviewer.UserId);
                            float newRate = emergencyInterviewer.PerSuccessRate;
                            float oldRate = oldEmergencyInterviewer.PerSuccessRate;
                            float percentChanges = (newRate - oldRate) * 100 / oldRate;
                            if (percentChanges != 0)
                            {
                                interviewerChangesData.Add(new InterviewerSuccessRateChangeData
                                {
                                    Type = "EmergencyInterviewer",
                                    IRFId = model.id,
                                    PercentChange = percentChanges,
                                    UserId = emergencyInterviewer.UserId
                                });
                            }
                        }
                        if (!model.EmergencyInterviewerCollection.Any(x => x.UserId == oldEmergencyInterviewer.UserId))
                        {
                            removedInterviewers.Add(new RemovedInterviewersFromSkillsChannelData
                            {
                                IRFId = model.id,
                                UserId = oldEmergencyInterviewer.UserId,
                                Type = "EmergencyInterviewer"
                            });
                        }
                    }

                    foreach (var newEmergencyInterviewer in model.EmergencyInterviewerCollection)
                    {
                        if (!model.OldEmergencyInterviewerCollection.Any(x => x.UserId == newEmergencyInterviewer.UserId))
                        {
                            addedNewInterviewers.Add(new AddedNewInterviewersInSkillsChannelData
                            {
                                Type = "EmergencyInterviewer",
                                IRFId = model.id,
                                PerSuccessRate = newEmergencyInterviewer.PerSuccessRate,
                                UserId = newEmergencyInterviewer.UserId
                            });
                        }
                    }
                }

                if (model.OldSkillCollection != null && model.SkillCollection != null && model.SkillCollection != model.OldSkillCollection)
                {
                    foreach (var oldSkill in model.OldSkillCollection)
                    {
                        if (!model.SkillCollection.Select(s => s.SkillId).Any(x => x == oldSkill.SkillId))
                        {
                            removedTechSkills.Add(new RemovedTechSkillsFromIRF
                            {
                                IRFId = model.id,
                                SkillId = oldSkill.SkillId
                            });
                        }
                    }
                }
                var result = await _irfRepository.UpdateIRFDetails(model, interviewerChangesData ?? null, removedInterviewers ?? null, addedNewInterviewers ?? null, removedTechSkills ?? null);
                if (result.Item1)
                {

                    if (model.SkillChannelSMEs != null && model.SkillChannelSMEs.Any(x => x.IsSelectedForIRF) && result.Item2)
                    {
                        //send notification to the sme here
                        var smeEmail = model.SkillChannelSMEs.Where(x => x.IsSelectedForIRF).FirstOrDefault().EmailId;
                        string channelOwner = string.Empty;
                        var client = (await _clientService.GetClient(model.ClientId))?.Name;

                        if (model.RisebirdHr != 0)
                        {
                            channelOwner = (await _userRepository.GetUser(model.RisebirdHr)).EmailId;
                        }
                        await _notificationHelper.SendAssignedNotificationToSME(smeEmail, model.IRFId, client, model.JDTiltle, model.PrimarySkillInvite, channelOwner);
                    }

                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateSkillsChannel;
                    activityTrackerModel.Particulars = model.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;

                    StringBuilder summary = new StringBuilder();
                    if (model.OldAssessmentGroupId != model.AssessmentGroupId)
                    {
                        summary.Append("Assessment Group changed");
                    }
                    if (model.OldCalendlyLink != model.CalendlyLink)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; Calendly Link changed");
                        }
                        else
                        {
                            summary.Append("Calendly Link changed");
                        }
                    }
                    if (model.OldJDLink != model.JDLink)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; JD Link changed");
                        }
                        else
                        {
                            summary.Append("JD Link changed");
                        }
                    }
                    if (summary.Length == 0)
                    {
                        summary.Append("Skill Channel updated");
                    }
                    activityTrackerModel.Summary = summary.ToString();
                    activityTrackerModel.ResourceUrl = model.ResourceUrl;

                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result.Item1;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion
        public IList<SkillComment> GetTechSkillComments(int id, string emailId = null)
        {
            try
            {
                var result = (IList<SkillComment>)_irfRepository.GetTechSkillComments(id, emailId);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IList<CandidateCuratedScore> GetCandidateCuratedScore(int id)
        {
            try
            {
                var result = (IList<CandidateCuratedScore>)_irfRepository.GetCandidateCuratedScore(id);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<List<string>> GetListOfInterviwerBasedOnIRFId(string irfId)
        {
            try
            {
                return (await _irfRepository.GetListOfInterviwerBasedOnIRFId(irfId)).ToList();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool CheckUniqueness(string value, bool isCloneTemplate = false)
        {
            try
            {
                return _irfRepository.CheckUniqueness(value, isCloneTemplate).Result;
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
        public async Task<InterviewPlanner> GetInterviewPlanner(int id)
        {
            try
            {
                return await _irfRepository.GetInterviewPlanner(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<InvoiceModel> GetInvoiceModel(int irfId, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST, string discount, string clientAddress)
        {
            try
            {
                var irfDetails = _irfRepository.GetIRFbyId(irfId);
                var currentMonth = DateTime.Now.Month;
                var interviewDetails = (await _irfRepository.GetIRFInterviewMeetings(irfId)).Where(x => x.MeetingStatus.HasValue && (x.MeetingStatus == 4 || x.MeetingStatus == 2) && x.interviewDate.Date <= endDate.Date && x.interviewDate.Date >= startDate.Date).ToList();// && x.interviewDate.Month ==currentMonth).ToList();
                var invoiceModel = new InvoiceModel();
                if (irfDetails != null && invoiceModel != null)
                {

                    var strb = new StringBuilder();
                    var addressArray = clientAddress.Split(",");
                    for (int i = 1; i <= addressArray.Length; i++)
                    {
                        if (i != addressArray.Length)
                        {
                            strb.Append(addressArray[i - 1] + "," + "<br/>");
                        }
                        else
                        {
                            strb.Append(addressArray[i - 1]);
                        }
                    }

                    invoiceModel.CurrentMonth = DateTime.Now.ToString("MMMM yyyy");
                    Random generator = new Random();
                    invoiceModel.SRNumber = srNumber;
                    invoiceModel.PONumber = poNumber;
                    invoiceModel.ProjectCode = projectCode;
                    invoiceModel.RequestNumber = reqNumber;
                    invoiceModel.CurrencyCode = currencyCode;
                    invoiceModel.IsGST = isGST;
                    invoiceModel.StartDate = startDate;
                    invoiceModel.EndDate = endDate;
                    invoiceModel.InvoiceNumber = generator.Next(0, 999999).ToString("D6");
                    invoiceModel.InvoiceReferenceCode = "RB" + irfDetails.ClientCompName.Substring(0, 2).ToUpper() + irfDetails.JDTiltle.Split("|")[0].Substring(0, 2).ToUpper() + invoiceModel.CurrentMonth.ToString().Substring(0, 2).ToUpper();
                    invoiceModel.InterviewCount = interviewDetails.Count();
                    invoiceModel.ClientAddress = strb.ToString();//irfDetails.ClientAddress;
                    invoiceModel.ClientGSTIN = irfDetails.ClientGST;
                    invoiceModel.ClientName = irfDetails.ClientCompName;
                    invoiceModel.RisebirdAddress = _configuration["RisebirdDetails:Address"];
                    invoiceModel.PricePerInterview = irfDetails.PerMintuteRate;
                    invoiceModel.RisebirdAccountName = _configuration["RisebirdDetails:AccountName"];
                    invoiceModel.RisebirdAccountNumber = _configuration["RisebirdDetails:AccountNumber"];
                    invoiceModel.RisebirdBankName = _configuration["RisebirdDetails:BankName"];
                    invoiceModel.RisebirdPAN = _configuration["RisebirdDetails:PAN"];
                    invoiceModel.RisebirdTAN = _configuration["RisebirdDetails:TAN"];
                    invoiceModel.RisebirdGSTIN = _configuration["RisebirdDetails:GSTIN"];
                    invoiceModel.RisebirdCIN = _configuration["RisebirdDetails:CIN"];
                    invoiceModel.RisebirdIFSC = _configuration["RisebirdDetails:IFSC"];
                    invoiceModel.RisebirdICICIBankSwiftCode = _configuration["RisebirdDetails:ICICIBankSWIFTCode"];
                    invoiceModel.PartialInterviewCount = invoiceModel.InterviewCount;
                    invoiceModel.Discount = Convert.ToDouble(discount);//irfDetails.Discount;
                    invoiceModel.DiscountedPricePerInterview = (1 - (invoiceModel.Discount / 100)) * irfDetails.PerMintuteRate;
                    invoiceModel.PartialPayable = invoiceModel.PartialInterviewCount * invoiceModel.DiscountedPricePerInterview;
                    if (isGST)
                    {
                        invoiceModel.IGST = (0.18) * (invoiceModel.PartialPayable);
                        invoiceModel.IGSTSymbol = "18%";
                    }
                    else
                    {
                        invoiceModel.IGST = 0;
                        invoiceModel.IGSTSymbol = "NA";

                    }
                    invoiceModel.Netpayable = invoiceModel.PartialPayable + invoiceModel.IGST;
                }

                return invoiceModel;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<IEnumerable<Candidate>> GetIRFCandidates(int id, string emailId = null)
        {
            try
            {
                return await _irfRepository.GetIRFCandidates(id, emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public async Task<IEnumerable<Candidate>> GetCandidateIRFInvites(string emailId)
        {
            try
            {
                return await _irfRepository.GetCandidateIRFInvites(emailId);//.Where(x=> x.EndDate.Date <= DateTime.Now.Date);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<string> AddCandidate(Candidate obj, string emailId, string profile)
        {
            try
            {

                //check if the candidate email exists for the IRFId
                var duplicate = (await _irfRepository.GetIRFCandidates(obj.IRFId)).Where(x => x.EmailId.ToLower() == obj.EmailId?.Trim().ToLower());
                if (duplicate != null && duplicate.Any())
                {
                    return "Sorry! Candidate already exists for the IRF.";
                }
                var userId = _irfRepository.GetUserId(emailId?.Trim().ToLower(), profile);
                obj.AddedByUserId = userId;
                //int numberOfLetters = obj.Phone.ToCharArray().Length;
                //var phoneValidate = numberOfLetters >= 9 && int.TryParse(obj.Phone, out int value);
                if (obj.Phone != null)
                {
                    int numberOfLetters = obj.Phone.ToCharArray().Length;
                    var phoneValidate = numberOfLetters >= 9 && long.TryParse(obj.Phone, out long value);
                    obj.Phone = phoneValidate ? obj.Phone : string.Empty;

                }
                //int numberOfLetters = !string.IsNullOrEmpty(str) ? str.ToCharArray().Length : 0;
                //var phoneValidate = numberOfLetters >= 9 && long.TryParse(str, out long phoneNumber);
                //obj.Phone = phoneValidate ? obj.Phone : string.Empty;

                var result = await _irfRepository.AddCandidate(obj);
                if (result.Contains("successfully"))
                {
                    //get all resume evaluators and hrs to send mails to
                    //var resumeEvaluators = _irfRepository.GetResumeEvaluatorsMailIds(obj.IRFId);
                    //_notificationHelper.SendCandidateOnboardedMail(resumeEvaluators.ToList(), obj.Name, obj.IRFName, "Evaluator");

                    //var hrs = _irfRepository.GetHRsMailIds(obj.IRFId);
                    //send mails
                    //_notificationHelper.SendCandidateOnboardedMail(hrs.ToList(), obj.Name, obj.IRFName, "HR");

                    //get candidateId
                    var candidateId = await _irfRepository.GetCandidateIdByEmail(obj.EmailId);

                    //add candidate status 
                    await AddCandidateStatus(emailId, candidateId, obj.IRFId, "Candidate added", (int)CandidateStatusOptions.Added);

                }
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not add candidate.";
            }
        }
        public async Task<string> AddMultipleCandidates(List<Candidate> candidateList, string emailId, string profile)
        {
            try
            {
                var candidates = await _irfRepository.GetIRFCandidates(candidateList.FirstOrDefault().IRFId);
                foreach (var candidate in candidateList ?? new List<Candidate>())
                {
                    //check if the candidate email exists for the IRFId
                    var duplicate = candidates.Where(x => x.EmailId == candidate.EmailId);
                    if (duplicate != null && duplicate.Any())
                    {
                        return "Sorry! One or more candidates already exist for the IRF.";
                    }
                }
                var userId = _irfRepository.GetUserId(emailId, profile);
                return _irfRepository.AddMultipleCandidates(candidateList, userId).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not import candidates.";
            }
        }
        public async Task<Candidate> GetCandidate(int id, int irfId)
        {
            try
            {
                var candidate = (await _irfRepository.GetIRFCandidates(irfId)).FirstOrDefault(x => x.id == id);
                return candidate;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateCandidate(Candidate obj, string emailId)
        {
            try
            {
                return _irfRepository.UpdateCandidate(obj);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> SendInvite(int irfId, string email, string loggedUserEmailId, string linkurl)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var candidate = (await _irfRepository.GetIRFCandidates(irfId)).FirstOrDefault(x => x.EmailId == email);
                //var candidateInviteCount = candidate.TotalInvites;
                if (email != null && calendlyLink != null)
                {
                    var jdLink = irf.JDLink;
                    var client = await _clientService.GetClient(irf.ClientId);
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    //var staffingAgency = await _candidateRepository.GetCandidateStaffingAgency(candidate.id);
                    string primarySkill = string.Empty;
                    primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];
                    string oneTimeSchedulingLink = string.Empty;
                    string oneTimeSchedulingLink1 = string.Empty;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length - 1];
                        var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length - 1];
                        oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, email);
                        if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                        {
                            oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, email);
                        }
                        oneTimeSchedulingLink1 = linkurl + "/InterviewScheduler/ScheduleEvent?id=" + oneTimeSchedulingLink;
                    }
                    else
                    {
                        oneTimeSchedulingLink = await _calendlyHelper.GenerateOneTimeSchedulingLink(irfId);
                        oneTimeSchedulingLink1 = oneTimeSchedulingLink;
                    }
                    if (irf.BussinessType == "B2B")
                    {
                        //_notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink);
                        await _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource, irfId);

                    }
                    else
                    {
                        //_notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact);
                        await _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, irfId);

                    }
                    WhatsAppVM _whatsapp = new WhatsAppVM();
                    _whatsapp.Emailid = email;
                    _whatsapp.InterviewDate = primarySkill;
                    _whatsapp.Header = client.Name;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        _whatsapp.Event = "Interview Schedule";
                    }
                    else
                    {
                        _whatsapp.Event = "Interview Schedule Risebird";
                    }
                    _whatsapp.MessageValue = ((JobLevels)(irf.JobLevel)).GetDescription();
                    if (!string.IsNullOrEmpty(oneTimeSchedulingLink))
                    {
                        _whatsapp.buttonlink = oneTimeSchedulingLink.Replace("https://calendly.com/", "");
                    }
                    else
                    {
                        _whatsapp.buttonlink = !string.IsNullOrEmpty(calendlyLink) ? calendlyLink.Replace("https://calendly.com/", "") : "";
                    }
                    _whatsapp.irfId = irfId;
                    _ = await _whatsAppHelper.WhatsAppSendScheduling(_whatsapp);
                    if (candidate.TotalInvites == 0)
                    {
                        _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                    }
                    _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));


                    try
                    {
                        await _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, loggedUserEmailId);

                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }

                    //add candidate status
                    await AddCandidateStatus(loggedUserEmailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent);


                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> CancelInvite(int irfId, string email)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                //var inviteCount = irf.TotalInvites;
                //var calendlyLink = irf.CalendlyLink;
                var candidate = (await _irfRepository.GetIRFCandidates(irfId)).FirstOrDefault(x => x.EmailId == email);
                //var candidateInviteCount = candidate.TotalInvites;
                if (email != null)
                {
                    var jdLink = irf.JDLink;
                    var ClientName = (await _clientService.GetClient(irf.ClientId))?.Name;
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    var result = await _notificationHelper.CancelCandidateInvite(ClientName, irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0], email, irfId);
                    if (result.ToLower().Contains("success"))
                    {
                        return true;
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model)
        {
            try
            {
                return await _irfRepository.AddUpdateInterviewPlanner(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IRFFunnel> GetFunnelData(int irfId, string irfName)
        {
            try
            {
                //total number of candidates
                int waste = 0;
                var emailList = new List<string>();
                var funnelData = new IRFFunnel();
                var candidates = await _irfRepository.GetIRFCandidates(irfId);
                if (candidates.Count() == 0)
                {
                    //get candidates from Meeting table by passing IRFId
                    emailList = _irfRepository.GetIRFCandidatesEmail(irfName).ToList();
                    funnelData.TotalCandidates = emailList.Count();
                    funnelData.TotalInvites = emailList.Count();
                }
                else
                {
                    emailList = candidates.Select(x => x.EmailId).ToList();
                    funnelData.TotalCandidates = candidates.Count();
                    funnelData.TotalInvites = candidates.Where(x => x.TotalInvites > 0).Count();
                }

                //get interviewMeetings where candidateEmail and irfName matches
                var interviewIdList = new List<int>();
                foreach (var email in emailList ?? new List<string>())
                {
                    var interviewId = _irfRepository.InterviewMeetings(irfName, email);
                    if (interviewId != 0)
                    {
                        interviewIdList.Add(interviewId);
                    }
                }

                funnelData.TotalConfirmations = interviewIdList.Count();


                string ids = string.Join<int>(",", interviewIdList);
                var meetingData = _irfRepository.GetInterviewComments(ids);
                if (meetingData != null)
                {
                    var completed = meetingData.Where(x => x.MeetingStatus == 4);

                    funnelData.TotalCompleted = completed.Count();

                    var risebirdSelected = completed.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4));

                    funnelData.TotalRisebirdSelection = risebirdSelected.Count();

                    var approved = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved == 1);

                    funnelData.TotalApproved = approved.Count();
                    var approvalFilter = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved > 2);
                    waste = waste + approvalFilter.Count(x => x.IsApproved > 3);
                    var finalSelected = approved.Where(x => x.IsSelected.HasValue && x.IsSelected == 1);

                    funnelData.TotalFinalSelections = finalSelected.Count();
                    waste = waste + approved.Count(x => x.IsSelected.HasValue && x.IsSelected > 7);
                    var offered = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus == 1);

                    funnelData.TotalOffered = offered.Count();
                    var offerDeductions = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus > 3);

                    waste = waste + offerDeductions.Count(x => x.OfferStatus.HasValue && x.OfferStatus != 4);
                    var joined = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus == 1);
                    //var totalJoined = joined.Count();
                    funnelData.TotalJoined = joined.Count();
                    var joinedDeductions = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus > 2);
                    //var joinedDenominator = totalOffered - joinedDeductions.Count();
                    //funnelData.JoiningRate = joinedDenominator != 0 ? ((totalJoined * 100 / joinedDenominator) * 100) : 0;
                    waste += joinedDeductions.Count(x => x.JoiningStatus.HasValue && x.JoiningStatus >= 5);
                    funnelData.WasteRate = risebirdSelected.Count() != 0 ? (waste * 100 / risebirdSelected.Count()) : 0;
                }

                return funnelData;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public int GetUserId(string emailId, string profile = null)
        {
            int result = 0;
            try
            {
                result = _irfRepository.GetUserId(emailId, profile);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }

        public List<InterviewVM> GetUpComingInterviewList()
        {
            try
            {
                return _irfRepository.GetUpComingInterviewList()?.ToList() ?? new List<InterviewVM>();

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new List<InterviewVM>();
            }
        }

        public List<IRFVm> GetUpComingIRFList()
        {
            try
            {
                return _irfRepository.GetUpComingIRFList()?.ToList() ?? new List<IRFVm>();

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new List<IRFVm>();
            }
        }
        public async Task<bool> PauseIRF(int irfId, string irfName)
        {
            try
            {
                //update meeting status
                var updatedMeetings = await _irfRepository.UpdateMeetingStatus(irfName);

                //send mails
                if (updatedMeetings != null && updatedMeetings.Count() != 0)
                {
                    //get emails
                    foreach (var item in updatedMeetings ?? new List<MeetingVM>())
                    {
                        await _notificationHelper.SendInterviewCancelledMail(item.interviewer, item.interviewDate.ToString("MMMM dd, yyyy"), item.IRFId, irfId);
                        //insert meeting in PausedIRF
                        // _irfRepository.AddPausedMeetings(irfId, item.imid);

                        await _notificationHelper.SendInterviewCancelledMailtoCandidate(item.candidateEmail, item.ClientHrContact, item.interviewDate.ToString("MMMM dd, yyyy"), irfId);

                    }
                    //_notificationHelper.CancelCandidateInvite(ClientName, irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0], email);

                }
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public bool PlayIRF(int irfId, string irfName)
        {
            try
            {

                //update meeting status and IsPaused flag in IRF
                var updatedMeetings = _irfRepository.PlayIRF(irfName);

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

        public async Task<IEnumerable<IRF>> GetAllIRFs(string emailId, string profileName, int? pageNumber, string searchValue = null, IRFFilter filterModel = null)
        {
            try
            {
                var irfList = (await _irfRepository.GetAllIRFs(emailId, profileName, pageNumber, searchValue, filterModel)).ToList();
                foreach (var irf in irfList ?? new List<IRF>())
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
                return irfList;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> SubscribeCuratedGroup(int id, string emailId, string profile)
        {
            try
            {
                return await _irfRepository.SubscribeCuratedGroup(id, emailId, profile);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UnSubscribeCuratedGroup(int id, string emailId, string profile)
        {
            try
            {
                return await _irfRepository.UnSubscribeCuratedGroup(id, emailId, profile);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool SaveCandidateStatus(int candidateId, int status)
        {
            try
            {
                return _irfRepository.SaveCandidateStatus(candidateId, status);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<CuratedGroup> GetAllCuratedGroups()
        {
            try
            {
                return _irfRepository.GetAllCuratedGroups().Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<CuratedIRFMapping> GetIRFListForCuratedMapping()
        {
            try
            {
                return _irfRepository.GetIRFListForCuratedMapping();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddCuratedGroup(int[] irfList, string cgName)
        {
            try
            {
                return await _irfRepository.AddCuratedGroup(irfList, cgName);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public CuratedGroup GetCuratedGroupMappingsById(int curatedGroupId)
        {
            try
            {
                var cgModel = new CuratedGroup();
                var list = _irfRepository.GetCuratedGroupMappingsById(curatedGroupId).Result;
                var mappings = list.Where(x => x.IsAdded).ToList();
                mappings.AddRange(list.Where(x => !x.IsAdded));
                cgModel.CuratedIRFMappings = (IList<CuratedIRFMapping>)(mappings);
                cgModel.Name = cgModel.CuratedIRFMappings.FirstOrDefault().CuratedGroupName;
                cgModel.id = curatedGroupId;
                return cgModel;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateCuratedGroup(int[] irfList, string cgName, int cgId)
        {
            try
            {
                return _irfRepository.UpdateCuratedGroup(irfList, cgName, cgId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public CuratedGroupUsersVM GetCuratedGroupUsers(int curatedGroupId)
        {
            try
            {
                var model = new CuratedGroupUsersVM
                {
                    CuratedGroupUserList = (IList<CuratedGroupUsers>)_irfRepository.GetCuratedGroupUsers(curatedGroupId).Result
                };
                var list = model.CuratedGroupUserList.Where(x => x.IsAdded).ToList();
                list.AddRange(model.CuratedGroupUserList.Where(x => !x.IsAdded));
                model.CuratedGroupUserList = (IList<CuratedGroupUsers>)list;
                model.CuratedGroupName = model.CuratedGroupUserList.FirstOrDefault().CuratedGroupName;
                model.CuratedGroupId = model.CuratedGroupUserList.FirstOrDefault().CuratedGroupId;
                return model;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddUpdateCuratedGroupUsers(int[] users, int curatedGroupId)
        {
            try
            {
                return _irfRepository.AddUpdateCuratedGroupUsers(users, curatedGroupId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<CuratedUserGroupData> GetCuratedGroupsForCuratedUser(string emailId)
        {
            try
            {
                return _irfRepository.GetCuratedGroupsForCuratedUser(emailId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<IRFInterviewerMembers>> GetIRFInterviewerMembers(int irfId)
        {
            try
            {
                return await _irfRepository.GetIRFInterviewerMembers(irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<InterviewerVM> GetSkillDetails()
        {
            try
            {
                return _irfRepository.GetSkillDetails();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string Addassignmentgroup(Skilldetails _skilldetails)
        {
            try
            {
                if (_skilldetails.SeniorityLevelArray != null)
                {
                    _skilldetails.SeniorityLevel = string.Join(',', _skilldetails.SeniorityLevelArray);
                }
                if (_skilldetails.ProfileTypeArray != null)
                {
                    _skilldetails.ProfileType = string.Join(',', _skilldetails.ProfileTypeArray);
                }
                if (_skilldetails.EmploymentTypeArray != null)
                {
                    _skilldetails.EmploymentType = string.Join(',', _skilldetails.EmploymentTypeArray);
                }
                if (_skilldetails.AssessmentVerificationType == "MCQ")
                {
                    _skilldetails.CodeLanguage = "";
                }
                else
                {
                    _skilldetails.MCQSetsValue = "";
                }
                return _irfRepository.Addassignmentgroup(_skilldetails).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Failed";
            }
        }
        public IEnumerable<AssignmentVM> FetchAssessmentdetails(int id)
        {
            try
            {
                return _irfRepository.FetchAssessmentdetails(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AssessmentLike(int id, string emailid)
        {
            try
            {
                return _irfRepository.AssessmentLike(id, emailid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }

        public async Task<IEnumerable<string>> GetPrimarySkillFromIRF()
        {
            try
            {
                return await _irfRepository.GetPrimarySkillFromIRF();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddCandidateStatus(string performedBy, int candidateId, int irfId, string comments, int status, int? interviewid = null, string audioLink = null)
        {
            try
            {
                var model = new CandidateStatus();
                model.CandidateId = candidateId;
                model.IRFId = irfId;
                model.AudioFileLink = audioLink;
                model.Comments = comments;
                model.Status = status;
                model.PerformedBy = performedBy;
                model.InterviewId = interviewid;
                return await _irfRepository.AddCandidateStatus(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> AddCandidateStatus(CandidateStatus model)
        {
            try
            {
                return await _irfRepository.AddCandidateStatus(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<CandidateStatus>> GetCandidatesStatusList(int irfId, int candidateId)
        {
            try
            {
                return await _irfRepository.GetCandidatesStatusList(irfId, candidateId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public async Task<CandidateStatus> GetCandidateStatusById(int id)
        {
            try
            {
                return await _irfRepository.GetCandidateStatusById(id);

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> EditCandidateStatus(CandidateStatus model)
        {
            try
            {
                return await _irfRepository.EditCandidateStatus(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteCandidateStatus(int id)
        {
            try
            {
                return await _irfRepository.DeleteCandidateStatus(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<Level>> GetLevels()
        {
            try
            {
                return await _irfRepository.GetLevels();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<LevelTech>> GetLevelTech()
        {
            try
            {
                return await _irfRepository.GetLevelTech();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewersByTag(string tag)
        {
            try
            {
                return await _irfRepository.GetInterviewersByTag(tag);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(int id, string emailId = null)
        {
            try
            {
                return await _irfRepository.GetCandidateTimeline(id, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        //public async Task<string> AddQuickMultipleCandidates(List<CandidateQuickAddModel> candidateList, string emailId, string profile, int irfId, bool isSendInvite)
        //{
        //    try
        //    {
        //        var result = string.Empty;
        //        var candidates = await _irfRepository.GetIRFCandidates(irfId);
        //        foreach (var candidate in candidateList ?? new List<CandidateQuickAddModel>())
        //        {
        //            //check if the candidate email exists for the IRFId
        //            var duplicate = candidates.Where(x => x.EmailId == candidate.EmailId);
        //            if (duplicate != null && duplicate.Any())
        //            {

        //                candidateList.Remove(candidate);

        //            }
        //        }
        //        var userId = _irfRepository.GetUserId(emailId, profile);
        //        result = _irfRepository.AddQuickMultipleCandidates(candidateList, userId).Result;
        //        if (result.Contains("successfully"))
        //        {
        //            var irf = _irfRepository.GetIRFbyId(irfId);
        //            var inviteCount = irf.TotalInvites;
        //            var calendlyLink = irf.CalendlyLink;
        //            var jdLink = irf.JDLink;
        //            var client = _clientService.GetClient(irf.ClientId);
        //            var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
        //            string primarySkill = string.Empty;
        //            primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];

        //            foreach (var item in candidateList)
        //            {
        //                var candidate = await _irfRepository.GetCandidateByIRF(irfId, item.EmailId);
        //                //await AddCandidateStatus(emailId, candidate.CandidateMasterId, irfId, "Candidate added", (int)CandidateStatusOptions.Added);
        //                if (isSendInvite)
        //                {
        //                    if (item.EmailId != null && calendlyLink != null)
        //                    {
        //                        string mailResult = string.Empty;
        //                        if (irf.BussinessType == "B2B")
        //                        {
        //                            mailResult = _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, item.EmailId, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink);

        //                        }
        //                        else
        //                        {
        //                            mailResult = _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, item.EmailId, candidate.Name, candidate.ClientHRAttemptContact);

        //                        }
        //                        if (mailResult.ToLower().Contains("success"))
        //                        {
        //                            if (candidate.TotalInvites == 0)
        //                            {
        //                                _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

        //                            }

        //                            _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));

        //                            await AddCandidateStatus(emailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent);

        //                        }


        //                        //add candidate status


        //                    }
        //                }
        //            }

        //        }

        //        return result;
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return "Sorry! Could not import candidates.";
        //    }
        //}

        public async Task AddCandidateUsingResumeParserJob(List<CandidateQuickAddModel> candidateList, int irfId)
        {
            try
            {
                string linkurl = _configuration["HostDetails:baseUrl"];
                var data = await _irfRepository.AddQuickMultipleCandidates(candidateList, null, true);

                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var jdLink = irf.JDLink;
                var client = await _clientService.GetClient(irf.ClientId);
                var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                string primarySkill = string.Empty;
                primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];

                if (data != null)
                {
                    var candidateResumeList = new List<CandidateResume>();

                    foreach (var row in data.Rows.OfType<System.Data.DataRow>())
                    {
                        string value = row.ItemArray[1].ToString();
                        if (!string.IsNullOrEmpty(value) && value.Contains("successfully"))
                        {
                            candidateResumeList.Add(new CandidateResume { id = Convert.ToInt32(row.ItemArray[2].ToString()), IsResumeParsed = true, IsProcessed = true, IsResumeParsingFailed = false });
                            string candidateEmail = row.ItemArray[0].ToString();

                            if (candidateEmail != null && calendlyLink != null)
                            {
                                var candidate = await _irfRepository.GetCandidateByIRF(irfId, candidateEmail);
                                string mailResult = string.Empty;
                                string oneTimeSchedulingLink = string.Empty;
                                string oneTimeSchedulingLink1 = string.Empty;
                                if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                                {
                                    string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length - 1];
                                    var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length - 1];
                                    oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                                    if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                                    {
                                        oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                                    }
                                    oneTimeSchedulingLink1 = linkurl + "/InterviewScheduler/ScheduleEvent?id=" + oneTimeSchedulingLink;
                                }
                                else
                                {
                                    oneTimeSchedulingLink = await _calendlyHelper.GenerateOneTimeSchedulingLink(irfId);
                                    oneTimeSchedulingLink1 = oneTimeSchedulingLink;
                                }

                                if (irf.BussinessType == "B2B")
                                {
                                    mailResult = _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource, irfId).Result;
                                }
                                else
                                {
                                    mailResult = _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, irfId).Result;
                                }
                                var index = data.Rows.IndexOf(row);
                                if (mailResult.ToLower().Contains("success"))
                                {

                                    //data.Rows[index][1] = row.ItemArray[1].ToString() + "(Invitation sent successfully)";


                                    if (candidate.TotalInvites == 0)
                                    {
                                        _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                                    }

                                    _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));

                                    var result = AddCandidateStatus(candidate.AddedByEmailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent).Result;

                                }
                                //else
                                //{
                                //    data.Rows[index][1] = row.ItemArray[1].ToString() + "(Failed to send invite)";

                                //}

                                //auto call candidate here
                                //try
                                //{
                                //    await _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, "Risebird");

                                //}
                                //catch (Exception ex)
                                //{
                                //    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                //    return;
                                //}


                            }
                        }
                        else
                        {
                            candidateResumeList.Add(new CandidateResume { id = Convert.ToInt32(row.ItemArray[2].ToString()), IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false });
                        }
                    }

                    
                    //Parallel.ForEach(data.Rows.OfType<System.Data.DataRow>(), async row =>
                    //{
                    //    string value = row.ItemArray[1].ToString();
                    //    if (!string.IsNullOrEmpty(value) && value.Contains("successfully"))
                    //    {
                    //        candidateResumeList.Add(new CandidateResume { id = Convert.ToInt32(row.ItemArray[2].ToString()), IsResumeParsed = true, IsProcessed = true, IsResumeParsingFailed = false });
                    //        string candidateEmail = row.ItemArray[0].ToString();

                    //        if (candidateEmail != null && calendlyLink != null)
                    //        {
                    //            var candidate = await _irfRepository.GetCandidateByIRF(irfId, candidateEmail);
                    //            string mailResult = string.Empty;
                    //            string oneTimeSchedulingLink = string.Empty;
                    //            string oneTimeSchedulingLink1 = string.Empty;
                    //            if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    //            {
                    //                string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length - 1];
                    //                var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length - 1];
                    //                oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                    //                if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                    //                {
                    //                    oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                    //                }
                    //                oneTimeSchedulingLink1 = linkurl + "/InterviewScheduler/ScheduleEvent?id=" + oneTimeSchedulingLink;
                    //            }
                    //            else
                    //            {
                    //                oneTimeSchedulingLink = await _calendlyHelper.GenerateOneTimeSchedulingLink(irfId);
                    //                oneTimeSchedulingLink1 = oneTimeSchedulingLink;
                    //            }

                    //            if (irf.BussinessType == "B2B")
                    //            {
                    //                mailResult = _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource, irfId).Result;
                    //            }
                    //            else
                    //            {
                    //                mailResult = _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, irfId).Result;
                    //            }
                    //            var index = data.Rows.IndexOf(row);
                    //            if (mailResult.ToLower().Contains("success"))
                    //            {

                    //                //data.Rows[index][1] = row.ItemArray[1].ToString() + "(Invitation sent successfully)";


                    //                if (candidate.TotalInvites == 0)
                    //                {
                    //                    _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                    //                }

                    //                _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));

                    //                var result = AddCandidateStatus(candidate.AddedByEmailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent).Result;

                    //            }
                    //            //else
                    //            //{
                    //            //    data.Rows[index][1] = row.ItemArray[1].ToString() + "(Failed to send invite)";

                    //            //}

                    //            //auto call candidate here
                    //            try
                    //            {
                    //                await _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, "Risebird");

                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    //                return;
                    //            }


                    //        }
                    //    }
                    //    else
                    //    {
                    //        candidateResumeList.Add(new CandidateResume { id = Convert.ToInt32(row.ItemArray[2].ToString()), IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false });
                    //    }
                    //});






                    //update CandidateResume table with isParsed and isProcessed bool values here
                    if (candidateResumeList != null)
                    {
                        await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList);
                        //bulk update call to database
                    }
                }


                return;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return;
            }
        }
        public async Task<StringBuilder> AddQuickMultipleCandidates(List<CandidateQuickAddModel> candidateList, string emailId, string profile, int irfId, bool isSendInvite, string linkurl)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                var userId = _irfRepository.GetUserId(emailId, profile);
                var data = await _irfRepository.AddQuickMultipleCandidates(candidateList, userId);

                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var jdLink = irf.JDLink;
                var client = await _clientService.GetClient(irf.ClientId);
                var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                string primarySkill = string.Empty;
                primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];

                if (data != null)
                {
                    if (isSendInvite)
                    {

                        //int i = 0;

                        Parallel.ForEach(data.Rows.OfType<System.Data.DataRow>(), async row =>
                       {
                           string value = row.ItemArray[1].ToString();
                           if (!string.IsNullOrEmpty(value) && value.Contains("successfully"))
                           {
                               string candidateEmail = row.ItemArray[0].ToString();

                               if (candidateEmail != null && calendlyLink != null)
                               {
                                   var candidate = await _irfRepository.GetCandidateByIRF(irfId, candidateEmail);
                                   string mailResult = string.Empty;
                                   string oneTimeSchedulingLink = string.Empty;
                                   string oneTimeSchedulingLink1 = string.Empty;
                                   if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                                   {
                                       string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length - 1];
                                       var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length - 1];
                                       oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                                       if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                                       {
                                           oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, candidateEmail);
                                       }
                                       oneTimeSchedulingLink1 = linkurl + "/InterviewScheduler/ScheduleEvent?id=" + oneTimeSchedulingLink;
                                   }
                                   else
                                   {
                                       oneTimeSchedulingLink = await _calendlyHelper.GenerateOneTimeSchedulingLink(irfId);
                                       oneTimeSchedulingLink1 = oneTimeSchedulingLink;
                                   }

                                   if (irf.BussinessType == "B2B")
                                   {
                                       mailResult = _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource, irfId).Result;
                                   }
                                   else
                                   {
                                       mailResult = _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, irfId).Result;
                                   }
                                   var index = data.Rows.IndexOf(row);
                                   if (mailResult.ToLower().Contains("success"))
                                   {

                                       data.Rows[index][1] = row.ItemArray[1].ToString() + "(Invitation sent successfully)";


                                       if (candidate.TotalInvites == 0)
                                       {
                                           _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                                       }

                                       _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));

                                       var result = AddCandidateStatus(emailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent).Result;

                                   }
                                   else
                                   {
                                       data.Rows[index][1] = row.ItemArray[1].ToString() + "(Failed to send invite)";

                                   }

                                   //auto call candidate here
                                   try
                                   {
                                       await _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, emailId);

                                   }
                                   catch (Exception ex)
                                   {
                                       await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                       return;
                                   }


                               }
                           }
                       });

                    }
                    //Table start.
                    sb.Append("<div class='import' style='overflow-y:auto;'><table  style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>"); ;

                    //Adding HeaderRow.
                    sb.Append("<tr>");
                    foreach (DataColumn column in data.Columns)
                    {
                        sb.Append("<th style='background-color: #B8DBFD;    border-right: 1px #ccc solid;width:200px'>" + column.ColumnName + "</th>");
                    }
                    sb.Append("</tr>");


                    //Adding DataRow.
                    foreach (DataRow row in data.Rows)
                    {
                        sb.Append("<tr>");
                        foreach (DataColumn column in data.Columns)
                        {
                            sb.Append("<td style='border-left: 1px #ccc solid;'>" + row[column.ColumnName].ToString() + "</td>");
                        }
                        sb.Append("</tr>");
                    }

                    //Table end.
                    sb.Append("</table></div>");
                }

                else
                {
                    sb.Append("Failed to import candidates");
                }



                //StringBuilder sb = new StringBuilder();



                return sb;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return sb.Append("Failed to import candidates!");
            }
        }

        public async Task<IRF> GetIRFForCall(int irfId)
        {
            try
            {
                return await _irfRepository.GetIRFForCall(irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public bool CheckChannelOwnerByIrfid(string IRFId, string emailid)
        {
            try
            {
                return _irfRepository.CheckChannelOwnerByIrfid(IRFId, emailid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<TemplateInterviewerData>> GetInterviewersByAssessmentGroup(string assessmentGroupId)
        {
            try
            {
                return await _irfRepository.GetInterviewersByAssessmentGroup(assessmentGroupId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<string> GetInterviewerSkillsByUserId(int userId)
        {
            try
            {
                return await _irfRepository.GetInterviewerSkillsByUserId(userId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<Skill>> GetSkills()
        {
            try
            {
                return await _irfRepository.GetSkills();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRF>> GetAllIRFDetails(DateTime startDate, DateTime endDate)
        {
            try
            {
                var irfList = await _irfRepository.GetAllIRFDetails(startDate, endDate);
                foreach (var irf in irfList)
                {
                    //int diff = (irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings - irf.CandidateCount);

                    //if(diff > 0)
                    //{
                    //    irf.ReScheduledCount = irf.ReScheduledCount - diff;
                    //    if(irf.ReScheduledCount < 0)
                    //    {
                    //        irf.ReScheduledCount = 0;
                    //    }
                    //}
                    //if((irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings + irf.NotScheduledCount) != irf.CandidateCount)
                    //{
                    //    int totalDiff = irf.CandidateCount - (irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings + irf.NotScheduledCount);

                    //    if (totalDiff > 0)
                    //    {
                    //        irf.NotScheduledCount = irf.NotScheduledCount + totalDiff;
                    //    }
                    //}
                    if (irf.CandidateCount > 0)
                    {
                        irf.ScheduledPercentage = ((irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings + irf.PendingCount) * 100) / irf.CandidateCount;
                        irf.CompletionRate = (irf.CompletedInterviews * 100) / irf.CandidateCount;
                    }

                    if (irf.CompletedInterviews > 0)
                    {
                        irf.SelectionRate = irf.SelectedMeetings.HasValue ? (irf.SelectedMeetings.Value * 100) / irf.CompletedInterviews : 0;
                    }

                    if (irf.ScheduledPercentage < 75)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.CompletionRate < 60)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.SelectionRate < 20)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    //Start: merging 911 dashboard
                    //if (irf.UnAssignedMeeting > 0)
                    //{
                    //    irf.Flags = irf.Flags + 1;
                    //}
                    //if (irf.UnfilledInterviewStatusCount > 0)
                    //{
                    //    irf.Flags = irf.Flags + 1;
                    //}
                    if (irf.UnassignedInterviewsCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.UnfilledMeetingStatusCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.UnfilledCommentsCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.MissingZoomLink > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.MissingVideoLink > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.FeedbackCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    //End: merging 911 dashboard
                }
                return irfList;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<SkillCahnnelDetailsVM> GetAllSkillChannelDetails()
        {
            try
            {
                var irfList = _irfRepository.GetAllSkillChannelDetails().Result.ToList();
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<int> GetInterviewerQCRate(int userId)
        {
            try
            {
                return await _irfRepository.GetInterviewerQCRate(userId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<int> GetInterviewerRate(int userId, int? companyType, int? countryType = null)
        {
            try
            {
                string companyTypeDesc = null;
                if (companyType != null && companyType.HasValue)
                {
                    companyTypeDesc = ((CompanyType)companyType).GetDescription();
                }
                var rate = await _irfRepository.GetInterviewerRate(userId, companyTypeDesc, countryType);
                return rate;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }

        public async Task<int> GetRenewedInterviewerRate(int? planType, int? countryType = null, int? companyType = null)
        {
            try
            {
                string planTypeDesc = null;
                if (planType != null && planType.HasValue)
                {
                    planTypeDesc = ((Heuristicsscorelevel)planType).GetDescription();
                }
                var rate = await _irfRepository.GetRenewedInterviewerRate(planTypeDesc, countryType, null, companyType);
                return rate;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<bool> IsQuestionUsedInReport(string question)
        {
            try
            {
                return await _irfRepository.IsQuestionUsedInReport(question);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<TransSkill>> GetTechSkillswithWeightage(int id)
        {
            try
            {
                var skills = await _irfRepository.GetSkillsbyIRFid(id);
                return skills;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRF>> GetAllIRFDetailsByIRFId(int IRFId)
        {
            try
            {
                var irfList = await _irfRepository.GetAllIRFDetailsByIRFId(IRFId);
                foreach (var irf in irfList)
                {
                    if (irf.CandidateCount > 0)
                    {
                        irf.ScheduledPercentage = ((irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings + irf.PendingCount) * 100) / irf.CandidateCount;
                        irf.CompletionRate = (irf.CompletedInterviews * 100) / irf.CandidateCount;
                    }
                    if (irf.CompletedInterviews > 0)
                    {
                        irf.SelectionRate = irf.SelectedMeetings.HasValue ? (irf.SelectedMeetings.Value * 100) / irf.CompletedInterviews : 0;
                    }
                    if (irf.ScheduledPercentage < 75)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.CompletionRate < 60)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.SelectionRate < 20)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    //Start: merging 911 dashboard
                    if (irf.UnAssignedMeeting > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.UnfilledInterviewStatusCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.UnfilledCommentsCount > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.MissingZoomLink > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.MissingVideoLink > 0)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                }
                return irfList;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAssessmentGroupInterviewerMembers(int Assessmentid)
        {
            try
            {
                return await _irfRepository.GetAssessmentGroupInterviewerMembers(Assessmentid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<AssessmentIRFListVM>> GetAssessmentGroupIRFList(int Assessmentid)
        {
            try
            {
                return await _irfRepository.GetAssessmentGroupIRFList(Assessmentid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string FetchIRFAudioLink(int irfid)
        {
            try
            {
                return _irfRepository.FetchIRFAudioLink(irfid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public bool SaveIRFAudioLink(int irfid, string commentAudioLink)
        {
            try
            {
                return _irfRepository.SaveIRFAudioLink(irfid, commentAudioLink);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IEnumerable<AssessmentIRFListVM>> GetAssessmentGroupQuestionList(int Assessmentid)
        {
            try
            {
                return await _irfRepository.GetAssessmentGroupQuestionList(Assessmentid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> IsClonableTemplateNameInUse(string name)
        {
            try
            {
                return await _irfRepository.IsClonableTemplateNameInUse(name);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public IEnumerable<TransInterviewer> GetInterviewersByAssessmentGroupId(int assessmentGroupId)
        {
            try
            {
                return _irfRepository.GetInterviewersByAssessmentGroup(assessmentGroupId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<Tuple<bool, int>> CreateSkillChannelFromTemplate(IRFVm irfDetails, string loggedInUser, string profile)
        {
            try
            {
                string channelOwner;
                if (profile == ProfileEnum.Client.GetDescription() || profile == ProfileEnum.Admin.GetDescription() || profile == ProfileEnum.SupportAdmin.GetDescription())
                {
                    var eligibleHrs = _irfRepository.GetEligibleChannelOwners()?.ToList();
                    //eligibleHrs.RemoveAt(0);
                    var random = new Random();
                    int index = random.Next(eligibleHrs.Count);
                    irfDetails.RisebirdHr = eligibleHrs[index].id;
                    channelOwner = eligibleHrs[index].EmailId;
                }
                else
                {
                    irfDetails.RisebirdHr = GetUserId(loggedInUser);
                    channelOwner = loggedInUser;
                }
                irfDetails.PrimarySkillInvite = irfDetails.JDTiltle;
                irfDetails.CompanyDetails = string.Empty;
                irfDetails.PerMintuteRate = 0;
                irfDetails.TotalBudget = 0;
                irfDetails.NoOfPosition = 0;
                irfDetails.Type = "B2B";
                irfDetails.PriorityLevel = (int)PriorityLevel.Medium;

                //irfDetails.InterviewerCollection = irfDetails.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                var result = await _irfRepository.CreateSkillChannelFromTemplate(irfDetails, loggedInUser);
                if (result.Item1)
                {
                    var clientName = await _clientService.GetClientName(irfDetails.ClientId);
                    //send mail to the HR here
                    await _notificationHelper.SendIRFCreatedFromTemplateNotificationToChannelOwner(channelOwner, irfDetails.IRFId, irfDetails.TemplateName, irfDetails.JDTiltle, clientName);

                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.AddSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Skill channel created";

                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new Tuple<bool, int>(false, 0);
                //return false;
            }
        }

        /// <summary>
        /// Edit Skill Channel created from Template exclusively for Client.
        /// </summary>
        /// <param name="irfDetails"></param>
        /// <param name="loggedInUser"></param>
        /// <returns></returns>
        public async Task<bool> EditSkillChannelCreatedFromTemplate(IRFVm irfDetails, string loggedInUser)
        {
            try
            {
                irfDetails.PrimarySkillInvite = irfDetails.JDTiltle;
                //irfDetails.CompanyDetails = string.Empty;
                //irfDetails.PerMintuteRate = 0;
                //irfDetails.TotalBudget = 0;
                //irfDetails.NoOfPosition = 0;
                //irfDetails.Type = "B2B";
                //irfDetails.PriorityLevel = (int)PriorityLevel.Medium;

                //irfDetails.InterviewerCollection = irfDetails.InterviewerCollection?.Where(x => x.IsActive)?.ToList();
                var result = await _irfRepository.EditSkillChannelCreatedFromTemplate(irfDetails);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Skill channel Updated";

                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        /// <summary>
        /// Create Skill Channel For Client from Single click interview link pop-up
        /// </summary>
        /// <param name="irfDetails"></param>
        /// <param name="loggedInUser"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, int>> CreateSkillChannelUsingTemplateByInterviewLinkPopUp(IRFVm irfDetails, string loggedInUser, string profile)
        {
            try
            {
                string channelOwner;
                if (profile == ProfileEnum.Client.GetDescription() || profile == ProfileEnum.Admin.GetDescription() || profile == ProfileEnum.SupportAdmin.GetDescription())
                {
                    var eligibleHrs = _irfRepository.GetEligibleChannelOwners()?.ToList();
                    //eligibleHrs.RemoveAt(0);
                    var random = new Random();
                    int index = random.Next(eligibleHrs.Count);
                    irfDetails.RisebirdHr = eligibleHrs[index].id;
                    channelOwner = eligibleHrs[index].EmailId;
                }
                else
                {
                    irfDetails.RisebirdHr = GetUserId(loggedInUser);
                    channelOwner = loggedInUser;
                }

                //get meeting platform from template by Id
                if (string.IsNullOrEmpty(irfDetails.MeetingPlatform))
                {
                    var meetingPlatform = await _clonableSkillChannelTemplateRepository.GetClonableSkillChannelMeetingPlatformById(irfDetails.CreatedFromTemplateId);
                    irfDetails.MeetingPlatform = meetingPlatform.MeetingPlatform;
                    irfDetails.IsAadhaarVerificationRequired = meetingPlatform.IsAadhaarVerificationRequired;
                }


                irfDetails.PrimarySkillInvite = irfDetails.JDTiltle;
                irfDetails.CompanyDetails = string.Empty;
                irfDetails.PerMintuteRate = 0;
                irfDetails.TotalBudget = 0;
                irfDetails.NoOfPosition = 0;
                irfDetails.Type = "B2B";
                irfDetails.PriorityLevel = (int)PriorityLevel.Medium;

                var transTechSkills = _irfRepository.GetClonableTechSkillsbyId(irfDetails.CreatedFromTemplateId);
                var irfRecommendations = await _irfRepository.GetIRFRecommendation(irfDetails.CreatedFromTemplateId);

                if (irfRecommendations != null)
                {
                    irfDetails.AssessmentGroupName = irfRecommendations.AssessmentGroupName;
                    irfDetails.JobCountry = irfRecommendations.CountryType?.ToString();
                    //irfDetails.JobLevel = irfRecommendations.SeniorityLevelId;
                    irfDetails.JobLevel = (int)JobLevels.SE;
                    irfDetails.CompanyType = irfRecommendations.CompanyType?.ToString();
                    //irfDetails.Heuristicsscore = irfRecommendations.CompetencyLevelId.HasValue ? irfRecommendations.CompetencyLevelId.Value : irfDetails.Heuristicsscore;
                    irfDetails.Heuristicsscore = (int)Heuristicsscorelevel.Standard;
                }
                if(irfDetails.MarketPlacePlanType > 0)
                {
                    irfDetails.Heuristicsscore = irfDetails.MarketPlacePlanType;

                }
                //var templateInterviewers = _irfRepository.GetClonableIRFTransInterviewer(id);

                irfDetails.InterviewerCollection = (IList<TransInterviewer>)_irfRepository.GetClonableIRFTransInterviewer(irfDetails.CreatedFromTemplateId);
                irfDetails.SkillCollection = (IList<TransSkill>)(transTechSkills);

                int clientUserId = _irfRepository.GetUserId(loggedInUser);
                irfDetails.ClientCollection = new List<TransClientUser>();
                irfDetails.ClientCollection.Add(new TransClientUser { ClientUserId = clientUserId, EmailId = loggedInUser, Role = _irfRepository.GetClientUserRole(clientUserId) });

                var result = await _irfRepository.CreateSkillChannelFromTemplate(irfDetails, loggedInUser);
                if (result.Item1)
                {
                    var clientName = await _clientService.GetClientName(irfDetails.ClientId);
                    //send mail to the HR here
                    await _notificationHelper.SendIRFCreatedFromTemplateNotificationToChannelOwner(channelOwner, irfDetails.IRFId, irfDetails.TemplateName, irfDetails.JDTiltle, clientName);


                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.AddSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Skill channel created";

                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new Tuple<bool, int>(false, 0);
                //return false;
            }
        }

        public async Task<IEnumerable<IRFFilterViewModel>> GetIRFFilterData(string emailId, string profileName, IRFFilter filterModel = null)
        {
            try
            {
                return await _irfRepository.GetIRFFilterData(emailId, profileName, filterModel);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<Tuple<bool, int>> CreateSkillChannelUsingNewInterviewRequest(IRFVm irfDetails, string loggedInUser, string profile)
        {
            try
            {
                if (profile == ProfileEnum.Client.GetDescription())
                {
                    var eligibleHrs = _irfRepository.GetEligibleChannelOwners()?.ToList();
                    //eligibleHrs.RemoveAt(0);
                    var random = new Random();
                    int index = random.Next(eligibleHrs.Count);
                    irfDetails.RisebirdHr = eligibleHrs[index].id;
                }
                else
                {
                    irfDetails.RisebirdHr = GetUserId(loggedInUser);
                }
                irfDetails.PrimarySkillInvite = irfDetails.JDTiltle;
                irfDetails.CompanyDetails = string.Empty;
                irfDetails.PerMintuteRate = 0;
                irfDetails.TotalBudget = 0;
                irfDetails.NoOfPosition = 0;
                irfDetails.Type = "B2B";
                irfDetails.PriorityLevel = (int)PriorityLevel.Medium;
                irfDetails.InterviewerCollection = (List<TransInterviewer>)_irfRepository.GetIRFInterviewerSkillsByAssessmentGroup(irfDetails.AssessmentGroupId);

                int clientUserId = _irfRepository.GetUserId(loggedInUser);
                irfDetails.ClientCollection = new List<TransClientUser>();
                irfDetails.ClientCollection.Add(new TransClientUser { ClientUserId = clientUserId, EmailId = loggedInUser, Role = _irfRepository.GetClientUserRole(clientUserId) });

                var result = await _irfRepository.CreateSkillChannelFromTemplate(irfDetails, loggedInUser);
                if (result.Item1)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.AddSkillsChannel;
                    activityTrackerModel.Particulars = irfDetails.IRFId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Skill channel created";

                    await _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new Tuple<bool, int>(false, 0);

                //return false;
            }
        }
        public async Task<IEnumerable<IRF>> GetAllIRFDetailsIRF(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                var irfList = await _irfRepository.GetAllIRFDetailsIRF(startDate, endDate, profile, emailid);
                foreach (var irf in irfList ?? new List<IRF>())
                {
                    if (irf.CandidateCount > 0)
                    {
                        irf.ScheduledPercentage = ((irf.CompletedInterviews + irf.ReScheduledCount + irf.FutureScheduledMeetings + irf.PendingCount) * 100) / irf.CandidateCount;
                        irf.CompletionRate = (irf.CompletedInterviews * 100) / irf.CandidateCount;
                    }
                    if (irf.CompletedInterviews > 0)
                    {
                        irf.SelectionRate = irf.SelectedMeetings.HasValue ? (irf.SelectedMeetings.Value * 100) / irf.CompletedInterviews : 0;
                    }
                    if (irf.CompletionRate < 60)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (irf.SelectionRate < 20)
                    {
                        irf.Flags = irf.Flags + 1;
                    }
                    if (profile != "Client")
                    {
                        if (irf.FeedbackCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.ScheduledPercentage < 75)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.UnassignedInterviewsCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.UnfilledMeetingStatusCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.UnfilledCommentsCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.MissingZoomLink > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.MissingVideoLink > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                    }
                    else
                    {
                        if (irf.NotScheduledCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                        if (irf.ReScheduledCount > 0)
                        {
                            irf.Flags = irf.Flags + 1;
                        }
                    }
                }
                return irfList;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateIntervieweravailbilityDetails(int irfId)
        {
            try
            {
                return await _irfRepository.UpdateIntervieweravailbilityDetails(irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<List<InterviewDetailsVM>> FetchIRFCompletedInterviewDetails(int id, int type, DateTime startDate, DateTime endDate)
        {
            try
            {
                var result = (await _irfRepository.FetchIRFCompletedInterviewDetails(id, type, startDate, endDate)).ToList();
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewDetailsVM> FetchIRFInterviewerDetailsById(int id)
        {
            try
            {
                var result = await _irfRepository.FetchIRFInterviewerDetailsById(id);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<string>> FetchSupportAdminEmailid()
        {
            try
            {
                var result = (await _irfRepository.FetchSupportAdminEmailid()).ToList();
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SendRedoInvite(int irfId, string email, string loggedUserEmailId, string linkurl)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var candidate = (await _irfRepository.GetIRFCandidates(irfId)).FirstOrDefault(x => x.EmailId == email);
                if (email != null && calendlyLink != null)
                {
                    var jdLink = irf.JDLink;
                    var client = await _clientService.GetClient(irf.ClientId);
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    string primarySkill = string.Empty;
                    primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];
                    string oneTimeSchedulingLink = string.Empty;
                    string oneTimeSchedulingLink1 = string.Empty;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length - 1];
                        var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length - 1];
                        oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, email);
                        if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                        {
                            oneTimeSchedulingLink = await _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, email);
                        }
                        oneTimeSchedulingLink1 = linkurl + "/InterviewScheduler/ScheduleEvent?id=" + oneTimeSchedulingLink;
                    }
                    else
                    {
                        oneTimeSchedulingLink1 = await _calendlyHelper.GenerateOneTimeSchedulingLink(irfId);
                    }
                    await _notificationHelper.SendRedoCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink1)) ? oneTimeSchedulingLink1 : calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, client.LogoLink);
                    WhatsAppVM _whatsapp = new WhatsAppVM();
                    _whatsapp.Emailid = email;
                    _whatsapp.InterviewDate = primarySkill;
                    _whatsapp.Header = client.Name;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        _whatsapp.Event = "Interview Schedule";
                    }
                    else
                    {
                        _whatsapp.Event = "Interview Schedule Risebird";
                    }
                    _whatsapp.MessageValue = ((JobLevels)(irf.JobLevel)).GetDescription();
                    if (!string.IsNullOrEmpty(oneTimeSchedulingLink))
                    {
                        _whatsapp.buttonlink = oneTimeSchedulingLink.Replace("https://calendly.com/", "");
                    }
                    else
                    {
                        _whatsapp.buttonlink = !string.IsNullOrEmpty(calendlyLink) ? calendlyLink.Replace("https://calendly.com/", "") : "";
                    }
                    _whatsapp.irfId = irfId;
                    _ = await _whatsAppHelper.WhatsAppSendScheduling(_whatsapp);
                    if (candidate.TotalInvites == 0)
                    {
                        _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));
                    }
                    _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites + 1));
                    try
                    {
                        await _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, loggedUserEmailId);
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                    //add candidate status
                    await AddCandidateStatus(loggedUserEmailId, candidate.CandidateMasterId, irfId, "Invitation sent", (int)CandidateStatusOptions.InvitationSent);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> SendClientRedoInvite(int irfId, int interviewid, string email)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var candidate = (await _irfRepository.GetIRFCandidates(irfId)).FirstOrDefault(x => x.EmailId == email);
                var interviewdetails = await _candidateRepository.FetchChannelOwnerandInterviewer(interviewid);
                var jdLink = irf.JDLink;
                var client = await _clientService.GetClient(irf.ClientId);
                var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                string primarySkill = string.Empty;
                primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];
                string oneTimeSchedulingLink = String.Empty;
                await _notificationHelper.SendRedoClientInvite(client.Name, irfClientUsers.ToList(), primarySkill, oneTimeSchedulingLink, email, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource, interviewdetails.interviewDate.ToString(), client.LogoLink);
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<ClientInterviewMapped>> GetCompletedInterviewCountMappedClient(DateTime startDate, DateTime endDate)
        {
            try
            {
                var irfList = await _irfRepository.GetCompletedInterviewCountMappedClient(startDate, endDate);
                return irfList;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetInterviewerAssessmentGroups(string emailid)
        {
            try
            {
                return await _irfRepository.GetInterviewerAssessmentGroups(emailid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<AssessmentGroupInterviewers>> GetAssessmentGroupInterviewerMembersForSME(int agId)
        {
            try
            {
                return await _irfRepository.GetAssessmentGroupInterviewerMembersForSME(agId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<SkillChannelSME>> GetEligibleSMEForSkillChannel(int agId, int? irfId)
        {
            try
            {
                var members = await _irfRepository.GetEligibleSMEForSkillChannel(agId, irfId);
                return members?.Where(x => x.IsSME)?.ToList();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddUpdateAssessmentGroupSMEInterviewer(IList<AssessmentGroupInterviewers> interviewers)
        {
            try
            {
                return await _irfRepository.AddUpdateAssessmentGroupSMEInterviewer(interviewers);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IEnumerable<IRF>> GetAllNewCreatedIRFs()
        {
            try
            {
                return await _irfRepository.GetAllNewCreatedIRFs();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAllIRFAssessmentInterviewerMembers(int irfId)
        {
            try
            {
                return await _irfRepository.GetAllIRFAssessmentInterviewerMembers(irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<AssessmentGroupInterviewerDetails>> GetAvailableAssessmentGroupInterviewerMembers(int assessmentGroupId, int? irfId = null)
        {
            try
            {
                return await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(assessmentGroupId, irfId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFClientReviewVM>> GetIRFClientReviews(int irfId)
        {
            try
            {
                return await _irfRepository.GetIRFClientReviews(irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<CommonInterviewDetailsVM> GetIRFBillingOwnerById(int irfId)
        {
            try
            {
                var irf = await _irfRepository.GetIRFBillingOwnerById(irfId);
                return irf;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateIRFRedFlags()
        {
            try
            {
                return await _irfRepository.UpdateIRFRedFlags();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public async Task<IEnumerable<InterviewDetailsVM>> GetSkillChannelStatsDetails(int irfid, string type)
        {
            try
            {
                var interviewlist = await _irfRepository.GetSkillChannelStatsDetails(irfid, type);
                return interviewlist;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRF> GetSkillChannelDetailsByName(string irfName)
        {
            try
            {
                var result = await _irfRepository.GetSkillChannelDetailsByName(irfName);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


    }
}
