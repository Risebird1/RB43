using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using static System.Data.CommandType;
using System.Threading.Tasks;
using System.Data;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;

namespace Risebird.Repository
{
    public class InterviewerSkillsRepository : IInterviewerSkillsRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

                    var irfList = SqlMapper.QueryAsync<InterviewerSkillsVM>(
                        con, "select * from [dbo].[InterviewerSkillsChannel] orderby ", null, commandType: Text).Result.AsList();
                    var InterviewerSkillId = irfList.Where(x => x.InterviewerSkillId == InterviewerSkillsDetails.InterviewerSkillId).Last().id;//irfList.Find(x => x.IRFId == InterviewerSkillsDetails.IRFId).id;



                    foreach (var item in InterviewerSkillsDetails.Selectability)
                    {
                        string sqlSelectability = "INSERT INTO TransInterviewerSkillsSelectablity (InterviewerSkillId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
                        var affectedRows = con.ExecuteAsync(sqlSelectability, new
                        {
                            IRFId = InterviewerSkillId,
                            LevelId = item.LevelId,
                            Type = item.Type,
                            Value = item.Value,
                            Weightage = item.Weightage
                        }).Result;
                    }


                    //update general skill table
                    foreach (var item in InterviewerSkillsDetails.SkillGeneral)
                    {

                        string sqlSkillGeneral = "INSERT INTO TransInterviewerSkillsGeneralSkills (InterviewerSkillId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
                        var affectedRows = con.ExecuteAsync(sqlSkillGeneral, new
                        {
                            IRFId = InterviewerSkillId,
                            LevelId = item.LevelId,
                            Type = item.Type,
                            Value = item.Value,
                            Weightage = item.Weightage
                        }).Result;
                    }

                    //update tech skill table
                    foreach (var item in InterviewerSkillsDetails.SkillCollection)
                    {
                        string sqlSkillTech = "INSERT INTO TransInterviewerSkillsTechSkills (InterviewerSkillId, LevelId, SkillId, Weightage) Values (@IRFId, @LevelId, @SkillId, @Weightage);";
                        var affectedRows = con.Execute(sqlSkillTech, new
                        {
                            IRFId = InterviewerSkillId,
                            LevelId = item.LevelId,
                            SkillId = item.SkillId,
                            Weightage = item.Weightage
                        });
                    }

                    //update trans interviewer table
                    foreach (var item in InterviewerSkillsDetails.InterviewerCollection)
                    {
                        DynamicParameters param = new DynamicParameters();
                        param.Add("@InterviewerSkillId", InterviewerSkillId);
                        param.Add("@InterviewerUserId", item.UserId);
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

        private DynamicParameters SetStoredProcedureParameterForInterviewerSkills(InterviewerSkillsVM entity)
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Interviewer> GetInterviewerSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(
                        con, "select Interviewer.*, UserList.[EmailId] from Interviewer inner join UserList on Interviewer.UserId = UserList.id inner join InterviewerProfile on  InterviewerProfile.UserId = UserList.id where InterviewerProfile.IsLeadInterviewer=1", null, commandType: Text);
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
                    return SqlMapper.Query<Interviewer>(
                        con, "select inv.UserId, inv.PrimarySkill, UserList.[EmailId] from Interviewer inv inner join UserList on inv.UserId = UserList.id inner join InterviewerProfile IP  on IP.UserId = UserList.id where IP.IsLeadInterviewer=1", null, commandType: Text);
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
        public bool CheckUniqueness(string value)
        {
            try
            {
                var query = "Select * from InterviewerSkillsChannel where InterviewerSkillId = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", value);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewerSkillsVM>(query, dynamicParameters);
                    if (results.Count() > 0)
                        return false;
                    else
                        return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public IEnumerable<TransInterviewerSkillsofGeneralSkill> GetSkillsGeneralbyInterviewerSkillId(int id)
        {
            try
            {
                var query = "Select * from TransInterviewerSkillsGeneralSkills where InterviewerSkillId = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransInterviewerSkillsofGeneralSkill>(query, dynamicParameters);
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
                param.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransInterviewerSkillsLeadInterviewer>(con, "usp_GetTransInterviewerSkillsLeadInterviewer", param,
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
        public IEnumerable<TransInterviewerSkillsSelectability> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select * from TransInterviewerSkillsSelectablity where InterviewerSkillId = @InterviewerSkillId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransInterviewerSkillsSelectability>(query, dynamicParameters);
                    return results;
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
                DynamicParameters parameters = SetStoredProcedureParameterForInterviewerSkills(InterviewerSkillsDetails);
                
                parameters.Add("@id", InterviewerSkillsDetails.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerSkillsChannel", parameters,
                       commandType: CommandType.StoredProcedure);

                    if (InterviewerSkillsDetails.Selectability != null)
                    {
                        foreach (var item in InterviewerSkillsDetails.Selectability)
                        {
                            string sqlSelectability = "INSERT INTO TransInterviewerSkillsSelectablity (InterviewerSkillId, LevelId, Type, Value, Weightage) Values (@InterviewerSkillId, @LevelId, @Type, @Value, @Weightage);";
                            var affectedRows = con.ExecuteAsync(sqlSelectability, new
                            {
                                InterviewerSkillId = InterviewerSkillsDetails.id,
                                LevelId = item.LevelId,
                                Type = item.Type,
                                Value = item.Value,
                                Weightage = item.Weightage
                            }).Result;
                        }
                    }
                    //update general skill table

                    if (InterviewerSkillsDetails.SkillGeneral != null)
                    {
                        foreach (var item in InterviewerSkillsDetails.SkillGeneral)
                        {

                            string sqlSkillGeneral = "INSERT INTO TransInterviewerSkillsGeneralSkills (InterviewerSkillId, LevelId, Type, Value, Weightage) Values (@InterviewerSkillId, @LevelId, @Type, @Value, @Weightage);";
                            var affectedRows = con.ExecuteAsync(sqlSkillGeneral, new
                            {
                                InterviewerSkillId = InterviewerSkillsDetails.id,
                                LevelId = item.LevelId,
                                Type = item.Type,
                                Value = item.Value,
                                Weightage = item.Weightage
                            }).Result;
                        }
                    }



                    //update tech skill table
                    if (InterviewerSkillsDetails.SkillCollection != null)
                    {
                        foreach (var item in InterviewerSkillsDetails.SkillCollection)
                        {

                            string sqlSkillTech = "INSERT INTO TransInterviewerSkillsTechSkills (InterviewerSkillId, LevelId, SkillId, Weightage) Values (@InterviewerSkillId, @LevelId, @SkillId, @Weightage);";
                            var affectedRows = con.Execute(sqlSkillTech, new
                            {
                                InterviewerSkillId = InterviewerSkillsDetails.id,
                                LevelId = item.LevelId,
                                SkillId = item.SkillId,
                                Weightage = item.Weightage
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
        public async Task<bool> DeleteInterviewerSkills(int InterviewerSkillId)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewerSkillId", InterviewerSkillId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteInterviewerSkills", parameters,
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
        //public async Task<bool> UnArchiveInterviewerSkills(int IRFId)
        //{
        //    try
        //    {
        //        var query = "update IRF set IsDeleted=0 where id = @IRFId;";
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@IRFId", IRFId);

        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var results = con.QueryAsync<int>(query, dynamicParameters);
        //            return true;
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return false;
        //    }

        //}

        public IEnumerable<InterviewerSkillsVM> GetInterviewerSkillsInterviewer(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewerSkillsVM>(con, "sp_RisebirdInterviewerForInterviewerSkills", parameters,
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
        public bool AddInterviewer(int id, bool IsChecked)
        {
            try
            {
                var query = "insert into [dbo].[TransInterviewerListForSkills] (UserId,InterviewerSkillId,IsApproved) Values(" + 1 + "," + id + "," + IsChecked + ") ";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.ExecuteAsync(query, null).Result;
                }
                return true;

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
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
                            var results = con.ExecuteAsync(query, new
                            {
                                UserId = item.UserId,
                                PrimarySkill = item.PrimarySkill,
                                InterviewerSkillId=item.InterviewerSkillId,
                                IsAdded = item.IsAdded
                            }).Result;
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
        public async Task<IEnumerable<InterviewerSkillsVM>> GetUnApprovedInterviewerList(int id)
        {

            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewerSkillId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewerSkillsVM>(con, "sp_UnApprovedInterviewerForInterviewerSkills", null,
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

        public async Task<bool> DeleteInterviewerList(int id)
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

    }
}
