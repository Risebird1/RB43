using System;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.CommonHelper;

namespace Risebird.Business
{
    public class MeetingService : IMeetingService
    {
        private IMeetingRespository _meetingRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;

        public MeetingService(IMeetingRespository meetingRespository, IIRFService irfFService, INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _meetingRespository = meetingRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfFService;
            _notificationHelper = notificationHelper;
        }

        public List<Meeting> GetMeetingList()
        {
            try
            {
                return _meetingRespository.GetAllInterviewMeetings().Result.OrderByDescending(x => x.dtCreatedOn).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public bool AddMeeting(MeetingVM meeting)
        {
            try
            {
                if (_meetingRespository.AddMeeting(meeting).Result)
                {
                    var interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(meeting.IRFId);
                    if (interviwers != null && string.IsNullOrEmpty(meeting.interviewer))
                    {
                        _notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.interviewDate.ToString());
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(meeting.interviewer))
                    {
                        _notificationHelper.SendInterviewerMailOnAssinment(meeting.interviewer, meeting.interviewDate.ToString());
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

        public MeetingVM GetMeeting(int id)
        {
            try
            {
                var meeting = _meetingRespository.GetInterviewMeetingById(id);
                if (meeting == null)
                    return null;
                else
                    return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public SelectList GetIRFList()
        {
            try
            {
                return new SelectList(_meetingRespository.GetIRFList(), "IRFId", "IRFId");
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<TransClientUser> GetClientsByRoles(string IRF)
        {
            try
            {
                
                return _meetingRespository.GetClientsByRoles(IRF);
                
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateMeeting(MeetingVM meetingToUpdate)
        {
            try
            {
                return _meetingRespository.UpdateMeeting(meetingToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public List<Meeting> GetAllOpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = _meetingRespository.GetAllOpenInterviews(emailId).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetAllQCInterviews(string emailId)
        {
            try
            {
                var qcMeeting = _meetingRespository.GetAllQCInterviews(emailId).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
                return qcMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<MeetingVM> GetAllClientInterviews(string emailId)
        {
            try
            {
                var clientMeeting = _meetingRespository.GetAllClientInterviews(emailId).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
                return clientMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetAllAssignedInterviews(string emailID)
        {
            try
            {
                var assignedMeeting = _meetingRespository.GetAllAssignedInterview(emailID).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Assing Yourself
        /// </summary>
        /// <param name="meetingid"></param>
        /// <param name="interviewerMail"></param>
        /// <returns></returns>
        public string AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                return _meetingRespository.AssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }

        public bool UnassignYourself(int meetingid)
        {
            try
            {
                return _meetingRespository.UnassignYourself(meetingid).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public InterviewComments GetInterviewComments(int interviewId)
        {

            try
            {
                var comments = _meetingRespository.GetInterviewComments(interviewId);

                if (comments == null)
                    return null;
                else
                    return comments.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<SkillComment> GetTechSkillComments(int IRFid)
        {

            try
            {
                var comments = _meetingRespository.GetTechSkillComments(IRFid);

                if (comments == null)
                    return null;
                else
                    return comments;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateComments(InterviewComments comments)
        {
            try
            {
                return _meetingRespository.UpdateComments(comments).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<InterviewReport> GetInteviewReport(int interviewId, int irfId)
        {
            try
            {
                //check whether an IRF exists for the given IRFId
                var check = _meetingRespository.GetIRFbyId(irfId);
                if(!check)
                {
                    return null;
                }
                // check whether a feedback for the interviewId has already been submitted
                var interviewReport = _meetingRespository.GetInterviewReportById(interviewId, irfId);
                if (interviewReport.Count() > 0 && interviewReport.FirstOrDefault().Rating > 0)
                {
                    //call Edit Feedbackform method
                    return EditInterviewReport(interviewReport.FirstOrDefault());
                }
                else
                {
                    var model = interviewReport.Count() > 0 ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //get all values from database
                    var levels = _meetingRespository.GetLevels();
                    var levelTech = _meetingRespository.GetLevelTech();
                    var skills = _meetingRespository.GetSkills();
                    var interviewDetails = _meetingRespository.GetInterviewMeetingById(interviewId).Result;
                    //var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
                    var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(irfId);
                    var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
                    var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
                    var selectabilityDetails = _meetingRespository.GetTransSelectability(irfId);
                    var techSkillList = new List<TechSkillReport>();


                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillReport obj = new TechSkillReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                        obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                        obj.WeightageRequired = item.Weightage;

                        techSkillList.Add(obj);
                    }
                    model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;

                    var genSkillList = new List<SkillGeneralReport>();

                    foreach (var item in skillGeneralDetails)
                    {
                        SkillGeneralReport obj = new SkillGeneralReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        genSkillList.Add(obj);
                    }

                    model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)genSkillList;

                    var joinabilityList = new List<JoinabilityReport>();


                    foreach (var item in joinabilityDetails)
                    {
                        JoinabilityReport obj = new JoinabilityReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Name = item.Name;
                        obj.Type = item.Type;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        joinabilityList.Add(obj);
                    }
                    model.JoinabilityReportCollection = (IList<JoinabilityReport>)joinabilityList;

                    var selectabilityList = new List<SelectabilityReport>();
                    foreach (var item in selectabilityDetails)
                    {
                        SelectabilityReport obj = new SelectabilityReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        selectabilityList.Add(obj);
                    }

                    model.SelectabilityReportCollection = (IList<SelectabilityReport>)selectabilityList;

                    model.InterviewId = interviewId;
                    model.CandidateName = interviewDetails.candidateName;
                    model.interviewDate = interviewDetails.interviewDate.ToString();
                    model.resumeLink = interviewDetails.resumeLink;
                    model.IRFId = irfId;
                    model.IRFName = _meetingRespository.GetIRFName(irfId).FirstOrDefault();
                    model.JobLevelName = Enum.GetName(typeof(JobLevels), interviewDetails.JobLevel);
                    model.currentEmployerName = interviewDetails.currentEmployerName;
                    model.expectedPerHourRate = interviewDetails.expectedPerHourRate;
                    model.PreferredLocation = interviewDetails.preferredLocation;
                    return model;
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private InterviewReport EditInterviewReport(InterviewReport model)
        {
            model.JoinabilityReportCollection = (IList<JoinabilityReport>)(_meetingRespository.GetJoinabilityReport(model.InterviewId));
            model.SelectabilityReportCollection = (IList<SelectabilityReport>)(_meetingRespository.GetSelectabilityReport(model.InterviewId));
            model.QuestionsCollection = (IList<QuestionsDiscussed>)(_meetingRespository.GetQuestionsDiscussed(model.InterviewId));
            model.TechSkillReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));
            model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)(_meetingRespository.GetSkillGeneralReport(model.InterviewId));
            model.Proctoring = _meetingRespository.GetProctoringReport(model.InterviewId);
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _meetingRespository.GetInterviewMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            // model.MeetingStatus = Enum.GetName(typeof(MeetingStatus), model.MeetingStatusList);

            return model;
        }

        public async Task<bool> AddInterviewReport(InterviewReport reportModel)
        {
            try
            {
                return _meetingRespository.AddInterviewReport(reportModel).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}


