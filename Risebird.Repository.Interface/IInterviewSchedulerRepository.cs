using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewSchedulerRepository
    {
        List<daytimes_report> FetchEventList();
        bool CreateEvent(daytimes_report model);
        daytimes_report FetchEventById(int eventid);
        List<daytimes_second> FetchTimeslotsByEventId(int eventid);
        List<daytimes_second> FetchDetailsByUniqueId(string uniqueid);
        bool DeactivateEventById(int eventid, int status);
        IEnumerable<IRF> GetIRFList();
        List<daytimes_second> FetchSlots(DateTime value, string uniqueid);
        Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime);
        Task<string> FetchJDTitle(string IRFId);
        string UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accountemail);
        ScheculeInterviewIRFVM fetchIRFClientDetails(string IRFId);
        int AutoCreateEvent(daytimes_report model);
        int FetchEventidByUniqueid(string _uniqueid);
        bool UpdateSchedulingLinkDetails(string _uniqueid);
        string GenerateOneTimeEvent(string olduniqueid);
        Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid);
        Task<bool> CheckSkillChannelPaused(string SkillChannel);
        Task<bool> UpdateMeetingLinkDetails(int interviewid, string zoomLink,string zoommeetingid);
        List<string> fetchSkillChannelHRDetails(string IRFId);
        string fetchSkillChannelStaffingDetails(string IRFId);
    }
}
