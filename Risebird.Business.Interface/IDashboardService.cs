using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDashboardService
    {
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date, string Emailid);
        Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date, string Emailid);
        Task<DashboardInterviewerCount> GetInterviewerCount();
        Task<IEnumerable<IRF>> GetIRFProgress(DateTime date, string Emailid);
        IEnumerable<IrfProgress> GetProgressOptions();
        Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList);
        Task<MeetingResultCount> GetMeetingResultCount(string Emailid);
        Task<CandidatesCount> GetCandidatesCount(string Emailid);
        Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId);
        Task<IRFFunnel> GetClientFunnelData(string emailId);
        Task<IEnumerable<ClientDailySlotsStatus>> GetDateWiseClientMeetingsSlots(DateTime date, string emailId);
        Task<CandidatesCount> GetClientCandidatesCount(string emailId);
        Task<MeetingResultCount> GetClientMeetingsResults(string emailId);
        IRF FetchActivityDetails(int id, int idprogess);
        Task<List<ChannelManagementVM>> FetchChannelRequests();
        Task<ChannelVM> FetchChannelRequestsByid(int id);
        Task<string> AddChannelRequest(ChannelVM request);
        Task<List<string>> FetchAdminandHRList();
        Task<string> AddChannelInterviewer(ChannelInterviewerVM request);
        Task<List<ChannelInterviewerVM>> FetchInterviewerBychannelid(int id);
        Task<string> DeleteChannelInterviewerByid(int id, int channelid);
        Task<ChannelInterviewerVM> FecthInterviewerById(int id);
        Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCount(string Emailid, bool getDeletedZoomMeetingsCount = false);
        Task<List<string>> FetchHRList(string searchterm);
        bool AddChannelApprovedInterviewer(IEnumerable<ChannelApprovedInterviewer> model, int channelid);
        IEnumerable<ChannelApprovedInterviewer> GetChannelApprovedInterviewer(int id);
        IEnumerable<ChannelApprovedInterviewer> GetChannelInterviewers(int id);
        Task<bool> DeleteChannelInterviewer(int id);
        Task<bool> ChannelInterviewerAction(int id, string action);
        Task<CandidateCurrentStatusCount> GetClientCandidatesCurrentStatusCount(string emailId);
        Task<List<MeetingVM>> FetchRibbonDetailsValues(string actionstatus, string emailid, int actiontype, string profileName);
        Task<List<MeetingVM>> FetchRibbonDetailsValuesbydate(string actionstatus, string emailid, int actiontype, string profileName);
        Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCountbyHr(string Emailid, bool getDeletedZoomMeetingsCount = false);
        Task<List<MeetingDetailsForParticipant>> SendNotoificationForParticipantWaiting();
        Task<List<ZoomMeetingHistoryDetails>> FetchZoomMeetingHistory(string meetingid);
        Task<IEnumerable<InterviewManagementVM>> GetInterviewManagement(string Emailid);
        Task<InterviewerDashboardVM> GetInterviewDetailsCount(string emailId);
        Task<List<string>> FetchAlertMessage();
        Task<List<MeetingVM>> GetUpcomingMeetingsDeletedFromZoom(string emailId);
    }
}
