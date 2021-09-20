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
    public class ClientRepository : IClientRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ClientRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public IEnumerable<Client> GetClients(string emailid, string profilename)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (profilename == "SeniorHR")
                    {
                        DynamicParameters dp = new DynamicParameters();
                        dp.Add("@Emailid", emailid);
                        return SqlMapper.Query<Client>(
                            con, "Select cl.* From SeniorHR sh join Client cl on cl.id=sh.Clientid join Userlist ul on ul.id=sh.userid where ul.Emailid=@Emailid", dp, commandType: Text);
                    }
                    else
                    {
                        return SqlMapper.Query<Client>(
                            con, "select * from Client", null, commandType: Text);
                    }
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

        public async Task<bool> AddClient(Client client)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForClient(client);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddClient", parameters,
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

        public async Task<Client> GetClient(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    Client client = await SqlMapper.QuerySingleAsync<Client>(con, "[usp_GetClientById]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return client;
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

        public async Task<bool> UpdateClient(Client entity)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForClient(entity);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "[usp_UpdateClient]", parameters,
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

        public async Task<bool> DeleteClient(int id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "[usp_DeleteClient]", parameters,
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

        public async Task<IEnumerable<ClientRole>> GetClientRoles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<ClientRole>(con, "[sp_GetClientRoles]", null,
                        commandType: CommandType.StoredProcedure);
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

        private DynamicParameters SetStoredProcedureParameterForClient(Client entity)
        {
            var parameters = new DynamicParameters();

            if (entity.id > 0)
                parameters.Add("@id", entity.id);

            parameters.Add("@clientName", entity.Name);
            parameters.Add("@clientDescr", entity.Description);
            parameters.Add("@GST", entity.GST);
            parameters.Add("@officeAddress", entity.OfficeAddress);
            parameters.Add("@Createdby", entity.Createdby);
            parameters.Add("@LogoLink", entity.LogoLink);
            parameters.Add("@CompanyLink", entity.CompanyLink);
            parameters.Add("@CompanyVideoLink", entity.CompanyVideoLink);
            return parameters;
        }
        public async Task<string> AddClientJd(ClientJdVM _client)
        {
            try
            {
                int userid = 0;
                var query = "Select id from UserList where Emailid = @Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", _client.CreatedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    userid = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                }
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", _client.id);
                parameters.Add("@StartDate", _client.StartDate);
                parameters.Add("@EndDate", _client.EndDate);
                parameters.Add("@AssessmentGroup", _client.AssessmentGroup);
                parameters.Add("@JobDescription", _client.JobDescription);
                parameters.Add("@CreatedBy", userid);
                parameters.Add("@ClientId", _client.ClientId);
                parameters.Add("@JDTitle", _client.JDTitle);
                parameters.Add("@Filelink", _client.Filelink);

                parameters.Add("@Heuristicsscore", _client.Heuristicsscore);
                parameters.Add("@JDBudget", _client.JDBudget);
                parameters.Add("@JobRole", _client.JobRole);
                parameters.Add("@Location", _client.Location);
                parameters.Add("@ClientUrl", _client.ClientUrl);
                parameters.Add("@persuccessrate", _client.persuccessrate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "[usp_AddClientJD]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return "";
        }
        public IEnumerable<ClientJdVM> FetchClientJd(int clientid)
        {
            try
            {
                if (clientid == 0)
                {
                    var query = "Select ul.Emailid as ClientEmail,c.Name,cl.*,cl.AssessmentGroupid as AssessmentGroup, ag.AssessmentGroup as AssessmentGroupName from Clientjd cl join AssessmentGroup ag on cl.assessmentgroupid=ag.id join Client c on c.id=cl.Clientid left join UserList ul on ul.id=cl.Createdby";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var result = con.Query<ClientJdVM>(query, null);
                        return result;
                    }
                }
                else
                {
                    //Set the parameters to be passed to store procedure
                    var query = "Select cl.*,cl.AssessmentGroupid as AssessmentGroup,ag.AssessmentGroup as AssessmentGroupName from Clientjd cl join AssessmentGroup ag on cl.assessmentgroupid=ag.id where Clientid=@ClientId";
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@ClientId", clientid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var result = con.Query<ClientJdVM>(query, parameters);
                        return result;
                    }
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
        public string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore, string candidateProfile)
        {
            try
            {
                if (candidateProfile == "AddCandidate")
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        string sqlDeletePrevious = "Delete from CandidateJDScore where ClientJdid =@IRFId;";
                        var deletedRows = con.Execute(sqlDeletePrevious, new
                        {
                            IRFId = IrfId,

                        });
                        foreach (var item in candidateCuratedScore)
                        {

                            string sqlCuratedCandidate = "INSERT INTO CandidateJDScore (ClientJdid,IRFId, CandidateEmailId, CuratedEmailId,Score) Values (@ClientJdid,@IRFId, @CandidateEmailId, @CuratedEmailId,@Score);";
                            var affectedRows = con.Execute(sqlCuratedCandidate, new
                            {
                                ClientJdid = IrfId,
                                IRFId = item.IRFDetailsID,
                                item.CandidateEmailId,
                                item.CuratedEmailId,
                                item.Score

                            });
                        }
                    }
                }
                else
                {
                    foreach (var item in candidateCuratedScore)
                    {
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            string sqlCuratedCandidate = "Update CandidateJDScore set Priority=@Priority where Id=@Id";
                            var affectedRows = con.Execute(sqlCuratedCandidate, new
                            {
                                Priority = item.Priority,
                                Id = item.IRFId
                            });
                        }
                    }
                }
                var query = "Select EmailId from ClientJd cj join UserList ul on cj.Createdby=ul.id where cj.id=@ClientId ";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ClientId", IrfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, parameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public IEnumerable<CandidateCuratedScore> GetCandidateJDScore(int id)
        {
            try
            {
                var query = "Select cjs.id as IRFID,cjs.ClientJdid,cjs.IRFId as IRFDetailsID,cjs.CandidateEmailId,cjs.Score,cjs.Priority,cj.JDTitle from CandidateJDScore cjs join ClientJd cj on  cj.id=cjs.ClientJdid where ClientJdid = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<CandidateCuratedScore>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Wallet> FetchClientWallet(int clientid)
        {
            try
            {
                var query = "select * from ClientWallet where Clientid=@clientid order by CreatedOn desc";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@clientid", clientid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Wallet>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> AddWalletPoints(int ClientId, string emailid, int points, string comment)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@ClientId", ClientId);
                parameters.Add("@emailid", emailid);
                parameters.Add("@points", points);
                parameters.Add("@comment", comment);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "[usp_AddWalletPoints]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "failed";
        }
        public string ResetCandidate(int Clientjdid, string candidateEmail, string emailid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Clientjdid", Clientjdid);
                parameters.Add("@candidateEmailid", candidateEmail);
                parameters.Add("@emailid", emailid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "[Usp_resetCandidateDetails]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public string DeleteClientJd(int assessmentid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string sqlCuratedCandidate = "Delete from CandidateJDScore where ClientJdid=@Id";
                    var affectedRows = con.Execute(sqlCuratedCandidate, new
                    {
                        Id = assessmentid
                    });
                    string sqljd = "Delete from clientjd where id=@Id";
                    var affectedRow = con.Execute(sqljd, new
                    {
                        Id = assessmentid
                    });
                    return "Success";
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }

        public async Task<int> GetClientRoleByEmailId(string emailId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_GetClientRoleByEmailId]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<int> GetUniqueClientRoleByEmailId(string emailId, string id)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@uniqueid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_GetuniqueClientRoleByEmailId]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<bool> PublicClientUrlSave(string emailId, string uniqueid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_SaveClientPublicURL]", parameters,
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
    }
}
