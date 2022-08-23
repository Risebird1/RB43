using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class AadhaarRepository : IAadhaarRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AadhaarRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<bool> SaveAadhaarDetails(AadhaarData model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Name", model.Name);
                    parameters.Add("@Address", model.Address);
                    parameters.Add("@email", model.email);
                    parameters.Add("@reference_id", model.reference_id);
                    parameters.Add("@gender", model.gender);
                    parameters.Add("@dob", model.dob);
                    parameters.Add("@doc_face", model.doc_face);
                    parameters.Add("@verified_using", model.verified_using);
                    await SqlMapper.QueryAsync(con, "usp_SaveAadhaarDetails", parameters,
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
        public async Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@MCQId", MCQId);
                    parameters.Add("@candidateEmail", candidateEmail);
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetAadhaarDetails", parameters,
                         commandType: CommandType.StoredProcedure);
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
        public async Task<string> FetchCandidateEmailidByUniqueId(string uniqueid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var uniques = SqlMapper.Query<string>(
                        con, "Select EmailId From UserList where uniqueid=@uniqueid ", parameters, commandType: System.Data.CommandType.Text);
                    return uniques.FirstOrDefault();
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
        public async Task<string> FetchUniqueIdByEmailid(string emailid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var uniques = SqlMapper.Query<string>(
                        con, "Select uniqueid From UserList where EmailId=@emailid ", parameters, commandType: System.Data.CommandType.Text);
                    return uniques.FirstOrDefault();
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
        public async Task<AadhaarData> FetchInterviewAadhaarDetails(string candidateEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@candidateEmail", candidateEmail);
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetInterviewAadhaarDetails", parameters,
                         commandType: CommandType.StoredProcedure);
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
    }
}
