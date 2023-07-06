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

        public async Task<IEnumerable<Client>> GetClients(string emailid, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        DynamicParameters dp = new DynamicParameters();
                        dp.Add("@Emailid", emailid);
                        return await SqlMapper.QueryAsync<Client>(
                            con, "Select cl.* From SeniorHR sh join Client cl on cl.id=sh.Clientid join Userlist ul on ul.id=sh.userid where ul.Emailid=@Emailid", dp, commandType: Text);
                    }
                    else
                    {
                        return await SqlMapper.QueryAsync<Client>(
                            con, "select * from Client", null, commandType: Text);
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
                    Client client = await SqlMapper.QueryFirstAsync<Client>(con, "[usp_GetClientById]", parameters,
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
            parameters.Add("@IsPasswordReset", entity.IsPasswordReset);
            parameters.Add("@IsCandidateavailability", entity.IsCandidateavailability);
            parameters.Add("@IsMeetingNotificationSend", entity.IsMeetingNotificationSend);
            parameters.Add("@PerInterviewRate", entity.PerInterviewRate);
            parameters.Add("@BillingType", entity.BillingType);
            parameters.Add("@InterviewDuration", entity.InterviewDuration);
            parameters.Add("@IsMeetingAutoCancelled", entity.IsMeetingAutoCancelled);
            parameters.Add("@IsQCRequired", entity.IsQCRequired);
            parameters.Add("@IsRedoInterview", entity.IsRedoInterview);
            parameters.Add("@IsClientCancelInterview", entity.IsClientCancelInterview);
            parameters.Add("@PaymentTerm", entity.PaymentTerm);
            if (entity.GSTApplicable == 1)
            {
                parameters.Add("@GSTApplicable", true);
            }
            else
            {
                parameters.Add("@GSTApplicable", false);
            }
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
                    userid = await con.QueryFirstOrDefaultAsync<int>(query, dynamicParameters);
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
        public async Task<IEnumerable<ClientJdVM>> FetchClientJd(int clientid)
        {
            try
            {
                if (clientid == 0)
                {
                    var query = "Select ul.Emailid as ClientEmail,c.Name,cl.*,cl.AssessmentGroupid as AssessmentGroup, ag.AssessmentGroup as AssessmentGroupName from Clientjd cl join AssessmentGroup ag on cl.assessmentgroupid=ag.id join Client c on c.id=cl.Clientid left join UserList ul on ul.id=cl.Createdby";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var result = await con.QueryAsync<ClientJdVM>(query, null);
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
                        var result = await con.QueryAsync<ClientJdVM>(query, parameters);
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
        public async Task<Tuple<int, string>> GetClientRoleAndCompanyNameByEmailId(string emailId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "[usp_GetClientRoleAndCompanyNameByEmailId]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return new Tuple<int, string>(result.FirstOrDefault().ClientId, result.FirstOrDefault().Name);
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
        public async Task<string> PublicClientUrlSave(string emailId, string uniqueid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "[usp_SaveClientPublicURL]", parameters,
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
        public async Task<string> PublicInterviewerUrlSave(string emailId, string uniqueid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "[usp_SaveInterviewerPublicURL]", parameters,
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
        public async Task<string> Fetchlogoforclient(string emailId, string id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "[usp_FetchlogoforclientByEmailId]", parameters,
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
        public async Task<IEnumerable<IRF>> GetRecruiterReportByClient(string emailID, DateTime st, DateTime se)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@st", st);
                    parameters.Add("@se", se);
                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetClientIRFCountDetails", parameters, commandTimeout: 600,
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
        public async Task<int> GetClientByEmailId(string emailId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_GetClientByEmailId]", parameters,
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
        public async Task<bool> UpdateRecruiterStatus(int userid, int clientid, int Irfid, bool status)
        {
            try
            {
                var query = "update tc set tc.isActive=@Status from TransClientUser tc where tc.ClientUserId=@Userid and tc.IRFId=@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Userid", userid);
                dynamicParameters.Add("@Status", status);
                dynamicParameters.Add("@IRFId", Irfid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<int> GetClientBillingType(int Clientid)
        {
            try
            {
                var query = "Select ISNULL(BillingType,1) From Client where id=@Clientid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Clientid", Clientid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
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
            return 0;
        }

        public async Task<IEnumerable<Client>> GetAllClients()
        {
            try
            {
                //Set the parameters to be passed to store procedure

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Client>(con, "usp_GetAllClients",
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

        public async Task<IEnumerable<ClientCommentsForReport>> GetClientCommentsForReport(int interviewId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientCommentsForReport>(con, "[usp_GetClientCommentsForReport]", parameters,
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

        public async Task<bool> AddUpdateClientCommentsForReport(ClientCommentsForReport model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Comment", model.Comment);
                parameters.Add("@id", model.id);
                parameters.Add("@InterviewId", model.InterviewId);
                parameters.Add("@CreatedBy", model.CreatedBy);
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@Rating", model.Rating);
                parameters.Add("@ClientActionOnInterviewer", model.ClientActionOnInterviewer);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "[usp_AddUpdateClientCommentsForReport]", parameters,
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

        public async Task<IEnumerable<ClientAudioInstructions>> GetClientAudioForIRF(int interviewId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientAudioInstructions>(con, "[usp_GetClientAudioForIRF]", parameters,
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
        public async Task<IEnumerable<ClientAudioInstructions>> GetClientInstructionsAudioForReport(int interviewId, string emailId = null, string profile = null)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", interviewId);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientAudioInstructions>(con, "[usp_GetClientInstructionsAudioForReport]", parameters,
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
        public async Task<List<Clientaudiofeedback>> Getclientaudiofeedbackdetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Clientaudiofeedback>(con, "usp_Fetchclientaudiofeedbackdetails", null,
                   commandType: CommandType.StoredProcedure);
                    return (List<Clientaudiofeedback>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> GetClientName(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@id", id);
                    var result = await SqlMapper.QueryAsync<string>(
                        con, "select [Name] from Client where id = @id", dp, commandType: Text);
                    return result?.FirstOrDefault();
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
        public async Task<string> GetClientLogoByIRFId(int irfId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@irfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<string>(con, "[usp_GetClientLogoByIRFId]", parameters,
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

        public async Task<IEnumerable<StatsVM>> GetStaffingAgencyStats(int? irfId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    return await SqlMapper.QueryAsync<StatsVM>(con, "dbo.usp_GetStaffingAgencyStats", parameters, commandTimeout: 600,
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

        public async Task<IEnumerable<StatsVM>> GetClientHRStats(int? irfId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    return await SqlMapper.QueryAsync<StatsVM>(con, "dbo.usp_GetClientHRStats", parameters, commandTimeout: 600,
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

        public async Task<IEnumerable<StatsDetailsVM>> GetClientRoleWiseStatsDetails(int clientUserId, string type, string clientRole, int? irfId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@clientUserId", clientUserId);
                    parameters.Add("@roleType", clientRole);
                    parameters.Add("@type", type);
                    parameters.Add("@startDateValue", startDate);
                    parameters.Add("@endDateValue", endDate);
                    return await SqlMapper.QueryAsync<StatsDetailsVM>(con, "dbo.usp_GetClientRoleWiseStatsDetails", parameters, commandTimeout: 600,
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

        public async Task<IEnumerable<Meeting>> GetDateRangeWiseClientMeetings(DateTime startDate, DateTime endDate, string emailId, int clientId)//, string emailId, string clientName
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@startDateValue", startDate);
                parameters.Add("@endDateValue", endDate);
                parameters.Add("@clientId", clientId);
                parameters.Add("@emailId", emailId);
                //parameters.Add("@clientName", clientName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetDateRangeWiseClientMeetings", parameters,
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

        //public async Task<ClientRoleWiseStatsWrapper> GetDateRangeWiseMeetingStatusForClientRoles(DateTime startDate, DateTime endDate, string emailId, int clientId)
        //{
        //    try
        //    {
        //        var parameters = new DynamicParameters();
        //        parameters.Add("@startDateValueParam", startDate);
        //        parameters.Add("@endDateValueParam", endDate);
        //        parameters.Add("@clientIdParam", clientId);
        //        parameters.Add("@emailIdParam", emailId);
        //        parameters.Add("@timeNowParam", DateTime.Now);
        //        //parameters.Add("@clientName", clientName);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var result = await SqlMapper.QueryAsync<StatsDerived>(con, "usp_GetDateRangeWiseMeetingStatusForClientRoles", parameters,
        //                commandType: CommandType.StoredProcedure);
        //            ClientRoleWiseStatsWrapper _resultVM = new ClientRoleWiseStatsWrapper();
        //            _resultVM.ClientHRStats =(List<ClientHRStatsDerived>)result.Where(x => x.ClientRole == "ClientHR");
        //            _resultVM.StaffingAgencyStats =(List<StaffingAgencyStatsDerived>)result.Where(x => x.ClientRole == "StaffingAgency");

        //            return _resultVM;
        //        }

        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return null;
        //}

        public async Task<ClientRoleWiseStatsWrapper> GetDateRangeWiseMeetingStatusForClientRoles(DateTime startDate, DateTime endDate, string emailId, int clientId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    ClientRoleWiseStatsWrapper _resultVM = new ClientRoleWiseStatsWrapper();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetDateRangeWiseMeetingStatusForClientRoles", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@emailId", emailId);
                    adapter.SelectCommand.Parameters.AddWithValue("@startDateValue", startDate);
                    adapter.SelectCommand.Parameters.AddWithValue("@endDateValue", endDate);
                    adapter.SelectCommand.Parameters.AddWithValue("@clientId", clientId);
                    adapter.SelectCommand.Parameters.AddWithValue("@timeNow", DateTime.Now);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var listClientHRMeetings = new List<ClientHRStatsDerived>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listClientHRMeetings.Add(new ClientHRStatsDerived
                                {
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    //IRFnSkill = !string.IsNullOrEmpty(dt.Rows[i]["IRFnSkill"].ToString()) ? dt.Rows[i]["IRFnSkill"].ToString() : null,
                                    UserId = !string.IsNullOrEmpty(dt.Rows[i]["UserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["UserId"]) : 0,
                                    TotalMeetings = !string.IsNullOrEmpty(dt.Rows[i]["TotalMeetings"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalMeetings"]) : 0,
                                    TotalCompletedMeetings = !string.IsNullOrEmpty(dt.Rows[i]["TotalCompletedMeetings"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalCompletedMeetings"]) : 0,
                                    TotalSelected = !string.IsNullOrEmpty(dt.Rows[i]["TotalSelected"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalSelected"]) : 0,
                                    TotalRejected = !string.IsNullOrEmpty(dt.Rows[i]["TotalRejected"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalRejected"]) : 0,
                                    TotalBorderLine = !string.IsNullOrEmpty(dt.Rows[i]["TotalBorderLine"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalBorderLine"]) : 0,
                                    TotalProxy = !string.IsNullOrEmpty(dt.Rows[i]["TotalProxy"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalProxy"]) : 0,
                                    TotalDropped = !string.IsNullOrEmpty(dt.Rows[i]["TotalDropped"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalDropped"]) : 0,
                                    TotalNoShow = !string.IsNullOrEmpty(dt.Rows[i]["TotalNoShow"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalNoShow"]) : 0,
                                    StatusPending = !string.IsNullOrEmpty(dt.Rows[i]["StatusPending"].ToString()) ? Convert.ToInt32(dt.Rows[i]["StatusPending"]) : 0,
                                    Scheduled = !string.IsNullOrEmpty(dt.Rows[i]["Scheduled"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Scheduled"]) : 0


                                });
                            }
                            _resultVM.ClientHRStats = listClientHRMeetings;
                        }

                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            var listStaffingAgencyMeetings = new List<StaffingAgencyStatsDerived>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                listStaffingAgencyMeetings.Add(new StaffingAgencyStatsDerived
                                {
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    //IRFnSkill = !string.IsNullOrEmpty(dt.Rows[i]["IRFnSkill"].ToString()) ? dt.Rows[i]["IRFnSkill"].ToString() : null,
                                    UserId = !string.IsNullOrEmpty(dt.Rows[i]["UserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["UserId"]) : 0,
                                    TotalMeetings = !string.IsNullOrEmpty(dt.Rows[i]["TotalMeetings"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalMeetings"]) : 0,
                                    TotalCompletedMeetings = !string.IsNullOrEmpty(dt.Rows[i]["TotalCompletedMeetings"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalCompletedMeetings"]) : 0,
                                    TotalSelected = !string.IsNullOrEmpty(dt.Rows[i]["TotalSelected"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalSelected"]) : 0,
                                    TotalRejected = !string.IsNullOrEmpty(dt.Rows[i]["TotalRejected"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalRejected"]) : 0,
                                    TotalBorderLine = !string.IsNullOrEmpty(dt.Rows[i]["TotalBorderLine"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalBorderLine"]) : 0,
                                    TotalProxy = !string.IsNullOrEmpty(dt.Rows[i]["TotalProxy"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalProxy"]) : 0,
                                    TotalDropped = !string.IsNullOrEmpty(dt.Rows[i]["TotalDropped"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalDropped"]) : 0,
                                    TotalNoShow = !string.IsNullOrEmpty(dt.Rows[i]["TotalNoShow"].ToString()) ? Convert.ToInt32(dt.Rows[i]["TotalNoShow"]) : 0,
                                    StatusPending = !string.IsNullOrEmpty(dt.Rows[i]["StatusPending"].ToString()) ? Convert.ToInt32(dt.Rows[i]["StatusPending"]) : 0,
                                    Scheduled = !string.IsNullOrEmpty(dt.Rows[i]["Scheduled"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Scheduled"]) : 0


                                });
                            }
                            _resultVM.StaffingAgencyStats = listStaffingAgencyMeetings;
                        }

                    }

                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<StatsDerived>> GetDateRangeWiseMeetingStatusForClientByIRF(DateTime startDate, DateTime endDate, string emailId, int clientId, string clientUserEmailId, string role = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@startDateValue", startDate);
                parameters.Add("@endDateValue", endDate);
                parameters.Add("@clientId", clientId);
                parameters.Add("@emailId", emailId);
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@clientUserEmailId", clientUserEmailId);
                parameters.Add("@role", role);
                //parameters.Add("@clientName", clientName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<StatsDerived>(con, "usp_GetDateRangeWiseMeetingStatusForClientUserByIRF", parameters,
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
    }
}
