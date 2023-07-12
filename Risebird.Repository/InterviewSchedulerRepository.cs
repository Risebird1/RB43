using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class InterviewSchedulerRepository : IInterviewSchedulerRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewSchedulerRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<daytimes_report>> FetchEventList()
        {
            try
            {
                var query = "Select * from ScheduleInterviewEvent where ISNull(IsSingleUse,0)!=1";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<daytimes_report>(query, null);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> CreateEvent(daytimes_report model)
        {
            try
            {
                Guid _guid = Guid.NewGuid();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", model.id);
                dynamicParameters.Add("@EventName", model.EventName);
                dynamicParameters.Add("@SkillChannel", model.SkillChannel);
                dynamicParameters.Add("@StartDate", model.StartDate);
                dynamicParameters.Add("@EndDate", model.EndDate);
                dynamicParameters.Add("@duration", model.duration);
                dynamicParameters.Add("@Uniqueid", _guid.ToString());
                dynamicParameters.Add("@Concurrency", model.Concurrency);
                dynamicParameters.Add("@BufferTime", model.BufferTime);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var eventids = await SqlMapper.QueryAsync<int>(con, "usp_AddscheduleInterviewEvent", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    int eventid = eventids.FirstOrDefault();
                    //Insert bulk DayType
                    DataTable timeslot = new DataTable();
                    timeslot.Columns.Add("Daytype", typeof(string));
                    timeslot.Columns.Add("Start", typeof(string));
                    timeslot.Columns.Add("End", typeof(string));
                    timeslot.Columns.Add("dayCheck", typeof(bool));
                    timeslot.Columns.Add("Eventid", typeof(int));
                    foreach (var item in model.setvalue)
                    {
                        foreach (var timeitem in item.dayvalue)
                        {
                            timeslot.Rows.Add(item.Daytype, timeitem.Start, timeitem.End, item.dayCheck, eventid);
                        }
                    }
                    //Start:Need to change
                    SqlCommand cmdTechReport = new SqlCommand("usp_EventTimeSlotsBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdTechReport.Parameters.Add(new SqlParameter("@EventTimeslotBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@EventTimeslotBulkData"].Value = timeslot;
                    con.Open();
                    var changes = await cmdTechReport.ExecuteNonQueryAsync();
                    if (await cmdTechReport.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }
                    //END:Need to change
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<daytimes_report> FetchEventById(int eventid)
        {
            try
            {
                var query = "Select * from ScheduleInterviewEvent where id=@id";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@id", eventid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<daytimes_report>(query, dp);
                    return results.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<List<daytimes_second>> FetchTimeslotsByEventId(int eventid)
        {
            try
            {
                var query = "Select * From ScheduleEventSlots where Eventid=@Eventid";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@Eventid", eventid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<daytimes_second>(query, dp);
                    return (List<daytimes_second>)results;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<daytimes_second>> FetchDetailsByUniqueId(string uniqueid)
        {
            try
            {
                //var query = "Select * from ScheduleInterviewEvent sie where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0";
                //var query = "Select sie.*,IRF.MeetingPlatform from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannel where uniqueid=@uniqueid and  IsActive=1 and IsNull(IsSingleUse,0)=0 union Select sie.*,IRF.MeetingPlatform from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannelRelation where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0 and IsSingleUse=1";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<daytimes_second>(con, "usp_FetchSchedulerDetailsByUniqueId", dp,
                                commandType: CommandType.StoredProcedure);
                    return (List<daytimes_second>)result;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> DeactivateEventById(int eventid, int status)
        {
            try
            {
                var query = "Update ScheduleInterviewEvent set IsActive=@Status where id=@id";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@id", eventid);
                dp.Add("@Status", status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<daytimes_report>(query, dp);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<IRF>> GetIRFList()
        {
            try
            {
                var query = "Select * from IRF where IsDeleted= 0";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<IRF>(query);
                    return results;
                    //return new SelectList(results.Where(x => x.EndDate > DateTime.Now), "IRFId", "IRFId");
                }
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
                var query = "Select * From ScheduleInterviewEvent sie join ScheduleEventSlots ses on sie.id=ses.Eventid where ses.dayCheck=1 and IsActive=1 and SkillChannel in(Select SkillChannelRelation from ScheduleInterviewEvent where SkillChannel Is null and uniqueid=@uniqueid) union Select * from ScheduleInterviewEvent sie join ScheduleEventSlots ses on sie.id=ses.Eventid where ses.dayCheck=1 and uniqueid=@uniqueid and IsActive=1";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<daytimes_second>(query, dp);
                    return (List<daytimes_second>)results;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> CheckInstaMeetingAccount(DateTime starttime, DateTime endtime)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startdate", starttime);
                    parameters.Add("@enddate", endtime);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_CheckInstaOnlineMettings", parameters,
                            commandType: CommandType.StoredProcedure);
                    if (result != null && result.Count() > 0)
                    {
                        return (result.FirstOrDefault());
                    }
                    else
                        return "";
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }

        public async Task<JDDetails> FetchJDTitle(string IRFId)
        {
            try
            {
                //var query = "Select JDTiltle, JDLink from IRF where IRFId=@IRFId";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<JDDetails>(con, "usp_FetchJDTitleByIRFId", dp,
                            commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startdate", starttime);
                    parameters.Add("@enddate", endtime);
                    parameters.Add("@Accountemail", Accountemail);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_UpdateInstaOnlineMettings", parameters,
                            commandType: CommandType.StoredProcedure);
                    return "success";
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<ScheculeInterviewIRFVM> fetchIRFClientDetails(string IRFId)
        {
            try
            {
                var query = "Select Cl.LogoLink,Cl.Name,IRF.JDTiltle, cl.CompanyLink,IRF.id as inIRFid from IRF Left join Client Cl on Cl.id=IRF.ClientId where IRFId=@IRFId";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<ScheculeInterviewIRFVM>(query, dp);
                    return results.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<int> AutoCreateEvent(daytimes_report model)
        {
            try
            {
                Guid _guid = Guid.NewGuid();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", model.id);
                dynamicParameters.Add("@EventName", model.EventName);
                dynamicParameters.Add("@SkillChannel", model.SkillChannel);
                dynamicParameters.Add("@StartDate", model.StartDate);
                dynamicParameters.Add("@EndDate", model.EndDate);
                dynamicParameters.Add("@duration", model.duration);
                dynamicParameters.Add("@Uniqueid", _guid.ToString());
                dynamicParameters.Add("@Concurrency", model.Concurrency);
                dynamicParameters.Add("@BufferTime", model.BufferTime);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var eventids = await SqlMapper.QueryAsync<int>(con, "usp_AddscheduleInterviewEvent", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    int eventid = eventids.FirstOrDefault();
                    //Insert bulk DayType
                    DataTable timeslot = new DataTable();
                    timeslot.Columns.Add("Daytype", typeof(string));
                    timeslot.Columns.Add("Start", typeof(string));
                    timeslot.Columns.Add("End", typeof(string));
                    timeslot.Columns.Add("dayCheck", typeof(bool));
                    timeslot.Columns.Add("Eventid", typeof(int));
                    foreach (var item in model.setvalue)
                    {
                        foreach (var timeitem in item.dayvalue)
                        {
                            timeslot.Rows.Add(item.Daytype, timeitem.Start, timeitem.End, item.dayCheck, eventid);
                        }
                    }
                    //Start:Need to change
                    SqlCommand cmdTechReport = new SqlCommand("usp_EventTimeSlotsBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdTechReport.Parameters.Add(new SqlParameter("@EventTimeslotBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@EventTimeslotBulkData"].Value = timeslot;
                    con.Open();
                    var changes = await cmdTechReport.ExecuteNonQueryAsync();
                    if (await cmdTechReport.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }
                    //END:Need to change
                    return eventid;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
        public async Task<int> FetchEventidByUniqueid(string _uniqueid)
        {
            try
            {
                var query = "Select id from ScheduleInterviewEvent where uniqueid=@uniqueid and IsActive=1";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@uniqueid", _uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dp);
                    return results.FirstOrDefault();
                }
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
                var query = "Update ScheduleInterviewEvent set IsOnetimeUsed=1 where Uniqueid=@uniqueid and IsActive=1";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@uniqueid", _uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync(query, dp);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> GenerateOneTimeEvent(string olduniqueid, string candidateEmail)
        {
            try
            {
                daytimes_report resultset = new daytimes_report();
                var query = "Select * from ScheduleInterviewEvent where uniqueid=@uniqueid and IsActive=1";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@uniqueid", olduniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<daytimes_report>(query, dp);
                    resultset = (daytimes_report)results.FirstOrDefault();
                }
                Guid _guid = Guid.NewGuid();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EventName", resultset.EventName);
                dynamicParameters.Add("@SkillChannel", resultset.SkillChannel);
                dynamicParameters.Add("@StartDate", resultset.StartDate);
                dynamicParameters.Add("@EndDate", resultset.EndDate);
                dynamicParameters.Add("@duration", resultset.duration);
                dynamicParameters.Add("@Uniqueid", _guid.ToString());
                dynamicParameters.Add("@Concurrency", resultset.Concurrency);
                dynamicParameters.Add("@candidateEmail", candidateEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var eventids = await SqlMapper.QueryAsync<int>(con, "usp_GenerateOneTimeInterviewEvent", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    int eventid = eventids.FirstOrDefault();
                    return _guid.ToString();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> CheckInterviewConcurrency(DateTime interviewdate, string IRFId, string Uniqueid)
        {
            try
            {
                var query = ";With CTE as(Select Count(id) as InterviewCount from InterviewMeetings where interviewDate=@interviewdate and IRFId=@IRFId) Select Case When CTE.InterviewCount<Isnull(si.ConCurrency,0) Then 'True' When CTE.InterviewCount>=Isnull(si.ConCurrency,0) Then 'False' End from CTE join ScheduleInterviewEvent si on (si.SkillChannel=@IRFId or SkillChannelRelation=@IRFId) where si.Uniqueid=@Uniqueid";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                dp.Add("@interviewdate", interviewdate);
                dp.Add("@Uniqueid", Uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dp);
                    return Convert.ToBoolean(results.FirstOrDefault());
                }
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
                var query = "Select IsNull(IsPaused,0) from IRF where IRFId=@IRFId";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", SkillChannel);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<bool>(query, dp);
                    return results.FirstOrDefault();
                }
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
                var query = "Update InterviewMeetings set BackUpLink=@BackUpLink,BackUpLinkId=@BackUpLinkId where id=@interviewid";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@interviewid", interviewid);
                dp.Add("@BackUpLink", zoomLink);
                dp.Add("@BackUpLinkId", zoommeetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync(query, dp);
                    return true;
                }
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
                var query = "Select distinct ul.EmailId From IRF join TransClientUser tc on tc.IRFId=IRF.id join UserList ul on ul.Id=tc.ClientUserId where (tc.ClientRoleId=1 or tc.ClientRoleId=2) and IRF.IRFId=@IRFId";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dp);
                    return (List<string>)results;
                }
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
                var query = "Select ul.EmailId From IRF join TransClientUser tc on tc.IRFId=IRF.id join UserList ul on ul.Id=tc.ClientUserId where tc.ClientRoleId=4 and IRF.IRFId=@IRFId";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dp);
                    return (List<string>)results;
                }
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startTime", startTime);
                    parameters.Add("@endTime", endTime);
                    parameters.Add("@currentZoomAccount", currentZoomAccount != string.Empty ? currentZoomAccount : null);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetAvailableZoomAccount", parameters,
                            commandType: CommandType.StoredProcedure);
                    if (result != null && result.Count() > 0)
                    {
                        return (result.FirstOrDefault());
                    }
                    else
                        return "";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }
        public async Task<bool> CheckEmailAddressAllow(string EmailId)
        {
            try
            {
                var query = "Select Count(*) From UserList where Emailid=@EmailId and idprofile!=23";
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@EmailId", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dp);
                    if (results.FirstOrDefault() > 0)
                    {
                        return true;
                    }
                    return false;
                }
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
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@candidateEmail", EmailId);
                dp.Add("@IRFId", IRFId);
                dp.Add("@timenow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewAvailabilityResponse>(con, "usp_CheckInterviewScheduleavailability", dp,
                            commandType: CommandType.StoredProcedure);
                    return result;
                }
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
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@IRFId", IRFId);
                dp.Add("@uniqueid", _uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidateIRFData>(con, "usp_GetCandidateIRFDetails", dp,
                            commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<RescheduleInterviewDetails> UpdateReSchedulingInterview(int interviewid, DateTime newinterviewdate)
        {
            try
            {
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@interviewid", interviewid);
                dp.Add("@source", "Reschedule on " + newinterviewdate.ToString());
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RescheduleInterviewDetails>(con, "usp_UpdateReSchedulingInterview", dp,
                            commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
