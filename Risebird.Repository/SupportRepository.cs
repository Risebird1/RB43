using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
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
    public class SupportRepository : ISupportRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public SupportRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<List<Support>> GetAllSupportHR(string emailId = null)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Support>(con, "usp_GetSupportBasedOnProfile", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.ToList();

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddSupportHR(Support hrDetails)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Name", hrDetails.Name);
                parameters.Add("@EmailId", hrDetails.EmailId);
                parameters.Add("@MobileNumber", hrDetails.MobileNumber);
                parameters.Add("@WhatsappNumber", hrDetails.WhatsappNumber);
                parameters.Add("@AvailabilityHours", hrDetails.AvailabilityHours);
                parameters.Add("@OnVoiceCall", hrDetails.OnVoiceCall);
                parameters.Add("@OnWhatsapp", hrDetails.OnWhatsapp);
                parameters.Add("@OnEmail", hrDetails.OnEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddSupportHR", parameters,
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
        public async Task<Support> GetSupportHR(int id)
        {
            try
            {
                var query = "select * from Support where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<Support>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> UpdateSupportHR(Support hrDetails)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", hrDetails.id);
                parameters.Add("@Name", hrDetails.Name);
                parameters.Add("@EmailId", hrDetails.EmailId);
                parameters.Add("@MobileNumber", hrDetails.MobileNumber);
                parameters.Add("@WhatsappNumber", hrDetails.WhatsappNumber);
                parameters.Add("@AvailabilityHours", hrDetails.AvailabilityHours);
                parameters.Add("@OnVoiceCall", hrDetails.OnVoiceCall);
                parameters.Add("@OnWhatsapp", hrDetails.OnWhatsapp);
                parameters.Add("@OnEmail", hrDetails.OnEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateSupportHR", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public async Task<bool> DeleteSupportHR(int id)
        {
            try
            {
                var query = "delete from Support where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<List<string>> GetAllSupportHREmails()
        {
            try
            {
                //var query = "Select * from Support";
                var query = "Select EmailId from support";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<string>(query);
                    return results.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> SaveRaisedIssueDetails(RaiseBugsVM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RequestType", model.RequestType);
                parameters.Add("@Comment", model.Comment);
                parameters.Add("@AttachmentURL", model.AttachmentURL);
                parameters.Add("@RequestedBy", model.RequestedBy);
                parameters.Add("@RequestedOn", DateTime.Now);
                parameters.Add("@Requester", model.Requester);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_SaveRaisedIssueDetails", parameters,
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
        public async Task<IEnumerable<RaiseBugsVM>> GetRaisedIssueDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<RaiseBugsVM>(con, "usp_GetRaisedIssues", null,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> UpdateRaisedRequestStatus(RaiseBugsVM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", model.id);
                parameters.Add("@Comment", model.Comment);
                parameters.Add("@RequestedBy", model.RequestedBy);
                parameters.Add("@RequestedOn", DateTime.Now);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateRaisedIssueDetails", parameters,
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
