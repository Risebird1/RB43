using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
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
    public class InstaInterviewService : IInstaInterviewService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IInstaInterviewRepository _instaInterviewRepository;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFRepository _irfRepository;

        public InstaInterviewService(IApplicationErrorLogger applicationErrorLogger, IInstaInterviewRepository instaInterviewRepository, IMeetingRespository meetingRespository, INotificationHelper notificationHelper, IIRFRepository irfRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _instaInterviewRepository = instaInterviewRepository;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
            _irfRepository = irfRepository;
        }
        public IEnumerable<InstaMeetingVM> GetAllInstaRequests(string emailId, string profileName)
        {
            try
            {
                var irfList = _instaInterviewRepository.GetAllInstaRequests(emailId, profileName).Result.ToList();
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool CreateRequest(InstaMeetingVM meeting)
        {
            try
            {
                return _instaInterviewRepository.CreateRequest(meeting).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public InstaMeetingVM FetchRequestById(int id)
        {
            try
            {
                var result = _instaInterviewRepository.FetchRequestById(id).Result;
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new InstaMeetingVM();
            }
        }
        public List<InstaMeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate, int id, string emailId = null)
        {
            try
            {
                return _instaInterviewRepository.GetAllInterviewMeetings(startDate, endDate, id, emailId).Result.OrderByDescending(x => x.RequestedDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool AddInstaMeeting(InstaMeetingVM meeting)
        {
            try
            {
                if (_instaInterviewRepository.AddInstaMeeting(meeting).Result)
                {
                    var model = _instaInterviewRepository.FetchRequestById(meeting.RequestId);
                    string RequiredSkill = String.Empty;
                    foreach (var item in model.Result.TechSkill)
                    {
                        if (String.IsNullOrEmpty(RequiredSkill))
                            RequiredSkill = item.SkillId;
                        else
                            RequiredSkill = RequiredSkill + "," + item.SkillId;
                    }
                    //Send mail to Client
                    Mail _mail = new Mail
                    {
                        TO = model.Result.RequestedBy,
                        Subject = "Risebird: Insta interview request for " + meeting.CandidateName
                    };
                    var emailsend = _notificationHelper.InstaClientNotification(_mail, RequiredSkill);
                    //meeting.RequestedBy
                    //Send mail to Interviewers
                    var Interviewers = _instaInterviewRepository.FetchInstaIntervieweByAssessmentGroup(model.Result.AssessmentGroup);
                    Mail _mail1 = new Mail
                    {
                        TO = string.Join(";", Interviewers),
                        Subject = "Risebird: Insta interview request for " + meeting.CandidateName
                    };
                    var emailsent = _notificationHelper.InstaInterviewerNotification(_mail1, meeting.RequiredSkill);
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
        public async Task<InstaMeetingVM> GetInstaMeeting(int id)
        {
            try
            {
                var meeting = await _instaInterviewRepository.GetInstaMeeting(id);
                return meeting;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateInstaMeeting(InstaMeetingVM meeting)
        {
            try
            {
                if (_instaInterviewRepository.UpdateInstaMeeting(meeting).Result)
                {
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
        public string FetchSkillsByAssessmentGroup(int id)
        {
            try
            {
                var meeting = _instaInterviewRepository.FetchSkillsByAssessmentGroup(id);
                return meeting;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public List<InstaMeetingVM> GetAllInstaOpenInterviews(string emailID)
        {
            try
            {
                var openMeeting = new List<InstaMeetingVM>();
                openMeeting = _instaInterviewRepository.GetAllInstaOpenInterviews(emailID).Result.ToList();
                openMeeting = openMeeting.OrderByDescending(x => x.RequestedDate).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string InstaAssignYourself(int meetingid, string interviewerMail, DateTime interviewDate)
        {
            try
            {
                var assignedMeeting = _meetingRespository.GetAllAssignedInterviewsForTheWeek(interviewerMail).Result;
                var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == interviewDate);
                var clashingInterviews = assignedMeeting.Where(x => (x.interviewDate > interviewDate && x.interviewDate < interviewDate.AddMinutes(60)) || (x.interviewDate < interviewDate && interviewDate.AddMinutes(-60) < x.interviewDate));
                if (checkDateMeeting != null && checkDateMeeting.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled at this time. Please contact Risebird HR.";
                }
                else if (clashingInterviews != null && clashingInterviews.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled within 60 minutes. Please contact Risebird HR.";
                }
                else
                {
                    return _instaInterviewRepository.InstaAssignYourself(meetingid, interviewerMail);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assign interview! Please contact Risebird HR.";
            }
        }
        public List<InstaMeetingVM> GetAllInstaAssignedInterviews(string emailID)
        {
            try
            {
                var assignedMeeting = _instaInterviewRepository.GetAllInstaAssignedInterviews(emailID).Result.OrderByDescending(x => x.RequestedDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<InstaMeetingVM> GetInstaInteviewReport(int interviewId)
        {
            try
            {
                var interviewDetails = _instaInterviewRepository.GetInstaInteviewReport(interviewId).Result;
                return interviewDetails;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddInstaInterviewReport(InstaMeetingVM reportModel, string emailId)
        {
            try
            {
                var result = await _instaInterviewRepository.AddInstaInterviewReport(reportModel);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public string UpdateInstaZoomLink(int meetingid, string Zoomlink, DateTime starttime, DateTime endtime, string accountEmail)
        {
            try
            {
                var result = _instaInterviewRepository.UpdateInstaZoomLink(meetingid, Zoomlink, accountEmail);
                var succeresult = _instaInterviewRepository.UpdateInstaZoomDetails(starttime, endtime, accountEmail);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed";
            }
        }
        public async Task<string> CheckInstaMeetingAccount(DateTime startTime, DateTime endTime)
        {
            try
            {
                return await _instaInterviewRepository.CheckInstaMeetingAccount(startTime, endTime);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
    }
}
