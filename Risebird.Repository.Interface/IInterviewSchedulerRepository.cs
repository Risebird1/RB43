using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewSchedulerRepository
    {
        Task<List<daytimes_report>> FetchEventList();
        Task<bool> CreateEvent(daytimes_report model);
        Task<daytimes_report> FetchEventById(int eventid);
        Task<List<daytimes_second>> FetchTimeslotsByEventId(int eventid);
        Task<List<daytimes_second>> FetchDetailsByUniqueId(string uniqueid);
        Task<bool> DeactivateEventById(int eventid, int status);
        Task<IEnumerable<IRF>> GetIRFList();
        Task<List<daytimes_second>> FetchSlots(DateTime value, string uniqueid);
        Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime);
        Task<JDDetails> FetchJDTitle(string IRFId);
        Task<string> UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accountemail);
        Task<ScheculeInterviewIRFVM> fetchIRFClientDetails(string IRFId);
        Task<int> AutoCreateEvent(daytimes_report model);
        Task<int> FetchEventidByUniqueid(string _uniqueid);
        Task<bool> UpdateSchedulingLinkDetails(string _uniqueid);
        Task<string> GenerateOneTimeEvent(string olduniqueid);
        Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid);
        Task<bool> CheckSkillChannelPaused(string SkillChannel);
        Task<bool> UpdateMeetingLinkDetails(int interviewid, string zoomLink, string zoommeetingid);
        Task<List<string>> fetchSkillChannelHRDetails(string IRFId);
        Task<List<string>> fetchSkillChannelStaffingDetails(string IRFId);
        Task<string> GetAvailableZoomAccount(DateTime startTime, DateTime endTime, string currentZoomAccount = null);
        Task<bool> CheckEmailAddressAllow(string EmailId);
    }
}
