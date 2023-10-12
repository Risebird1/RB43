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
using System.Net;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboarRepository _dashboardRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRepository;
        private readonly IZoomHelper _zoomHelper;
        private readonly IMeetingRespository _meetingRespository;
        public DashboardService(IDashboarRepository dashboardRespository, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRepository, IZoomHelper zoomHelper, IMeetingRespository meetingRespository)
        {
            _dashboardRespository = dashboardRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRepository = meetingRepository;
            _zoomHelper = zoomHelper;
            _meetingRespository = meetingRespository;
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date, string Emailid)
        {
            try
            {
                return _dashboardRespository.GetDateWiseMeetings(date, Emailid).Result.OrderBy(x => x.interviewDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date, string Emailid)
        {
            try
            {
                return _dashboardRespository.GetDateWiseMeetingsSlots(date, Emailid).Result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<DashboardInterviewerCount> GetInterviewerCount()
        {
            try
            {
                return await _dashboardRespository.GetInterviewerCount();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<IRF>> GetIRFProgress(DateTime date, string Emailid)
        {
            try
            {
                return await _dashboardRespository.GetIRFProgress(date, Emailid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<IrfProgress>> GetProgressOptions()
        {
            try
            {
                return await _dashboardRespository.GetProgressOptions();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList)
        {
            try
            {
                return await _dashboardRespository.UpdateIRFProgress(irfProgressList);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<MeetingResultCount> GetMeetingResultCount(string Emailid)
        {
            try
            {
                return await _dashboardRespository.GetMeetingResultCount(Emailid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<CandidatesCount> GetCandidatesCount(string Emailid)
        {
            try
            {
                var countObj = await _dashboardRespository.GetCandidatesCount(Emailid);
                countObj.TotalCandidates = (countObj.Selected + countObj.Rejected + countObj.BorderLine + countObj.Proxy + countObj.NoShowCount + countObj.DroppedCount + countObj.ScheduledCount + countObj.Pending);
                return countObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId)
        {
            try
            {
                return _dashboardRespository.GetDateWiseClientMeetings(date, emailId).Result.OrderBy(x => x.interviewDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IRFFunnel> GetClientFunnelData(string emailId)
        {
            try
            {
                return await _dashboardRespository.GetClientFunnelData(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<ClientDailySlotsStatus>> GetDateWiseClientMeetingsSlots(DateTime date, string emailId)
        {
            try
            {
                return await _dashboardRespository.GetDateWiseClientMeetingsSlots(date, emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidatesCount> GetClientCandidatesCount(string emailId)
        {
            try
            {
                var countObj = await _dashboardRespository.GetClientCandidatesCount(emailId);
                return countObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidateCurrentStatusCount> GetClientCandidatesCurrentStatusCount(string emailId)
        {
            try
            {
                var countObj = new CandidateCurrentStatusCount();
                var obj = await _dashboardRespository.GetClientCandidatesCurrentStatus(emailId);
                if (obj != null && obj.Count() > 0)
                {

                    var meetingStatObj = obj.Where(x => x.StatusValue == "Meeting Status");
                    if (meetingStatObj != null && meetingStatObj.Count() > 0)
                    {
                        countObj.NoShow = meetingStatObj.Where(x => x.Comments == MeetingStatus.NoShow.GetDescription())?.Count();
                        countObj.Dropped = meetingStatObj.Where(x => x.Comments == MeetingStatus.Dropped.GetDescription())?.Count();
                        countObj.Completed = meetingStatObj.Where(x => x.Comments == MeetingStatus.Completed.GetDescription())?.Count();
                    }
                    var inteviewStatObj = obj.Where(x => x.StatusValue == "Interview Status");
                    if (inteviewStatObj != null && inteviewStatObj.Count() > 0)
                    {
                        countObj.Proxy = inteviewStatObj.Where(x => x.Comments == InterviewStatus.Proxy.GetDescription())?.Count();
                        countObj.Selected = inteviewStatObj.Where(x => x.Comments == InterviewStatus.Selected.GetDescription())?.Count();
                        countObj.Rejected = inteviewStatObj.Where(x => x.Comments == InterviewStatus.Rejected.GetDescription())?.Count();
                        countObj.BorderLine = inteviewStatObj.Where(x => x.Comments == InterviewStatus.BorderLine.GetDescription())?.Count();
                    }

                    countObj.NotYetScheduled = obj.Where(x => x.Status != (int)CandidateStatusOptions.Scheduled && x.Comments != "Meeting Status" && x.Comments != "Interview Status" && x.Status != (int)CandidateStatusOptions.QCPending)?.Count();

                    countObj.Scheduled = obj.Where(x => x.Status == (int)CandidateStatusOptions.Scheduled)?.Count();
                    //countObj.QCPending = obj.Where(x => x.Status == (int)CandidateStatusOptions.QCPending)?.Count();


                    countObj.TotalCount = countObj.NoShow + countObj.Dropped + countObj.Scheduled + countObj.Completed
                                         + countObj.Selected + countObj.Rejected + countObj.BorderLine + countObj.Proxy
                                         /*+ countObj.QCPending +*/  + countObj.NotYetScheduled;
                }
                return countObj;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<MeetingResultCount> GetClientMeetingsResults(string emailId)
        {
            try
            {
                return await _dashboardRespository.GetClientMeetingsResults(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IRF FetchActivityDetails(int id, int idprogess)
        {
            try
            {
                return _dashboardRespository.FetchActivityDetails(id, idprogess);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<ChannelManagementVM>> FetchChannelRequests()
        {
            try
            {
                return await _dashboardRespository.FetchChannelRequests();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<ChannelVM> FetchChannelRequestsByid(int id)
        {
            try
            {
                return await _dashboardRespository.FetchChannelRequestsByid(id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> AddChannelRequest(ChannelVM request)
        {
            try
            {
                return await _dashboardRespository.AddChannelRequest(request);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<List<string>> FetchAdminandHRList()
        {
            try
            {
                return await _dashboardRespository.FetchAdminandHRList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> AddChannelInterviewer(ChannelInterviewerVM request)
        {
            try
            {
                return await _dashboardRespository.AddChannelInterviewer(request);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<List<ChannelInterviewerVM>> FetchInterviewerBychannelid(int id)
        {
            try
            {
                return await _dashboardRespository.FetchInterviewerBychannelid(id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> DeleteChannelInterviewerByid(int id, int channelid)
        {
            try
            {
                return await _dashboardRespository.DeleteChannelInterviewerByid(id, channelid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<ChannelInterviewerVM> FecthInterviewerById(int id)
        {
            try
            {
                return await _dashboardRespository.FecthInterviewerById(id);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCount(string Emailid, bool getDeletedZoomMeetingsCount = false)
        {
            try
            {
                var model = new NotUpdatedMeetings();
                model = await _dashboardRespository.GetNotUpdatedMeetingsCount(Emailid);
                if (getDeletedZoomMeetingsCount)
                {
                    var upcomingMeetings = await _meetingRepository.GetInterviewsScheduledWithinNextTwoHours(Emailid);
                    int deletedZoomMeetingsCount = 0;

                    if (upcomingMeetings != null && upcomingMeetings.Count() > 0)
                    {
                        var zoomToken = _zoomHelper.ZoomToken();
                        foreach (var meeting in upcomingMeetings)
                        {
                            var responseStatusCode = await _zoomHelper.CheckMeetingbyId(meeting.ZoomMeetingId, zoomToken);
                            if (responseStatusCode != HttpStatusCode.OK)
                            {
                                deletedZoomMeetingsCount = deletedZoomMeetingsCount + 1;
                            }
                        }
                    }
                    model.UpcomingMeetingsDeletedFromZoom = deletedZoomMeetingsCount;
                }
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<string>> FetchHRList(string searchterm)
        {
            try
            {
                var result = _dashboardRespository.FetchHRList(searchterm);
                return result.Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddChannelApprovedInterviewer(IEnumerable<ChannelApprovedInterviewer> model, int channelid)
        {
            try
            {
                return _dashboardRespository.AddChannelApprovedInterviewer(model.Where(x => x.IsAdded), channelid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<ChannelApprovedInterviewer> GetChannelApprovedInterviewer(int id)
        {
            try
            {
                return _dashboardRespository.GetChannelApprovedInterviewer(id).Result.ToList();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<ChannelApprovedInterviewer> GetChannelInterviewers(int id)
        {
            try
            {
                return _dashboardRespository.GetChannelInterviewers(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> DeleteChannelInterviewer(int id)
        {
            try
            {
                return _dashboardRespository.DeleteChannelInterviewer(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> ChannelInterviewerAction(int id, string action)
        {
            try
            {
                return _dashboardRespository.ChannelInterviewerAction(id, action).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<List<MeetingVM>> FetchRibbonDetailsValues(string actionstatus, string emailid, int actiontype, string profileName)
        {
            try
            {
                return await _dashboardRespository.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingVM>> FetchRibbonDetailsValuesbydate(string actionstatus, string emailid, int actiontype, string profileName)
        {
            try
            {
                return await _dashboardRespository.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCountbyHr(string Emailid, bool getDeletedZoomMeetingsCount = false)
        {
            try
            {
                var model = new NotUpdatedMeetings();
                model = await _dashboardRespository.GetNotUpdatedMeetingsCountbyHr(Emailid);
                if (getDeletedZoomMeetingsCount)
                {
                    var upcomingMeetings = await _meetingRepository.GetInterviewsScheduledWithinNextTwoHours(Emailid);
                    int deletedZoomMeetingsCount = 0;

                    if (upcomingMeetings != null && upcomingMeetings.Count() > 0)
                    {
                        var zoomToken = _zoomHelper.ZoomToken();
                        foreach (var meeting in upcomingMeetings)
                        {
                            var responseStatusCode = await _zoomHelper.CheckMeetingbyId(meeting.ZoomMeetingId, zoomToken);
                            if (responseStatusCode != HttpStatusCode.OK)
                            {
                                deletedZoomMeetingsCount = deletedZoomMeetingsCount + 1;
                            }
                        }
                    }
                    model.UpcomingMeetingsDeletedFromZoom = deletedZoomMeetingsCount;
                }

                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<MeetingDetailsForParticipant>> SendNotoificationForParticipantWaiting()
        {
            try
            {
                return await _dashboardRespository.SendNotoificationForParticipantWaiting();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<ZoomMeetingHistoryDetails>> FetchZoomMeetingHistory(string meetingid)
        {
            try
            {
                return await _dashboardRespository.FetchZoomMeetingHistory(meetingid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<InterviewManagementVM>> GetInterviewManagement(string Emailid)
        {
            try
            {
                return await _dashboardRespository.GetInterviewManagement(Emailid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<InterviewerDashboardVM> GetInterviewDetailsCount(string emailId)
        {
            try
            {
                return await _dashboardRespository.GetInterviewDetailsCount(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<string>> FetchAlertMessage()
        {
            try
            {
                return await _dashboardRespository.FetchAlertMessage();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<MeetingVM>> GetUpcomingMeetingsDeletedFromZoom(string emailId)
        {
            try
            {
                var upcomingMeetings = (await _meetingRepository.GetInterviewsScheduledWithinNextTwoHours(emailId)).ToList();
                var deletedZoomMeetingList = new List<MeetingVM>();
                if (upcomingMeetings != null && upcomingMeetings.Count() > 0)
                {
                    var zoomToken = _zoomHelper.ZoomToken();
                    foreach (var meeting in upcomingMeetings)
                    {
                        var responseStatusCode = await _zoomHelper.CheckMeetingbyId(meeting.ZoomMeetingId, zoomToken);
                        if (responseStatusCode != HttpStatusCode.OK)
                        {
                            deletedZoomMeetingList.Add(meeting);
                            //upcomingMeetings.Remove(meeting);
                        }
                    }
                }
                return deletedZoomMeetingList;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<AlterMessage>> FetchAlertMessageList()
        {
            try
            {
                return await _dashboardRespository.FetchAlertMessageList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> AddAlertMessage(AlterMessage model)
        {
            try
            {
                return await _dashboardRespository.AddAlertMessage(model);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<BlackListCompanyVM>> GetBlackkListComapany()
        {
            try
            {
                return await _dashboardRespository.GeBlackListCompany();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UndoBlackListCompanyService(BlackListCompanyVM model)
        {
            try
            {
                await _dashboardRespository.UndoBlackListCompany(model);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> ApprovedBlackListCompanyService(BlackListCompanyVM model)
        {
            try
            {
                await _dashboardRespository.ApprovedBlackListCompany(model);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<QCFormVM> GetRedFlagsQCReportService(int Interviewid)
        {
            try
            {
                return await _dashboardRespository.GetRedflagsQcReportRepository(Interviewid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UndoSuspiciousInterviewerService(int InterviewerProfileId)
        {
            try
            {
                await _dashboardRespository.UndoSuspiciousInterviewer(InterviewerProfileId);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> ApprovedRedoInterviewsService(int InterviewId,string modifyBy,DateTime modifyDate)
        {
            try
            {
                await _dashboardRespository.ApprovedRedoIntervies(InterviewId,modifyBy,modifyDate);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<MeetingVM>> GetRedoRequestService()
        {
            try
            {
                return await _dashboardRespository.GetRedoRrquest();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<InterviewerRedmarkers>> GetHighRiskInterviewersService()
        {
            try
            {
                return await _dashboardRespository.FecthHighRiskInterviews();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> MoveToUnassignService(int meetingId)
        {
            try
            {
                await _meetingRespository.UnassignYourself(meetingId);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> AddRedoInterviewsService(int InterviewId, string Type)
        {
            try
            {
                await _dashboardRespository.AddRedoIntervies(InterviewId,Type);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

    }
}
