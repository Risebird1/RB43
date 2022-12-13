using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewSchedulerService
    {
        Task<List<daytimes_report>> FetchEventList();
        Task<bool> CreateEvent(daytimes_report model);
        Task<daytimes_report> FetchEventById(int eventid);
        Task<List<daytimes_second>> FetchTimeslotsByEventId(int eventid);
        Task<List<daytimes_second>> FetchDetailsByUniqueId(string uniqueid);
        Task<bool> DeactivateEventById(int eventid, int status);
        Task<SelectList> GetIRFList();
        Task<List<daytimes_second>> FetchSlots(DateTime value, string uniqueid);
        Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime);
        Task<string> FetchJDTitle(string IRFId);
        Task<string> UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accountemail);
        Task<ScheculeInterviewIRFVM> fetchIRFClientDetails(string IRFId);
        Task<int> AutoCreateEvent(daytimes_report model);
        Task<int> FetchEventidByUniqueid(string _uniqueid);
        Task<bool> UpdateSchedulingLinkDetails(string _uniqueid);
        Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid);
        Task<bool> CheckSkillChannelPaused(string SkillChannel);
        Task<bool> UpdateMeetingLinkDetails(int interviewid, string zoomLink, string zoommeetingid);
        Task<List<string>> fetchSkillChannelHRDetails(string IRFId);
        Task<List<string>> fetchSkillChannelStaffingDetails(string IRFId);
        Task<string> GetAvailableZoomAccount(DateTime startTime, DateTime endTime, string currentZoomAccount = null);
        int FetchCurrentoffset(int offset, DateTime value, string TimeZonevalue);
    }
}
