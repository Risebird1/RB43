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
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class MeetingRespository : IMeetingRespository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public MeetingRespository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<MeetingVM>(con, "usp_GetIAllInterviewMeetings", null,
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

        public async Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("startDate", startDate);
                    parameters.Add("endDate", endDate);
                    var result = SqlMapper.Query<MeetingVM>(con, "usp_GetIAllDateWiseInterviewMeetings", parameters,
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
        public async Task<IEnumerable<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid)
        {
            try
            {
                if (profile == "CuratedUser")
                {
                    Emailid = Emailid.Replace("\"", "");
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("clientid", clientId);
                        parameters.Add("Emailid", Emailid);
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForCuratedUser", parameters,
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
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForExternalUser", parameters,//create sp
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
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetings", parameters,
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
                        return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetRiseTubeMeetingsForExternalUser", parameters,//create sp
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
        public async Task<IEnumerable<MeetingVM>> MultipleCandidateInvites()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_MultipleCandidateInvites", null,
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


        public async Task<IEnumerable<Meeting>> GetQCOpenInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyQCOpenInterviews", parameters,
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

        public async Task<IEnumerable<Meeting>> GetAllAssignedInterview(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
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


        public async Task<IEnumerable<Meeting>> GetQualityControlInterview(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyQualityControlInterviews", parameters,
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

        public async Task<IEnumerable<int>> CheckCompletedInterviews(string emailID, int clientId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailID", emailID);
                    parameters.Add("@ClientId", clientId);
                    parameters.Add("@irfId", irfId);
                    //usp_CheckCompletedInterviews
                    return await SqlMapper.QueryAsync<int>(con, "usp_CheckCompletedInterviews", parameters,
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



        private DynamicParameters SetStoredProcedureParameterForMeeting(MeetingVM entity)
        {
            var parameters = new DynamicParameters();
            if (entity.imid > 0)
                parameters.Add("@interviewId", entity.imid);

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
            if (entity.HRCoordinatorEmailId != null)
            {
                parameters.Add("@HRCoordinatorEmailId", entity.HRCoordinatorEmailId);

            }
            return parameters;
        }
        public async Task<bool> AddMeeting(MeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForMeeting(meeting);
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_GetMeetingById", parameters,
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

        public async Task<bool> UpdateMeeting(MeetingVM meeting)
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

        public string AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid }, commandType: Text);
                    return "Interview assigned successfully.";
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


        public string QCAssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                var query = "update [dbo].[InterviewMeetings] set QCInterviewer=@value where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@value", interviewerMail);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return "Interview assigned successfully.";
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }


        public bool CancelInterview(int meetingid)
        {
            try
            {
                var query = "update [dbo].[InterviewMeetings] set confirmationStatus=@value where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@value", 2);

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

        public async Task<bool> UnassignYourself(int meetingid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
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

        public async Task<bool> QCUnassignYourself(int meetingid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "usp_UnAssignQCInterviewer", parameters, commandType: CommandType.StoredProcedure);
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
                    return await SqlMapper.QuerySingleAsync<InterviewComments>(con, "usp_GetInterviewComments", parameters,
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

        public IEnumerable<TransSkill> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransTechSkill where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkill>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id)
        {
            try
            {
                var query = "Select tsg.*, gsm.Name as Name from TransSkillGeneral tsg join GeneralSkillMatrix gsm on tsg.type = gsm.Id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkillGeneral>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransJoinability> GetTransJoinability(int id)
        {
            try
            {
                var query = "Select tj.*, jm.Name as Name from TransJoinability tj join JoinabilityMatrix jm on tj.type = jm.Id where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransJoinability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public IEnumerable<SkillComment> GetTechSkillComments(int id)
        {
            try
            {


                var query = "Select * from TechSkillComments where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SkillComment>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Level> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<LevelTech> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<LevelTech>(
                        con, "select * from LevelTech", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Skill> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Skill>(
                        con, "select * from Skill", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public async Task<bool> AddInterviewReport(InterviewReport reportDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@InterviewId", reportDetails.InterviewId);
                    paramx.Add("@confirmationStatus", "");
                    paramx.Add("@OverallScore", reportDetails.OverallScore);
                    paramx.Add("@Rating", reportDetails.Rating);
                    paramx.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    paramx.Add("@SkillGeneralComments", reportDetails.SkillGeneralComments);
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
                    if (reportDetails.IsEdited)
                    {
                        var result = SqlMapper.Query<int>(con, "usp_UpdateInterviewReport", paramx,
                                            commandType: CommandType.StoredProcedure);

                    }
                    else
                    {
                        //add to interviewReport table
                        string sqlInsertReport = "INSERT INTO InterviewReport (IRFId, InterviewId, CandidateName,confirmationStatus,OverallScore, Rating, TechSkillComments, SkillGeneralComments, SelectabilityComments, JoinabilityComments, ProctoringCount ) Values (@IRFId, @InterviewId, @CandidateName, @confirmationStatus, @OverallScore, @Rating, @TechSkillComments, @SkillGeneralComments, @SelectabilityComments, @JoinabilityComments, @ProctoringCount);";
                        var affectedRows = con.ExecuteAsync(sqlInsertReport, new
                        {
                            reportDetails.IRFId,
                            reportDetails.InterviewId,
                            reportDetails.CandidateName,
                            reportDetails.confirmationStatus,
                            reportDetails.OverallScore,
                            reportDetails.Rating,
                            reportDetails.TechSkillComments,
                            reportDetails.SkillGeneralComments,
                            reportDetails.SelectabilityComments,
                            reportDetails.JoinabilityComments,
                            reportDetails.ProctoringCount

                        }).Result;

                    }

                    DataTable techReportDt = new DataTable();
                    techReportDt.Columns.Add("InterviewId", typeof(int));
                    techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    techReportDt.Columns.Add("SkillId", typeof(int));
                    techReportDt.Columns.Add("ActualLevelId", typeof(int));
                    techReportDt.Columns.Add("WeightageRequired", typeof(int));
                    techReportDt.Columns.Add("WeightageActual", typeof(int));


                    foreach (var item in reportDetails.TechSkillReportCollection)
                    {
                        techReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual);
                    }

                    SqlCommand cmdTechReport = new SqlCommand("usp_TechSkillReportBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                    con.Open();
                    if (cmdTechReport.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    DataTable skillGeneralReportDt = new DataTable();
                    skillGeneralReportDt.Columns.Add("InterviewId", typeof(int));
                    skillGeneralReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    skillGeneralReportDt.Columns.Add("Type", typeof(string));
                    skillGeneralReportDt.Columns.Add("ActualLevelId", typeof(int));
                    skillGeneralReportDt.Columns.Add("WeightageRequired", typeof(int));
                    skillGeneralReportDt.Columns.Add("WeightageActual", typeof(int));
                    skillGeneralReportDt.Columns.Add("Value", typeof(string));


                    foreach (var item in reportDetails.SkillGeneralReportCollection)
                    {
                        skillGeneralReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.Name, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.Value);
                    }

                    SqlCommand cmdSkillGeneralReport = new SqlCommand("usp_SkillGeneralReportBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdSkillGeneralReport.Parameters.Add(new SqlParameter("@SkillGeneralReportBulkData", SqlDbType.Structured));
                    cmdSkillGeneralReport.Parameters["@SkillGeneralReportBulkData"].Value = skillGeneralReportDt;
                    con.Open();
                    if (cmdSkillGeneralReport.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }


                    DataTable questionsDt = new DataTable();
                    questionsDt.Columns.Add("InterviewId", typeof(int));
                    questionsDt.Columns.Add("Comment", typeof(string));
                    questionsDt.Columns.Add("RatingId", typeof(int));
                    questionsDt.Columns.Add("Timing", typeof(int));
                    questionsDt.Columns.Add("SkillName", typeof(string));


                    foreach (var item in reportDetails.QuestionsCollection)
                    {
                        questionsDt.Rows.Add(reportDetails.InterviewId, item.Comment, item.RatingId, item.Timing, item.SkillName);
                    }

                    SqlCommand cmdQuestions = new SqlCommand("usp_QuestionsDiscussedBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDiscussedBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@QuestionsDiscussedBulkData"].Value = questionsDt;
                    con.Open();
                    if (cmdQuestions.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    if (reportDetails.Proctoring != null)
                    {
                        //update protoring report table
                        string sqlProcReport = "INSERT INTO ProctoringReport Values (@InterviewId, @EyesDirection, @EyesReflection, @HeadDirection, @LipsOverhead, @LipsDarklight, @LipsHeadphone, @LatencyJitter, @DesktopSuspiciousApps, @DesktopScreenSharing, @VoiceAudioSplitter, @VoiceToneExpression, @VoicePersonalUnsatisfactory, @VoiceSuspicious, @LinkedInPhotoVerification);";
                        var result3 = con.Execute(sqlProcReport, new
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

        public IEnumerable<InterviewReport> GetInterviewReportById(int id, int irfId = 0)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", id);//@IrfId
                dynamicParameters.Add("@IrfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewReport>(con, "usp_GetInterviewReportById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                var query = "Select * from QuestionsDiscussed where InterviewId = @id";
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

        public IEnumerable<string> GetIRFName(int id)
        {
            try
            {
                var query = "Select IRFId from IRF where IsDeleted = @IsDeleted and id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IRFDetails GetIRFDetails(int id)
        {
            try
            {
                var query = "Select IRFId, JDTiltle from IRF where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRFDetails>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TechSkillReport> GetTechSkillReport(int id)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select TechSkillReport.*, [LevelTech].[Name] as RequiredLevel from TechSkillReport inner join [LevelTech] on TechSkillReport.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id where InterviewId = @id";
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
                var query = "select SkillGeneralReport.*,SkillGeneralReport.Type as Name, [Level].[Name] as RequiredLevel from SkillGeneralReport inner join [Level] on SkillGeneralReport.RequiredLevelId=[Level].id where InterviewId = @id";
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
        public bool GetIRFbyId(int irfId)
        {
            try
            {
                var query = "Select * from IRF where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<IRF>(query, dynamicParameters);
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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

        public bool SaveQCFeedback(int imid, int qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled)
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
                    var result = SqlMapper.Query<int>(con, "usp_SaveQCFeedback", dynamicParameters,
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
            parameters.Add("@QCStatus", entity.QCStatus);
            parameters.Add("@QCComment", entity.QCComment);
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
            return parameters;
        }

        public int GetCandidateId(int id, string irfName)
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
                    result = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
        public async Task AddTotalViewCount(int id, string emailaddress)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Interviewid", id);
                parameters.Add("@EmailId", emailaddress);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<string>(con, "usp_AddinterviewView", parameters,
                       commandType: CommandType.StoredProcedure);
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
                    var result = await SqlMapper.QueryFirstAsync<string>(con, "usp_AddinterviewvedioShare", parameters,
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
        public List<VedioComments> GetvedioComments(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@VedioId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<VedioComments>(con, "usp_GetinterviewvedioComment", parameters,
                       commandType: CommandType.StoredProcedure);
                    return (List<VedioComments>)result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;

        }
        public EmailDetails GetvedioShortList(string vedioid, string email)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@VedioId", vedioid);
                parameters.Add("@emailId", email);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<EmailDetails>(con, "usp_AddinterviewvedioShortList", parameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string CandidateSuitable(int id, string email, string Comment)
        {
            try
            {
                email = email.Replace("\"", "");
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewId", id);
                parameters.Add("@EmailId", email);
                parameters.Add("@Comment", Comment);
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
        public VedioComments Intrestreaction(string vedioid, string Status)
        {
            try
            {
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
                    var result = con.Query<VedioComments>(finalquery, finaldynamicParameters);
                    return (VedioComments)result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    id = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                    // return results;
                }
                var finalquery = "Select ul2.EmailId as ClientName, IRF.JDTiltle, IRF.JDLink, su.id as id,su.ShortListedOn as interviewDate,IsNull(su.Status,'') as Status  from IRF inner join InterviewMeetings im on im.IRFId=IRF.IRFId inner join RiseTubeVedioShortList su on im.id=su.Interviewid inner join UserList ul on im.candidateEmail=ul.EmailId inner join UserList ul2 on ul2.id=su.userid Where ul.id=@Id and im.IRFId=@IRFID";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Id", id);
                finaldynamicParameters.Add("@IRFID", IRFID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Meeting>(finalquery, finaldynamicParameters);
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
                    id = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                    // return results;
                }
                var finalquery = "Select ul2.EmailId as ClientName,IRF.JDTiltle, IRF.JDLink, IRF.IRFId as IRFID,su.id as id,su.InterstedOn as interviewDate,IsNull(su.Status,'') as Status,su.Comment as ApprovalComments from IRF inner join InterviewMeetings im on im.IRFId=IRF.IRFId inner join SuitableCandidate su on im.id=su.Interviewid inner join UserList ul on im.candidateEmail=ul.EmailId inner join UserList ul2 on ul2.id=su.userid Where ul.id=@Id and im.IRFId=@IRFID";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Id", id);
                finaldynamicParameters.Add("@IRFID", IRFID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Meeting>(finalquery, finaldynamicParameters);
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
                        if (String.IsNullOrEmpty(emailidreturn))
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
        public bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", interviewId);//@IrfId
                dynamicParameters.Add("@FeedbackAudioLink", feedbackAudioLink);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_SaveFeedbackAudio", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
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
                    var result = SqlMapper.QueryAsync<string>(con, "usp_GetEmailsforBroadcast", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result.Result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<Meeting>> GetHROpenInterviews()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_HROpenInterviews",
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
                    return "Interview assigned successfully.";
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

        public bool SaveHRComment(int id, string comment)
        {
            try
            {
                var query = "update InterviewMeetings set HRComment = @comment where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@comment", comment);
                dynamicParameters.Add("@id", id);

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

    }
}
