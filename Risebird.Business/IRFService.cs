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

        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger,
            INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration, IActivityTrackerRepository activityTrackerRepository, IWhatsAppHelper whatsAppHelper, ITwilioHelper twilioHelper, ICalendlyHelper calendlyHelper, ICandidateRepository candidateRepository, IInterviewSchedulerRepository interviewSchedulerRepository)
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
        }

        #region IRF Functions
        public async Task<bool> AddIRFDetails(IRFVm irfDetails, string loggedInUser, string domain)
        {
            try
            {
                irfDetails.IRFId = irfDetails.IRFId.Trim();
                irfDetails.CreatedOn = DateTime.UtcNow;
                int irfId = 0;
                var result = _irfRepository.AddIRFDetails(irfDetails, out irfId);
                if (result)
                {
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

                //var call = _twilioHelper.MakePhoneCall("+919284487005");

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

        public IEnumerable<Interviewer> GetInterviewers(string assessmentgroupid)
        {
            try
            {
                var interviewers = _irfRepository.GetIRFInterviewerSkills(assessmentgroupid);
                //var assessmentgroupidlist = assessmentgroupid.Split(",");
                //var list = new List<Interviewer>();
                //foreach (var item in interviewers ?? new List<Interviewer>())
                //{
                //    foreach (var groupid in assessmentgroupidlist ?? new List<string>().ToArray())
                //    {
                //        if (groupid == "" || String.IsNullOrEmpty(item.AssessmentGroupId))
                //        {
                //            continue;
                //        }
                //        if (item.AssessmentGroupId.Contains(groupid))
                //        {
                //            list.Add(item);
                //            break;
                //        }
                //    }
                //}
                //list = list.Distinct().ToList();
                return interviewers;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        /// <summary>
        /// TODO Move this code in stored Procedure
        /// </summary>
        /// <param name="assessmentgroupid"></param>
        /// <returns></returns>
        public IEnumerable<Interviewer> GetInterviewersForIRF(string assessmentgroupid)
        {
            try
            {
                //var interviewers = _irfRepository.GetInterviewerSkills().Where(x=> x.IsApproved==true);
                var interviewers = _irfRepository.GetIRFInterviewerSkills(assessmentgroupid);
                interviewers = interviewers.Distinct().ToList();
                return interviewers;
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


        public IEnumerable<Skill> GetTechSkills(int id)
        {
            try
            {
                List<Skill> list = new List<Skill>();
                var skills = _irfRepository.GetSkillsbyIRFid(id);
                var result = _irfRepository.GetSkills();
                foreach (var item in skills ?? new List<TransSkill>())
                {

                    var obj = result.FirstOrDefault(x => x.id == item.SkillId);
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
                return _irfRepository.AddSkillComments(skillComments).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
                return _irfRepository.ArchiveIRF(id).Result;
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
                return _irfRepository.UnArchiveIRF(id).Result;
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

        public async Task<IRFVm> GetEditIRF(int id, bool isClone = false)
        {
            try
            {
                var model = new IRFVm();

                //get all values from database
                var IRF = _irfRepository.EditIRF(id);
                var transTechSkills = _irfRepository.GetSkillsbyIRFid(id);
                var transSkillsGeneral = _irfRepository.GetSkillsGeneralbyIRFid(id);
                var transJoinability = _irfRepository.GetTransJoinability(id);
                //var transClientUser = _irfRepository.GetTransClientUser(id);
                var transInterviewer = _irfRepository.GetTransInterviewer(id);
                var transQualityController = _irfRepository.GetTransQualityController(id);
                var transSelectability = _irfRepository.GetTransSelectability(id);
                var transEmergencyInterviewer = _irfRepository.GetTransEmergencyInterviewer(id);
                var transResumeEvaluator = _irfRepository.GetTransResumeEvaluator(id);
                var transHR = _irfRepository.GetTransHR(id);
                var transRecruiter = _irfRepository.GetTransRecruiter(id);

                //assign values retrieved to the model
                model.id = IRF.id;
                model.ClientCompName = IRF.ClientCompName;
                model.IRFId = IRF.IRFId;
                model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                model.EmergencyInterviewerCollection = (IList<TransEmergencyInterviewer>)(transEmergencyInterviewer);
                model.JDLink = IRF.JDLink;
                model.JDTiltle = IRF.JDTiltle;
                model.JobCountry = IRF.JobCountry;
                model.Joinability = (IList<TransJoinability>)(transJoinability);
                model.NoOfIntterview = IRF.NoOfIntterview;
                model.NoOfPosition = IRF.NoOfPosition;
                model.PerMintuteRate = IRF.PerMintuteRate;
                model.QualityController = (IList<TransQualityController>)(transQualityController);
                model.RisebirdHr = IRF.RisebirdHr;
                model.CompanyDetails = IRF.CompanyDetails;
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                model.SkillGeneral = (IList<TransSkillGeneral>)(transSkillsGeneral);
                model.Selectability = (IList<TransSelectability>)(transSelectability);
                model.StartDate = IRF.StartDate;
                if (!isClone)
                {
                    var transClientUser = _irfRepository.GetTransClientUser(id);
                    model.ClientCollection = (IList<TransClientUser>)(transClientUser);
                }
                model.ResumeEvaluatorCollection = (IList<TransResumeEvaluator>)(transResumeEvaluator);
                model.RecruiterCollection = (IList<TransRecruiter>)(transRecruiter);
                model.HRCollection = (IList<TransHR>)(transHR);
                model.ClientId = IRF.ClientId;
                model.ClientName = IRF.ClientName;
                model.CompanyType = IRF.CompanyType;
                model.EndDate = IRF.EndDate;
                model.JobLevel = IRF.JobLevel;
                model.CalendlyLink = IRF.CalendlyLink;
                model.TotalInvites = IRF.TotalInvites;
                model.TotalBudget = IRF.TotalBudget;
                model.IsBiometricEnabled = IRF.IsBiometricEnabled;
                model.PriorityLevel = IRF.PriorityLevel;
                model.Heuristicsscore = IRF.Heuristicsscore;
                model.Type = IRF.BussinessType;
                model.PrimarySkillInvite = IRF.PrimarySkillInvite;
                model.AssessmentGroupId = IRF.AssessmentGroupId;
                model.BillingOwner = IRF.BillingOwner;
                var skillCount = transTechSkills.Count();
                model.OldAssessmentGroupId = model.AssessmentGroupId;
                model.OldCalendlyLink = model.CalendlyLink;
                model.OldJDLink = model.JDLink;
                model.OldInterviewerCollection = model.InterviewerCollection;
                model.OldEmergencyInterviewerCollection = model.EmergencyInterviewerCollection;
                model.OldSkillCollection = model.SkillCollection;
                model.IsQCRequired = IRF.IsQCRequired;
                model.IsJoinabilitySelectabilityRequired = IRF.IsJoinabilitySelectabilityRequired;
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
                var result = _irfRepository.UpdateIRFDetails(model, interviewerChangesData ?? null, removedInterviewers ?? null, addedNewInterviewers ?? null, removedTechSkills ?? null).Result;
                if (result)
                {
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
                return result;
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
                return _irfRepository.CheckUniqueness(value).Result;
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
                var result = _irfRepository.AddCandidate(obj).Result;
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

        public async Task<bool> SendInvite(int irfId, string email, string loggedUserEmailId)
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
                    var client = _clientService.GetClient(irf.ClientId);
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    //var staffingAgency = await _candidateRepository.GetCandidateStaffingAgency(candidate.id);
                    string primarySkill = string.Empty;
                    primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];
                    string oneTimeSchedulingLink = String.Empty;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length];
                        var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length];
                        oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid);
                        if (String.IsNullOrEmpty(oneTimeSchedulingLink))
                        {
                            oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid);
                        }
                    }
                    else
                    {
                        oneTimeSchedulingLink = _calendlyHelper.GenerateOneTimeSchedulingLink(irfId).Result;
                    }
                    if (irf.BussinessType == "B2B")
                    {
                        //_notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink);
                        _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink)) ? oneTimeSchedulingLink : calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource);

                    }
                    else
                    {
                        //_notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact);
                        _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink)) ? oneTimeSchedulingLink : calendlyLink, email, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource);

                    }
                    WhatsAppVM _whatsapp = new WhatsAppVM();
                    _whatsapp.Emailid = email;
                    _whatsapp.InterviewDate = primarySkill;
                    _whatsapp.Header = client.Name;
                    if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                    {
                        _whatsapp.Event = "Interview Schedule Risebird";
                    }
                    else
                    {
                        _whatsapp.Event = "Interview Schedule";
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
                    _whatsAppHelper.WhatsAppSendScheduling(_whatsapp);
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
                    var ClientName = _clientService.GetClient(irf.ClientId)?.Name;
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    var result = _notificationHelper.CancelCandidateInvite(ClientName, irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0], email);
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
                return _irfRepository.AddUpdateInterviewPlanner(model).Result;
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
                var candidates = _irfRepository.GetIRFCandidates(irfId);
                if (candidates.Result.Count() == 0)
                {
                    //get candidates from Meeting table by passing IRFId
                    emailList = _irfRepository.GetIRFCandidatesEmail(irfName).ToList();
                    funnelData.TotalCandidates = emailList.Count();
                    funnelData.TotalInvites = emailList.Count();
                }
                else
                {
                    emailList = candidates.Result.Select(x => x.EmailId).ToList();
                    funnelData.TotalCandidates = candidates.Result.Count();
                    funnelData.TotalInvites = candidates.Result.Where(x => x.TotalInvites > 0).Count();
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
        public bool PauseIRF(int irfId, string irfName)
        {
            try
            {
                //update meeting status
                var updatedMeetings = _irfRepository.UpdateMeetingStatus(irfName).Result.ToList();

                //send mails
                if (updatedMeetings != null && updatedMeetings.Count() != 0)
                {
                    //get emails
                    foreach (var item in updatedMeetings ?? new List<MeetingVM>())
                    {
                        _notificationHelper.SendInterviewCancelledMail(item.interviewer, item.interviewDate.ToString("MMMM dd, yyyy"), item.IRFId);
                        //insert meeting in PausedIRF
                        // _irfRepository.AddPausedMeetings(irfId, item.imid);


                        _notificationHelper.SendInterviewCancelledMailtoCandidate(item.candidateEmail, item.ClientHrContact, item.interviewDate.ToString("MMMM dd, yyyy"));

                    }
                    //_notificationHelper.CancelCandidateInvite(ClientName, irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0], email);

                }
                return true;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public IEnumerable<IRF> GetAllIRFs(string emailId, string profileName)
        {
            try
            {
                var irfList = _irfRepository.GetAllIRFs(emailId, profileName).Result.ToList();
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> SubscribeCuratedGroup(int id, string emailId, string profile)
        {
            try
            {
                return _irfRepository.SubscribeCuratedGroup(id, emailId, profile).Result;
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
                return _irfRepository.UnSubscribeCuratedGroup(id, emailId, profile).Result;
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
                return _irfRepository.AddCuratedGroup(irfList, cgName).Result;
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
        public async Task<bool> AddCandidateStatus(string performedBy, int candidateId, int irfId, string comments, int status, string audioLink = null)
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

        public IEnumerable<Level> GetLevels()
        {
            try
            {
                return _irfRepository.GetLevels();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<LevelTech> GetLevelTech()
        {
            try
            {
                return _irfRepository.GetLevelTech();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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


        public async Task<StringBuilder> AddQuickMultipleCandidates(List<CandidateQuickAddModel> candidateList, string emailId, string profile, int irfId, bool isSendInvite)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                var userId = _irfRepository.GetUserId(emailId, profile);
                var data = _irfRepository.AddQuickMultipleCandidates(candidateList, userId).Result;

                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var jdLink = irf.JDLink;
                var client = _clientService.GetClient(irf.ClientId);
                var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                string primarySkill = string.Empty;
                primarySkill = irf.PrimarySkillInvite != null ? irf.PrimarySkillInvite : irf.JDTiltle.Split("|")[0];

                if (data != null)
                {
                    if (isSendInvite)
                    {

                        //int i = 0;

                        Parallel.ForEach(data.Rows.OfType<System.Data.DataRow>(), row =>
                       {
                           string value = row.ItemArray[1].ToString();
                           if (!string.IsNullOrEmpty(value) && value.Contains("successfully"))
                           {
                               string candidateEmail = row.ItemArray[0].ToString();

                               if (candidateEmail != null && calendlyLink != null)
                               {
                                   var candidate = _irfRepository.GetCandidateByIRF(irfId, candidateEmail).Result;
                                   string mailResult = string.Empty;
                                   string oneTimeSchedulingLink = String.Empty;
                                   if (calendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                                   {
                                       string stepfirst = calendlyLink.Split("?")[calendlyLink.Split("?").Length];
                                       var olduniqueid = stepfirst.Split("=")[stepfirst.Split("=").Length];
                                       oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid);
                                       if (String.IsNullOrEmpty(oneTimeSchedulingLink))
                                       {
                                           oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid);
                                       }
                                   }
                                   else
                                   {
                                       oneTimeSchedulingLink = _calendlyHelper.GenerateOneTimeSchedulingLink(irfId).Result;
                                   }

                                   if (irf.BussinessType == "B2B")
                                   {
                                       mailResult = _notificationHelper.SendB2BCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink)) ? oneTimeSchedulingLink : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, client.LogoLink, client.CompanyLink, client.CompanyVideoLink, candidate.ProfileSource);
                                   }
                                   else
                                   {
                                       mailResult = _notificationHelper.SendCandidateInvite(client.Name, jdLink, irfClientUsers.ToList(), primarySkill, (!string.IsNullOrEmpty(oneTimeSchedulingLink)) ? oneTimeSchedulingLink : calendlyLink, candidateEmail, candidate.Name, candidate.ClientHRAttemptContact, candidate.ProfileSource);
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
                                       _twilioHelper.AutoCallCandidate(candidate.Phone, irfId, candidate.CandidateMasterId, emailId);

                                   }
                                   catch (Exception ex)
                                   {
                                       _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public IEnumerable<Skill> GetSkills()
        {
            try
            {
                return _irfRepository.GetSkills();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
        public IEnumerable<TransSkill> GetTechSkillswithWeightage(int id)
        {
            try
            {
                var skills = _irfRepository.GetSkillsbyIRFid(id);
                return skills;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
    }
}
