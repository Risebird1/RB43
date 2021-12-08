using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
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
    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRespository _meetingRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IUserService _userService;
        private readonly IEvaluatorRepository _evaluatorRespository;
        private readonly IVoiceItRepository _voiceItRepository;
        private readonly ISupportRepository _supportRespository;
        private readonly IActivityTrackerRepository _activityTrackerRepository;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly IInterviewerRepository _interviewerRepository;
        private readonly ICandidateRepository _candidateRepository;

        public MeetingService(IMeetingRespository meetingRespository, IIRFService irfFService, IUserService userService, INotificationHelper notificationHelper, IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger, IEvaluatorRepository evaluatorRespository, IVoiceItRepository voiceItRepository, ISupportRepository supportRespository, IActivityTrackerRepository activityTrackerRepository, IWhatsAppHelper whatsAppHelper, IInterviewerRepository interviewerRepository, ICandidateRepository candidateRepository)
        {
            _meetingRespository = meetingRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfFService;
            _notificationHelper = notificationHelper;
            _irfRepository = irfRepository;
            _userService = userService;
            _evaluatorRespository = evaluatorRespository;
            _voiceItRepository = voiceItRepository;
            _supportRespository = supportRespository;
            _activityTrackerRepository = activityTrackerRepository;
            _whatsAppHelper = whatsAppHelper;
            _interviewerRepository = interviewerRepository;
            _candidateRepository = candidateRepository;
        }

        public List<MeetingVM> GetMeetingList()
        {
            try
            {
                return _meetingRespository.GetAllInterviewMeetings().Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<MeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                return _meetingRespository.GetAllInterviewMeetings(startDate, endDate, emailId).Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid, bool IsClientCurated = false)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetings(clientId, profile, Emailid, IsClientCurated);
                return result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetRiseTubeMeetingsPaged(string profile, string Emailid, int Page, string searchValue = null, int clientId = 0, bool IsClientCurated = false)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetingsPaged(profile, Emailid, Page, searchValue, clientId, IsClientCurated);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetRiseTubeMeetingsPagedFiltered(string profile, string Emailid, int Page, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetingsPagedFiltered(profile, Emailid, Page, startDate, endDate, searchValue, clientId);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetRiseTubeMeetingsByScore(int clientId, string profile, string Emailid, bool IsClientCurated = false)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetings(clientId, profile, Emailid, IsClientCurated);
                return result.OrderByDescending(x => x.Score).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetRiseTubeMeetingsByIRFId(int clientId, string Emailid, string irfId, DateTime startDate, DateTime endDate, string profileName = null)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetingsByIRFId(clientId, Emailid, irfId, startDate, endDate, profileName);
                return result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern, DateTime startDate, DateTime endDate, string profileName = null)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetingsByJD(clientId, Emailid, JDpattern, startDate, endDate, profileName);
                return result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetRiseTubeMeetingsForCuratedUserByIRF(string Emailid, string actionStatus, int irfId)
        {
            try
            {
                var result = await _meetingRespository.GetRiseTubeMeetingsForCuratedUserByIRF(Emailid, actionStatus, irfId);
                return result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetCPMeetings(int clientId, string profile, string Emailid)
        {
            try
            {
                var dataResult = await _meetingRespository.GetCPMeetings(clientId, profile, Emailid);
                return dataResult.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// FInd duplicate interview invite based on Skill Channel
        /// </summary>
        /// <param name="IRFId"></param>
        /// <returns></returns>
        public List<MeetingVM> IRFMultipleCandidateInvites(string IRFId)
        {
            try
            {

                return _meetingRespository.IRFMultipleCandidateInvites(IRFId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Delete Interview Record
        /// </summary>
        /// <param name="imid"></param>
        /// <returns></returns>
        public async Task<bool> DeleteInterview(int imid)
        {
            try
            {
                await _meetingRespository.DeleteInterview(imid);
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;

            }
        }

        public List<MeetingVM> MultipleCandidateInvites(string emailid)
        {
            try
            {

                return _meetingRespository.MultipleCandidateInvites(emailid).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<MeetingVM> GetClientMeetingList(string emailId)
        {
            try
            {
                var meetings = _meetingRespository.GetClientMeetings(emailId).Result.OrderByDescending(x => x.interviewDate).ToList();

                return meetings ?? new List<MeetingVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<MeetingVM>> GetClientSelectedMeetings(string emailId, bool isFilter = false, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var meetings = await _meetingRespository.GetClientSelectedMeetings(emailId, isFilter, startDate, endDate);
                return meetings;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<string> GetClientHRContactList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId && x.ClientHrContact != string.Empty).ToList();
                var finalList = meetingList.Select(x => x.ClientHrContact).ToList();

                foreach (var item in finalList ?? new List<string>())
                {
                    if (item != null)
                        item.Replace("\n", "");
                }
                finalList = finalList.Distinct().ToList();

                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<string> GetEndClientList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId).ToList();
                var finalList = meetingList.Select(x => x.ClientName).ToList();
                foreach (var item in finalList ?? new List<string>())
                {
                    item.Replace("\n", "");
                }
                finalList = finalList.Distinct().ToList();

                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<string> GetIRFList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                //var clientHrList = _meetingRespository.GetClientHRContactList().Result.ToList();
                //Get client user details
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId /*&& x.ClientHrContact != string.Empty*/).ToList();
                var finalList = meetingList.Select(x => x.IRFId).ToList();
                finalList = finalList.Distinct().ToList();
                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public bool AddMeeting(AddUpdateMeetingVM meeting)
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

        public async Task<AddUpdateMeetingVM> GetMeeting(int id)
        {
            try
            {
                var meeting = await _meetingRespository.GetInterviewToUpdateMeetingById(id);
                meeting.OldConfirmationStatus = meeting.confirmationStatus;
                meeting.OldHRCoordinatorEmailId = meeting.HRCoordinatorEmailId;
                meeting.OldinterviewDate = meeting.interviewDate;
                meeting.OldInterviewer = meeting.interviewer;
                meeting.OldIRFId = meeting.IRFId;
                meeting.OldQCInterviewer = meeting.QCInterviewer;
                meeting.OldrisebirdHR = meeting.risebirdHR;
                meeting.OldzoomLink = meeting.zoomLink;
                return meeting;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public SelectList GetIRFList()
        {
            try
            {
                return new SelectList(_meetingRespository.GetIRFList().Where(x => x.EndDate > DateTime.Now), "IRFId", "IRFId");
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<SelectList> GetIRFInterviewingMembersEmails(string irfId)
        {
            try
            {
                var list = await _meetingRespository.GetIRFInterviewingMembersEmails(irfId);
                return new SelectList(list ?? new List<string>() { string.Empty });
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<SelectList> GetIRFQualityControllersEmails(string irfId)
        {
            try
            {
                var list = await _meetingRespository.GetIRFQualityControllerEmails(irfId);
                return new SelectList(list ?? new List<string>() { string.Empty });
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<List<ChannelMembersForInterviewData>> GetIRFMembersEmailsForInterviewDetails(string irfId)
        {
            try
            {
                var list = await _meetingRespository.GetIRFMembersEmailsForInterviewDetails(irfId);
                return (list != null) ? list.ToList() : new List<ChannelMembersForInterviewData>();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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


        public bool PostApproval(string imid, string value, string comment)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(imid))
            {
                var val = Convert.ToInt32(value);
                var result = _meetingRespository.PostApproval(Convert.ToInt32(imid), val, comment);
                return result;
            }
            return false;
        }

        public bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP, string clientEmailId)
        {
            if (imid != 0)
            {
                var result = _meetingRespository.SaveClientFeedback(imid, isApproved, approvalComment, isSelected, selectionComment, joiningStatus, joiningComment, offerStatus, offerComment, movetoCP);
                if (movetoCP)
                {
                    //send mail to other client users of the same client
                    //var clientUsers = _meetingRespository.GetClientUsers(clientEmailId);
                    //clientUsers = clientUsers.Where(x => x != clientEmailId);
                    //var candidateDetails = _meetingRespository.GetCandidateDetails(imid);
                    //_notificationHelper.SendCommonPoolMail(clientUsers.ToList(), clientEmailId, candidateDetails);
                }
                return result;
            }
            return false;
        }

        public bool SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled, string oldQcStatus, string performedBy = null)
        {
            if (!string.IsNullOrEmpty(qcStatus) && !string.IsNullOrEmpty(imid))
            {
                var status = Convert.ToInt16(qcStatus);
                var result = _meetingRespository.SaveQCFeedback(Convert.ToInt32(imid), status, comment, FraudLipSync, QuestionsRelevance, HandsOnCoding, BiasAndCandidateHandling, TechnicalCommunicationSkills, ChapteringCompleted, QuestionsFilled, TechSkillsMetric, SelectabilityMetric, JoinabilityMetric, GeneralSkillMetric, CommentsFilled, MeaningfulAssessments, CorrectEnglishGrammar, ProctoringReportFilled);

                if (result && status == (int)QCStatus.Failed)
                {
                    var meetingDetails = _meetingRespository.GetMeetingDetails(Convert.ToInt32(imid)).Result;
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.QCFailed;
                    activityTrackerModel.Particulars = "QC Failed for: " + meetingDetails.candidateEmail + " dated " + meetingDetails.interviewDate.ToString();
                    activityTrackerModel.PerformedBy = performedBy;

                    activityTrackerModel.Summary = "QC Failed";
                    //activityTrackerModel.ResourceUrl = meetingToUpdate.ResourceUrl;

                    _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                //var oldStatus = Convert.ToInt16(oldQcStatus);
                if (result && status == (int)QCStatus.Passed && (oldQcStatus == null || Convert.ToInt16(oldQcStatus) != (int)QCStatus.Passed))
                {
                    var meetingDetails = _meetingRespository.GetMeetingDetails(Convert.ToInt32(imid)).Result;
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.QCPassed;
                    activityTrackerModel.Particulars = "QC Passed for: " + meetingDetails.candidateEmail + " dated " + meetingDetails.interviewDate.ToString();
                    activityTrackerModel.PerformedBy = performedBy;

                    activityTrackerModel.Summary = "QC Passed";
                    //activityTrackerModel.ResourceUrl = meetingToUpdate.ResourceUrl;

                    _activityTrackerRepository.AddActivity(activityTrackerModel);
                    //add candidate status
                    _irfRepository.AddQCCandidateStatus(Convert.ToInt32(imid), performedBy);
                }
                return result;
            }
            return false;
        }
        public bool PostSelectionStatus(string imid, string value, string comment)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(imid))
            {
                var val = Convert.ToInt32(value);
                var result = _meetingRespository.PostSelectionStatus(Convert.ToInt32(imid), val, comment);
                return result;
            }
            return false;
        }

        public async Task<bool> UpdateMeeting(AddUpdateMeetingVM meetingToUpdate)
        {
            try
            {
                if (!string.IsNullOrEmpty(meetingToUpdate.NewActiveInterviewer))
                {
                    meetingToUpdate.interviewer = meetingToUpdate.NewActiveInterviewer;
                }
                if (!string.IsNullOrEmpty(meetingToUpdate.NewActiveQCInterviewer))
                {
                    meetingToUpdate.QCInterviewer = meetingToUpdate.NewActiveQCInterviewer;
                }
                var result = await _meetingRespository.UpdateMeeting(meetingToUpdate);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateMeeting;
                    activityTrackerModel.Particulars = "Meeting with: " + meetingToUpdate.candidateEmail;
                    activityTrackerModel.PerformedBy = meetingToUpdate.LoggedInUser;

                    StringBuilder summary = new StringBuilder();
                    if (meetingToUpdate.OldConfirmationStatus != meetingToUpdate.confirmationStatus)
                    {
                        if (meetingToUpdate.confirmationStatus == ((int)ReceiveMeetingStatus.Cancelled).ToString())
                        {
                            summary.Append("Meeting Cancelled");
                        }
                        else if (meetingToUpdate.confirmationStatus == ((int)ReceiveMeetingStatus.Confirmed).ToString())
                        {
                            summary.Append("Meeting Confirmed");
                        }
                    }
                    if (meetingToUpdate.OldHRCoordinatorEmailId != meetingToUpdate.HRCoordinatorEmailId)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; HR Co-ordinator changed");
                        }
                        else
                        {
                            summary.Append("HR Co-ordinator changed");
                        }
                    }
                    if (meetingToUpdate.OldinterviewDate != meetingToUpdate.interviewDate)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; Interview Date changed");
                        }
                        else
                        {
                            summary.Append("Interview Date changed");
                        }
                    }
                    if (meetingToUpdate.OldInterviewer != meetingToUpdate.interviewer)
                    {
                        if (summary.Length > 0)
                        {
                            if (!string.IsNullOrEmpty(meetingToUpdate.OldInterviewer))
                            {
                                summary.Append("; Interviewer changed from " + meetingToUpdate.OldInterviewer + " to " + meetingToUpdate.interviewer);
                            }
                            else
                            {
                                summary.Append("; Interviewer assigned (" + meetingToUpdate.interviewer + ")");

                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(meetingToUpdate.OldInterviewer))
                            {
                                summary.Append("Interviewer changed from " + meetingToUpdate.OldInterviewer + " to " + meetingToUpdate.interviewer);
                            }
                            else
                            {
                                summary.Append("Interviewer assigned (" + meetingToUpdate.interviewer + ")");
                            }
                        }
                    }
                    if (meetingToUpdate.OldIRFId != meetingToUpdate.IRFId)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; Skill Channel changed");
                        }
                        else
                        {
                            summary.Append("Skill Channel changed");
                        }
                    }
                    if (meetingToUpdate.OldQCInterviewer != meetingToUpdate.QCInterviewer)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; QC Interviewer changed");
                        }
                        else
                        {
                            summary.Append("QC Interviewer changed");
                        }
                    }

                    if (meetingToUpdate.OldrisebirdHR != meetingToUpdate.risebirdHR)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; Zoom Login Details changed");
                        }
                        else
                        {
                            summary.Append("Zoom Login Details changed");
                        }
                    }
                    if (meetingToUpdate.OldzoomLink != meetingToUpdate.zoomLink)
                    {
                        if (summary.Length > 0)
                        {
                            summary.Append("; Zoom Link changed");
                        }
                        else
                        {
                            summary.Append("Zoom Link changed");
                        }
                    }
                    if (summary.Length == 0)
                    {
                        summary.Append("Meeting updated");
                    }
                    activityTrackerModel.Summary = summary.ToString();
                    activityTrackerModel.ResourceUrl = meetingToUpdate.ResourceUrl;

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

        public List<Meeting> GetAllOpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = new List<Meeting>();
                openMeeting = _meetingRespository.GetAllOpenInterviews(emailId).Result.ToList();//.OrderByDescending(x => x.interviewDate).ToList();
                var emergencyMeetings = _meetingRespository.GetAllEmergencyOpenInterviews(emailId).Result.ToList();
                if (emergencyMeetings != null)
                {
                    foreach (var item in emergencyMeetings ?? new List<Meeting>())
                    {
                        openMeeting.Add(item);
                    }
                }
                openMeeting = openMeeting.Distinct().ToList();
                openMeeting = openMeeting.OrderByDescending(x => x.interviewDate).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetQCOpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = _meetingRespository.GetQCOpenInterviews(emailId).Result.OrderByDescending(x => x.interviewDate).ToList().Where(x => x.QCInterviewer == null).ToList();
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
                var qcMeeting = _meetingRespository.GetAllQCInterviews(emailId).Result.OrderByDescending(x => x.interviewDate).ToList();
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
                var assignedMeeting = _meetingRespository.GetAllAssignedInterview(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();

                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<Meeting>> GetCandidateInterviews(string emailID, string status)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = (await _meetingRespository.GetCandidateInterviews(emailID, status)).OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public List<Meeting> GetQualityControlInterviews(string emailID)
        {
            try
            {
                //var finalList = new List<Meeting>();
                var assignedMeeting = _meetingRespository.GetQualityControlInterview(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();

                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        /// <summary>
        /// Assign Yourself
        /// </summary>
        /// <param name="meetingid"></param>
        /// <param name="interviewerMail"></param>
        /// <param name="interviewDate"></param>
        /// <returns></returns>
        //public string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate)
        //{
        //    try
        //    {
        //        //var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
        //        var assignedMeeting = _meetingRespository.GetAllAssignedInterviewsForToday(interviewerMail).Result;
        //        var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == interviewDate);
        //        if (checkDateMeeting !=null && checkDateMeeting.Count() > 0)
        //        {
        //            return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
        //        }
        //        var upComingInterviews = assignedMeeting.Where(x => Convert.ToInt32(Math.Round((x.interviewDate - interviewDate).TotalMinutes)) <= 30);
        //        if(upComingInterviews != null && upComingInterviews.Count()>0)
        //        {
        //            return "Failed! You already have an interview scheduled within the next 30 minutes. Please contact Risebird HR.";

        //        }
        //        return _meetingRespository.AssignYourself(meetingid, interviewerMail);

        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return "Failed to assigned interview, Please contact Risebird HR.";
        //    }
        //    //try
        //    //{
        //    //    var newArr = InterviewDate.Split(" ")[0].Split("/");
        //    //    var date = newArr[1] + "-" + newArr[0] + "-" + newArr[2] +" "+ InterviewDate.Split(" ")[1];
        //    //    var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
        //    //    var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == DateTime.Parse(date));
        //    //    if (checkDateMeeting.Count() > 0)
        //    //    {
        //    //        return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
        //    //    }
        //    //    return _meetingRespository.AssignYourself(meetingid, interviewerMail);

        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    //    return "Failed to assigned interview, Please contact Risebird HR.";
        //    //}
        //}

        public string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate)
        {
            try
            {
                //var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
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
                return _meetingRespository.AssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assign interview! Please contact Risebird HR.";
            }
            //try
            //{
            //    var newArr = InterviewDate.Split(" ")[0].Split("/");
            //    var date = newArr[1] + "-" + newArr[0] + "-" + newArr[2] +" "+ InterviewDate.Split(" ")[1];
            //    var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
            //    var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == DateTime.Parse(date));
            //    if (checkDateMeeting.Count() > 0)
            //    {
            //        return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
            //    }
            //    return _meetingRespository.AssignYourself(meetingid, interviewerMail);

            //}
            //catch (Exception ex)
            //{
            //    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            //    return "Failed to assigned interview, Please contact Risebird HR.";
            //}
        }
        public string QCAssignYourself(int meetingid, string interviewerMail)
        {
            try
            {

                return _meetingRespository.QCAssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }

        public bool UnassignYourself(int meetingid, string email, bool isPenalty)
        {
            try
            {
                var meeting = _meetingRespository.GetInterviewMeetingById(meetingid);

                if (meeting != null && meeting.Result != null)
                {
                    _meetingRespository.UnassignYourself(meetingid);
                    //TimeSpan timeSpan = meeting.Result.interviewDate - DateTime.Now;
                    var interviwers = new List<string>();
                    if (isPenalty)
                    {
                        //update flag for Emergency Interview
                        _irfRepository.UpdateEmergencyFlag(meetingid, email, meeting.Result.interviewDate);
                        interviwers = _irfRepository.GetEmergencyInterviwerBasedOnIRFId(meeting.Result.irfIntId).Result.ToList();
                    }
                    else
                    {
                        interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(meeting.Result.IRFId);
                    }

                    if (interviwers != null)
                    {
                        interviwers.Remove(email);
                        _notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.Result.interviewDate.ToString());

                    }
                    var hrs = _userService.GetAllHrsEmailld()?.ToList();
                    if (hrs != null)
                    {
                        var candidateEmail = meeting.Result.candidateEmail;
                        var Skillchannelid = meeting.Result.IRFId;
                        var client = _interviewerRepository.FetchClientName(meeting.Result.IRFId);
                        _notificationHelper.SendAlertHRForUnassignedInterview(hrs, meeting.Result.interviewDate.ToString(), candidateEmail, email, Skillchannelid, client);

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

        public bool QCUnassignYourself(int meetingid, string email)
        {
            try
            {
                return _meetingRespository.QCUnassignYourself(meetingid).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public bool CancelInterview(int meetingid, string interviewDate, string candidateEmail)
        {
            try
            {
                if (_meetingRespository.CancelInterview(meetingid))
                {
                    //send mail to the HRs
                    var interviewers = _meetingRespository.ListInterviewersMail(candidateEmail, interviewDate);
                    var irfOwnersAndRestrictedOwners = _candidateRepository.GetCandidateStaffingAgencyAndClientHRByInterviewId(meetingid).Result;
                    _notificationHelper.SendInterviewCancelledMail(interviewers, interviewDate, irfOwnersAndRestrictedOwners?.ToList());
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



        public InterviewComments GetInterviewComments(int interviewId)
        {

            try
            {
                return _meetingRespository.GetInterviewComments(interviewId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<SkillComment>> GetTechSkillComments(int irfId, int userId = 0)
        {

            try
            {
                return await _meetingRespository.GetTechSkillComments(irfId, userId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateComments(InterviewComments comments, string performedBy = null)
        {
            try
            {
                var result = _meetingRespository.UpdateComments(comments).Result;
                if (result && comments.QCStatus.HasValue && comments.QCStatus == (int)QCStatus.Failed)
                {
                    //add to activity tracker
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.QCFailed;
                    activityTrackerModel.Particulars = "QC Failed for: " + comments.candidateEmail + " dated " + comments.interviewDate.ToString();
                    activityTrackerModel.PerformedBy = performedBy;

                    activityTrackerModel.Summary = "QC Failed";
                    //activityTrackerModel.ResourceUrl = meetingToUpdate.ResourceUrl;

                    var activityUpdate = _activityTrackerRepository.AddActivity(activityTrackerModel).Result;
                }

                if (result && comments.QCStatus.HasValue && comments.QCStatus == (int)QCStatus.Passed && (comments.OldQCStatus == null || comments.OldQCStatus != (int)QCStatus.Passed))
                {
                    //add to activity tracker
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.QCPassed;
                    activityTrackerModel.Particulars = "QC Passed for: " + comments.candidateEmail + " dated " + comments.interviewDate.ToString();
                    activityTrackerModel.PerformedBy = performedBy;

                    activityTrackerModel.Summary = "QC Passed";
                    //activityTrackerModel.ResourceUrl = meetingToUpdate.ResourceUrl;

                    var activityUpdate = _activityTrackerRepository.AddActivity(activityTrackerModel).Result;


                    //add candidate status
                    _irfRepository.AddQCCandidateStatus(comments.InterviewId, performedBy);

                }
                if (result && ((comments.QCStatus.HasValue && comments.QCStatus == (int)QCStatus.Passed) || (comments.FinalInterviewMeetingStatus.HasValue && (comments.FinalInterviewMeetingStatus == (int)FinalInterviewMeetingStatus.Selected || comments.FinalInterviewMeetingStatus == (int)FinalInterviewMeetingStatus.Completed))))
                {
                    //notify clients
                    var clientEmails = _meetingRespository.GetIRFClientsEmails(comments.irfIntId).Result;

                    var interviewStatus = string.Empty;
                    if (comments.InterviewStatus.HasValue && (comments.InterviewStatus == (int)InterviewStatus.Selected || comments.InterviewStatus == (int)InterviewStatus.BorderLine))
                    {
                        interviewStatus = ((InterviewStatus)comments.InterviewStatus).GetDescription();
                    }
                    if (clientEmails != null && clientEmails.Any() && !string.IsNullOrEmpty(interviewStatus))
                    {
                        _notificationHelper.SendQCPassNotificationToClient(clientEmails.ToList(), comments.candidateEmail, comments.PrimarySkill, interviewStatus, comments.interviewDate.ToString("MMMM dd, yyyy"));
                        //WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        //_whatsAppVM.Emailid = string.Join(";", clientEmails.ToList());
                        //_whatsAppVM.InterviewDate = comments.PrimarySkill;
                        //_whatsAppVM.UserName = comments.candidateEmail;
                        //_whatsAppVM.Event = "ClientCandidateConfirmation";
                        //_whatsAppVM.MessageValue = interviewStatus;
                        //_whatsAppVM.buttonlink = "Risetube/VideoPage/" + comments.InterviewId;
                        //_whatsAppHelper.WhatsAppSendClientCandidateConfirmation(_whatsAppVM);
                    }
                }
                return result;
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
                var reportModel = new InterviewReport();
                var irfDetails = _meetingRespository.GetIRFDetails(irfId);
                var levels = await _meetingRespository.GetLevels();
                //check whether an IRF exists for the given IRFId
                var check = _meetingRespository.GetIRFbyId(irfId);
                var interviewDetails = _meetingRespository.GetInterviewMeetingById(interviewId).Result;

                if (!check)
                {
                    return null;
                }
                // check whether a feedback for the interviewId has already been submitted
                var interviewReport = _meetingRespository.GetInterviewReportById(interviewId, irfId);
                if (interviewReport != null && interviewReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport.FirstOrDefault();
                    await EditInterviewReport(reportModel);
                }
                else
                {
                    var model = (interviewReport != null && interviewReport.Count() > 0) ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //model.IsEdited = true;

                    //get all values from database
                    var levelTech = await _meetingRespository.GetLevelTech();
                    var skills = await _meetingRespository.GetSkills();
                    var techSkillsDetails = await _meetingRespository.GetSkillsbyIRFid(irfId);
                    var skillGeneralDetails = await _meetingRespository.GetSkillsGeneralbyIRFid(irfId);

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

                    var genSkillList = new List<SkillGeneralReport>();

                    foreach (var item in skillGeneralDetails ?? new List<TransSkillGeneral>())
                    {
                        SkillGeneralReport obj = new SkillGeneralReport
                        {
                            InterviewId = interviewId,
                            RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        genSkillList.Add(obj);
                    }

                    model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)genSkillList;

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
                    model.VideoLink = interviewDetails.VideoLink;
                    reportModel = model;
                }
                // joinability and Selectability

                //get candidateId
                reportModel.CandidateId = _meetingRespository.GetCandidateId(interviewId, irfDetails.IRFId);
                reportModel.CandidateName = interviewDetails.candidateName;
                reportModel.TotalExperience = interviewDetails.TotalExperience + " Years";
                reportModel.SelectabilityComments = await _evaluatorRespository.GetSelectabilityComments(irfId, interviewDetails.candidateEmail);
                reportModel.CandidateEmail = interviewDetails.candidateEmail;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;
                var selectabilityReport = await _evaluatorRespository.GetSelectabilityReport(reportModel.CandidateId, irfId);
                if (selectabilityReport != null && selectabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    var selectabilityDetails = await _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails ?? new List<TransSelectability>())
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport
                        {
                            CandidateId = reportModel.CandidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        selectabilityList.Add(obj);
                    }

                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }

                reportModel.JoinabilityComments = await _evaluatorRespository.GetJoinabilityComments(irfId, interviewDetails.candidateEmail);
                var joinabilityReport = await _evaluatorRespository.GetJoinabilityReport(reportModel.CandidateId, irfId);
                if (joinabilityReport != null && joinabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    var joinabilityDetails = await _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails ?? new List<TransJoinability>())
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport
                        {
                            CandidateId = reportModel.CandidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId).Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        joinabilityList.Add(obj);
                    }

                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                reportModel.IRFName = irfDetails.IRFId;
                reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                reportModel.joiningTime = interviewDetails.joiningTime;
                reportModel.ThumbnailTimeSeconds = interviewDetails.ThumbnailTimeSeconds;
                reportModel.VideoDuration = interviewDetails.VideoDuration;
                reportModel.IsQCRequired = interviewDetails.IsQCRequired;
                if (reportModel.IsEdited)
                {
                    reportModel.OldInterviewerComment = reportModel.InterviewerComment;
                    reportModel.OldInterviewStatus = reportModel.InterviewStatus;
                    reportModel.OldMeetingStatus = reportModel.MeetingStatus;
                }

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
            var techReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));
            //model.TechSkillReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));

            if (!techReportCollection.Any())
            {
                var techSkillsDetails = await _meetingRespository.GetSkillsbyIRFid(model.IRFId);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = await _meetingRespository.GetLevelTech();
                var skills = await _meetingRespository.GetSkills();

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


            model.QuestionsCollection = (IList<QuestionsDiscussed>)(_meetingRespository.GetQuestionsDiscussed(model.InterviewId));
            model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)(_meetingRespository.GetSkillGeneralReport(model.InterviewId));
            model.Proctoring = _meetingRespository.GetProctoringReport(model.InterviewId);
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _meetingRespository.GetInterviewMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.encodedresumeLink = interviewMeetingsDetails.encodedresumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            model.OldInterviewStatus = model.InterviewStatus;
            model.OldMeetingStatus = model.MeetingStatus;

            return model;
        }

        public async Task<bool> AddInterviewReport(InterviewReport reportModel, string emailId)
        {
            try
            {
                if(reportModel.MeetingStatus == (int)MeetingStatus.DroppedSelected)
                {
                    reportModel.InterviewStatus = (int)InterviewStatus.Selected;
                }
                else if (reportModel.MeetingStatus == (int)MeetingStatus.DroppedRejected)
                {
                    reportModel.InterviewStatus = (int)InterviewStatus.Rejected;
                }
                else if (reportModel.MeetingStatus == (int)MeetingStatus.DroppedBorderLine)
                {
                    reportModel.InterviewStatus = (int)InterviewStatus.BorderLine;
                }
                var result = await _meetingRespository.AddInterviewReport(reportModel);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    if (!reportModel.IsEdited)
                    {
                        if (reportModel.MeetingStatus.HasValue && reportModel.MeetingStatus.Value != (int)MeetingStatus.Scheduled)
                        {
                            string statusDesc = string.Empty;
                            if(reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected)
                            {
                                statusDesc = MeetingStatus.Completed.GetDescription();
                            }
                            else
                            {
                                statusDesc = ((MeetingStatus)reportModel.MeetingStatus.Value).GetDescription();
                            }
                            await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, statusDesc, (int)CandidateStatusOptions.MeetingStatus, reportModel.InterviewId);
                            if (reportModel.MeetingStatus.Value == (int)MeetingStatus.Completed || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected)
                            {
                                //await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus);

                                if (reportModel.InterviewStatus.HasValue)
                                {
                                    if (!reportModel.IsQCRequired)
                                    {
                                        await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus, reportModel.InterviewId);

                                    }
                                    else
                                    {
                                        if (reportModel.InterviewStatus.Value == (int)InterviewStatus.Proxy || reportModel.InterviewStatus.Value == (int)InterviewStatus.Rejected)
                                        {
                                            await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus, reportModel.InterviewId);

                                        }
                                        else
                                        {
                                            await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, CandidateStatusOptions.QCPending.GetDescription(), (int)CandidateStatusOptions.QCPending, reportModel.InterviewId);

                                        }
                                    }

                                }

                            }

                        }

                        activityTrackerModel.ActivityType = (int)ActivityTypes.CreateCandidateFeedback;
                        activityTrackerModel.Particulars = "Feedback for: " + reportModel.CandidateEmail;
                        activityTrackerModel.PerformedBy = emailId;

                        activityTrackerModel.Summary = "Feedback created";
                        activityTrackerModel.ResourceUrl = reportModel.ResourceUrl;

                        await _activityTrackerRepository.AddActivity(activityTrackerModel);
                    }
                    else
                    {
                        if (reportModel.MeetingStatus != reportModel.OldMeetingStatus)
                        {
                            if (reportModel.MeetingStatus.HasValue && reportModel.MeetingStatus.Value != (int)MeetingStatus.Scheduled)
                            {
                                string statusDesc = string.Empty;
                                if (reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected)
                                {
                                    statusDesc = MeetingStatus.Completed.GetDescription();
                                }
                                else
                                {
                                    statusDesc = ((MeetingStatus)reportModel.MeetingStatus.Value).GetDescription();
                                }
                                await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, statusDesc, (int)CandidateStatusOptions.MeetingStatus, reportModel.InterviewId);

                            }
                        }
                        if (reportModel.OldInterviewStatus != reportModel.InterviewStatus)
                        {
                            if (reportModel.MeetingStatus.Value == (int)MeetingStatus.Completed || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || reportModel.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected)
                            {
                                //await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus);

                                if (reportModel.InterviewStatus.HasValue)
                                {
                                    if (!reportModel.IsQCRequired)
                                    {
                                        await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus, reportModel.InterviewId);

                                    }
                                    else
                                    {
                                        if (reportModel.InterviewStatus.Value == (int)InterviewStatus.Proxy || reportModel.InterviewStatus.Value == (int)InterviewStatus.Rejected)
                                        {
                                            await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, ((InterviewStatus)reportModel.InterviewStatus.Value).GetDescription(), (int)CandidateStatusOptions.InterviewStatus, reportModel.InterviewId);

                                        }
                                        else
                                        {
                                            await AddCandidateStatus(emailId, reportModel.CandidateId, reportModel.IRFId, CandidateStatusOptions.QCPending.GetDescription(), (int)CandidateStatusOptions.QCPending, reportModel.InterviewId);
                                        }
                                    }

                                }

                            }
                        }

                        activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateCandidateFeedback;
                        activityTrackerModel.Particulars = "Feedback for: " + reportModel.CandidateEmail;
                        activityTrackerModel.PerformedBy = emailId;

                        StringBuilder summary = new StringBuilder();
                        if (reportModel.OldInterviewerComment != reportModel.InterviewerComment)
                        {
                            summary.Append("Interviewer comment changed");
                        }
                        if (reportModel.OldInterviewStatus != reportModel.InterviewStatus)
                        {
                            if (summary.Length > 0)
                            {
                                summary.Append("; Interview Status changed");
                            }
                            else
                            {
                                summary.Append("Interview Status changed");
                            }
                        }
                        if (reportModel.OldMeetingStatus != reportModel.MeetingStatus)
                        {
                            if (summary.Length > 0)
                            {
                                summary.Append("; Meeting Status changed");
                            }
                            else
                            {
                                summary.Append("Meeting Status changed");
                            }
                        }

                        if (summary.Length == 0)
                        {
                            summary.Append("Feedback updated");
                        }
                        activityTrackerModel.Summary = summary.ToString();
                        activityTrackerModel.ResourceUrl = reportModel.ResourceUrl;

                        await _activityTrackerRepository.AddActivity(activityTrackerModel);
                    }
                }
                if (reportModel.InterviewStatus.HasValue && !reportModel.IsEdited)
                {
                    if (reportModel.InterviewStatus.Value == 2 || reportModel.InterviewStatus.Value == 4)
                    {

                        var irfQualityControllers = _irfRepository.GetTransQualityController(reportModel.IRFId);
                        var mailIds = new List<string>();
                        foreach (var item in irfQualityControllers ?? new List<TransQualityController>())
                        {
                            if (item.EmailId != null && item.IsActive)
                            {
                                mailIds.Add(item.EmailId);
                            }
                        }

                        _notificationHelper.SendQCInterviewerMail(mailIds, reportModel.interviewDate);

                        if (reportModel.InterviewStatus.Value == 2)
                        {
                            var namemodel = String.Empty;

                            if (reportModel.CandidateName != null && reportModel.CandidateName.Length >= 2)
                                namemodel = namemodel + reportModel.CandidateName?.ToCharArray()[0] + reportModel.CandidateName?.ToCharArray()[1];
                            else if (reportModel.CandidateName != null && reportModel.CandidateName.Length == 1)
                                namemodel += reportModel.CandidateName?.ToCharArray()[0];
                            //get curated users
                            var curatedMailIds = _irfRepository.GetCuratedUserMailId(reportModel.IRFId);
                            _notificationHelper.SendCuratedUserMail(curatedMailIds.ToList(), reportModel.interviewDate, reportModel.IRFName, reportModel.PrimarySkill, namemodel);
                        }

                    }
                }

                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private async Task<bool> AddCandidateStatus(string performedBy, int candidateId, int irfId, string comments, int status, int? interviewId = null, string audioLink = null)
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
                model.InterviewId = interviewId;
                return await _irfRepository.AddCandidateStatus(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<VerifyCandidateAudio> GetCandidateAudios(int candidateId, int irfId)
        {
            try
            {
                var model = new VerifyCandidateAudio
                {
                    CandidateAudios = (IList<CandidateAudio>)(await _evaluatorRespository.GetCandidateAudios(candidateId, irfId)).Where(x => x.IsVoiceBiometric == true).ToList(),
                    CandidateId = candidateId
                };
                if (model.CandidateAudios.Any())
                {
                    model.CandidateName = model.CandidateAudios.First().CandidateName;
                    model.IRFId = irfId;
                    model.IRFName = model.CandidateAudios.First().IRFName;
                    model.VoiceItUserId = model.CandidateAudios.First().VoiceItUserId;
                    model.PhraseDetail = model.CandidateAudios.First().PhraseDetail;
                }
                return model;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string VerifyCandidateVoice(string userId, string fileUrl, string phrase, int meetingId, string type)
        {
            try
            {
                string finalStatus = string.Empty;

                // verify the audio
                var verificationResult = _voiceItRepository.VerifyUser(userId, fileUrl, phrase);

                if (verificationResult.status == "200")
                {
                    if (verificationResult.responseCode == "SUCC")
                    {
                        finalStatus = verificationResult.message + " with confidence " + verificationResult.confidence;
                        if (type == "interview")
                        {
                            _meetingRespository.UpdateInterviewVerificationScore(meetingId, verificationResult.confidence);
                        }
                        if (type == "joining")
                        {
                            _meetingRespository.UpdateJoiningVerificationScore(meetingId, verificationResult.confidence);

                        }
                    }
                    else
                    {
                        finalStatus = verificationResult.message;//"User could not be verified!";
                    }
                }
                else
                {
                    finalStatus = "Verification operation failed!";
                }
                //update verification score in the meeting table


                return finalStatus;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<MeetingVM> GetMeetingbyid(int id, string Email)
        {
            try
            {
                return await _meetingRespository.GetMeetingbyid(id, Email);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public MeetingVM VedioLike(string value, string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.VedioLikeAsync(value, vedioid, email);
                return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return null;
            }

        }
        public async Task AddTotalViewCount(int id, string emailaddress, string flag)
        {
            try
            {
                await _meetingRespository.AddTotalViewCount(id, emailaddress, flag);

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }
        }
        public string VedioShare(string value, string vedioid)
        {
            try
            {
                var meeting = _meetingRespository.VedioShare(value, vedioid);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public string VedioShared(string value, string vedioid, string Userid)
        {
            try
            {
                var meeting = _meetingRespository.VedioShared(value, vedioid, Userid);
                return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public string VedioComment(string value, string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.VedioComment(value, vedioid, email);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public List<VedioComments> GetvedioComments(int id)
        {
            try
            {
                var meeting = _meetingRespository.GetvedioComments(id);
                return (List<VedioComments>)meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public EmailDetails GetvedioShortList(string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.GetvedioShortList(vedioid, email);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public string CandidateSuitable(int id, string email, string Comment)
        {
            try
            {
                var meeting = _meetingRespository.CandidateSuitable(id, email, Comment);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public VedioComments Intrestreaction(string vedioid, string Status)
        {
            try
            {
                var assignedMeeting = _meetingRespository.Intrestreaction(vedioid, Status);
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string ShortListreaction(string vedioid, string Status)
        {
            try
            {
                var assignedMeeting = _meetingRespository.ShortListreaction(vedioid, Status);
                return "";
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> ShortListedClient(string emailID, string IRFID)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _meetingRespository.ShortListedClient(emailID, IRFID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> InterestListClient(string emailID, string IRFID)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _meetingRespository.InterestListClient(emailID, IRFID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string Fetchusername(string Emailid)
        {
            try
            {
                var assignedMeeting = _meetingRespository.Fetchusername(Emailid);
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<VideoCountdetail>> GetTotalViewCount(int id)
        {
            try
            {
                return await _meetingRespository.GetTotalViewCount(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return null;
            }
        }
        public bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink, int candidateType = 0)
        {
            try
            {
                return _meetingRespository.SaveFeedbackAudio(interviewId, feedbackAudioLink, candidateType);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<string> Broadcast(BroadcastModel model)
        {
            try
            {

                //check the receiver and get all their emails from the db
                var receiverEmails = _meetingRespository.GetEmailsforBroadcast(model.BroadcastReciever).Result;

                var supportEmails = _supportRespository.GetAllSupportHREmails();

                var result = _notificationHelper.SendBroadcastMail(receiverEmails.ToList(), supportEmails.ToList(), model);
                return result;


            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Broadcast failed.";
            }

        }

        public async Task<string> BroadcastIRFmembers(BroadcastModel model)
        {
            try
            {
                var receiverEmails = _meetingRespository.GetIRFMembersEmails(model.IRFId).Result;
                var supportEmails = _supportRespository.GetAllSupportHREmails();
                model.BroadcastReciever = "Interviewers";
                var result = _notificationHelper.SendBroadcastMail(receiverEmails.ToList(), supportEmails.ToList(), model);
                return result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Broadcast failed.";
            }

        }

        public IEnumerable<Meeting> GetHROpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = _meetingRespository.GetHROpenInterviews(emailId).Result.OrderByDescending(x => x.interviewDate);
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string HRAssignYourself(int meetingid, string hrMailId)
        {
            try
            {
                return _meetingRespository.HRAssignYourself(meetingid, hrMailId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview!";
            }

        }

        public IEnumerable<Meeting> GetHRAssignedInterviews(string emailID)
        {
            try
            {
                var assignedMeeting = _meetingRespository.GetHRAssignedInterviews(emailID).Result.OrderByDescending(x => x.interviewDate);

                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public bool HRUnassignYourself(int meetingid)
        {
            try
            {
                return _meetingRespository.HRUnassignYourself(meetingid);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> SaveHRComment(int id, string comment, string imageFileURL, string performedBy, string candidateNameEmail)
        {
            try
            {
                var result = await _meetingRespository.SaveHRComment(id, comment, imageFileURL);
                var activityTrackerModel = new ActivityTrackerVM();
                activityTrackerModel.ActivityType = (int)ActivityTypes.HRComment;
                activityTrackerModel.Particulars = candidateNameEmail;
                activityTrackerModel.PerformedBy = performedBy;
                activityTrackerModel.FileUrl = imageFileURL;
                activityTrackerModel.Summary = comment;

                await _activityTrackerRepository.AddActivity(activityTrackerModel);
                return result;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public IEnumerable<int> GetCuratedGroupIRFs(int curatedGroupId)
        {
            try
            {
                return _meetingRespository.GetCuratedGroupIRFs(curatedGroupId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> AddSharedVideoDetails(InterviewReport reportModel)
        {
            try
            {
                var result = _meetingRespository.AddSharedVideoDetails(reportModel);
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<int> SharedGetInterviewId(int videoid)
        {
            try
            {
                var result = _meetingRespository.SharedGetInterviewId(videoid);
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<InterviewReport> GetSharedInteviewReport(int interviewId, int irfId, int id)
        {
            try
            {
                var reportModel = new InterviewReport();
                var irfDetails = _meetingRespository.GetIRFDetails(irfId);
                var levels = await _meetingRespository.GetLevels();
                //check whether an IRF exists for the given IRFId
                var check = _meetingRespository.GetIRFbyId(irfId);
                var interviewDetails = _meetingRespository.GetInterviewMeetingById(interviewId).Result;
                var candidatedetails = _meetingRespository.GetCandidateSharedDetails(id);

                if (!check)
                {
                    return null;
                }
                // check whether a feedback for the interviewId has already been submitted
                var interviewReport = _meetingRespository.GetInterviewReportById(interviewId, irfId);
                if (interviewReport != null && interviewReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport.FirstOrDefault();
                    await EditSharedInterviewReport(reportModel, id);
                }
                else
                {
                    var model = interviewReport.Any() ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //get all values from database
                    var levelTech = await _meetingRespository.GetLevelTech();
                    var skills = await _meetingRespository.GetSkills();
                    var techSkillsDetails = await _meetingRespository.GetSkillsbyIRFid(irfId);
                    var skillGeneralDetails = await _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
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

                    var genSkillList = new List<SkillGeneralReport>();

                    foreach (var item in skillGeneralDetails ?? new List<TransSkillGeneral>())
                    {
                        SkillGeneralReport obj = new SkillGeneralReport
                        {
                            InterviewId = interviewId,
                            RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name,
                            RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        genSkillList.Add(obj);
                    }

                    model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)genSkillList;

                    model.InterviewId = interviewId;
                    model.CandidateName = candidatedetails.Name;
                    model.LogoUrl = candidatedetails.LogoUrl;
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
                reportModel.CandidateId = _meetingRespository.GetCandidateId(interviewId, irfDetails.IRFId);
                reportModel.CandidateName = candidatedetails.Name;
                reportModel.SelectabilityComments = await _evaluatorRespository.GetSelectabilityComments(irfId, interviewDetails.candidateEmail);
                reportModel.CandidateEmail = candidatedetails.EmailId;
                reportModel.LogoUrl = candidatedetails.LogoUrl;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;
                reportModel.InterviewerComment = candidatedetails.InterviewerComments;

                var selectabilityReport = await _evaluatorRespository.GetSelectabilityReport(reportModel.CandidateId, irfId);
                if (selectabilityReport != null && selectabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    var selectabilityDetails = await _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails ?? new List<TransSelectability>())
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport
                        {
                            CandidateId = reportModel.CandidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        selectabilityList.Add(obj);
                    }
                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }

                reportModel.JoinabilityComments = await _evaluatorRespository.GetJoinabilityComments(irfId, interviewDetails.candidateEmail);
                var joinabilityReport = await _evaluatorRespository.GetJoinabilityReport(reportModel.CandidateId, irfId);
                if (joinabilityReport != null && joinabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    var joinabilityDetails = await _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails ?? new List<TransJoinability>())
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport
                        {
                            CandidateId = reportModel.CandidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        joinabilityList.Add(obj);
                    }

                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                reportModel.IRFName = irfDetails.IRFId;
                reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                return reportModel;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        private async Task<InterviewReport> EditSharedInterviewReport(InterviewReport model, int id)
        {
            var techReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetSharedTechSkillReport(id));
            //model.TechSkillReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));

            if (!techReportCollection.Any())
            {
                var techSkillsDetails = await _meetingRespository.GetSkillsbyIRFid(model.IRFId);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = await _meetingRespository.GetLevelTech();
                var skills = await _meetingRespository.GetSkills();

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


            model.QuestionsCollection = (IList<QuestionsDiscussed>)(_meetingRespository.GetSharedQuestionsDiscussed(id));
            model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)(_meetingRespository.GetSkillGeneralReport(model.InterviewId));
            model.Proctoring = _meetingRespository.GetProctoringReport(model.InterviewId);
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _meetingRespository.GetInterviewMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.encodedresumeLink = interviewMeetingsDetails.encodedresumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;

            return model;
        }
        public IEnumerable<VideoDetails> GetVideoSumaryDetails(int Interviewid)
        {
            try
            {
                var result = _meetingRespository.GetVideoSumaryDetails(Interviewid);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task AddTimeSpend(int id, string emailaddress, string flag, TimeSpan TimeSpend, string CurrentTime)
        {
            try
            {
                await _meetingRespository.AddTimeSpend(id, emailaddress, flag, TimeSpend, CurrentTime);

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }
        }
        public IEnumerable<VideoDetails> GetallVideoSumaryDetails()
        {
            try
            {
                var result = _meetingRespository.GetallVideoSumaryDetails();
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string AddVedioShare(string email, string vedioid, string Emailids)
        {
            try
            {
                var meeting = _meetingRespository.AddVedioShare(email, vedioid, Emailids);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }
        }
        public int CheckVedioShare(string vedioid, string Emailid)
        {
            try
            {
                var meeting = _meetingRespository.CheckVedioShare(vedioid, Emailid);
                return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public string AddVideoIntrestDetails(string videoid, string UrlType, string Emailid)
        {
            try
            {
                var result = _meetingRespository.AddVideoIntrestDetails(videoid, UrlType, Emailid);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public IEnumerable<VideoDetails> GetVideoIntrestDetails()
        {
            try
            {
                var result = _meetingRespository.GetVideoIntrestDetails();
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IEnumerable<string> FetchShareEmails(int Interviewid)
        {
            try
            {
                var result = _meetingRespository.FetchShareEmails(Interviewid);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IEnumerable<VideoDetails> GetVideoCandidateSumaryDetails(int Interviewid, string emailid)
        {
            try
            {
                var result = _meetingRespository.GetVideoCandidateSumaryDetails(Interviewid, emailid);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string UpdateCandidateStatus(int videoid, string Status)
        {
            try
            {
                return _meetingRespository.UpdateCandidateStatus(videoid, Status);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }

        public async Task<SentimentPercentage> GetSentimentAnalysis(int imid)
        {
            try
            {
                var percentObj = new SentimentPercentage();
                var data = await _meetingRespository.GetSentimentAnalysis(imid);
                var total = data.Select(x => x.Value).Sum();
                percentObj.Anger = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Anger)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Contempt = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Contempt)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Disgust = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Disgust)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Fear = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Fear)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Happiness = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Happiness)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Neutral = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Neutral)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Sadness = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Sadness)).Select(x => x.Value).Sum()) * 100) / (total);
                percentObj.Surprise = ((data.Where(x => x.EmotionType == Convert.ToInt16(SentimentType.Surprise)).Select(x => x.Value).Sum()) * 100) / (total);
                return percentObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> GetSharedRiseTubeMeetings()
        {
            try
            {
                var result = await _meetingRespository.GetSharedRiseTubeMeetings();
                return result.OrderByDescending(x => x.Score).ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetSharedRiseTubeMeetingsPaged(int page, string searchValue = null, string profileName = null)
        {
            try
            {
                var result = await _meetingRespository.GetSharedRiseTubeMeetingsPaged(page, searchValue, profileName);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> GetRisetubeIndexMeetingsCount(string profile, string Emailid, string searchValue = null, int clientId = 0, bool IsClientCurated = false)
        {
            try
            {
                return await _meetingRespository.GetRisetubeIndexMeetingsCount(profile, Emailid, searchValue, clientId, IsClientCurated);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> GetRisetubeIndexMeetingsFilteredCount(string profile, string Emailid, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0)
        {
            try
            {
                return await _meetingRespository.GetRisetubeIndexMeetingsFilteredCount(profile, Emailid, startDate, endDate, searchValue, clientId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<int> GetSharedIndexMeetingsCount(string searchValue = null, string profileName = null)
        {
            try
            {
                return await _meetingRespository.GetSharedIndexMeetingsCount(searchValue, profileName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<MeetingVM>> GetSharedRiseTubeMeetingsForClientPagedFiltered(int page, string primarySkills, string heuristicLevels, string searchValue = null)
        {
            try
            {
                var result = await _meetingRespository.GetSharedRiseTubeMeetingsForClientPagedFiltered(page, primarySkills, heuristicLevels, searchValue);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> GetSharedIndexMeetingsCountforClientFiltered(string primarySkills, string heuristicLevels, string searchValue = null)
        {
            try
            {
                return await _meetingRespository.GetSharedIndexMeetingsCountforClientFiltered(primarySkills, heuristicLevels, searchValue);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<List<QuestionDetails>> FetchQuestionDetails(int interviewid)
        {
            try
            {
                var result = await _meetingRespository.FetchQuestionDetails(interviewid);
                return result.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string UpdateQuestionDetails(List<QuestionDetails> _question)
        {
            try
            {
                var result = _meetingRespository.UpdateQuestionDetails(_question);
                return result.ToString();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public IEnumerable<MeetingVM> GetClientJdIRFs(int Clientjdid, string emailId)
        {
            try
            {
                return _meetingRespository.GetClientJdIRFs(Clientjdid, emailId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public SurveyCandidateInterviewer FetchCanddiateInterviewer(int interviewid)
        {
            try
            {
                var result = _meetingRespository.FetchCanddiateInterviewer(interviewid);
                return result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string CandidateSuitableJd(int id, string email, string Comment, int Clientjdid)
        {
            try
            {
                var meeting = _meetingRespository.CandidateSuitableJd(id, email, Comment, Clientjdid);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public IEnumerable<SurveyDetails> FetchSurveyInterviewer(List<SurveyDetails> _lst)
        {
            try
            {
                var _survey = _meetingRespository.FetchSurveyInterviewer(_lst);
                return _survey;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> RBDetails(string EmailId)
        {
            try
            {
                var result = _meetingRespository.RBDetails(EmailId);
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> RBUpdateStatusDetails(string EmailId)
        {
            try
            {
                var result = await _meetingRespository.RBUpdateStatusDetails(EmailId);
                return result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FetchFNDetails(string EmailId)
        {
            try
            {
                var result = _meetingRespository.FetchFNDetails(EmailId);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> RBInterviewerDetails(int interviewerid, int flag)
        {
            try
            {
                var result = _meetingRespository.RBInterviewerDetails(interviewerid, flag);
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> RBAddPoints(int interviewerid, int Points, string Comment, DateTime PointDate)
        {
            try
            {
                var result = _meetingRespository.RBAddPoints(interviewerid, Points, Comment, PointDate);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> RBConfirmPayment(int interviewerid, int index, DateTime st, DateTime se, int paymenthistory)
        {
            try
            {
                var result = _meetingRespository.RBConfirmPayment(interviewerid, index, st, se, paymenthistory);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<string>> FetchInterviewerList(string searchterm)
        {
            try
            {
                var result = _meetingRespository.FetchInterviewerList(searchterm);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> InsertAdvancePayment(string Interviewer, string Comment, int Amount, int PaymentHistoryId)
        {
            try
            {
                var result = _meetingRespository.InsertAdvancePayment(Interviewer, Comment, Amount, PaymentHistoryId);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<RBInterviewDetails>> FNGenerateInvoiceDetails(DateTime st, DateTime se)
        {
            try
            {
                var result = _meetingRespository.FNGenerateInvoiceDetails(st, se);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string LockorUnlockVideo(string imid, int islock, string emailid)
        {
            try
            {
                var meeting = _meetingRespository.LockorUnlockVideo(imid, islock, emailid);
                return meeting;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "fail";
            }
        }
        public List<string> FetchClientUsers(string ClientName)
        {
            try
            {
                var meeting = _meetingRespository.FetchClientUsers(ClientName);
                return meeting;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string SetCandidateAvailability(string imid, int isavailable)
        {
            try
            {
                var meeting = _meetingRespository.SetCandidateAvailability(imid, isavailable);
                return meeting;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "fail";
            }
        }
        public string MoveToCP(string imid, int movetocp)
        {
            try
            {
                var meeting = _meetingRespository.MoveToCP(imid, movetocp);
                return meeting;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "fail";
            }
        }
        public List<MeetingVM> GetAllSelectedCandidateInterviewList(DateTime startDate, DateTime endDate, string email)
        {
            try
            {
                return _meetingRespository.GetAllSelectedCandidateInterviewList(startDate, endDate, email).Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FetchAllFNDetail()
        {
            try
            {
                var result = _meetingRespository.FetchAllFNDetail();
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNAllGeneratedInvoiceDetails(DateTime st, DateTime se)
        {
            try
            {
                var result = _meetingRespository.FNAllGeneratedInvoiceDetails(st, se);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<int> FetchAllInterviewidfromIrf(string IRFid)
        {
            try
            {
                var _survey = _meetingRespository.FetchAllInterviewidfromIrf(IRFid);
                return _survey;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<int> FetchAllInterviewidByClient(int Clientid, string emailid)
        {
            try
            {
                var _survey = _meetingRespository.FetchAllInterviewidByClient(Clientid, emailid);
                return _survey;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<SurveyDetails> FetchSurveyClientName(List<SurveyDetails> _lst)
        {
            try
            {
                var _survey = _meetingRespository.FetchSurveyClientName(_lst);
                return _survey;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<SurveyDetails> FetchSurveyCandidateEmails(List<SurveyDetails> _lst)
        {
            try
            {
                var _survey = _meetingRespository.FetchSurveyCandidateEmails(_lst);
                return _survey;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<string> InsertPaymentHistory(string Interviewer, int Amount, int TotalAmount, string actionvalue, string Comment)
        {
            try
            {
                var result = _meetingRespository.InsertPaymentHistory(Interviewer, Amount, TotalAmount, actionvalue, Comment);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<RBInterviewDetails>> FetchFNPaymentHistory()
        {
            try
            {
                var result = _meetingRespository.FetchFNPaymentHistory();
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<Meeting> FetchOpenMeetingById(string id)
        {
            try
            {
                var openMeeting = new List<Meeting>();
                openMeeting = _meetingRespository.FetchOpenMeetingById(id).Result.ToList();
                openMeeting = openMeeting.Distinct().ToList();
                openMeeting = openMeeting.OrderByDescending(x => x.interviewDate).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string ReassignMeeting(string id, string meetingid, string Emailid)
        {
            try
            {
                var assignedMeeting = _meetingRespository.CheckMeetingAssignationValidation(Emailid);
                if (assignedMeeting != "Valid")
                {
                    return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
                }
                return _meetingRespository.ReAssignMeeting(Convert.ToInt32(id), Emailid);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }
        public string FetchInterviewerCodedName(string emailID)
        {
            try
            {
                var codedname = _meetingRespository.FetchInterviewerCodedName(emailID).Result;

                return codedname;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<InterviewerRating> FetchRatingDetails(int interviewId, string emailid)
        {
            try
            {
                var ratingdetails = await _meetingRespository.FetchRatingDetails(interviewId, emailid);
                return ratingdetails;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> SaveRatingDetails(InterviewerRating model)
        {
            try
            {
                var ratingdetails = await _meetingRespository.SaveRatingDetails(model);
                return ratingdetails;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<MeetingVM> GetClientNotScheduledMeetingList(int Clientid, string Emailid)
        {
            try
            {
                var meetings = _meetingRespository.GetClientNotScheduledMeetingList(Clientid, Emailid).Result.ToList();

                return meetings ?? new List<MeetingVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<InvoiceDetailsVM>> FetchFNInvoiceDetails(string Startdate, string Enddate)
        {
            try
            {
                var result = _meetingRespository.FetchFNInvoiceDetails(Startdate, Enddate);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InvoiceDetailsVM> GetInvoiceDetailsById(int id)
        {
            try
            {
                var result = _meetingRespository.GetInvoiceDetailsById(id);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> SaveInvoiceDetails(InvoiceDetailsVM model)
        {
            try
            {
                var result = _meetingRespository.SaveInvoiceDetails(model);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public List<MeetingVM> GetClientNotScheduledMeetingListByIRFId(string IRFId, string emailId = null)
        {
            try
            {
                var meetings = _meetingRespository.GetClientNotScheduledMeetingListByIRFId(IRFId, emailId).Result.ToList();

                return meetings ?? new List<MeetingVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<MeetingVM> GetClientReScheduledMeetingList(int Clientid, string Emailid)
        {
            try
            {
                var meetings = _meetingRespository.GetClientReScheduledMeetingList(Clientid, Emailid).Result.ToList();

                return meetings ?? new List<MeetingVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<CallCatalogueVM> GetCallCatalogueList(string profile, string Emailid)
        {
            try
            {
                var meetings = _meetingRespository.GetCallCatalogueList(profile, Emailid).Result.ToList();
                return meetings ?? new List<CallCatalogueVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<string> FetchClientListByIRFID(int IRFID)
        {
            try
            {
                var meetings = _meetingRespository.FetchClientListByIRFID(IRFID).Result.ToList();
                return meetings ?? new List<string>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IEnumerable<int> FetchAllInterviewidBySeniorHR(string emailid)
        {
            try
            {
                var _survey = _meetingRespository.FetchAllInterviewidBySeniorHR(emailid);
                return _survey;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> GetPaymentHistoryDetailsView(int interviewerid)
        {
            try
            {
                var result = _meetingRespository.GetPaymentHistoryDetailsView(interviewerid);
                return result.Result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalAmountPaid(DateTime st, DateTime se)
        {
            try
            {
                var totalpaidamount = _meetingRespository.FNFetchToalAmountPaid(st, se);
                return totalpaidamount.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalAmountStatus(DateTime st, DateTime se)
        {
            try
            {
                var result = _meetingRespository.FNFetchToalAmountStatus(st, se);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalQCAmountStatus(DateTime st, DateTime se)
        {
            try
            {
                var result = _meetingRespository.FNFetchToalQCAmountStatus(st, se);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<SchedulingLinkVM>> FetchSchedulingLinks(string emailid)
        {
            try
            {
                var result = _meetingRespository.FetchSchedulingLinks(emailid);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool PublicCancelInterview(int meetingid)
        {
            try
            {
                if (_meetingRespository.CancelInterview(meetingid))
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
        public async Task<int> GetUniqueInterviewerByEmailId(string emailId, string id)
        {
            try
            {
                return await _meetingRespository.GetUniqueInterviewerByEmailId(emailId, id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<List<UnassignedInterviewVM>> FetchAllUnassignedInterviewes()
        {
            try
            {
                return await _meetingRespository.FetchAllUnassignedInterviewes();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool UpdateCancelInterviewDetails(int meetingid)
        {
            try
            {
                if (_meetingRespository.UpdateCancelInterviewDetails(meetingid))
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
        public async Task<List<UnassignedInterviewVM>> FetchCandidateMeetingsForReminder()
        {
            try
            {
                return await _meetingRespository.FetchCandidateMeetingsForReminder();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool UpdateCandidateMeetingsStatus(int meetingid, int status, string uniqueid, int Irfid, int candidateId)
        {
            try
            {
                if (_meetingRespository.UpdateCandidateMeetingsStatus(meetingid, status, uniqueid))
                {
                    if (status == 2)
                    {
                        var result1 = _irfService.AddCandidateStatus("Candidate", candidateId, Irfid, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<CustomerDetailsVM>> FetchCustomerDetails()
        {
            try
            {
                var result = _meetingRespository.FetchCustomerDetails();
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}


