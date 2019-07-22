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

namespace Risebird.Repository
{
    public class IRFRepository : IIRFRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

        public IRFRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
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
                        con, "select * from UserList where id in ("+ids+")", null, commandType: Text);
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

        public IEnumerable<Client> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client= SqlMapper.Query<Client>(
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

        public IEnumerable<InterviewerVM> GetInterviewers()
        {
            IEnumerable<InterviewerVM> interviewersVm = new List<InterviewerVM>();
            IEnumerable<Interviewer> interviewers = new List<Interviewer>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    interviewers =  SqlMapper.Query<Interviewer>(
                        con, "select * from Interviewer", null, commandType: Text);
                    var idList = new List<int>();
                    foreach(var item in interviewers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    interviewersVm = SqlMapper.Query<InterviewerVM>(
                        con, "select * from UserList where id in ("+ ids + ")", null, commandType: Text);
                    return interviewersVm;
                }
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

        public IEnumerable<Interviewer> GetInterviewerSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(
                        con, "select * from Interviewer", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Users> GetQualityController()
        {
            IEnumerable<Entities.Data.QualityController> qualityControllers = new List<Entities.Data.QualityController>();
            IEnumerable<Users> users = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    qualityControllers = SqlMapper.Query<QualityController>(
                        con, "select * from QualityController", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in qualityControllers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    users = SqlMapper.Query<Users>(
                        con, "select * from UserList where  id in (" + ids + ")", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return users;
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

        public IEnumerable<IRF> GetAllIRF()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<IRF>(
                        con, "select * from IRF", null, commandType: Text);
                    ////add skills
                    //foreach(var item in listIRF)
                    //{
                    //    var parameters = new DynamicParameters();

                    //    parameters.Add("@IRFId", item.id);

                    //    var skillName =  SqlMapper.QueryAsync<string>(con, "usp_GetIRFSkills", parameters,
                    //    commandType: CommandType.StoredProcedure);
                    //}

                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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

        public async Task<bool> AddIRFDetails(IRFVm irfDetails)
        {
            try
            {

                //string sql = "INSERT INTO IRF (IRFId, ClientName, JDTiltle, CompanyType, ClientId, JobCountry, StartDate, EndDate, JDLink, NoOfPosition, NoOfIntterview, RisebirdHr, PerMintuteRate) Values (@IRFId, @ClientName, @JDTiltle, @CompanyType, @ClientId, @JobCountry, @StartDate, @EndDate, @JDLink, @NoOfPosition, @NoOfIntterview, @RisebirdHr, @PerMintuteRate);";

                //using (var connection = new SqlConnection(connectionString))
                //{
                //    var affectedRows = connection.Execute(sql, new { IRFId = irfDetails.IRFId, ClientName = irfDetails.ClientName, JDTiltle = irfDetails.JDTiltle, CompanyType= irfDetails.CompanyType, ClientId= irfDetails.ClientId, JobCountry=irfDetails.JobCountry, StartDate = irfDetails.StartDate, EndDate = irfDetails.EndDate, JDLink = irfDetails.JDLink,
                //        NoOfPosition = irfDetails.NoOfPosition,
                //        NoOfIntterview= irfDetails.NoOfIntterview,
                //        RisebirdHr = irfDetails.RisebirdHr,
                //        PerMintuteRate = irfDetails.PerMintuteRate
                //    });
                //    if (affectedRows > 0)
                //        return true;
                //    else
                //        return false;

                //}
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForIRF(irfDetails);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                     await SqlMapper.QueryAsync<int>(con, "usp_AddIRFDetails", parameters,
                        commandType: CommandType.StoredProcedure);

                    var irfList =  SqlMapper.QueryAsync<IRFVm>(
                        con, "select * from IRF", null, commandType: Text).Result.AsList();
                    var irfID = irfList.Find(x => x.IRFId == irfDetails.IRFId).id;
                    //var irfId = SqlMapper.Query<string>(
                    //    con, "select id from IRF where IRFId = @IRFId ", new {IRFId = irfDetails.IRFId }, commandType: Text);

                    //update joinability matrix table
                    foreach (var item in irfDetails.Joinability)
                    {
                        string sqlJoinability = "INSERT INTO TransJoinability (IRFId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
                        var affectedRows = con.ExecuteAsync(sqlJoinability, new
                        {
                            IRFId = irfID,
                            LevelId = item.LevelId,
                            Type = item.Type,
                            Value = item.Value,
                            Weightage = item.Weightage
                        }).Result;
                    }


                    //update general skill table
                    foreach (var item in irfDetails.SkillGeneral)
                    {

                        string sqlSkillGeneral = "INSERT INTO TransSkillGeneral (IRFId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
                        var affectedRows = con.ExecuteAsync(sqlSkillGeneral, new
                        {
                            IRFId = irfID,
                            LevelId = item.LevelId,
                            Type = item.Type,
                            Value = item.Value,
                            Weightage = item.Weightage
                        }).Result;
                    }
                    
                    //update tech skill table
                    foreach (var item in irfDetails.SkillCollection)
                    {

                        string sqlSkillTech = "INSERT INTO TransTechSkill (IRFId, LevelId, SkillId, Weightage) Values (@IRFId, @LevelId, @SkillId, @Weightage);";
                        var affectedRows = con.Execute(sqlSkillTech, new
                        {
                            IRFId = irfID,
                            LevelId = item.LevelId,
                            SkillId = item.SkillId,
                            Weightage = item.Weightage
                        });
                    }
                    //update 
                    
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

        private DynamicParameters SetStoredProcedureParameterForIRF(IRFVm entity)
        {
            var parameters = new DynamicParameters();
            
            parameters.Add("@IRFId", entity.IRFId);
            parameters.Add("@ClientName", entity.ClientName);
            parameters.Add("@JDTiltle", entity.JDTiltle);
            parameters.Add("@CompanyType", entity.CompanyType);
            parameters.Add("@ClientId", entity.ClientId);
            parameters.Add("@JobCountry", entity.JobCountry);
            parameters.Add("@StartDate", entity.StartDate);
            parameters.Add("@EndDate", entity.EndDate);
            parameters.Add("@JDLink", entity.JDLink);
            parameters.Add("@NoOfPosition", entity.NoOfPosition);
            parameters.Add("@NoOfIntterview", entity.NoOfIntterview);
            parameters.Add("@RisebirdHr", entity.RisebirdHr);
            parameters.Add("@PerMintuteRate", entity.PerMintuteRate);
            parameters.Add("@InterviewerUserId", entity.InterviewerCollection[0]?.InterviewerId);
            //parameters.Add("@QCUserId", entity.QualityController[0].QualityControllerId);
            parameters.Add("@ClientUserId", entity.ClientCollection[0]?.ClientUserId);
            parameters.Add("@ClientRoleId", entity.ClientCollection[0]?.ClientRoleId);
            parameters.Add("@QCUserId", entity.QualityController[0]?.UserId);
            parameters.Add("@idIrf", SqlDbType.Int, direction: ParameterDirection.Output);
            
            return parameters;
        }
    }
}
