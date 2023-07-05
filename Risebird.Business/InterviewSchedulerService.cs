using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewSchedulerService : IInterviewSchedulerService
    {
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewSchedulerService(IInterviewSchedulerRepository interviewSchedulerRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _interviewSchedulerRepository = interviewSchedulerRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<daytimes_report>> FetchEventList()
        {
            try
            {
                return await _interviewSchedulerRepository.FetchEventList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> CreateEvent(daytimes_report model)
        {
            try
            {
                return await _interviewSchedulerRepository.CreateEvent(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<daytimes_report> FetchEventById(int eventid)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchEventById(eventid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<daytimes_second>> FetchTimeslotsByEventId(int eventid)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchTimeslotsByEventId(eventid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<daytimes_second>> FetchDetailsByUniqueId(string uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchDetailsByUniqueId(uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> DeactivateEventById(int eventid, int status)
        {
            try
            {
                return await _interviewSchedulerRepository.DeactivateEventById(eventid, status);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<SelectList> GetIRFList()
        {
            try
            {
                var results = await _interviewSchedulerRepository.GetIRFList();
                return new SelectList(results.Where(x => x.EndDate > DateTime.Now), "IRFId", "IRFId");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<daytimes_second>> FetchSlots(DateTime value, string uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchSlots(value, uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckInstaMeetingAccount(starttime, endtime);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<JDDetails> FetchJDTitle(string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchJDTitle(IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> UpdateInstaZoomDetails(DateTime starttime, DateTime endtime, string Accountemail)
        {
            try
            {
                return await _interviewSchedulerRepository.UpdateInstaZoomDetails(starttime, endtime, Accountemail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<ScheculeInterviewIRFVM> fetchIRFClientDetails(string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.fetchIRFClientDetails(IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<int> AutoCreateEvent(daytimes_report model)
        {
            try
            {
                return await _interviewSchedulerRepository.AutoCreateEvent(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<int> FetchEventidByUniqueid(string _uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.FetchEventidByUniqueid(_uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<bool> UpdateSchedulingLinkDetails(string _uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.UpdateSchedulingLinkDetails(_uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckInterviewConcurrency(interviewdate, IRFId, Uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> CheckSkillChannelPaused(string SkillChannel)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckSkillChannelPaused(SkillChannel);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> UpdateMeetingLinkDetails(int interviewid, string zoomLink, string zoommeetingid)
        {
            try
            {
                return await _interviewSchedulerRepository.UpdateMeetingLinkDetails(interviewid, zoomLink, zoommeetingid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<string>> fetchSkillChannelHRDetails(string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.fetchSkillChannelHRDetails(IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<string>> fetchSkillChannelStaffingDetails(string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.fetchSkillChannelStaffingDetails(IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> GetAvailableZoomAccount(DateTime startTime, DateTime endTime, string currentZoomAccount = null)
        {
            try
            {
                return await _interviewSchedulerRepository.GetAvailableZoomAccount(startTime, endTime, currentZoomAccount);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public int FetchCurrentoffset(int offset, DateTime value, string TimeZonevalue)
        {
            try
            {
                int currentOffset = offset;
                string timezonevalue = string.Empty;
                string hh = string.Empty;

                var valuecheck = (-offset / 60);
                if (valuecheck < 10 && valuecheck > 0)
                {
                    hh = "0" + valuecheck;
                }
                if (valuecheck > -10 && valuecheck < 0)
                {
                    hh = "-0" + valuecheck.ToString().Replace("-", "");
                }
                var offsetvalue = (hh + ":" + ((offset % 60) < 0 ? (-offset % 60) : (offset % 60))).ToString();
                var timeZones = TimeZoneInfo.GetSystemTimeZones();
                foreach (TimeZoneInfo timeZone in timeZones)
                {
                    var flag = false;
                    if (!string.IsNullOrWhiteSpace(TimeZonevalue) && timeZone.DisplayName.Contains(TimeZonevalue))
                    {
                        if (timeZone.DisplayName.Contains(offsetvalue))
                        {
                            flag = timeZone.IsDaylightSavingTime(value);
                            if (flag)
                            {
                                TimeSpan currentOffsetvalue = timeZone.GetUtcOffset(value);
                                currentOffset = -(currentOffsetvalue.Hours * 60);
                            }
                        }
                    }
                }
                return currentOffset;
            }
            catch (Exception ex)
            {
                return offset;
            }
        }
        public async Task<bool> CheckEmailAddressAllow(string EmailId)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckEmailAddressAllow(EmailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<InterviewAvailabilityResponse>> CheckInterviewScheduleavailability(string EmailId, string IRFId)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckInterviewScheduleavailability(EmailId, IRFId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<CandidateIRFData> CheckCandidateIRFDetails(string IRFId, string _uniqueid)
        {
            try
            {
                return await _interviewSchedulerRepository.CheckCandidateIRFDetails(IRFId, _uniqueid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> UpdateReSchedulingInterview(int interviewid, DateTime newinterviewdate)
        {
            try
            {
                return await _interviewSchedulerRepository.UpdateReSchedulingInterview(interviewid, newinterviewdate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
