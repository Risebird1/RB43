using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDashboardService
    {
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date);
        Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date);
        Task<DashboardInterviewerCount> GetInterviewerCount();
        Task<IEnumerable<IRF>> GetIRFProgress(DateTime date);
        IEnumerable<IrfProgress> GetProgressOptions();
        Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList);
        Task<MeetingResultCount> GetMeetingResultCount();
        Task<CandidatesCount> GetCandidatesCount();
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
        Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCount();
        Task<IEnumerable<MeetingVM>> GetNotUpdatedMeetings();
        Task<List<string>> FetchHRList(string searchterm);
    }
}
