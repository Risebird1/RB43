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
    public class InterviewerSkillsRepository : IInterviewerSkillsRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewerSkillsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<IEnumerable<InterviewerSkillsChannelModel>> GetAllInterviewerSkillsChannel(string profileName, string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Profile", profileName);
                    parameters.Add("@EmailId", emailId);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<InterviewerSkillsChannelModel>(con, "usp_GetAllInterviewerSkillsChannel", parameters,
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

        public IEnumerable<InterviewerVM> GetInterviewers()
        {
            IEnumerable<InterviewerVM> interviewersVm = new List<InterviewerVM>();
            IEnumerable<InterviewerProfileVM> Leadinterviewers = new List<InterviewerProfileVM>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    Leadinterviewers = SqlMapper.Query<InterviewerProfileVM>(
                        con, "select * from InterviewerProfile where IsLeadInterviewer=1", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in Leadinterviewers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    interviewersVm = SqlMapper.Query<InterviewerVM>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return interviewersVm;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Client> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client = SqlMapper.Query<Client>(
                        con, "select * from Client", null, commandType: Text);
                }
                return client;
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
        public IEnumerable<Users> GetCientUsers()
        {
            IEnumerable<ClientUserDTO> clientUsers = new List<ClientUserDTO>();
            IEnumerable<Users> clients = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    clientUsers = SqlMapper.Query<ClientUserDTO>(
                        con, "select * from ClientUser", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in clientUsers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    clients = SqlMapper.Query<Users>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return clients;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Risebird.Entities.Data.ClientRole> GetClientRoles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ClientRole>(
                        con, "select * from ClientRole", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Recruiter> GetRecruiters()
        {
            try
            {
                var query = "select Recruiter.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from Recruiter join UserList ul on Recruiter.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Recruiter>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ResumeEvaluator> GetResumeEvaluators()
        {
            try
            {
                var query = "select re.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from ResumeEvaluator re join UserList ul on re.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ResumeEvaluator>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<HR> GetHRs()
        {
            try
            {
                var query = "select HR.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber from HR join UserList ul on HR.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<HR>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddInterviewerSkillsDetails(InterviewerSkillsVM InterviewerSkillsDetails)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForInterviewerSkills(InterviewerSkillsDetails);
                //parameters.Add("@id", InterviewerSkillsDetails.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerSkillsChannel", parameters,
                       commandType: CommandType.StoredProcedure);

                    var InterviewerSkillId = parameters.Get<int>("@idIsc");

                    //var irfList = SqlMapper.QueryAsync<InterviewerSkillsVM>(
                    //    con, "select * from [dbo].[InterviewerSkillsChannel]", null, commandType: Text).Result.AsList();
                    //var InterviewerSkillId = irfList.Where(x => x.InterviewerSkillId == InterviewerSkillsDetails.InterviewerSkillId).Last().id;//irfList.Find(x => x.IRFId == InterviewerSkillsDetails.IRFId).id;

                    //update tech skill table
                    foreach (var item in InterviewerSkillsDetails.SkillCollection)
                    {
                        string sqlSkillTech = "INSERT INTO TransInterviewerSkillsTechSkills (InterviewerSkillId, LevelId, SkillId, Weightage) Values (@IRFId, @LevelId, @SkillId, @Weightage);";
                        var affectedRows = con.Execute(sqlSkillTech, new
                        {
                            IRFId = InterviewerSkillId,
                            item.LevelId,
                            item.SkillId,
                            item.Weightage
                        });
                    }

                    //update trans interviewer table
                    foreach (var item in InterviewerSkillsDetails.InterviewerCollection)
                    {
                        DynamicParameters param = new DynamicParameters();
                        param.Add("@InterviewerSkillId", InterviewerSkillId);
                        param.Add("@InterviewerUserId", item.UserId);
                        param.Add("@InterviewerPerSuccessRate", item.PerSuccessRate);
                        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransInterviewerSkillsLeadInterviewer", param,
                                commandType: CommandType.StoredProcedure);


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

        private DynamicParameters SetStoredProcedureParameterForInterviewerSkills(InterviewerSkillsVM entity, bool isUpdate = false)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@InterviewerSkillId", entity.InterviewerSkillId);
            parameters.Add("@JDTiltle", entity.JDTiltle);
            parameters.Add("@StartDate", entity.StartDate);
            parameters.Add("@EndDate", entity.EndDate);
            parameters.Add("@RisebirdHr", entity.RisebirdHr);
            parameters.Add("@PerMintuteRate", entity.PerMintuteRate);
            parameters.Add("@PrimarySkill", entity.PrimarySkill);
            parameters.Add("@CalendlyLink", entity.CalendlyLink);
            parameters.Add("@HeuristicProfileType", entity.HeuristicProfileType);
            parameters.Add("@JDLink", entity.JDLink);
            parameters.Add("@AssessmentGroupId", entity.AssessmentGroupId);
            if (!isUpdate)
            {
                parameters.Add("@idIsc", dbType: DbType.Int32, direction: ParameterDirection.Output);

            }

            return parameters;
        }

        public IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<GeneralSkillMatrix>(
                        con, "select * from GeneralSkillMatrix", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<SelectabilityMatrix>(
                        con, "select * from SelectabilityMatrix", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Users> GetUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Users>(
                        con, "select * from UserList", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Interviewer> GetInterviewerSkills(int userId)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@UserId", userId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(con, "usp_GetInterviewerSkills", param,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Interviewer> GetInterviewerSkillofInterviewerSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(con, "usp_GetLeadInterviewersSkills",
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public Skill GetSkillById(int id)
        {
            try
            {
                var query = "select id, [Name] from Skill where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Skill>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> CheckUniqueness(string value)
        {
            try
            {
                //var query = "Select * from InterviewerSkillsChannel where InterviewerSkillId = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@value", value);
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFISCUniqueness", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    if (result.Count() > 0)
                        return false;
                    else
                        return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }
        public InterviewerSkillsChannelModel EditInterviewerSkills(int InterviewerSkillID)
        {
            try
            {
                var query = "Select * from InterviewerSkillsChannel where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", InterviewerSkillID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewerSkillsChannelModel>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TransInterviewerSkillsofSkills> GetSkillsbyInterviewerSkillId(int id)
        {
            try
            {
                var query = "Select * from TransInterviewerSkillsTechSkills where InterviewerSkillId = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransInterviewerSkillsofSkills>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public IEnumerable<TransInterviewerSkillsLeadInterviewer> GetTransInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@ISCId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransInterviewerSkillsLeadInterviewer>(con, "usp_GetTransISCLeadInterviewer", param,
                                            commandType: CommandType.StoredProcedure);
                    return result;
                }


            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string GetPrimarySkill(int id)
        {
            try
            {
                var query = "Select PrimarySkill from InterviewerSkillsChannel where id = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.QuerySingle<string>(query, dynamicParameters);
                    return results.ToString();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateInterviewerSkillsDetails(InterviewerSkillsVM InterviewerSkillsDetails)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForInterviewerSkills(InterviewerSkillsDetails, true);

                parameters.Add("@id", InterviewerSkillsDetails.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerSkillsChannel", parameters,
                       commandType: CommandType.StoredProcedure);





                    //update tech skill table
                    if (InterviewerSkillsDetails.SkillCollection != null)
                    {
                        foreach (var item in InterviewerSkillsDetails.SkillCollection)
                        {

                            string sqlSkillTech = "INSERT INTO TransInterviewerSkillsTechSkills (InterviewerSkillId, LevelId, SkillId, Weightage) Values (@InterviewerSkillId, @LevelId, @SkillId, @Weightage);";
                            var affectedRows = con.Execute(sqlSkillTech, new
                            {
                                InterviewerSkillId = InterviewerSkillsDetails.id,
                                item.LevelId,
                                item.SkillId,
                                item.Weightage
                            });
                        }
                    }

                    //update trans interviewer table

                    if (InterviewerSkillsDetails.InterviewerCollection != null)
                    {
                        foreach (var item in InterviewerSkillsDetails.InterviewerCollection)
                        {
                            DynamicParameters param = new DynamicParameters();
                            param.Add("@InterviewerSkillId", InterviewerSkillsDetails.id);
                            param.Add("@InterviewerUserId", item.UserId);
                            param.Add("@@InterviewerPerSuccessRate", item.PerSuccessRate);
                            var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransInterviewerSkillsLeadInterviewer", param,
                                    commandType: CommandType.StoredProcedure);
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
        public async Task<bool> DeleteInterviewerSkillsChannel(int id)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ISCId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteInterviewerSkillsChannel", parameters,
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
        public async Task<bool> UnArchiveISC(int id)
        {
            try
            {
                var query = "update InterviewerSkillsChannel set IsDeleted=0 where id = @ISCId;";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ISCId", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.QueryAsync<int>(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public IEnumerable<ISCInterviewer> GetISCInterviewers(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ISCId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ISCInterviewer>(con, "usp_GetInterviewerSkillsChannelInterviewers", parameters,
                        commandType: StoredProcedure);
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
        //TODO need to check
        public int GetUserId(string emailId, string profile)
        {
            int result = 0;
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profile);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = SqlMapper.QuerySingle<int>(con, "usp_GetUserId", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }
        //TODO need to check
        public bool AddUnApprovedInterviewer(IEnumerable<UnApprovedInterviewer> model, int iscId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    foreach (var item in model)
                    {
                        string sqlAddInterviewer = "INSERT INTO TransInterviewerListInterviewerSkills (UserId, ISCId, InterviewerProfileId) Values (@UserId, @ISCId, @InterviewerProfileId);";
                        var result1 = con.Execute(sqlAddInterviewer, new
                        {
                            item.UserId,
                            ISCId = iscId,
                            item.InterviewerProfileId

                        });
                    }
                    return true;

                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        //TODO Need to check
        public bool AddInterviewer(InterviewerSkillsVM Model)
        {
            try
            {
                foreach (var item in Model.InterviewerListsCollection)
                {
                    DynamicParameters objDynamicParameters = new DynamicParameters();
                    objDynamicParameters.Add("@UserId", item.UserId);
                    objDynamicParameters.Add("@InterviewerSkillId", item.InterviewerSkillId);
                    var querys = " select Count(*) from TransInterviewerListInterviewerSkills where InterviewerSkillId = @InterviewerSkillId and UserId = @UserId";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var Count = con.QuerySingle<int>(querys, objDynamicParameters);
                        if (Convert.ToInt32(Count) == 0)
                        {
                            var query = "insert into [dbo].[TransInterviewerListInterviewerSkills] (UserId,PrimarySkill,InterviewerSkillId,IsAdded) Values(@UserId,@PrimarySkill,@InterviewerSkillId,@IsAdded) ";
                            var results = con.Execute(query, new
                            {
                                item.UserId,
                                item.PrimarySkill,
                                item.InterviewerSkillId,
                                item.IsAdded
                            });
                        }
                    }

                }
                return true;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        //public async Task<IEnumerable<MeetingVM>> UpdateMeetingStatus(string InterviewerSkillId)
        //{
        //    try
        //    {
        //        //using (SqlConnection con = new SqlConnection(connectionString))
        //        //{
        //        //    var dynamicParameters = new DynamicParameters();
        //        //    dynamicParameters.Add("@IRFId", IRFId);
        //        //    dynamicParameters.Add("@date", DateTime.Now);
        //        //    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_UpdateMeetingStatus", dynamicParameters,
        //        //         commandType: CommandType.StoredProcedure);
        //        //}
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return null;

        //}
        public bool PlayInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {
                var query = "update InterviewerSkillsChannel set IsPaused = 0 where Id=@InterviewerSkillId and InterviewerSkillId=@InterviewerSkillName";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", InterviewerSkillId);
                dynamicParameters.Add("@InterviewerSkillName", InterviewerSkillName);

                //dynamicParameters.Add("@MeetingId", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, dynamicParameters);
                }
                return true;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;

        }

        public bool PauseInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName)
        {
            try
            {
                var query = "update InterviewerSkillsChannel set IsPaused = 1 where Id=@InterviewerSkillId and InterviewerSkillId=@InterviewerSkillName";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", InterviewerSkillId);
                dynamicParameters.Add("@InterviewerSkillName", InterviewerSkillName);

                //dynamicParameters.Add("@MeetingId", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, dynamicParameters);
                }
                return true;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;

        }
        public async Task<IEnumerable<UnApprovedInterviewer>> GetUnApprovedInterviewerList(int id)
        {

            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ISCId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<UnApprovedInterviewer>(con, "usp_GetUnApprovedInterviewers", dynamicParameters,
                        commandType: StoredProcedure);
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

        public async Task<bool> DeleteISCInterviewer(int id)
        {
            try
            {

                var query = "Delete from TransInterviewerListInterviewerSkills where Id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
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

        public InterviewerSkillsChannelModel GetISCbyId(int id)
        {
            try
            {

                var query = "select isc.*, skill.[Name] as SkillName, ul.EmailId as HREmailId from InterviewerSkillsChannel isc join Skill on Skill.id = isc.PrimarySkill join UserList ul on ul.id = isc.RisebirdHr where isc.id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewerSkillsChannelModel>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateInterviewerInvites(int iscId, int interviewerProfileId)
        {
            try
            {
                var query = "update TransInterviewerListInterviewerSkills set TotalInvites = TotalInvites+1 where ISCId=@ISCId and InterviewerProfileId = @InterviewerProfileId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ISCId", iscId);
                dynamicParameters.Add("@InterviewerProfileId", interviewerProfileId);

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
        public async Task<List<MeetingVM>> GetInterviewerRiseTubeMeetingsPaged(int Page, string searchvalue)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startFromParam", ((Page - 1) * 12));
                    parameters.Add("@endWithParam", Page * 12);
                    parameters.Add("@searchvalue", searchvalue);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetInterviewerRiseTubeMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return (List<MeetingVM>)result;
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
        public async Task<int> GetInterviewerRiseTubeMeetingsCount(string searchvalue)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@searchvalue", searchvalue);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_GetInterviewerRisetubeIndexMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
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
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_GetInterviewerVideoMeetingsbyId", parameters,
                        commandType: CommandType.StoredProcedure);
                    //Update Table For Interviewer
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
        public async Task<IEnumerable<MeetingVM>> GetInterviewerRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@clientid", clientId);
                    parameters.Add("@Emailid", Emailid);
                    parameters.Add("@JDpattern", JDpattern);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetInterviewerRiseTubeMeetingsByJD", parameters,
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



        public async Task<IEnumerable<TransSkill>> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select id,InterviewerSkillId as IRFId,SkillId,LevelId,Weightage from [dbo].[TransInterviewerSkillsTechSkills] where InterviewerSkillId = @IRFId";
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
        public int GetCandidateId(int id, string irfName)
        {
            int result = 0;
            try
            {
                var query = "select cm.id from UnapprovedInterviewerMeetings im join UserList ul on im.candidateEmail = ul.EmailId join TransInterviewerListInterviewerSkills cm on cm.UserId=ul.id where im.ISCId = @irfName and im.id =@id";
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

        public async Task<List<string>> FetchAdminList()
        {
            try
            {
                var query = "Select Emailid from UserList where idProfile in(1)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(query, null);
                    return (List<string>)result;
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
        public async Task<IEnumerable<MeetingVM>> GetInterviewerSkillInterviewMeetings(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetISCInterviewMeetings", parameters,
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
        public IEnumerable<Interviewer> GetInterviewersByAssessmentGroup(string assessmentgroupid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@assessmentgroupid", assessmentgroupid);
                    return con.Query<Interviewer>(
                         "select intp.UserId,intp.AssessmentGroupId, ul.EmailId,intp.IsLeadInterviewer FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId  where intp.IsApproved=1 and intp.IsLeadInterviewer=1 and intp.AssessmentGroupId like '%'+@assessmentgroupid+'%'", dp);
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
