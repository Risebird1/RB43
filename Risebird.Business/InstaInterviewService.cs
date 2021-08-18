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

        public InstaInterviewService(IApplicationErrorLogger applicationErrorLogger, IInstaInterviewRepository instaInterviewRepository, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _instaInterviewRepository = instaInterviewRepository;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }
        public List<InstaMeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                return _instaInterviewRepository.GetAllInterviewMeetings(startDate, endDate, emailId).Result.OrderByDescending(x => x.RequestedDate).ToList();
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
                    //Send mail to Client
                    Mail _mail = new Mail
                    {
                        TO = meeting.RequestedBy,
                        Subject = "Risebird: Insta interview request"
                    };
                    var emailsend = _notificationHelper.InstaClientNotification(_mail, meeting.RequiredSkill);
                    //meeting.RequestedBy
                    //Send mail to Interviewers
                    var Interviewers = _instaInterviewRepository.FetchInstaIntervieweByAssessmentGroup(meeting.AssessmentGroup);
                    Mail _mail1 = new Mail
                    {
                        TO = string.Join(";", Interviewers),
                        Subject = "Risebird: Insta interview request"
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
                if (checkDateMeeting != null && checkDateMeeting.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled at this time. Please contact Risebird HR.";
                }
                var clashingInterviews = assignedMeeting.Where(x => (x.interviewDate > interviewDate && x.interviewDate <= interviewDate.AddMinutes(30)) || (x.interviewDate < interviewDate && interviewDate.AddMinutes(-30) <= x.interviewDate));
                if (clashingInterviews != null && clashingInterviews.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled within 30 minutes. Please contact Risebird HR.";
                }
                return _instaInterviewRepository.InstaAssignYourself(meetingid, interviewerMail);

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
    }
}
