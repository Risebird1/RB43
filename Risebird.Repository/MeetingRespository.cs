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
using System.Reflection;
using System.Threading.Tasks;
using static Dapper.SqlMapper;
using static Risebird.Entities.Data.RchilliResumeMapFields;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class MeetingRespository : IMeetingRespository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        public MeetingRespository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _notificationHelper = notificationHelper;
        }

        public async Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetIAllInterviewMeetings", null,
                        commandType: CommandType.StoredProcedure);
                    return result;

                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    parameters.Add("EmailId", emailId);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetIAllDateWiseInterviewMeetings", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<IEnumerable<MeetingVM>> GetAllCancelInterview(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    parameters.Add("EmailId", emailId);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetAllCancelInterviews", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        /// <summary>
        /// Risetube meeting
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid, bool IsClientCurated = false)
        {
            try
            {
                if (profile == "CuratedUser" || IsClientCurated)
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("clientid", clientId);
                        parameters.Add("Emailid", Emailid);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForCuratedUser", parameters, commandTimeout: 600,
                                 commandType: CommandType.StoredProcedure);
                    }
                }
                else if (profile == "ExternalUser")
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("clientid", clientId);
                        parameters.Add("Emailid", Emailid);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForExternalUser", parameters, commandTimeout: 600,
                                commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientid", clientId);
                        parameters.Add("@Emailid", Emailid);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetings", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetingsPaged(string profile, string Emailid, int Page, string searchValue = null, int clientId = 0, bool IsClientCurated = false)
        {
            try
            {
                if (profile == "CuratedUser")
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@startFrom", ((Page - 1) * 12));
                        parameters.Add("@endWith", Page * 12);
                        parameters.Add("Emailid", Emailid);
                        parameters.Add("@searchValue", searchValue);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeIndexMeetingsForCuratedUserPaged", parameters, commandTimeout: 600,
                                 commandType: CommandType.StoredProcedure);
                    }
                }

                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientIdParam", clientId);
                        parameters.Add("@EmailIdParam", Emailid);
                        parameters.Add("@startFromParam", ((Page - 1) * 12));
                        parameters.Add("@endWithParam", Page * 12);
                        parameters.Add("@searchValueParam", searchValue);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsPaged", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetingsPagedFiltered(string profile, string Emailid, int Page, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0)
        {
            try
            {
                if (profile == "CuratedUser")
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@startFromParam", ((Page - 1) * 12));
                        parameters.Add("@endWithParam", Page * 12);
                        parameters.Add("EmailidParam", Emailid);
                        parameters.Add("@searchValueParam", searchValue);
                        parameters.Add("@startDateParam", startDate);
                        parameters.Add("@endDateParam", endDate);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeIndexMeetingsForCuratedUserFilteredDup", parameters, commandTimeout: 600,
                                 commandType: CommandType.StoredProcedure);
                    }
                }

                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientIdParam", clientId);
                        parameters.Add("@EmailIdParam", Emailid);
                        parameters.Add("@startFromParam", ((Page - 1) * 12));
                        parameters.Add("@endWithParam", Page * 12);
                        parameters.Add("@searchValueParam", searchValue);
                        parameters.Add("@startDateParam", startDate);
                        parameters.Add("@endDateParam", endDate);
                        //parameters.Add("@timeNow", DateTime.Now);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsFilteredDup", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetingsByIRFId(int clientId, string Emailid, string irfId, DateTime startDate, DateTime endDate, string profileName = null)
        {
            try
            {
                if (profileName != null && profileName == "CuratedUser")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@Emailid", Emailid);
                        parameters.Add("@IRFId", irfId);
                        parameters.Add("@startDate", startDate);
                        parameters.Add("@endDate", endDate);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsByIRFIdForCuratedUser", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientid", clientId);
                        parameters.Add("@Emailid", Emailid);
                        parameters.Add("@IRFId", irfId);
                        parameters.Add("@startDate", startDate);
                        parameters.Add("@endDate", endDate);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsByIRFId", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern, DateTime startDate, DateTime endDate, string profileName = null)
        {
            try
            {
                if (profileName != null && profileName == "CuratedUser")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@Emailid", Emailid);
                        parameters.Add("@JDpattern", JDpattern);
                        parameters.Add("@startDate", startDate);
                        parameters.Add("@endDate", endDate);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsByJDforCuratedUser", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientid", clientId);
                        parameters.Add("@Emailid", Emailid);
                        parameters.Add("@JDpattern", JDpattern);
                        parameters.Add("@startDate", startDate);
                        parameters.Add("@endDate", endDate);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsByJD", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetingsForCuratedUserByIRF(string EmailId, string actionStatus, int irfId)
        {
            try
            {
                //if (profile == "CuratedUser" || IsClientCurated)
                //{

                //}
                EmailId = EmailId.Replace("\"", "");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", EmailId);
                    parameters.Add("@actionStatus", actionStatus);
                    parameters.Add("@irfId", irfId);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForCuratedUserByIRF", parameters, commandTimeout: 600,
                             commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<IEnumerable<MeetingVM>> GetCPMeetings(int clientId, string profile, string Emailid)
        {
            try
            {
                if (profile == "ExternalUser")
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("clientid", clientId);
                        parameters.Add("Emailid", Emailid);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForExternalUser", parameters, commandTimeout: 600,
                                commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@clientid", clientId);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetCPMeetings", parameters,
                            commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> IRFMultipleCandidateInvites(string IRFId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("IRFId", IRFId);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_IRFMultipleCandidateInvites", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task DeleteInterview(int imid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("imid", imid);
                    await SqlMapper.QueryAsync<MeetingVM>(con, "usp_DeleteInterview", parameters,
                       commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

        }
        public async Task<IEnumerable<MeetingVM>> MultipleCandidateInvites(string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_MultipleCandidateInvites", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Meeting>> GetAllOpenInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_OpenInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<Meeting>> GetAllEmergencyOpenInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_OpenEmergencyInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Meeting>> GetAllQCInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_QCInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetAllClientInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientInterview", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Meeting>> GetAllAssignedInterview(string emailID, string meetingLineUpType = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    parameters.Add("type", meetingLineUpType);
                    parameters.Add("timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }

        public async Task<IEnumerable<Meeting>> GetAllAssignedInterviewsForToday(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    parameters.Add("DateToday", DateTime.Now);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_GetAllAssignedInterviewsForToday", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }

        public async Task<IEnumerable<Meeting>> GetAllAssignedInterviewsForTheWeek(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    parameters.Add("DateToday", DateTime.Now);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_GetAllAssignedInterviewsForTheWeek", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }

        public async Task<IEnumerable<Meeting>> GetCandidateInterviews(string emailID, string status)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyCandidateInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }

        public async Task<int> CheckCompletedInterviews(string emailID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailID))
                    return 0;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailID", emailID);
                    //usp_CheckCompletedInterviews
                    return await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_CheckCompletedInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;

        }



        private DynamicParameters SetStoredProcedureParameterForMeeting(AddUpdateMeetingVM entity)
        {
            var parameters = new DynamicParameters();
            if (entity.imid > 0)
            {
                parameters.Add("@interviewId", entity.imid);
                parameters.Add("@IsEmergency", entity.IsEmergency);
                parameters.Add("@HasDistinctParticipants", entity.HasDistinctParticipants);

            }
            parameters.Add("@IRFId", entity.IRFId); // Normal parametersmeters           
            parameters.Add("@interviewDate", entity.interviewDate);
            parameters.Add("@interviewSlot", entity.interviewSlot);
            parameters.Add("@interviewDuration", entity.interviewDuration);
            parameters.Add("@candidateName", entity.candidateName);
            parameters.Add("@candidateEmail", entity.candidateEmail);
            parameters.Add("@hrRiseBirdAssinged", entity.risebirdHR);
            parameters.Add("@resumeLink", entity.resumeLink);
            parameters.Add("@zoomLink", entity.zoomLink);
            parameters.Add("@ClientHrContact", entity.ClientHrContact);
            parameters.Add("@comfirmationStatus", entity.confirmationStatus);
            parameters.Add("@interviewer", entity.interviewer);
            parameters.Add("@preferredLocation", entity.preferredLocation);
            parameters.Add("@expectedPerHourRate", entity.expectedPerHourRate);
            parameters.Add("@joiningTime", entity.joiningTime);
            parameters.Add("@staffingAgency", entity.staffingAgency);
            parameters.Add("@currentEmployerName", entity.currentEmployerName);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@HRCoordinatorEmailId", entity.HRCoordinatorEmailId);
            //parameters.Add("@QCInterviewer", entity.QCInterviewer);
            parameters.Add("@zoomMeetingId", entity.ZoomMeetingId);

            return parameters;
        }



        public async Task<bool> AddMeeting(AddUpdateMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", meeting.IRFId); // Normal parametersmeters           
                parameters.Add("@interviewDate", meeting.interviewDate);
                parameters.Add("@interviewSlot", meeting.interviewSlot);
                parameters.Add("@interviewDuration", meeting.interviewDuration);
                parameters.Add("@candidateName", meeting.candidateName);
                parameters.Add("@candidateEmail", meeting.candidateEmail);
                parameters.Add("@hrRiseBirdAssinged", meeting.risebirdHR);
                parameters.Add("@resumeLink", meeting.resumeLink);
                parameters.Add("@zoomLink", meeting.zoomLink);
                parameters.Add("@ClientHrContact", meeting.ClientHrContact);
                parameters.Add("@comfirmationStatus", meeting.confirmationStatus);
                parameters.Add("@interviewer", meeting.interviewer);
                parameters.Add("@preferredLocation", meeting.preferredLocation);
                parameters.Add("@expectedPerHourRate", meeting.expectedPerHourRate);
                parameters.Add("@joiningTime", meeting.joiningTime);
                parameters.Add("@staffingAgency", meeting.staffingAgency);
                parameters.Add("@currentEmployerName", meeting.currentEmployerName);
                parameters.Add("@MobileNumber", meeting.MobileNumber);
                //parameters.Add("@QCInterviewer", meeting.QCInterviewer);
                parameters.Add("@zoomMeetingId", meeting.ZoomMeetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddMeeting", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public async Task<MeetingVM> GetInterviewMeetingById(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetMeetingById", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<AddUpdateMeetingVM> GetInterviewToUpdateMeetingById(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<AddUpdateMeetingVM>(con, "usp_GetMeetingById", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateMeeting(AddUpdateMeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForMeeting(meeting);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateMeeting", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<string>(
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid }, commandType: Text);


                    var query = "Select videoConfirmation from InterviewerProfile where videoConfirmation=1 and UserId=(select Id from userList where Emailid=@Emailid)";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@EmailId", interviewerMail);
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    if (results.FirstOrDefault() == 1)
                    {
                        return "Interview assigned Successfully. Interviewer video needs to be mandatory on throughout the interview duration. INR 100 will be paid additional for US interviews and INR 50 will be paid additional for India interview. Failing to on the video may result in 100% payment deduction by client. Rest assured your data is 100% private and you will never be mapped to your current company. If you are not comfortable with opening your video, please unassign this interview.";
                    }
                    else
                    {
                        return "Interview assigned Successfully.";
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }

        public async Task<bool> CancelInterview(int meetingid)
        {
            try
            {
                var query = "update [dbo].[InterviewMeetings] set confirmationStatus=@value where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@value", 2);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    _ = await con.QueryAsync<int>(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UnassignYourself(int meetingid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<string>(
                        con, "usp_UnAssignInterviewer", parameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public List<string> ListInterviewersMail(string candidateEmail, string interviewDate)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@candidateEmail", candidateEmail);
                parameters.Add("@interviewDate", interviewDate);
                parameters.Add("@IRFID", "");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<string>(
                        con, "usp_ListOfCancelledInterviewInterviwer", parameters, commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public async Task<InterviewComments> GetInterviewComments(int interviewId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryFirstAsync<InterviewComments>(con, "usp_GetInterviewComments", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateComments(InterviewComments interviewComments)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForComments(interviewComments);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewComments", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public async Task<IEnumerable<TransSkill>> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransTechSkill where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TransSkill>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<TransSkillGeneral>> GetSkillsGeneralbyIRFid(int id)
        {
            try
            {
                var query = "Select tsg.*, gsm.Name as Name from TransSkillGeneral tsg join GeneralSkillMatrix gsm on tsg.type = gsm.Id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TransSkillGeneral>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<TransJoinability>> GetTransJoinability(int id)
        {
            try
            {
                var query = "Select tj.*, jm.Name as Name from TransJoinability tj join JoinabilityMatrix jm on tj.type = jm.Id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TransJoinability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<IRF> GetIRFList()
        {
            try
            {
                var query = "Select * from IRF where IsDeleted= 0";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>(query);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransSelectability> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select ts.*, sm.Name as Name from TransSelectability ts join SelectabilityMatrix sm on ts.type = sm.id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSelectability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<SkillComment>> GetTechSkillComments(int id, int userId)
        {

            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", id);
                parameters.Add("@UserId", userId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SkillComment>(con, "usp_GetTechSkillQuestionsByUserId", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<IEnumerable<Level>> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<LevelTech>> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<LevelTech>(
                        con, "select * from LevelTech", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<Skill>> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Skill>(
                        con, "select * from Skill", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ClientUser> GetClientDetails(string emailId)
        {
            try
            {
                var query = "select * from ClientUser where UserId in (select id from UserList where EmailId = @EmailId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ClientUser>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetClientInterviewMeetings()
        {
            try
            {
                var query = "select im.*, im.id as imid, IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink, ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,ic.ApprovalComments,ic.FinalComments,QCF.InterviewId as QCInterview, cm.id as CandidateId from InterviewMeetings im inner join IRF on im.IRFId = IRF.IRFId left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join QCFeedbackReport QCF on im.id=QCF.InterviewId left join CandidatesMaster cm on im.candidateEmail = cm.EmailId";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MeetingVM>(query);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        /// <summary>
        /// client meeting
        /// </summary>
        /// <param name="emailID"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingVM>> GetClientMeetings(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("EmailId", emailID);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientMeetings", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetClientSelectedMeetings(string emailID, bool isFilter = false, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("EmailId", emailID);
                    if (isFilter)
                    {
                        parameters.Add("@timeNow", null);
                    }
                    else
                    {
                        parameters.Add("@timeNow", DateTime.Now);
                    }
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientSelectedMeetings", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddInterviewReport(InterviewReport reportDetails, string loggedInEmailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@IRFId", reportDetails.IRFId);
                    paramx.Add("@CandidateName", reportDetails.CandidateName);
                    paramx.Add("@InterviewId", reportDetails.InterviewId);
                    paramx.Add("@confirmationStatus", reportDetails.confirmationStatus != null ? reportDetails.confirmationStatus : string.Empty);
                    paramx.Add("@OverallScore", reportDetails.OverallScore);
                    paramx.Add("@PerformedBy", loggedInEmailId);
                    paramx.Add("@PerformedAt", DateTime.Now);
                    //paramx.Add("@Rating", reportDetails.Rating);
                    paramx.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    //paramx.Add("@SkillGeneralComments", reportDetails.SkillGeneralComments);
                    paramx.Add("@SelectabilityComments", reportDetails.SelectabilityComments);
                    paramx.Add("@JoinabilityComments", reportDetails.JoinabilityComments);
                    paramx.Add("@ProctoringCount", reportDetails.ProctoringCount);
                    parameters.Add("@interviewId", reportDetails.InterviewId);
                    parameters.Add("@interviewStatus", reportDetails.InterviewStatus);
                    parameters.Add("@MeetingStatus", reportDetails.MeetingStatus);
                    parameters.Add("@interviewerComment", reportDetails.InterviewerComment);
                    parameters.Add("@audiolink", reportDetails.AudioLink);
                    parameters.Add("@videolink", reportDetails.VideoLink);
                    parameters.Add("@ThumbnailImage", reportDetails.ThumbnailImage);
                    parameters.Add("@FeedbackAudioLink", reportDetails.FeedbackAudioLink);
                    parameters.Add("@ThumbnailTimeSeconds", reportDetails.ThumbnailTimeSeconds);
                    parameters.Add("@VideoDuration", reportDetails.VideoDuration);
                    parameters.Add("@PerformedBy", loggedInEmailId);
                    parameters.Add("@PerformedAt", DateTime.Now);

                    DynamicParameters parametersPoints = new DynamicParameters();

                    //if (reportDetails.MeetingStatus == 1 || reportDetails.MeetingStatus == 3)
                    //{
                    //    var duration = reportDetails.VideoDuration;
                    //    int baseValue = 6;
                    //    float pointPercent = duration >= baseValue ? ((baseValue * 100) / 60) : ((duration * 100) / 60);
                    //    parametersPoints.Add("@Points", pointPercent);
                    //}
                    //else if (reportDetails.MeetingStatus == 5)
                    //{
                    //    parametersPoints.Add("@Points", 0);
                    //    //parametersPoints.Add("@Points", -200);
                    //    //parametersPoints.Add("@Points", -350);
                    //}
                    //else if (reportDetails.MeetingStatus == 4 || reportDetails.MeetingStatus == 6 || reportDetails.MeetingStatus == 7 || reportDetails.MeetingStatus == 8)
                    //{
                    //    int duration = reportDetails.VideoDuration;
                    //    if (reportDetails.InterviewStatus == (int)InterviewStatus.Rejected || reportDetails.InterviewStatus == (int)InterviewStatus.Proxy)
                    //    {
                    //        int baseValue = 30;

                    //        float pointPercent = duration >= baseValue ? ((baseValue * 100) / 60) : ((duration * 100) / 60);
                    //        parametersPoints.Add("@Points", pointPercent);
                    //    }
                    //    else
                    //    {
                    //        float pointPercent = duration >= 60 ? 100 : ((duration * 100) / 60);
                    //        parametersPoints.Add("@Points", pointPercent);
                    //    }
                    //}

                    parametersPoints.Add("@interviewId", reportDetails.InterviewId);
                    //parametersPoints.Add("@UserEmail", reportDetails.UserDetails);
                    //parametersPoints.Add("@Type", "Interview");
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_AddUpdateInterviewReport", paramx,
                                            commandType: CommandType.StoredProcedure);
                    if (reportDetails.TechSkillReportCollection != null && !reportDetails.TechSkillReportCollection.Any(x => x.ActualLevelId == 0))
                    {
                        DataTable techReportDt = new DataTable();
                        techReportDt.Columns.Add("InterviewId", typeof(int));
                        techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                        techReportDt.Columns.Add("SkillId", typeof(int));
                        techReportDt.Columns.Add("ActualLevelId", typeof(int));
                        techReportDt.Columns.Add("WeightageRequired", typeof(int));
                        techReportDt.Columns.Add("WeightageActual", typeof(int));
                        techReportDt.Columns.Add("TechComments", typeof(string));

                        foreach (var item in reportDetails.TechSkillReportCollection)
                        {
                            techReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.TechComments);
                        }

                        SqlCommand cmdTechReport = new SqlCommand("usp_TechSkillReportBulkInsert", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                        cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                        con.Open();
                        if (await cmdTechReport.ExecuteNonQueryAsync() != 0)
                        {
                            con.Close();
                        }
                    }
                    //if (reportDetails.SkillGeneralReportCollection != null && reportDetails.IsGeneralSkillRequired)
                    //{
                    //    DataTable skillGeneralReportDt = new DataTable();
                    //    skillGeneralReportDt.Columns.Add("InterviewId", typeof(int));
                    //    skillGeneralReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    //    skillGeneralReportDt.Columns.Add("Type", typeof(string));
                    //    skillGeneralReportDt.Columns.Add("ActualLevelId", typeof(int));
                    //    skillGeneralReportDt.Columns.Add("WeightageRequired", typeof(int));
                    //    skillGeneralReportDt.Columns.Add("WeightageActual", typeof(int));
                    //    skillGeneralReportDt.Columns.Add("Value", typeof(string));


                    //    foreach (var item in reportDetails.SkillGeneralReportCollection)
                    //    {
                    //        skillGeneralReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value);
                    //    }

                    //    SqlCommand cmdSkillGeneralReport = new SqlCommand("usp_SkillGeneralReportBulkInsert", con)
                    //    {
                    //        CommandType = CommandType.StoredProcedure
                    //    };
                    //    cmdSkillGeneralReport.Parameters.Add(new SqlParameter("@SkillGeneralReportBulkData", SqlDbType.Structured));
                    //    cmdSkillGeneralReport.Parameters["@SkillGeneralReportBulkData"].Value = skillGeneralReportDt;
                    //    con.Open();
                    //    if (cmdSkillGeneralReport.ExecuteNonQuery() != 0)
                    //    {
                    //        con.Close();
                    //    }
                    //}
                    if (reportDetails.QuestionsCollection != null)
                    {
                        DataTable questionsDt = new DataTable();
                        questionsDt.Columns.Add("InterviewId", typeof(int));
                        questionsDt.Columns.Add("Comment", typeof(string));
                        questionsDt.Columns.Add("RatingId", typeof(int));
                        questionsDt.Columns.Add("Timing", typeof(int));
                        questionsDt.Columns.Add("SkillName", typeof(string));


                        foreach (var item in reportDetails.QuestionsCollection)
                        {
                            if (!string.IsNullOrEmpty(item.Comment) && !string.IsNullOrEmpty(item.SkillName))
                            {
                                questionsDt.Rows.Add(reportDetails.InterviewId, item.Comment, item.RatingId, item.Timing, item.SkillName);
                            }
                        }

                        if (questionsDt.Rows != null && questionsDt.Rows.Count > 0)
                        {
                            SqlCommand cmdQuestions = new SqlCommand("usp_QuestionsDiscussedBulkInsert", con)
                            {
                                CommandType = CommandType.StoredProcedure
                            };
                            cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDiscussedBulkData", SqlDbType.Structured));
                            cmdQuestions.Parameters["@QuestionsDiscussedBulkData"].Value = questionsDt;
                            con.Open();
                            if (await cmdQuestions.ExecuteNonQueryAsync() != 0)
                            {
                                con.Close();
                            }
                        }
                    }

                    if (reportDetails.Proctoring != null)
                    {
                        //update protoring report table
                        string sqlProcReport = "INSERT INTO ProctoringReport Values (@InterviewId, @EyesDirection, @EyesReflection, @HeadDirection, @LipsOverhead, @LipsDarklight, @LipsHeadphone, @LatencyJitter, @DesktopSuspiciousApps, @DesktopScreenSharing, @VoiceAudioSplitter, @VoiceToneExpression, @VoicePersonalUnsatisfactory, @VoiceSuspicious, @LinkedInPhotoVerification);";
                        var result3 = await con.ExecuteAsync(sqlProcReport, new
                        {
                            reportDetails.InterviewId,
                            reportDetails.Proctoring.EyesDirection,
                            reportDetails.Proctoring.EyesReflection,
                            reportDetails.Proctoring.HeadDirection,
                            reportDetails.Proctoring.LipsOverhead,
                            reportDetails.Proctoring.LipsDarklight,
                            reportDetails.Proctoring.LipsHeadphone,
                            reportDetails.Proctoring.LatencyJitter,
                            reportDetails.Proctoring.DesktopSuspiciousApps,
                            reportDetails.Proctoring.DesktopScreenSharing,
                            reportDetails.Proctoring.VoiceAudioSplitter,
                            reportDetails.Proctoring.VoiceToneExpression,
                            reportDetails.Proctoring.VoicePersonalUnsatisfactory,
                            reportDetails.Proctoring.VoiceSuspicious,
                            reportDetails.Proctoring.LinkedInPhotoVerification
                        });

                    }

                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewReportComments", parameters,
                            commandType: CommandType.StoredProcedure);

                    if (reportDetails.MeetingStatus != 2)
                    {
                        try
                        {
                            //SqlMapper.Query(con, "usp_InterviewAddPoints", parametersPoints,
                            //        commandType: CommandType.StoredProcedure);
                            await SqlMapper.QueryAsync(con, "usp_InsertOrUpdateInterviewPoints", parametersPoints,
                                    commandType: CommandType.StoredProcedure);
                        }

                        catch (SqlException ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }


                    return true;
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<InterviewReport>> GetInterviewReportById(int id, int irfId = 0)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", id);//@IrfId
                dynamicParameters.Add("@IrfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewReport>(con, "usp_GetInterviewReportById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<JoinabilityReport> GetJoinabilityReport(int id)
        {
            try
            {
                var query = "select JoinabilityReport.*,JoinabilityReport.Type as Name, [Level].[Name] as RequiredLevel from JoinabilityReport inner join [Level] on JoinabilityReport.RequiredLevelId=[Level].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<JoinabilityReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<SelectabilityReport> GetSelectabilityReport(int id)
        {
            try
            {
                var query = "select SelectabilityReport.*,SelectabilityReport.Type as Name, [Level].[Name] as RequiredLevel from SelectabilityReport inner join [Level] on SelectabilityReport.RequiredLevelId=[Level].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SelectabilityReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<QuestionsDiscussed> GetQuestionsDiscussed(int id)
        {
            try
            {
                // var query = "Select * from QuestionsDiscussed where InterviewId = @id";
                var query = "Select  qd.id,qd.InterviewId,qd.Comment,qd.SkillName,qd.RatingId,qd.Timing,vc.VideoLink,vc.Priority from QuestionsDiscussed qd Left join VideoQuestionDetails vc on vc.Question=qd.Comment and vc.Interviewid =@id where qd.Interviewid= @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<QuestionsDiscussed>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<string>> GetIRFName(int id)
        {
            try
            {
                var query = "Select IRFId from IRF where IsDeleted = @IsDeleted and id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IRFDetails> GetIRFDetails(int id)
        {
            try
            {
                var query = "Select IRFId, JDTiltle, IsGeneralSkillRequired from IRF where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<IRFDetails>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IRFVm> GetIRFNameAndJoinability(int id)
        {
            try
            {
                var query = "Select IRFId, IsJoinabilitySelectabilityRequired from IRF where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<IRFVm>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TechSkillReport> GetTechSkillReport(int id)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select ts.*, [LevelTech].[Name] as RequiredLevel from TechSkillReport ts inner join [LevelTech] on ts.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TechSkillReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<SkillGeneralReport> GetSkillGeneralReport(int id)
        {
            try
            {
                var query = "select SkillGeneralReport.*,SkillGeneralReport.Type as Name, [LevelTech].[Name] as RequiredLevel from SkillGeneralReport inner join [LevelTech] on SkillGeneralReport.RequiredLevelId=[LevelTech].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SkillGeneralReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public ProctoringReport GetProctoringReport(int id)
        {
            try
            {
                var query = "select * from ProctoringReport where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ProctoringReport>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> GetIRFbyId(int irfId)
        {
            try
            {
                var query = "Select * from IRF where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<IRF>(query, dynamicParameters);
                    if (result.Count() > 0)
                    {
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<TransClientUser> GetClientsByRoles(string IRF)
        {
            try
            {
                var query = "select UserList.EmailId, a.* from (select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)) as a inner join UserList on a.ClientUserId = UserList.id";//"select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";//"Select * from TransClientUser where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRF);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public bool PostApproval(int imid, int value, string comment)
        {
            try
            {
                var query = "update InterviewComments set IsApproved=@IsApproved, ApprovalComments=@ApprovalComments where InterviewId=@InterviewId";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsApproved", value);
                dynamicParameters.Add("@ApprovalComments", comment);
                dynamicParameters.Add("@InterviewId", imid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsApproved", isApproved);
                dynamicParameters.Add("@ApprovalComments", approvalComment);

                dynamicParameters.Add("@IsSelected", isSelected);
                dynamicParameters.Add("@FinalComments", selectionComment);

                dynamicParameters.Add("@JoiningStatus", joiningStatus);
                dynamicParameters.Add("@JoiningComments", joiningComment);

                dynamicParameters.Add("@OfferStatus", offerStatus);
                dynamicParameters.Add("@OfferComments", offerComment);
                dynamicParameters.Add("@OfferComments", offerComment);
                dynamicParameters.Add("@movetoCP", movetoCP);
                dynamicParameters.Add("@id", imid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<int>(con, "usp_AddUpdateClientFeedback", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> SaveQCFeedback(int imid, int qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled)
        {
            try
            {
                //var query = "update InterviewMeetings set IsApproved=@IsApproved, ApprovalComments=@ApprovalComments where id=@id";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", imid);
                dynamicParameters.Add("@QCStatus", qcStatus);
                dynamicParameters.Add("@QCComment", comment);
                dynamicParameters.Add("@FraudLipSync", FraudLipSync);
                dynamicParameters.Add("@QuestionsRelevance", QuestionsRelevance);
                dynamicParameters.Add("@HandsOnCoding", HandsOnCoding);
                dynamicParameters.Add("@BiasAndCandidateHandling", BiasAndCandidateHandling);
                dynamicParameters.Add("@TechnicalCommunicationSkills", TechnicalCommunicationSkills);
                dynamicParameters.Add("@ChapteringCompleted", ChapteringCompleted);
                dynamicParameters.Add("@QuestionsFilled", QuestionsFilled);
                dynamicParameters.Add("@TechSkillsMetric", TechSkillsMetric);
                dynamicParameters.Add("@SelectabilityMetric", SelectabilityMetric);
                dynamicParameters.Add("@JoinabilityMetric", JoinabilityMetric);
                dynamicParameters.Add("@GeneralSkillMetric", GeneralSkillMetric);
                dynamicParameters.Add("@CommentsFilled", CommentsFilled);
                dynamicParameters.Add("@MeaningfulAssessments", MeaningfulAssessments);
                dynamicParameters.Add("@CorrectEnglishGrammar", CorrectEnglishGrammar);
                dynamicParameters.Add("@ProctoringReportFilled", ProctoringReportFilled);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_SaveQCFeedback", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                }
                DynamicParameters parametersPoints = new DynamicParameters();
                if (qcStatus == 1)
                {
                    parametersPoints.Add("@Points", 100);
                    parametersPoints.Add("@interviewId", imid);
                    parametersPoints.Add("@Type", "Quality Controller");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync(con, "usp_InterviewAddPoints", parametersPoints,
                                        commandType: CommandType.StoredProcedure);
                    }
                }
                return true;
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool PostSelectionStatus(int imid, int value, string comment)
        {
            try
            {
                var query = "update InterviewComments set IsSelected=@IsSelected, FinalComments=@FinalComments where InterviewId=@InterviewId";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsSelected", value);
                dynamicParameters.Add("@FinalComments", comment);
                dynamicParameters.Add("@InterviewId", imid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        private DynamicParameters SetStoredProcedureParameterForComments(InterviewComments entity)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@interviewId", entity.InterviewId); // Normal parametersmeters 
            parameters.Add("@@interviewStatus", entity.InterviewStatus);
            parameters.Add("@@interviewerComment", entity.InterviewerComment);
            parameters.Add("@fraudCheckStatus", entity.FraudCheckStatus);
            parameters.Add("@hrComment", entity.HRComment);
            parameters.Add("@ClientInterviewStatus", entity.FinalInterviewMeetingStatus);
            //parameters.Add("@QCStatus", entity.QCStatus);
            //parameters.Add("@QCComment", entity.QCComment);
            parameters.Add("@trainingStatus", entity.TrainingStatus);
            parameters.Add("@trainerComment", entity.TrainerComment);
            parameters.Add("@audiolink", entity.AudioLink);
            parameters.Add("@videolink", entity.VideoLink);
            parameters.Add("@MeetingStatus", entity.MeetingStatus);
            parameters.Add("@OfferStatus", entity.OfferStatus);
            parameters.Add("@PreparationStatus", entity.PreparationStatus);
            parameters.Add("@JoiningStatus", entity.JoiningStatus);
            parameters.Add("@ThumbnailImage", entity.ThumbnailImage);
            parameters.Add("@AadharFileLink", entity.AadharFileLink);
            parameters.Add("@CandidateAvailable", entity.CandidateAvailable);
            return parameters;
        }

        public async Task<int> GetCandidateId(int id, string irfName)
        {
            int result = 0;
            try
            {
                var query = "select cm.id from InterviewMeetings im join UserList ul on im.candidateEmail = ul.EmailId join CandidatesMaster cm on cm.UserId=ul.id where im.IRFId = @irfName and im.id =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@irfName", irfName);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = (await con.QueryAsync<int>(query, dynamicParameters)).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return result;
            }
        }

        public bool UpdateInterviewVerificationScore(int meetingId, double confidence)
        {
            try
            {
                var query = "update InterviewMeetings set InterviewBiometryScore = @InterviewBiometryScore where id =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewBiometryScore", confidence);
                dynamicParameters.Add("@id", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public bool UpdateJoiningVerificationScore(int meetingId, double confidence)
        {
            try
            {
                var query = "update InterviewMeetings set JoiningBiometryScore = @JoiningBiometryScore where id =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@JoiningBiometryScore", confidence);
                dynamicParameters.Add("@id", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<MeetingVM> GetMeetingbyid(int id, string Email)
        {
            try
            {
                if (id == 0)
                    return null;
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                parameters.Add("@Email", Email);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_GetInterviewMeetingsbyId", parameters,
                        commandType: CommandType.StoredProcedure);
                    var query = "Select InterviewId,VideoTitle,VideoLink,Priority,ThumbnailLink from VideoQuestionDetails Where InterViewid=@InterViewid and NULLIF(VideoLink, '') IS NOT NULL";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@InterViewid", id);
                    var resultset = con.Query<QuestionDetails>(query, dynamicParameters);
                    if (resultset.Count() != 0)
                    {
                        result.MeetingPatch = resultset.OrderBy(x => x.Priority).ToList();
                    }
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            return null;
        }
        public async Task<MeetingVM> VedioLikeAsync(string value, string vedioid, string email)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@isLiked", value);
                parameters.Add("@VedioId", vedioid);
                parameters.Add("@EmailID", email);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_AddinterviewvedioLike", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return null;
        }
        public async Task AddTotalViewCount(int id, string emailaddress, string flag)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Interviewid", id);
                parameters.Add("@EmailId", emailaddress);
                if (flag == "Public")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<string>(con, "usp_AddinterviewView", parameters,
                           commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<string>(con, "usp_SharedAddinterviewView", parameters,
                           commandType: CommandType.StoredProcedure);
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public async Task<string> VedioShare(string value, string vedioid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@isShared", value);
                parameters.Add("@VedioId", vedioid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_AddinterviewvedioShare", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "";
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }

        public async Task<string> VedioComment(string value, string vedioid, string email)
        {
            try
            {
                email = email.Replace("\"", "");

                var parameters = new DynamicParameters();
                parameters.Add("@Comment", value);
                parameters.Add("@VedioId", vedioid);
                parameters.Add("@EmailID", email);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<string>(con, "usp_AddinterviewvedioComment", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "";
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }
        public async Task<List<VedioComments>> GetvedioComments(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@VedioId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<VedioComments>(con, "usp_GetinterviewvedioComment", parameters,
                       commandType: CommandType.StoredProcedure);
                    return (List<VedioComments>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public async Task<EmailDetails> GetvedioShortList(string vedioid, string email)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@VedioId", vedioid);
                parameters.Add("@emailId", email);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<EmailDetails>(con, "usp_AddinterviewvedioShortList", parameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> CandidateSuitable(int id, string email, string Comment)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewId", id);
                parameters.Add("@EmailId", email);
                parameters.Add("@Comment", Comment);
                parameters.Add("@Clientjdid", 0);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_AddSuitableCandidate", parameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault().ToString();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<VedioComments> Intrestreaction(string vedioid, string Status)
        {
            try
            {
                //Debit Points from Client wallet
                if (Status == "Interest Accepted")
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Interviewid", vedioid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var updatestatus = await SqlMapper.QueryAsync<string>(con, "usp_UpdateWalletdetails", parameters,
                            commandType: CommandType.StoredProcedure);
                        //mail to client for insuficiant balance
                        if (!string.IsNullOrEmpty(updatestatus.FirstOrDefault()))
                        {
                            Mail _mail = new Mail
                            {
                                TO = updatestatus.FirstOrDefault(),
                                //CC = email,
                                Subject = "Risebird: Insuficient Balance in your wallet"
                            };
                            var result = await _notificationHelper.InsuficientBalancNotifiaction(_mail);
                        }
                    }
                }
                //Update Interest status
                var query = "Update SuitableCandidate set Status=@Status where id=@Id ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Id", vedioid);
                dynamicParameters.Add("@Status", Status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Query(query, dynamicParameters);
                }

                var finalquery = "Select ul.EmailId as Comment,(ul.Firstname +''+ ul.lastname) as UserName from SuitableCandidate sc Join UserList ul on sc.userid=ul.id where sc.id=@Id ";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Id", vedioid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<VedioComments>(finalquery, finaldynamicParameters);
                    return (VedioComments)result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string ShortListreaction(string vedioid, string Status)
        {
            try
            {
                var query = "Update RiseTubeVedioShortList set Status=@Status where id=@Id ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Id", vedioid);
                dynamicParameters.Add("@Status", Status);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Query(query, dynamicParameters);
                    return "Success";
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<Meeting>> ShortListedClient(string emailID, string IRFID)
        {
            try
            {
                var query = "select id from UserList where EmailId = @EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailID);
                var id = 0;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    id = await con.QueryFirstOrDefaultAsync<int>(query, dynamicParameters);
                    // return results;
                }
                var finalquery = "Select ul2.EmailId as ClientName, IRF.JDTiltle, IRF.JDLink, su.id as id,su.ShortListedOn as interviewDate,IsNull(su.Status,'') as Status  from IRF inner join InterviewMeetings im on im.IRFId=IRF.IRFId inner join RiseTubeVedioShortList su on im.id=su.Interviewid inner join UserList ul on im.candidateEmail=ul.EmailId inner join UserList ul2 on ul2.id=su.userid Where ul.id=@Id and im.IRFId=@IRFID";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Id", id);
                finaldynamicParameters.Add("@IRFID", IRFID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<Meeting>(finalquery, finaldynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public async Task<IEnumerable<Meeting>> InterestListClient(string emailID, string IRFID)
        {
            try
            {
                var query = "select id from UserList where EmailId = @EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailID);
                var id = 0;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    id = await con.QueryFirstOrDefaultAsync<int>(query, dynamicParameters);
                    // return results;
                }
                var finalquery = "Select ul2.EmailId as ClientName,IRF.JDTiltle, IRF.JDLink, IRF.IRFId as IRFID,su.id as id,su.InterstedOn as interviewDate,IsNull(su.Status,'') as Status,su.Comment as ApprovalComments from IRF inner join InterviewMeetings im on im.IRFId=IRF.IRFId inner join SuitableCandidate su on im.id=su.Interviewid inner join UserList ul on im.candidateEmail=ul.EmailId inner join UserList ul2 on ul2.id=su.userid Where ul.id=@Id and im.IRFId=@IRFID";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Id", id);
                finaldynamicParameters.Add("@IRFID", IRFID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<Meeting>(finalquery, finaldynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public string Fetchusername(string Emailid)
        {
            try
            {
                var finalquery = "Select (Firstname+' '+LastName) As Name  from InterviewMeetings Where EmailId=@Emailid";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(finalquery, finaldynamicParameters);
                    return results.FirstOrDefault().ToString();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public async Task<string> VedioShared(string Emailids, string vedioid, string Userid)
        {
            try
            {
                var emailidreturn = string.Empty;
                var emails = Emailids.Split(";");
                foreach (var item in emails)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Email", item);
                    parameters.Add("@VedioId", vedioid);
                    parameters.Add("@EmailAddress", Userid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var result = await SqlMapper.QueryFirstAsync<string>(con, "usp_AddinterviewvedioSharedetails", parameters,
                            commandType: CommandType.StoredProcedure);
                        if (string.IsNullOrEmpty(emailidreturn))
                        {
                            emailidreturn = result;
                        }
                        else
                        {
                            emailidreturn = emailidreturn + ";" + result;
                        }
                    }
                }
                return emailidreturn;

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }
        public async Task<IEnumerable<VideoCountdetail>> GetTotalViewCount(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Interviewid", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<VideoCountdetail>(con, "usp_GetinterviewView", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public bool SaveFeedbackAudio(int interviewId, string irfid, string feedbackAudioLink, int candidateType = 0, bool isClientFeedback = false, string currentUser = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", interviewId);//@IrfId
                dynamicParameters.Add("@irf_id", irfid);//@IrfId
                dynamicParameters.Add("@FeedbackAudioLink", feedbackAudioLink);
                if (isClientFeedback)
                {
                    dynamicParameters.Add("@emailId", currentUser);
                    dynamicParameters.Add("@timeNow", DateTime.Now);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        SqlMapper.Query(con, "usp_SaveClientFeedbackAudio", dynamicParameters,
                           commandType: CommandType.StoredProcedure);
                        return true;
                    }
                }
                else
                {
                    if (candidateType == (int)CandidateTypeEnum.Interviewer)
                    {
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            SqlMapper.Query(con, "usp_SaveInterviewerFeedbackAudio", dynamicParameters,
                               commandType: CommandType.StoredProcedure);
                            return true;
                        }
                    }
                    else
                    {
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            SqlMapper.Query(con, "usp_SaveFeedbackAudio", dynamicParameters,
                               commandType: CommandType.StoredProcedure);
                            return true;
                        }
                    }
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<IEnumerable<string>> GetEmailsforBroadcast(string profile)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Profile", profile);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetEmailsforBroadcast", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<Meeting>> GetHROpenInterviews(string emailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_HROpenInterviews", dynamicParameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public string HRAssignYourself(int meetingid, string hrMailId)
        {
            try
            {
                var query = "update [dbo].[InterviewMeetings] set HRCoordinatorEmailId=@hrMailId where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@hrMailId", hrMailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return "Interview assigned Successfully.";
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview!";
            }

        }

        public async Task<IEnumerable<Meeting>> GetHRAssignedInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyHRInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }

        public bool HRUnassignYourself(int meetingid)
        {
            try
            {
                var query = "update [dbo].[InterviewMeetings] set HRCoordinatorEmailId = null where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> SaveHRComment(int id, string comment, string imageFileURL)
        {
            try
            {
                var query = "update InterviewMeetings set HRComment = @comment, HRImageFileURL=@imageFileURL where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@comment", comment);
                dynamicParameters.Add("@imageFileURL", imageFileURL);
                dynamicParameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }
        public IEnumerable<int> GetCuratedGroupIRFs(int curatedGroupId)
        {
            try
            {
                var query = "select IRFId from CuratedIRFMapping where CuratedGroupId = @CuratedGroupId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@CuratedGroupId", curatedGroupId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return con.Query<int>(query, dynamicParameters);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public IEnumerable<string> GetClientUsers(string clientEmailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ClientEmailId", clientEmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(con, "usp_GetClientUsers", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public Candidate GetCandidateDetails(int imid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@imid", imid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Candidate>(con, "usp_GetCandidateDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure).FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> AddSharedVideoDetails(InterviewReport reportModel)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@InterviewId", reportModel.InterviewId);
                    paramx.Add("@TechSkillComments", reportModel.TechSkillComments);
                    paramx.Add("@CandidateName", reportModel.CandidateName);
                    paramx.Add("@CandidateEmail", reportModel.CandidateEmail);
                    paramx.Add("@InterviewerComment", reportModel.InterviewerComment);
                    paramx.Add("@ViewName", reportModel.ViewName);
                    paramx.Add("@LogoUrl", reportModel.VideoLink);


                    //paramx.Add("@SkillGeneralComments", reportDetails.SkillGeneralComments);
                    //paramx.Add("@SelectabilityComments", reportDetails.SelectabilityComments);
                    // paramx.Add("@JoinabilityComments", reportDetails.JoinabilityComments);

                    var resultset = await SqlMapper.QueryAsync<int>(con, "usp_AddSharedInterviewVideo", paramx,
                                        commandType: CommandType.StoredProcedure);

                    DataTable techReportDt = new DataTable();
                    techReportDt.Columns.Add("InterviewId", typeof(int));
                    techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    techReportDt.Columns.Add("SkillId", typeof(int));
                    techReportDt.Columns.Add("ActualLevelId", typeof(int));
                    techReportDt.Columns.Add("WeightageRequired", typeof(int));
                    techReportDt.Columns.Add("WeightageActual", typeof(int));
                    techReportDt.Columns.Add("TechComments", typeof(string));

                    foreach (var item in reportModel.TechSkillReportCollection)
                    {
                        if (string.IsNullOrWhiteSpace(item.TechComments))
                        {
                            item.TechComments = "";
                        }
                        techReportDt.Rows.Add(resultset.FirstOrDefault(), item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.TechComments);
                    }

                    SqlCommand cmdTechReport = new SqlCommand("usp_SharedTechSkillReportBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                    con.Open();
                    if (await cmdTechReport.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    DataTable questionsDt = new DataTable();
                    questionsDt.Columns.Add("InterviewId", typeof(int));
                    questionsDt.Columns.Add("Comment", typeof(string));
                    questionsDt.Columns.Add("RatingId", typeof(int));
                    questionsDt.Columns.Add("Timing", typeof(int));
                    questionsDt.Columns.Add("SkillName", typeof(string));


                    foreach (var item in reportModel.QuestionsCollection)
                    {
                        if (string.IsNullOrEmpty(item.QuestionStatus))
                        {
                            questionsDt.Rows.Add(resultset.FirstOrDefault(), item.Comment, item.RatingId, item.Timing, item.SkillName);
                        }
                        else if (item.QuestionStatus != "true")
                        {
                            questionsDt.Rows.Add(resultset.FirstOrDefault(), item.Comment, item.RatingId, item.Timing, item.SkillName);
                        }
                    }

                    SqlCommand cmdQuestions = new SqlCommand("usp_SharedQuestionsDiscussedBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDiscussedBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@QuestionsDiscussedBulkData"].Value = questionsDt;
                    con.Open();
                    if (await cmdQuestions.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }
                    var result = resultset.FirstOrDefault();
                    return resultset.FirstOrDefault().ToString();
                }
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
                var query = "Select InterviewId from SharedInterviewComments where id=@Id";
                var Parameters = new DynamicParameters();
                Parameters.Add("@Id", videoid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, Parameters);
                    return results.FirstOrDefault();
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public IEnumerable<TechSkillReport> GetSharedTechSkillReport(int id)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select SharedTechSkillReport.*, [LevelTech].[Name] as RequiredLevel from SharedTechSkillReport inner join [LevelTech] on SharedTechSkillReport.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TechSkillReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<QuestionsDiscussed> GetSharedQuestionsDiscussed(int id)
        {
            try
            {
                //var query = "Select * from SharedQuestionsDiscussed where InterviewId = @id";
                var query = "Select qd.id,qd.InterviewId,qd.Comment,qd.SkillName,qd.RatingId,qd.Timing,vc.VideoLink,vc.Priority from SharedQuestionsDiscussed qd Left join VideoQuestionDetails vc on vc.Question=qd.Comment join SharedInterviewComments ic on ic.id=qd.Interviewid where qd.Interviewid= @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<QuestionsDiscussed>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<VideoDetails> GetVideoSumaryDetails(int Interviewid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    VideoDetails interviewComments = new VideoDetails();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@InterviewId", Interviewid);
                    var resultset = SqlMapper.Query<VideoDetails>(con, "usp_FetchVideoSumaryDetails", paramx,
                                        commandType: CommandType.StoredProcedure);
                    return resultset;
                }
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
                var parameters = new DynamicParameters();
                parameters.Add("@Interviewid", id);
                parameters.Add("@EmailId", emailaddress);
                if (flag == "Public")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var query = "Select id from UserList where Emailid = @emailaddress";
                        var dynamicParameters = new DynamicParameters();
                        dynamicParameters.Add("@emailaddress", emailaddress);
                        var results = await con.QueryAsync<int>(query, dynamicParameters);
                        int userid = results.FirstOrDefault();
                        if (userid == 0)
                        {
                            await SqlMapper.QueryAsync<string>(
                            con, "Update VideoCountDetails set TimeSpend=@TimeSpend,CurrentVideoTime=@CurrentTime where Videoid=@id and Emailid=@emailaddress", new { TimeSpend = TimeSpend.ToString(), id = id, emailaddress = emailaddress, CurrentTime = CurrentTime }, commandType: Text);
                        }
                        else
                        {
                            await SqlMapper.QueryAsync<string>(
                                con, "Update VideoCountDetails set TimeSpend=@TimeSpend,CurrentVideoTime=@CurrentTime where Videoid=@id and Userid=@emailaddress", new { TimeSpend = TimeSpend.ToString(), id = id, emailaddress = userid, CurrentTime = CurrentTime }, commandType: Text);
                        }
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<string>(
                            con, "Update SharedVideoCountDetails set TimeSpend=@TimeSpend,CurrentVideoTime=@CurrentTime where Videoid=@id and Emailid=@emailaddress", new { TimeSpend = TimeSpend.ToString(), id = id, emailaddress = emailaddress, CurrentTime = CurrentTime }, commandType: Text);

                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public IEnumerable<VideoDetails> GetallVideoSumaryDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    VideoDetails interviewComments = new VideoDetails();
                    DynamicParameters paramx = new DynamicParameters();
                    var resultset = SqlMapper.Query<VideoDetails>(con, "usp_FetchAllVideoSumaryDetails", paramx,
                                        commandType: CommandType.StoredProcedure);
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<Candidate> GetCandidateSharedDetails(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryFirstOrDefaultAsync<Candidate>(con, "usp_GetCandidateSharedDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> AddVedioShare(string email, string vedioid, string Emailids)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Userid", email);
                parameters.Add("@VedioId", vedioid);
                parameters.Add("@Emails", Emailids);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync(con, "usp_AddInterviewShareDetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "";
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return "";
        }
        public async Task<int> CheckVedioShare(string vedioid, string Emailid)
        {
            try
            {
                var query = "Select id from InterviewShareDetails where Interviewid=@interviewid and Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", vedioid);
                dynamicParameters.Add("@Emailid", Emailid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return 0;
        }
        public string AddVideoIntrestDetails(string videoid, string UrlType, string Emailid)
        {
            try
            {

                if (UrlType == "Shared")
                {
                    var query = "Update SharedVideoCountDetails set IsIntrested='Interested',OnDate=@ondate where Videoid=@Videoid and Emailid=@Emailid";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@Videoid", videoid);
                    dynamicParameters.Add("@Emailid", Emailid);
                    dynamicParameters.Add("@ondate", DateTime.Now);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<int>(query, dynamicParameters);
                        return "Success";
                    }
                }
                else
                {
                    var id = 0;
                    var queryfind = "Select id from UserList where Emailid=@Emailid";
                    var dynamicParametersfinal = new DynamicParameters();
                    dynamicParametersfinal.Add("@Emailid", Emailid);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<int>(queryfind, dynamicParametersfinal);
                        id = results.FirstOrDefault();
                    }

                    var query = "Update VideoCountDetails set IsIntrested='Interested',OnDate=@ondate where Videoid=@interviewid and (Emailid=@Emailid or Userid=@id)";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@interviewid", videoid);
                    dynamicParameters.Add("@Emailid", Emailid);
                    dynamicParameters.Add("@id", id);
                    dynamicParameters.Add("@ondate", DateTime.Now);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<int>(query, dynamicParameters);
                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IEnumerable<VideoDetails> GetVideoIntrestDetails()
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var resultset = SqlMapper.Query<VideoDetails>(con, "usp_GetVideoIntrestDetails", dynamicParameters,
                                       commandType: CommandType.StoredProcedure);
                    return resultset;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IEnumerable<string> FetchShareEmails(int Interviewid)
        {
            try
            {
                var query = "Select Emailid from InterviewShareDetails where Intervewid=@Videoid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Videoid", Interviewid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results;
                }
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    VideoDetails interviewComments = new VideoDetails();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@InterviewId", Interviewid);
                    paramx.Add("@Userid", emailid);

                    var resultset = SqlMapper.Query<VideoDetails>(con, "usp_FetchVideoCandidateSumaryDetails", paramx,
                                        commandType: CommandType.StoredProcedure);
                    return resultset;
                }
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
                int CandidateAvailable = 0;
                if (Status == "Available")
                {
                    CandidateAvailable = 1;
                }
                var query = "Update InterviewComments set CandidateAvailable=@CandidateAvailable where Interviewid=@interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", videoid);
                dynamicParameters.Add("@CandidateAvailable", CandidateAvailable);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return "Success";
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<IEnumerable<VideoSentiment>> GetSentimentAnalysis(int imid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", imid);
                    return await SqlMapper.QueryAsync<VideoSentiment>(con, "usp_GetSentimentAnalysis", parameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public async Task<IEnumerable<MeetingVM>> GetSharedRiseTubeMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetSharedRiseTubeMeetingsForCuratedUser", null, commandTimeout: 600,
                             commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetSharedRiseTubeMeetingsPaged(int page, string searchValue = null, string profileName = null)
        {
            try
            {
                if (profileName != null && profileName == "Client")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@startFromParam", ((page - 1) * 12));
                        parameters.Add("@endWithParam", page * 12);
                        parameters.Add("@searchValueParam", searchValue);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetSharedRiseTubeMeetingsForClientPaged", parameters, commandTimeout: 600,
                                 commandType: CommandType.StoredProcedure);
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@startFromParam", ((page - 1) * 12));
                        parameters.Add("@endWithParam", page * 12);
                        parameters.Add("@searchValueParam", searchValue);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetSharedRiseTubeMeetingsForCuratedUserPaged", parameters, commandTimeout: 600,
                                 commandType: CommandType.StoredProcedure);
                    }
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MeetingVM>> GetSharedRiseTubeMeetingsForClientPagedFiltered(int page, string primarySkills, string heuristicLevels, string searchValue = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startFromParam", ((page - 1) * 12));
                    parameters.Add("@endWithParam", page * 12);
                    parameters.Add("@searchValueParam", searchValue);
                    parameters.Add("@primarySkillsParam", primarySkills);
                    parameters.Add("@heuristicLevelsParam", heuristicLevels);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetSharedRiseTubeMeetingsForClientPagedFiltered", parameters, commandTimeout: 600,
                             commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<QuestionDetails>> FetchQuestionDetails(int interviewid)
        {
            try
            {
                var finalquery = "Select Id,Question,InterviewId,VideoTitle,VideoLink,Priority,ThumbnailLink from VideoQuestionDetails Where InterViewid=@InterViewid";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@InterViewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<QuestionDetails>(finalquery, finaldynamicParameters);
                    if (results.Count() == 0)
                    {
                        var query = "Select 0 as Id,Comment as Question,InterviewId,'' as VideoTitle,'' as VideoLink,0 as Priority,'' as ThumbnailLink from QuestionsDiscussed Where InterViewid=@InterViewid";
                        var dynamicParameters = new DynamicParameters();
                        dynamicParameters.Add("@InterViewid", interviewid);
                        results = await con.QueryAsync<QuestionDetails>(query, dynamicParameters);
                    }
                    return results;

                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string UpdateQuestionDetails(List<QuestionDetails> _question)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable questionsDt = new DataTable();
                    questionsDt.Columns.Add("id", typeof(int));
                    questionsDt.Columns.Add("Interviewid", typeof(int));
                    questionsDt.Columns.Add("Question", typeof(string));
                    questionsDt.Columns.Add("Priority", typeof(int));
                    questionsDt.Columns.Add("VideoTitle", typeof(string));
                    questionsDt.Columns.Add("VideoLink", typeof(string));
                    questionsDt.Columns.Add("ThumbnailLink", typeof(string));


                    foreach (var item in _question)
                    {
                        questionsDt.Rows.Add(item.Id, item.InterviewId, item.Question, item.Priority, item.VideoTitle, item.VideoLink, item.Thumbnail);
                    }

                    SqlCommand cmdQuestions = new SqlCommand("usp_AddorEditQuestionsDetails", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDetailsBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@QuestionsDetailsBulkData"].Value = questionsDt;
                    con.Open();
                    if (cmdQuestions.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public IEnumerable<MeetingVM> GetClientJdIRFs(int Clientjdid, string emailId)
        {
            try
            {
                int Userid = 0;
                var query = "Select id from UserList where Emailid=@emailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@emailId", emailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    Userid = results.FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Clientjdid", Clientjdid);
                    parameters.Add("@id", Userid);
                    var result = SqlMapper.Query<MeetingVM>(con, "usp_GetRiseTubeMeetingsForJd", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<string>> GetIRFMembersEmails(int irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFMembersEmails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<string>> GetIRFInterviewingMembersEmails(string irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFInterviewingMembersEmails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<ChannelMembersForInterviewData>> GetIRFMembersEmailsForInterviewDetails(string irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ChannelMembersForInterviewData>(con, "usp_GetIRFMembersEmailsForInterviewDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<string>> GetIRFQualityControllerEmails(string irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFQualityControllerEmails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public SurveyCandidateInterviewer FetchCanddiateInterviewer(int interviewid)
        {
            try
            {
                var query = "Select CandidateEmail,Interviewer,ul.id,ir.JDTiltle as PrimarySkill,(ulc.FirstName) as CandidateName from interviewmeetings im join IRF ir on ir.IRFId=im.IRFId left join userlist ul on im.interviewer=ul.emailid left join Userlist ulc on ulc.Emailid=im.CandidateEmail where im.id=@interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SurveyCandidateInterviewer>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<int> GetRisetubeIndexMeetingsCount(string profile, string Emailid, string searchValue = null, int clientId = 0, bool IsClientCurated = false)
        {
            try
            {
                if (profile == "CuratedUser")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValue", searchValue);
                        parameters.Add("@Emailid", Emailid);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetRiseTubeIndexMeetingsCountForCuratedUser", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }

                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValueParam", searchValue);
                        parameters.Add("@EmailIdParam", Emailid);
                        parameters.Add("@clientIdParam", clientId);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetRisetubeIndexMeetingsCount", parameters, commandTimeout: 600,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }

        public async Task<int> GetRisetubeIndexMeetingsFilteredCount(string profile, string Emailid, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0)
        {
            try
            {
                if (profile == "CuratedUser")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValueParam", searchValue);
                        parameters.Add("@EmailidParam", Emailid);
                        parameters.Add("@startDateParam", startDate);
                        parameters.Add("@endDateParam", endDate);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetRiseTubeIndexMeetingsCountForCuratedUserFilteredDup", parameters,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }

                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValueParam", searchValue);
                        parameters.Add("@EmailIdParam", Emailid);
                        parameters.Add("@clientIdParam", clientId);
                        parameters.Add("@startDateParam", startDate);
                        parameters.Add("@endDateParam", endDate);
                        //parameters.Add("@timeNow", DateTime.Now);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetRisetubeIndexMeetingsFilteredCountDup", parameters,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }


        public async Task<int> GetSharedIndexMeetingsCount(string searchValue = null, string profileName = null)
        {
            try
            {
                //usp_GetSharedIndexMeetingsCountforClient
                if (profileName != null && profileName == "Client")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValueParam", searchValue);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetSharedIndexMeetingsCountforClient", parameters,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValueParam", searchValue);
                        var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetSharedIndexMeetingsCount", parameters,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                }


            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }

        public async Task<int> GetSharedIndexMeetingsCountforClientFiltered(string primarySkills, string heuristicLevels, string searchValue = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@searchValueParam", searchValue);
                    parameters.Add("@primarySkillsParam", primarySkills);
                    parameters.Add("@heuristicLevelsParam", heuristicLevels);
                    var result = await SqlMapper.QueryFirstOrDefaultAsync<int>(con, "usp_GetSharedIndexMeetingsCountforClientFiltered", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }



            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }

        public string CandidateSuitableJd(int id, string email, string Comment, int Clientjdid)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewId", id);
                parameters.Add("@EmailId", email);
                parameters.Add("@Comment", Comment);
                parameters.Add("@Clientjdid", Clientjdid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<string>(con, "usp_AddSuitableCandidate", parameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault().ToString();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<SurveyDetails>> FetchSurveyInterviewer(List<SurveyDetails> _lst)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable Surveysetdt = new DataTable();
                    Surveysetdt.Columns.Add("CuratedGroupId", typeof(int));
                    Surveysetdt.Columns.Add("IRFId", typeof(int));
                    Surveysetdt.Columns.Add("IsAdded", typeof(bool));

                    foreach (var item in _lst)
                    {
                        Surveysetdt.Rows.Add(item.Interviewerid, 0, true);
                    }

                    SqlCommand cmdSurveyset = new SqlCommand("usp_SurveyInterviewerDetails", con);
                    cmdSurveyset.CommandType = CommandType.StoredProcedure;
                    cmdSurveyset.Parameters.Add(new SqlParameter("@SurveyBulkDetails", SqlDbType.Structured));
                    cmdSurveyset.Parameters["@SurveyBulkDetails"].Value = Surveysetdt;
                    con.Open();
                    var result = new List<SurveyDetails>();
                    var dr = await cmdSurveyset.ExecuteReaderAsync();
                    while (dr.Read())
                    {
                        result.Add(new SurveyDetails { Interviewerid = Convert.ToInt32(dr["Interviewid"].ToString()), Intervieweremail = dr["EmailAddress"].ToString() });
                    }
                    con.Close();
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<string>> GetIRFClientsEmails(int irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFClientsEmails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }
        public async Task<List<RBInterviewDetails>> RBDetails(string EmailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "Usp_FetchInterviewPayDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return (List<RBInterviewDetails>)result.OrderByDescending(x => x.Date).ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> RBUpdateStatusDetails(string EmailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", EmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "Usp_UpdateRBPayableStatus", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return "";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FetchFNDetails(string EmailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "FetchInitialFNDetails", null,
                      commandType: CommandType.StoredProcedure);
                    return (List<RBInterviewDetails>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> RBInterviewerDetails(int interviewerid, int flag)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Userid", interviewerid);
                dynamicParameters.Add("@flag", flag);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "Usp_FetchRBInterviewerDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return (List<RBInterviewDetails>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> RBAddPoints(int interviewerid, int Points, string Comment, DateTime PointDate)
        {
            try
            {
                int interviewid = 0;
                var status = "Submitted";
                var Type = "Admin";
                var date = PointDate;
                var interviewerids = 0;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string sqlInsertReport = "INSERT INTO InterviewPoints (Interviewid, userid, Points,Type,Comment, Status, Date,Interviewerid ) Values (@interviewid, @interviewerid, @Points, @Type, @Comment, @status, @date,@interviewerids);";
                    var affectedRows = await con.ExecuteAsync(sqlInsertReport, new
                    {
                        interviewid,
                        interviewerid,
                        Points,
                        Type,
                        Comment,
                        status,
                        date,
                        interviewerids

                    });
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<string> RBConfirmPayment(int interviewerid, int index, DateTime st, DateTime se, int paymenthistory)
        {
            try
            {
                var query = "";
                if (index == 2)
                {
                    query = "Update ip Set ip.Status='Paid',PaymentHistoryId=@PaymentHistoryId from InterviewPoints ip left join InterviewMeetings im on im.id=ip.Interviewid where ip.userid= @Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewid!=0 and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@st", st);
                    dynamicParameters.Add("@se", se);
                    dynamicParameters.Add("@Userid", interviewerid);
                    dynamicParameters.Add("@PaymentHistoryId", paymenthistory);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await con.QueryAsync(query, dynamicParameters);
                    }
                    query = "Update ip Set ip.Status='Paid',PaymentHistoryId=@PaymentHistoryId from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id=ip.Interviewerid where ip.userid= @Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewerid!=0 and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await con.QueryAsync(query, dynamicParameters);
                    }
                    query = "Update ip Set ip.Status='Paid',PaymentHistoryId=@PaymentHistoryId from InterviewPoints ip where ip.userid= @Userid and (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewerid=0 and ip.interviewid=0 and (ip.Date>=@st and ip.Date<=@se)";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await con.QueryAsync(query, dynamicParameters);
                    }
                    query = "Update InterviewPoints set Status='Paid' where userid=@Userid and status='Advance'";
                    var dynamicParameters1 = new DynamicParameters();
                    dynamicParameters1.Add("@Userid", interviewerid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await con.QueryAsync(query, dynamicParameters1);
                    }
                    return "Success";
                }
                else
                {
                    query = "Update InterviewPoints set Status='Paid',PaymentHistoryId=@PaymentHistoryId where userid=@Userid and (status='Submitted' or status='Advance')";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@Userid", interviewerid);
                    dynamicParameters.Add("@PaymentHistoryId", paymenthistory);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await con.QueryAsync(query, dynamicParameters);
                        return "Success";
                    }
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<List<string>> FetchInterviewerList(string searchterm)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@term", searchterm);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "Usp_FetchInterviewerList", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return (List<string>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> InsertAdvancePayment(string Interviewer, string Comment, int Amount, int PaymentHistoryId)
        {
            try
            {
                int userid = 0;
                int Interviewerpresent = 0;
                var query = "Select id from UserList where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", Interviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    userid = results.FirstOrDefault();
                }
                var querycheck = "Select idProfile from UserList where id=@Userid";
                var dynamicParameterscheck = new DynamicParameters();
                dynamicParameterscheck.Add("@Userid", userid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(querycheck, dynamicParameterscheck);
                    Interviewerpresent = results.FirstOrDefault();
                }
                if (Interviewerpresent != 3)
                {
                    return "Failed";
                }
                int interviewid = 0;
                int interviewerid = 0;
                string Type = "Finance";
                string status = "Advance";
                var date = DateTime.Now;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string sqlInsertReport = "INSERT INTO InterviewPoints (Interviewid, userid, Points,Type,Comment, Status, Date ,Interviewerid,PaymentHistoryId) Values (@interviewid, @userid, @Amount, @Type, @Comment, @status, @date,@interviewerid,@PaymentHistoryId);";
                    var affectedRows = await con.ExecuteAsync(sqlInsertReport, new
                    {
                        interviewid,
                        userid,
                        Amount,
                        Type,
                        Comment,
                        status,
                        date,
                        interviewerid,
                        PaymentHistoryId
                    });
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<List<RBInterviewDetails>> FNGenerateInvoiceDetails(DateTime st, DateTime se)
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchFNGenerateInvoiceDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewDetails>)results;
                    return _list.OrderBy(x => x.Comment).ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> LockorUnlockVideo(string imid, int islock, string emailid)
        {
            try
            {
                int UserId = 0;
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                var query = "Select id from UserList where Emailid=@Emailid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    UserId = results.FirstOrDefault();
                }
                var dParameters = new DynamicParameters();
                dParameters.Add("@islock", islock);
                if (islock == 0)
                {
                    dParameters.Add("@UserId", 0);
                }
                else
                {
                    dParameters.Add("@UserId", UserId);
                }
                dParameters.Add("@imid", imid);
                var dquery = "Update InterviewComments Set IsLock = @islock,ClientUserid=@UserId Where Interviewid=@imid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync(dquery, dParameters);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "fail";
        }
        public async Task<List<string>> FetchClientUsers(string ClientName)
        {
            try
            {
                var dParameters = new DynamicParameters();
                dParameters.Add("@ClientName", ClientName);
                var dquery = "Select ul.EmailId From Client cl join ClientUser cu on cu.ClientId=cl.id join UserList ul on ul.id=cu.UserId where Name=@ClientName";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(dquery, dParameters);
                    return (List<string>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> SetCandidateAvailability(string imid, int isavailable)
        {
            try
            {
                var dParameters = new DynamicParameters();
                dParameters.Add("@isavailable", isavailable);
                dParameters.Add("@imid", imid);
                var dquery = "Update InterviewComments Set CandidateAvailable = @isavailable Where Interviewid=@imid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync(dquery, dParameters);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "fail";
        }
        public async Task<string> MoveToCP(string imid, int movetocp)
        {
            try
            {
                var dParameters = new DynamicParameters();
                dParameters.Add("@movetocp", movetocp);
                dParameters.Add("@imid", imid);
                var dquery = "Update InterviewComments Set MoveToCP = @movetocp Where Interviewid=@imid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync(dquery, dParameters);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "fail";
        }
        public async Task<IEnumerable<MeetingVM>> GetAllSelectedCandidateInterviewList(DateTime startDate, DateTime endDate, string email)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    parameters.Add("@email", email);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetIAllSelectedCandidateInterviewMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<RBInterviewDetails>> FetchAllFNDetail()
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchAllFNDetail", null,
                        commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewDetails>)results;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchAllFNDetailAdvance", null,
                        commandType: CommandType.StoredProcedure);
                    var resultset = (from c in _list
                                     join p in results on c.Userid equals p.Userid into ps
                                     from p in ps.DefaultIfEmpty()
                                     select new RBInterviewDetails
                                     {
                                         id = c.id,
                                         Points = p == null ? c.Points : c.Points - p.Points,
                                         PrePoints = c.PrePoints,
                                         Penaltyamount = c.Penaltyamount,
                                         Comment = c.Comment,
                                         Type = c.Type,
                                         InterviewId = c.InterviewId,
                                         Date = c.Date,
                                         Status = c.Status,
                                         PreferredLocation = c.PreferredLocation,
                                         MeetingStatus = c.MeetingStatus,
                                         candidateEmail = c.candidateEmail,
                                         candidateName = c.candidateName,
                                         Userid = c.Userid,
                                         FirstName = c.FirstName,
                                         ClientName = c.ClientName,
                                         VideoLink = c.VideoLink,
                                         InterviewerEmailid = c.InterviewerEmailid,
                                         InterviewStatus = c.InterviewStatus,
                                         VideoDuration = c.VideoDuration,
                                         ChannelOwner = c.ChannelOwner,
                                         Rate = c.Rate,
                                         PrimarySkillInvite = c.PrimarySkillInvite,
                                         IRFId = c.IRFId,
                                         IsEmergency = c.IsEmergency,
                                         MaxVideoDuration = c.MaxVideoDuration,
                                         Surcharge = c.Surcharge,
                                         CandidateRatingAmount = c.CandidateRatingAmount,
                                         ClientRatingAmount = c.ClientRatingAmount,
                                         QCPoints = c.QCPoints,
                                         PaymentSLA = c.PaymentSLA,
                                         VideoReward = c.VideoReward
                                     }).ToList();
                    return resultset.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNAllGeneratedInvoiceDetails(DateTime st, DateTime se)
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchFNAllGeneratedInvoiceDetails", dynamicParameters, commandTimeout: 600,
                    commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewDetails>)results;
                    return _list.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> InsertPaymentHistory(string Interviewer, int Amount, int TotalAmount, string actionvalue, string Comment)
        {
            try
            {
                int userid = 0;
                int Interviewerpresent = 0;
                var query = "Select id from UserList where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", Interviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    userid = results.FirstOrDefault();
                }
                var querycheck = "Select idProfile from UserList where id=@Userid";
                var dynamicParameterscheck = new DynamicParameters();
                dynamicParameterscheck.Add("@Userid", userid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(querycheck, dynamicParameterscheck);
                    Interviewerpresent = results.FirstOrDefault();
                }
                if (Interviewerpresent != 3)
                {
                    return "Failed";
                }
                string status = actionvalue;
                var PaymentDate = DateTime.Now;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@userid", userid);
                    parameters.Add("@PaymentDate", PaymentDate);
                    parameters.Add("@Amount", Amount);
                    parameters.Add("@status", status);
                    parameters.Add("@Comment", Comment);
                    parameters.Add("@TotalAmount", TotalAmount);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_InsertPaymentHistory", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                    //string sqlInsertReport = "Insert Into PaymentHistory (userid, PaymentDate,Amount,Status,Comment,TotalAmount) Values (@userid, @PaymentDate, @Amount, @status,@Comment,@TotalAmount);";
                    //var affectedRows = await con.ExecuteAsync(sqlInsertReport, new
                    //{
                    //    userid,
                    //    PaymentDate,
                    //    Amount,
                    //    status,
                    //    Comment,
                    //    TotalAmount
                    //});
                    //return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public async Task<List<RBInterviewDetails>> FetchFNPaymentHistory()
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                var query = "Select ph.id,ul.EmailId as InterviewerEmailid, ul.FirstName,ph.PaymentDate as Date,ph.Status,ph.Amount as Points,ip.AssessmentGroupId,ph.TotalAmount from PaymentHistory ph join Userlist ul on ul.id=ph.Userid join InterviewerProfile ip on ip.Userid=ul.id ";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<RBInterviewDetails>(query, null);
                    _list = (List<RBInterviewDetails>)results;
                }
                return _list;
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<int>> FetchAllInterviewidfromIrf(string IRFid)
        {
            try
            {
                var query = "Select im.id from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where Irfid=@irfid and ic.MeetingStatus=4";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", IRFid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    return results.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<int> FetchAllInterviewidByClient(int Clientid, string emailid)
        {
            try
            {
                int Userid = 0;
                var Usquery = "Select id from UserList where Emailid=@Emailid";
                var usdynamicParameters = new DynamicParameters();
                usdynamicParameters.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var resultset = con.Query<int>(Usquery, usdynamicParameters);
                    Userid = resultset.FirstOrDefault();
                }
                var query = "Select distinct im.id from IRF join InterviewMeetings im on im.IRFId=IRF.IRFId join TransClientUser tcu on tcu.IRFId=IRF.id Where IRF.ClientId=@Clientid and tcu.ClientUserId=@UserId and im.confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", Userid);
                dynamicParameters.Add("@Clientid", Clientid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<SurveyDetails> FetchSurveyClientName(List<SurveyDetails> _lst)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable Surveysetdt = new DataTable();
                    Surveysetdt.Columns.Add("CuratedGroupId", typeof(int));
                    Surveysetdt.Columns.Add("IRFId", typeof(int));
                    Surveysetdt.Columns.Add("IsAdded", typeof(bool));

                    foreach (var item in _lst)
                    {
                        Surveysetdt.Rows.Add(item.interviewid, 0, true);
                    }

                    SqlCommand cmdSurveyset = new SqlCommand("usp_SurveyClientDetails", con);
                    cmdSurveyset.CommandType = CommandType.StoredProcedure;
                    cmdSurveyset.Parameters.Add(new SqlParameter("@SurveyBulkDetails", SqlDbType.Structured));
                    cmdSurveyset.Parameters["@SurveyBulkDetails"].Value = Surveysetdt;
                    con.Open();
                    var result = new List<SurveyDetails>();
                    var dr = cmdSurveyset.ExecuteReader();
                    while (dr.Read())
                    {
                        result.Add(new SurveyDetails { interviewid = Convert.ToInt32(dr["interviewid"].ToString()), ClientName = dr["ClientName"].ToString(), interviewdate = dr["interviewdate"].ToString() });
                    }
                    con.Close();
                    return result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<SurveyDetails> FetchSurveyCandidateEmails(List<SurveyDetails> _lst)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable Surveysetdt = new DataTable();
                    Surveysetdt.Columns.Add("CuratedGroupId", typeof(int));
                    Surveysetdt.Columns.Add("IRFId", typeof(int));
                    Surveysetdt.Columns.Add("IsAdded", typeof(bool));

                    foreach (var item in _lst)
                    {
                        Surveysetdt.Rows.Add(item.interviewid, 0, true);
                    }

                    SqlCommand cmdSurveyset = new SqlCommand("usp_SurveyCandidateEmail", con);
                    cmdSurveyset.CommandType = CommandType.StoredProcedure;
                    cmdSurveyset.Parameters.Add(new SqlParameter("@SurveyBulkDetails", SqlDbType.Structured));
                    cmdSurveyset.Parameters["@SurveyBulkDetails"].Value = Surveysetdt;
                    con.Open();
                    var result = new List<SurveyDetails>();
                    var dr = cmdSurveyset.ExecuteReader();
                    while (dr.Read())
                    {
                        result.Add(new SurveyDetails { interviewid = Convert.ToInt32(dr["interviewid"].ToString()), Intervieweremail = dr["EmailAddress"].ToString() });
                    }
                    con.Close();
                    return result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<PendingVideoMeetingsData>> GetPendingVideoMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeEnd", DateTime.Now.AddHours(-3));
                    parameters.Add("@timeStart", DateTime.Now.AddDays(-7));
                    var result = await SqlMapper.QueryAsync<PendingVideoMeetingsData>(con, "usp_GetPendingVideoMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<PendingVideoMeetingsData>> GetZoomMeetingsToBeDeletedOnceUploadedOnBlob()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeEnd", DateTime.Now.AddHours(-3));
                    parameters.Add("@timeStart", DateTime.Now.AddDays(-7));
                    var result = await SqlMapper.QueryAsync<PendingVideoMeetingsData>(con, "usp_GetZoomMeetingsToBeDeletedOnceUploadedOnBlob", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateUploadedVideoLink(int interviewId, string videoLink, int videoDuration)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@videoLink", videoLink);
                    parameters.Add("@videoDuration", videoDuration);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateUploadedVideoLink", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> UpdateUploadedVideoLinkAndTranscriptLink(int interviewId, int? videoDuration = null, string videoLink = null, string transcriptLink = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@videoLink", videoLink);
                    parameters.Add("@videoDuration", videoDuration);
                    parameters.Add("@transcriptLink", transcriptLink);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateUploadedVideoLinkAndTranscriptLink", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<Meeting>> FetchOpenMeetingById(string id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    //parameters.Add("@meetingid", Convert.ToInt64(id));
                    parameters.Add("@meetingid", id);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_FetchOpenMeetingById", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string CheckMeetingAssignationValidation(string Emailid)
        {
            try
            {
                var query = "Select Count(*) from UserList where Emailid=@Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", Emailid.Trim());
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    if (results.FirstOrDefault() > 0)
                    {
                        return "Valid";
                    }
                    else
                    {
                        return "Email id is not valid.";
                    }
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }
        public string ReAssignMeeting(int id, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value,EIAssigned=1,IsEmergency=1 where id = @meetingid", new { value = interviewerMail, meetingid = id }, commandType: Text);
                    return "Interview assigned Successfully.";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }
        public async Task<InterviewerDetails> FetchInterviewerCodedName(string emailId)
        {
            try
            {
                var query = "Select ip.NickName,ip.CodedResumeLink,ISNULL(ip.IsNotificationAccepted,0) as IsNotificationAccepted from InterviewerProfile ip join UserList ul on ip.UserId=ul.id where ul.EmailId=@Emailid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@Emailid", emailId);
                    var results = await con.QueryAsync<InterviewerDetails>(query, dp);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public async Task<InterviewerRating> FetchRatingDetails(int interviewId, string emailid)
        {
            try
            {
                int userid = 0;
                var query = "Select id from Userlist where EmailId=@Emailid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@Emailid", emailid);
                    var result = await con.QueryAsync<int>(query, dp);
                    userid = result.FirstOrDefault();
                }
                //var queryfinal = "Select * From InterviewRatings where Userid=@Userid and Interviewid=@Interviewid";
                var queryfinal = "Select distinct ir.*,ti.IsActive,ISNULL(ti.ClientAction,tie.ClientAction) as ClientAction From InterviewRatings ir join InterviewMeetings im on im.id=ir.Interviewid join IRF on IRF.IRFId=im.IRFId join UserList ul on ul.EmailId=im.interviewer left join TransInterviewer ti on ti.UserId=ul.id and ti.IRFId=irf.id left join TransEmergencyInterviewer tie on tie.UserId=ul.id and ti.IRFId=irf.id where ir.Userid=@Userid and ir.Interviewid=@Interviewid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dpfinal = new DynamicParameters();
                    dpfinal.Add("@Userid", userid);
                    dpfinal.Add("@Interviewid", interviewId);
                    var result = await con.QueryAsync<InterviewerRating>(queryfinal, dpfinal);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> SaveRatingDetails(InterviewerRating model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", model.Emailid);
                    parameters.Add("@Interviewid", model.Interviewid);
                    parameters.Add("@StarRating", model.StarRating);
                    parameters.Add("@Comment", model.Comment);
                    parameters.Add("@ClientAction", model.ClientAction);
                    await SqlMapper.QueryAsync(con, "usp_SaveInterviewRating", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Save Successfully";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "Fail to save rating.";
        }
        public async Task<IEnumerable<MeetingVM>> GetClientNotScheduledMeetingList(int Clientid, string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("Clientid", Clientid);
                    parameters.Add("Emailid", Emailid);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientNotScheduledMeetings", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<InvoiceDetailsVM>> FetchFNInvoiceDetails(string Startdate, string Enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Startdate", Startdate);
                    parameters.Add("@Enddate", Enddate);
                    var result = await SqlMapper.QueryAsync<InvoiceDetailsVM>(con, "usp_FetchinvoiceDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return (List<InvoiceDetailsVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<InvoiceDetailsVM> GetInvoiceDetailsById(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", id);
                    var result = await SqlMapper.QueryAsync<InvoiceDetailsVM>(con, "usp_FetchinvoiceDetailsById", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> SaveInvoiceDetails(InvoiceDetailsVM model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", model.id);
                    parameters.Add("@ClientName", model.Name);
                    parameters.Add("@JobCountry", model.JobCountry);
                    parameters.Add("@StartDate", model.startdate);
                    parameters.Add("@Enddate", model.enddate);
                    parameters.Add("@AccountNumber", model.AccountNumber);
                    parameters.Add("@AccountDetails", model.AccountDetails);
                    parameters.Add("@Comment", model.Comment);
                    parameters.Add("@PaymentStatus", model.PaymentStatus);
                    parameters.Add("@ReferenceNumber", model.ReferenceNumber);
                    parameters.Add("@UploadedFiles", model.filelink);
                    parameters.Add("@Price", model.Price);
                    var result = await SqlMapper.QueryAsync<InvoiceDetailsVM>(con, "usp_SaveinvoiceDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Fail";
            }
        }
        public async Task<IEnumerable<MeetingVM>> GetClientNotScheduledMeetingListByIRFId(string IRFId, string emailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", IRFId);
                    parameters.Add("@emailId", emailId);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientNotScheduledMeetingsByIRFId", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> GetClientReScheduledMeetingList(int Clientid, string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("Clientid", Clientid);
                    parameters.Add("Emailid", Emailid);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientReScheduledMeetings", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<CallCatalogueVM>> GetCallCatalogueList(string profile, string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserRole", profile);
                    parameters.Add("@Emailid", Emailid);
                    return await SqlMapper.QueryAsync<CallCatalogueVM>(con, "usp_FetchCallCatalogue", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<string>> FetchClientListByIRFID(int IRFID)
        {
            try
            {
                var queryfinal = "Select cu.Emailid from TransClientUser tc join UserList cu on cu.id=tc.ClientUserid where ClientRoleId in(1,2) and IRFID=@IRFID";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dpfinal = new DynamicParameters();
                    dpfinal.Add("@IRFID", IRFID);
                    var result = await con.QueryAsync<string>(queryfinal, dpfinal);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<PendingVideoMeetingsData>> GetPendingVideoInstantMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeEnd", DateTime.Now);
                    parameters.Add("@timeStart", DateTime.Now.AddDays(-7));
                    var result = await SqlMapper.QueryAsync<PendingVideoMeetingsData>(con, "usp_GetPendingVideoInstantMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateUploadedVideoLinkForInstaInterview(int interviewId, string videoLink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@videoLink", videoLink);
                    //parameters.Add("@videoDuration", videoDuration);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateUploadedVideoLinkForInstaInterview", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public IEnumerable<int> FetchAllInterviewidBySeniorHR(string emailid)
        {
            try
            {
                var query = "Select distinct im.id from IRF join InterviewMeetings im on im.IRFId=IRF.IRFId join SeniorHR sh on IRF.ClientId=sh.Clientid join Userlist ul on ul.id=Sh.Userid Where ul.Emailid=@Emailid and im.confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<RBInterviewDetails>> GetPaymentHistoryDetailsView(int interviewerid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@PaymentHistoryId", interviewerid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "Usp_FetchPaymentHistoryDetailsbyId", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return (List<RBInterviewDetails>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalAmountPaid(DateTime st, DateTime se)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchFNToalAmountPaid", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return (List<RBInterviewDetails>)results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalAmountStatus(DateTime st, DateTime se)
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchFNToalAmountStatus", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewDetails>)results;
                    return _list.OrderBy(x => x.Comment).ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewDetails>> FNFetchToalQCAmountStatus(DateTime st, DateTime se)
        {
            try
            {
                List<RBInterviewDetails> _list = new List<RBInterviewDetails>();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewDetails>(con, "usp_FetchFNToalQCAmountStatus", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewDetails>)results;
                    return _list.OrderBy(x => x.Comment).ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<Meeting> GetMeetingDetails(int meetingId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingId", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetMeetingDetails", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<SchedulingLinkVM>> FetchSchedulingLinks(string emailid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                //var query = "Select IRF.IRFId,JDTiltle,CalendlyLink from IRF join TransClientUser tcu join Userlist ul on ul.id=tcu.ClientUserid on tcu.IRFId=IRF.id Where ul.Emailid=@Emailid and IsDeleted=0 and EndDate>DateAdd(mi,330,GETDATE())";
                var query = "Select IRF.IRFId,JDTiltle,CalendlyLink,uli.EmailId as BillingOwner,JDLink,IRF.id,IRF.IsPaused from IRF join TransClientUser tcu join Userlist ul on ul.id=tcu.ClientUserid on tcu.IRFId=IRF.id left join UserList uli on uli.id=BillingOwner Where ul.Emailid=@Emailid and IsDeleted=0 and EndDate>DateAdd(mi,330,GETDATE())";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SchedulingLinkVM>(query, dynamicParameters);
                    return (List<SchedulingLinkVM>)results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        /// <summary>
        /// Gets all the interviews which are yet unassigned with 3 hours to go
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<MeetingVM>> GetUnassignedOpenInterviews()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetUnassignedOpenInterviews", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<string>> GetIRFActiveInterviewers(string irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFActiveInterviewers", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }
        public async Task<int> GetUniqueInterviewerByEmailId(string emailId, string id)
        {
            try
            {
                var query = "Select id From InterviewerPublicURLs where EmailId = @emailId and uniqueid=@uniqueid ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@emailId", emailId);
                dynamicParameters.Add("@uniqueid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryFirstOrDefaultAsync<int>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }
        public async Task<List<UnassignedInterviewVM>> FetchAllUnassignedInterviewes()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<UnassignedInterviewVM>(con, "usp_FetchAllUnassignedInterviewAutoCancel", null,
                       commandType: CommandType.StoredProcedure);
                    return (List<UnassignedInterviewVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool UpdateCancelInterviewDetails(int meetingid)
        {
            try
            {
                var query = "Update InterviewMeetings set isAutoCancelled=1,Cancellationsource='System Auto' where id=@meetingid and confirmationStatus=2";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<UnassignedInterviewVM>> FetchCandidateMeetingsForReminder()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<UnassignedInterviewVM>(con, "usp_FetchCandidateMeetingsForReminders", null,
                       commandType: CommandType.StoredProcedure);
                    return (List<UnassignedInterviewVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string UpdateCandidateMeetingsStatus(int meetingid, int status, string uniqueid, bool response)
        {
            try
            {
                string query = string.Empty;
                string checkquery = string.Empty;
                //if (status == 1 || (status == 4 && response == true))
                if (status == 1 || status == 4)
                {
                    query = "Update im set IsCandidateResponsed=1,CandidateResponse=@status from InterviewMeetings im join UserList ul on ul.EmailId=im.candidateEmail where im.id=@meetingid and ul.uniqueid=@uniqueid";
                }
                else
                {
                    if (status == 2)
                    {
                        status = 0;
                    }
                    query = "Update im set IsCandidateResponsed=1,confirmationStatus=2,CandidateResponse=@status,Cancellationsource='Candidate' from InterviewMeetings im join UserList ul on ul.EmailId=im.candidateEmail where im.id=@meetingid and ul.uniqueid=@uniqueid";
                }
                checkquery = "Select count(im.id) from InterviewMeetings im join UserList ul on ul.EmailId=im.candidateEmail where im.id=@meetingid and ul.uniqueid=@uniqueid and CandidateResponse=0 and confirmationStatus=2";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@uniqueid", uniqueid);
                dynamicParameters.Add("@status", status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(checkquery, dynamicParameters);
                    if (results.FirstOrDefault() > 0)
                    {
                        return "Already Present";
                    }
                    else
                    {
                        var resultset = con.Query(query, dynamicParameters);
                        return "Success";
                    }
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Fail";
            }
        }
        public async Task<List<CustomerDetailsVM>> FetchCustomerDetails()
        {
            try
            {
                var query = "SELECT IRFid as IRFName,Name,JobCountry,CompanyType FROM IRF INNER JOIN Client ON Client.id = IRF.ClientId";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<CustomerDetailsVM>(query, null);
                    return (List<CustomerDetailsVM>)results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> ApplyInterviewPenalty(int meetingid, string JobCountry, string interviewer)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                if (JobCountry.ToUpper() == "INDIA" || JobCountry.ToUpper() == "1")
                {
                    parameters.Add("@points", -200);
                }
                else
                {
                    parameters.Add("@points", -350);
                }
                parameters.Add("@interviewer", interviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(
                        con, "usp_applyInterviewerpenalty", parameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<RBInterviewerPayoutDetails>> FNAllGeneratedPayoutDetails(DateTime st, DateTime se)
        {
            try
            {
                List<RBInterviewerPayoutDetails> _list = new List<RBInterviewerPayoutDetails>();
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@st", st);
                dynamicParameters.Add("@se", se);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewerPayoutDetails>(con, "usp_FetchInterviewerPayoutDetailsBydate", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewerPayoutDetails>)results;
                }
                if (_list.Count > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await SqlMapper.QueryAsync<RBInterviewerPayoutDetails>(con, "usp_FetchInterviewerPayoutDetailsAdvance", null,
                           commandType: CommandType.StoredProcedure);
                        var resultset = (from c in _list
                                         join p in results on c.Userid equals p.Userid into ps
                                         from p in ps.DefaultIfEmpty()
                                         select new RBInterviewerPayoutDetails
                                         {
                                             id = c.id,
                                             Points = p == null ? c.Points : c.Points - p.Points,
                                             Userid = c.Userid,
                                             FirstName = c.FirstName,
                                             InterviewerEmailid = c.InterviewerEmailid,
                                             AccountName = c.AccountName,
                                             AccountNumber = c.AccountNumber,
                                             PAN = c.PAN,
                                             IFSCCode = c.IFSCCode
                                         }).ToList();
                        return resultset.ToList();
                    }
                }
                else
                {
                    return _list.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<RBInterviewerPayoutDetails>> FetchAllPayoutDetails()
        {
            try
            {
                List<RBInterviewerPayoutDetails> _list = new List<RBInterviewerPayoutDetails>();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<RBInterviewerPayoutDetails>(con, "usp_FetchInterviewerPayoutDetails", null,
                       commandType: CommandType.StoredProcedure);
                    _list = (List<RBInterviewerPayoutDetails>)results;
                }
                if (_list.Count > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = await SqlMapper.QueryAsync<RBInterviewerPayoutDetails>(con, "usp_FetchInterviewerPayoutDetailsAdvance", null,
                           commandType: CommandType.StoredProcedure);
                        var resultset = (from c in _list
                                         join p in results on c.Userid equals p.Userid into ps
                                         from p in ps.DefaultIfEmpty()
                                         select new RBInterviewerPayoutDetails
                                         {
                                             id = c.id,
                                             Points = p == null ? c.Points : c.Points - p.Points,
                                             Userid = c.Userid,
                                             FirstName = c.FirstName,
                                             InterviewerEmailid = c.InterviewerEmailid,
                                             AccountName = c.AccountName,
                                             AccountNumber = c.AccountNumber,
                                             PAN = c.PAN,
                                             IFSCCode = c.IFSCCode
                                         }).ToList();
                        return resultset.ToList();
                    }
                }
                else
                {
                    return _list.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> SaveProctorReport(int id, string reporturl)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@interviewid", id);
                    parameters.Add("@ProctorReport", reporturl);
                    DynamicParameters parametersPoints = new DynamicParameters();
                    var result = await SqlMapper.QueryAsync(con, "usp_SaveProctorReport", parameters,
                                            commandType: CommandType.StoredProcedure);
                }
                return true;
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UpdateCheckIntervieweravailbilityStatus(int status, string uniqueid, int Irfid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", Irfid);
                dynamicParameters.Add("@uniqueid", uniqueid);
                dynamicParameters.Add("@Status", status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateIntervieweravailbilityStatus", dynamicParameters,
                                            commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateInterviewerAvailbilityStatusForIRF(int status, string uniqueId, int irfId, string rsc = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", irfId);
                dynamicParameters.Add("@uniqueId", uniqueId);
                dynamicParameters.Add("@Status", status);
                dynamicParameters.Add("@rsc", rsc);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateInterviewerAvailbilityStatusForIRF", dynamicParameters,
                                            commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public string SetInterviewerAvailability(int transid, int isavailable)
        {
            try
            {
                var dquery = string.Empty;
                var dquery1 = string.Empty;
                var dParameters = new DynamicParameters();
                dParameters.Add("@isavailable", isavailable);
                dParameters.Add("@imid", transid);
                var dParameters1 = new DynamicParameters();
                dParameters1.Add("@imid", transid);
                if (isavailable == 1)
                {
                    dquery = "Update TransInterviewer Set IsAvailable = @isavailable Where id=@imid";
                }
                else
                {
                    dquery = "Update TransInterviewer Set IsActive=0,IsAvailable = @isavailable Where id=@imid";
                    //dquery1 = "delete ti from TransInterviewer ti join UserList ul on ul.id=ti.Userid left join IRF on IRF.id=ti.IRFID Left join InterviewMeetings im on im.IRFid=IRF.IRFid and im.Interviewer=ul.Emailid Where im.id is null and ti.id=@imid";
                }
                //if (isavailable != 1)
                //{
                //    using (SqlConnection con = new SqlConnection(connectionString))
                //    {
                //        var results = con.Query(dquery1, dParameters1);
                //    }
                //}
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(dquery, dParameters);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "fail";
        }
        public async Task<List<InterviewerRating>> FetchFeedbackDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //var query = "Select im.interviewdate,im.interviewer,ul.Emailid,ir.Comment,ir.StarRating,im.candidateEmail,ir.Interviewid From [dbo].[InterviewRatings] ir join UserList ul on ul.id=ir.Userid join InterviewMeetings im on im.id=ir.Interviewid order by Addedon desc";
                    var query = "Select im.interviewdate,im.interviewer,ul.Emailid,ir.Comment,ir.StarRating,im.candidateEmail,ir.Interviewid,ir.Addedon From [dbo].[InterviewRatings] ir join UserList ul on ul.id=ir.Userid join InterviewMeetings im on im.id=ir.Interviewid union Select im.interviewdate,im.interviewer,ir.CreatedBy as Emailid,ir.Comment,ir.Rating as StarRating,im.candidateEmail,ir.Interviewid,ir.CreatedAt as Addedon  From ClientCommentsForReport ir join UserList ul on ul.EmailId=ir.CreatedBy join InterviewMeetings im on im.id=ir.Interviewid order by Addedon desc";
                    var results = await con.QueryAsync<InterviewerRating>(query, null);
                    return (List<InterviewerRating>)results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<InterviewerRating>> FetchFeedbackDetailsByIRFId(string IRFId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select im.interviewdate,im.interviewer,ul.Emailid,ir.Comment,ir.StarRating,im.candidateEmail,ir.Interviewid From [dbo].[InterviewRatings] ir join UserList ul on ul.id=ir.Userid join InterviewMeetings im on im.id=ir.Interviewid where im.IRFId=@irfid order by Addedon desc";
                    var results = await con.QueryAsync<InterviewerRating>(query, dynamicParameters);
                    return (List<InterviewerRating>)results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool ChangeInterviewerNotificationStatus(string emailID, int status)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(
                        con, "Update ip Set ip.IsNotificationAccepted=@Status from InterviewerProfile ip join UserList ul on ul.id=ip.UserId where ul.EmailId=@Emailid", new { Status = status, Emailid = emailID }, commandType: Text);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> ApplyLateReportPenalty()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(
                        con, "usp_applyLateReportpenalty", null, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public bool UpdateCancelRecruiterInterviewDetails(int meetingid)
        {
            try
            {
                var query = "Update InterviewMeetings set isAutoCancelled=1,Cancellationsource='Recruiter Disabled' where id=@meetingid and confirmationStatus=2";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<MeetingVM>> GetInterviewsScheduledWithinNextTwoHours(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetInterviewsScheduledWithinNextTwoHours", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> GetCompletedInterviewList(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetCompletedInterviewMeetings", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> DumpRbMeetVideoLink(int interviewId, string videoLink, int? videoDuration)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@videoLink", videoLink);
                    parameters.Add("@videoDuration", videoDuration);
                    var result = await SqlMapper.QueryAsync(con, "usp_DumpRbMeetVideoLink", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<QCFormVM> FetchQCFormDetailsById(int interviewid)
        {
            try
            {
               // var query = "Select tq.*,IRF.id as IRFId, im.interviewDate, im.candidateEmail,im.candidateName,im.interviewer From Trans_QCFormReport tq left join InterviewMeetings im on tq.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where tq.Interviewid=@Interviewid";
                var query = "Select tq.*,IRF.id as IRFId, im.interviewDate, im.candidateEmail,im.candidateName,im.interviewer From InterviewMeetings im left join Trans_QCFormReport tq  on tq.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    var results = await con.QueryFirstOrDefaultAsync<QCFormVM>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddorUpdateQCFormReport(QCFormVM model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Interviewid", model.Interviewid);
                    parameters.Add("@Emailid", model.Emailid);
                    parameters.Add("@Redflags", model.Redflags);
                    parameters.Add("@IsCodingPresent", model.IsCodingPresent);
                    parameters.Add("@IsVideoClearQuality", model.IsVideoClearQuality);
                    parameters.Add("@IsInterviewerCommunicationClear", model.IsInterviewerCommunicationClear);
                    parameters.Add("@IsSufficientVideoDuration", model.IsSufficientVideoDuration);
                    parameters.Add("@IsInterviewStructured", model.IsInterviewStructured);

                    parameters.Add("@IsScoringrubriccorrect", model.IsScoringrubriccorrect);
                    parameters.Add("@IsQuestionswithtime", model.IsQuestionswithtime);
                    parameters.Add("@IsCommentfilled", model.IsCommentfilled);
                    parameters.Add("@IsMeetingStatusfilled", model.IsMeetingStatusfilled);
                    parameters.Add("@IsRelevancequestions", model.IsRelevancequestions);

                    parameters.Add("@IsDelayresuminginterview", model.IsDelayresuminginterview);
                    parameters.Add("@IsBackgroundnoise", model.IsBackgroundnoise);
                    parameters.Add("@IsPoorinterviewerinfrastructure", model.IsPoorinterviewerinfrastructure);

                    parameters.Add("@IsPoorhandlingcandidatequery", model.IsPoorhandlingcandidatequery);
                    parameters.Add("@IsPersonalquestions", model.IsPersonalquestions);
                    parameters.Add("@IsAugmenting", model.IsAugmenting);
                    parameters.Add("@IsBiasedView", model.IsBiasedView);
                    parameters.Add("@IsLettingcandidatespeak", model.IsLettingcandidatespeak);

                    parameters.Add("@IsSuspiciousappfound", model.IsSuspiciousappfound);
                    parameters.Add("@IsLipsyncissue", model.IsLipsyncissue);
                    parameters.Add("@IsReportBlocked", model.IsReportBlocked);
                    parameters.Add("@VideoEnable", model.VideoEnable);
                    parameters.Add("@Payment", model.Payment);
                    parameters.Add("@QCReward", model.QCReward);
                    var result = await SqlMapper.QueryAsync(con, "usp_AddorUpdateQCFormReport", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<CallCatalogueVM>> GetInterviewerCallCatalogueList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CallCatalogueVM>(con, "usp_FetchCallCatalogueInterviewer", null,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<ResumeDetailsVM>> NotifiyUploadResumeDetails(DateTime startdate, DateTime enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startdate);
                    parameters.Add("@EndDate", enddate);
                    var result = await SqlMapper.QueryAsync<ResumeDetailsVM>(con, "usp_GetResumeDetailsForNotifiction", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> ApplyPenaltyQCForm(int interviewid, string interviewer, int redflags,int VideoReward)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewid", interviewid);
                    parameters.Add("@interviewer", interviewer);
                    parameters.Add("@redflags", redflags);
                    parameters.Add("@VideoReward", VideoReward);

                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_ApplyPenaltyForQCForm", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<string> GetClientHRContactByInterviewid(int interviewid)
        {
            try
            {
                var query = "Select ClientHrContact from InterviewMeetings where id=@interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    var results = await con.QueryFirstOrDefaultAsync<string>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<List<QCFormVM>> FetchAllQCFormDetails(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = "Select ul.EmailId as QCFormedBy,im.IRFId,cl.Name as Client,im.InterviewDate,im.interviewer as Interviewer,im.candidateEmail as Emailid,tq.* from Trans_QCFormReport tq join InterviewMeetings im on im.id=tq.Interviewid join UserList ul on ul.id=tq.Userid join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId where im.interviewDate>=@Startdate and im.interviewDate<=@Enddate";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Startdate", startDate);
                dynamicParameters.Add("@Enddate", endDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    var results = await con.QueryAsync<QCFormVM>(query, dynamicParameters);
                    return (List<QCFormVM>)results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> QCReportDetailsByInterviewer(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", emailId);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetQCReportDetailsByInterviewer", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> QCReportDetailsByDate(DateTime Startdate, DateTime Enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", Startdate);
                    parameters.Add("@EndDate", Enddate);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_FetchQCFormsOnDate", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> UpdateQCReportRedFlagsResolved(int interviewid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(
                        con, "update [dbo].[Trans_QCFormReport] set InterviewerAction=1 where Interviewid = @value", new { value = interviewid }, commandType: Text);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<MeetingVM>> GetReslovedRedFlagsInterviewList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    var parameters = new DynamicParameters();
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetReslovedRedFlagsInterviewMeetings", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<FnRevenueVM>> FetchRevenueDetails(DateTime stdate, DateTime endate, string year, int month)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var currentyear = Convert.ToInt32(year);
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", stdate);
                    parameters.Add("@EndDate", endate);
                    parameters.Add("@year", currentyear);
                    parameters.Add("@month", month);
                    var result = await SqlMapper.QueryAsync<FnRevenueVM>(con, "usp_GetRevenueDetailsByDate", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return (List<FnRevenueVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<FnRevenueVM>> FetchRevenueDetailsByClient(int clientid, DateTime stdate, DateTime endate, int billingType, string year, int month)
        {
            try
            {
                var currentyear = Convert.ToInt32(year);
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Clientid", clientid);
                dynamicParameters.Add("@StartDate", stdate);
                dynamicParameters.Add("@EndDate", endate);
                dynamicParameters.Add("@year", currentyear);
                dynamicParameters.Add("@month", month);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (billingType == 2)//For IRF Baised
                    {
                        var result = await SqlMapper.QueryAsync<FnRevenueVM>(con, "usp_GetRevenueDetailsByClient", dynamicParameters,
                          commandType: CommandType.StoredProcedure);
                        return (List<FnRevenueVM>)result;
                    }
                    else//For Channel Owner Baised
                    {
                        var result = await SqlMapper.QueryAsync<FnRevenueVM>(con, "usp_GetRevenueDetailsByClientBillingOwner", dynamicParameters,
                          commandType: CommandType.StoredProcedure);
                        return (List<FnRevenueVM>)result;
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<FnRevenueVM>> FetchAllRevenueDetails(DateTime stdate, DateTime endate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", stdate);
                    parameters.Add("@EndDate", endate);
                    var result = await SqlMapper.QueryAsync<FnRevenueVM>(con, "usp_GetAllRevenueDetailsByDate", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return (List<FnRevenueVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> SaveVideoDetailsById(int interviewid, string VideoURL, int videoduration)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewid", interviewid);
                    parameters.Add("@VideoURL", VideoURL);
                    parameters.Add("@videoduration", videoduration);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdatevideoDetailsById", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    DynamicParameters parametersPoints = new DynamicParameters();
                    parametersPoints.Add("@interviewId", interviewid);
                    await SqlMapper.QueryAsync(con, "usp_InsertOrUpdateInterviewPoints", parametersPoints,
                                    commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<ZoomMeetingsToBeDeleted>> GetZoomMeetingsToBeDeleted()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@cutOffDate", DateTime.Now.AddDays(-11));
                    var result = await SqlMapper.QueryAsync<ZoomMeetingsToBeDeleted>(con, "usp_GetZoomMeetingsToBeDeleted", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<FnRevenueVM> FetchRevenueStatusDetails(int clientid, string year, int month, int billingType, string Detailid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Clientid", clientid);
                dynamicParameters.Add("@year", year);
                dynamicParameters.Add("@month", month);
                dynamicParameters.Add("@billingType", billingType);
                dynamicParameters.Add("@Detailid", Detailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<FnRevenueVM>(con, "usp_GetRevenueStatusDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> SaveRevenueStatusDetails(int clientid, string year, int month, int billingType, string Detailid, int currentStatus, int invoiceamount, int poPending, string Emailid, int invoicerealization, int invoicesubmit)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Clientid", clientid);
                dynamicParameters.Add("@year", year);
                dynamicParameters.Add("@month", month);
                dynamicParameters.Add("@billingType", billingType);
                dynamicParameters.Add("@Detailid", Detailid);
                dynamicParameters.Add("@currentStatus", currentStatus);
                dynamicParameters.Add("@invoiceamount", invoiceamount);
                dynamicParameters.Add("@poPending", poPending);
                dynamicParameters.Add("@Emailid", Emailid);
                dynamicParameters.Add("@invoicerealization", invoicerealization);
                if (invoicesubmit == 1)
                {
                    dynamicParameters.Add("@invoiceSubmitted", true);
                }
                else
                {
                    dynamicParameters.Add("@invoiceSubmitted", false);
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_AddOrUpdateRevenueStatusDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateSingleParticipantFlagForMeeting()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@currentTime", DateTime.Now);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateSingleParticipantFlagForMeeting", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<CommonInterviewDetailsVM>> GetUpcomingInterviewsCandidates()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CommonInterviewDetailsVM>(con, "usp_GetUpcomingInterviewsCandidates", null,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<string>> FetchCandidateZoomMeetingsIdForCancel()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_FetchCandidateMeetingsIdForCancellation", null,
                       commandType: CommandType.StoredProcedure);
                    return (List<string>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<RedoInterviewVM> GetInterviewDetailsById(int id)
        {
            try
            {
                var query = "Select IRF.InterviewDuration as RequiredDuration,ic.VideoDuration as ActualDuration,IRF.id as IRFId from  InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId where im.id=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<RedoInterviewVM>(query, parameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> StoreClientBroadcastMessage(BroadcastModel model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@SentBy", model.SentBy);
                dynamicParameters.Add("@Message", model.Message);
                dynamicParameters.Add("@Subject", model.Subject);
                dynamicParameters.Add("@SentAt", DateTime.Now);
                dynamicParameters.Add("@IRFId", model.IRFId);
                dynamicParameters.Add("@InterviewId", model.InterviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_StoreClientBroadcastMessage", dynamicParameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> SaveRedoInterviewDetails(RedoInterviewVM model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", model.interviewid);
                dynamicParameters.Add("@CandidateFace", model.CandidateFace);
                dynamicParameters.Add("@RequiredDuration", model.RequiredDuration);
                dynamicParameters.Add("@ActualDuration", model.ActualDuration);
                dynamicParameters.Add("@Inappropriate", model.Inappropriate);
                dynamicParameters.Add("@HandsOfCoding", model.HandsOfCoding);
                dynamicParameters.Add("@Comments", model.Comments);
                dynamicParameters.Add("@ModifiedBy", model.ModifiedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_AddRedoInterviewDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<RedoInterviewVM> GetRedoInterviewDetailsById(int id)
        {
            try
            {
                var query = "Select * From RedoInterviews where interviewid=@id";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<RedoInterviewVM>(query, parameters);
                    return results.LastOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> UpdateRejectRedoInterviewDetails(RedoInterviewVM model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", model.interviewid);
                dynamicParameters.Add("@id", model.id);
                dynamicParameters.Add("@RejectionComment", model.RejectionComment);
                dynamicParameters.Add("@ModifiedBy", model.ModifiedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateRejectRedoInterviewDetails", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> ResetInterviewPayment(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_ResetInterviewPayment", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public bool CheckInterviewCancellable(int meetingid)
        {
            try
            {
                var query = "Select Count(*) From InterviewMeetings where id = @meetingid and interviewDate>@interviewdate";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@interviewdate", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    if (results.FirstOrDefault() > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> GetMeetingPlatformbyIRFId(string IRFId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetMeetingPlatformByIRFId", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> GetRedoInterviewList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRedoInterviewMeetings", null, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public bool CheckInterviewClerityTime(int meetingid)
        {
            try
            {
                var query = "Select Count(*) From InterviewMeetings where id = @meetingid and interviewDate>@interviewdate";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@interviewdate", DateTime.Now.AddHours(2));
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    if (results.FirstOrDefault() > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<CallRecordingDataVM>> GetCallRecordingData(string profile, string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserProfile", profile);
                    parameters.Add("@Emailid", Emailid);
                    return await SqlMapper.QueryAsync<CallRecordingDataVM>(con, "usp_FetchCallRecordingDataDB", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> CheckReportClientShareDetails(string Emailid, string uniqueid, int Interviewid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@uniqueid", uniqueid);
                    parameters.Add("@Emailid", Emailid);
                    parameters.Add("@Interviewid", Interviewid);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_CheckClientReportShareDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    if (result.FirstOrDefault() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<string> CheckReportClientUrlSave(string Emailid, string uniqueid, int Interviewid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", Emailid);
                parameters.Add("@uniqueid", uniqueid);
                parameters.Add("@Interviewid", Interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveReportClientUrlSave", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<bool> SaveInterviewsReview(int rating, string comments, string Emailid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", Emailid);
                parameters.Add("@rating", rating);
                parameters.Add("@comments", comments);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_saveClientreviewdetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<ClientReviewVM>> FetchInterviewsReview()
        {
            try
            {
                var query = "Select cr.*,cl.Name as ClientName From ClientReviewDetails cr join UserList ul on cr.ClientEmailid=ul.EmailId join ClientUser cu on cu.UserId=ul.id join Client cl on cl.id=cu.ClientId order by CreatedOn desc";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<ClientReviewVM>(query, null);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> BacklinkFailInterviewStatusUpdate(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_BacklinkFailInterviewStatusUpdate", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> UpdateProbableNoShowCandidates()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@currentTime", DateTime.Now);
                    var result = await SqlMapper.QueryAsync<PendingVideoMeetingsData>(con, "usp_AddUpdateProbableNoShowCandidates", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<IEnumerable<MeetingVM>> GetProbableNoShowCandidates()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetProbableNoShowCandidates", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> SaveSauveyIRFReview(int rating, string comments, string Emailid, string IRFId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", Emailid);
                parameters.Add("@rating", rating);
                parameters.Add("@comments", comments);
                parameters.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_saveClientIRFreviewdetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> SaveCapturedJoinMeetingTime(int interviewid, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewid", interviewid);
                    parameters.Add("@emailid", emailid);
                    parameters.Add("@JoiningDate", DateTime.Now);
                    var result = await SqlMapper.QueryAsync(con, "usp_SaveCapturedJoinMeetingTime", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> SaveSurchargeInterviewDetailsById(int interviewid, string surchargecomment, int surchargeamt)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewid", interviewid);
                    parameters.Add("@surchargecomment", surchargecomment);
                    parameters.Add("@surchargeamt", surchargeamt);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateSurchargeInterviewDetailsById", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<MeetingVM>> FetchCompletedInterviewsByClientId(int clientid, DateTime stdate, DateTime endate, string year, int month)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var currentyear = Convert.ToInt32(year);
                    var parameters = new DynamicParameters();
                    parameters.Add("@clientid", clientid);
                    parameters.Add("@StartDate", stdate);
                    parameters.Add("@EndDate", endate);
                    parameters.Add("@year", currentyear);
                    parameters.Add("@month", month);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetCompletedInterviewsByClientIdDate", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return (List<MeetingVM>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<string> GetTranscriptUrlByMeeting(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetTranscriptUrlByMeeting", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> AddBlackListCompany(BlackListCompanyVM model)
        {
            try
            {
                DynamicParameters dynamicParam = new DynamicParameters();
                dynamicParam.Add("@CompanyName", model.CompanyName);
                dynamicParam.Add("@Comment", model.Comment);
                dynamicParam.Add("@CreatedDate", model.CreatedDate);
                dynamicParam.Add("@CreatedBy", model.CreatedBy);
                dynamicParam.Add("@ModifyDate", model.ModifyDate);
                dynamicParam.Add("@ModifyBy", model.ModifyBy);
                using (SqlConnection conChange = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(conChange, "usp_InsertBlackListCompany", dynamicParam,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<PaymentSLAVM> FetchPaymentSlaDetailsById(int interviewid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryFirstOrDefaultAsync<PaymentSLAVM>(con, "usp_GetPaymentSlaDetailsById", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddorUpdatePaymentSLADetails(PaymentSLAVM model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Interviewid", model.Interviewid);
                    parameters.Add("@Emailid", model.CreatedBy);
                    parameters.Add("@CreatedOn", DateTime.Now);
                    parameters.Add("@PaymentDeduction", model.PaymentDeduction);
                    if (model.isapprovedchecked)
                    {
                        parameters.Add("@isapproved", 1);
                    }
                    else
                    {
                        parameters.Add("@isapproved", 0);
                    }

                    parameters.Add("@Inaccuratedecisionmaking", model.Inaccuratedecisionmaking);
                    parameters.Add("@TimeManagement", model.TimeManagement);
                    parameters.Add("@QuestionsRelevance", model.QuestionsRelevance);
                    parameters.Add("@ScoreRubricIgnorance", model.ScoreRubricIgnorance);
                    parameters.Add("@PoorFeedbackComments", model.PoorFeedbackComments);
                    parameters.Add("@CandidateInteractionQuality", model.CandidateInteractionQuality);
                    parameters.Add("@LimitedHandoncoding", model.LimitedHandoncoding);
                    parameters.Add("@WrongStatusUpdates", model.WrongStatusUpdates);
                    parameters.Add("@WeakInfrastructure", model.WeakInfrastructure);
                    parameters.Add("@ProctoringIgnorance", model.ProctoringIgnorance);
                    parameters.Add("@UnethicalPractices", model.UnethicalPractices);
                    parameters.Add("@PoorCommunicationSkills", model.PoorCommunicationSkills);
                    parameters.Add("@PaymentComplianceCount", model.PaymentComplianceCount);

                    var result = await SqlMapper.QueryAsync(con, "usp_AddorUpdatePaymentSLADetails", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<PaymentSLAVM>> FetchPaymentSlaDetails(string actionstatus)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@actionstatus", actionstatus);
                    var result = await SqlMapper.QueryAsync<PaymentSLAVM>(con, "usp_GetPaymentSlacompliance", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> PaymentSlaNotApproved(int interviewid, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Interviewid", interviewid);
                    parameters.Add("@createdon", DateTime.Now);
                    parameters.Add("@ModifiedBy", emailid);
                    var result = await SqlMapper.QueryAsync(con, "usp_PaymentSlaNotApproved", parameters, commandTimeout: 600,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> AgreeToPaymentSLAConditions(string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", emailid);
                    var result = await SqlMapper.QueryAsync(con, "usp_AgreeToPaymentSLAConditions", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> AddTransInterviewersReliabilityScores(TransInterviewersReliabilityScoresVm model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters(); 
                dynamicParameters.Add("@InterviewMeetingId", model.InterviewMeetingId);
                dynamicParameters.Add("@ReliabilityScoreTypeId", model.ReliabilityScoreTypeId);
                dynamicParameters.Add("@CreateDate",DateTime.Now);
                dynamicParameters.Add("@InterviewerEmail",model.InterviewerEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_TransInterviewersReliabilityScores", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> AddTransReliabilityScoresSelectAndReject(int InterviewMeetingId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewMeetingId",InterviewMeetingId);
                dynamicParameters.Add("@CreateDate", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_SelectionAndCommentTransInterviewersReliabilityScores", dynamicParameters,
                      commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}
