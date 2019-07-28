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

namespace Risebird.Business
{
    public class MeetingService : IMeetingService
    {
        private IMeetingRespository _meetingRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;

        public MeetingService(IMeetingRespository meetingRespository, IIRFService irfFService, INotificationHelper notificationHelper,IApplicationErrorLogger applicationErrorLogger)
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
                return  _meetingRespository.GetAllInterviewMeetings().Result.OrderByDescending(x => x.dtCreatedOn).ToList();
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
                    if (interviwers != null)
                    {
                        _notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.interviewDate.ToString());
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

        public List<Meeting> GetAllOpenInterviews(string emailId)
        {
            try
            {
               var openMeeting= _meetingRespository.GetAllOpenInterviews(emailId).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
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

        public List<ClientInterviewVM> GetAllClientInterviews(string emailId)
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
        public bool UpdateComments(InterviewComments comments)
        {
            try
            {
                return _meetingRespository.UpdateComments(comments).Result;
            }
            catch(Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<InterviewReport> GetInteviewReport(int interviewId, int irfId)
        {
            try
            {
                var model = new InterviewReport();

                //get all values from database
                var levels = _meetingRespository.GetLevels();
                var skills = _meetingRespository.GetSkills();
                var interviewDetails = _meetingRespository.GetAllInterviewMeetings().Result.Where(x=> x.id==interviewId).FirstOrDefault();
                var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
                var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(irfId);
                var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
                var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
                var techSkillList = new List<TechSkillReport>();


                //assign values to model
                foreach (var item in techSkillsDetails)
                {
                    TechSkillReport obj = new TechSkillReport();
                    obj.InterviewId = interviewId;
                    obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                    obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
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
                    obj.Type = item.Type;
                    obj.WeightageRequired = item.Weightage;
                    joinabilityList.Add(obj);
                }
                model.JoinabilityReportCollection = (IList<JoinabilityReport>)joinabilityList;
                var quesList = new List<QuestionsDiscussed>();

                foreach (var item in questionDetails)
                {
                    QuestionsDiscussed obj = new QuestionsDiscussed();
                    obj.InterviewId = interviewId;
                    obj.Comment = item.Comment;
                    quesList.Add(obj);
                }
                model.QuestionsCollection = (IList<QuestionsDiscussed>)quesList;
                model.InterviewId = interviewId;
                model.CandidateName = interviewDetails.candidateEmail;
                model.IRFId = irfId;
               
                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
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





