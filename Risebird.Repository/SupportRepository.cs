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

        public IEnumerable<Support> GetAllSupportHR(string emailId = null)
        {
            //try
            //{
            //    //var query = "Select * from Support";
            //    var query = "Select st.id,st.Name,st.EmailId,st.MobileNumber,st.WhatsappNumber,st.OnVoiceCall,st.OnWhatsapp,st.OnEmail,st.AvailabilityHours,1 from support st";
            //    using (SqlConnection con = new SqlConnection(connectionString))
            //    {
            //        var results = con.Query<Support>(query);
            //        return results;
            //    }

            //}
            //catch (SqlException ex)
            //{
            //    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            //}

            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result  = SqlMapper.QueryAsync<Support>(con, "usp_GetSupportBasedOnProfile", parameters,
                        commandType: CommandType.StoredProcedure).Result;
                    return result;

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
        public Support GetSupportHR(int id)
        {
            try
            {
                var query = "select * from Support where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<Support>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public bool UpdateSupportHR(Support hrDetails)
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
                    SqlMapper.QueryAsync(con, "usp_UpdateSupportHR", parameters,
                        commandType: CommandType.StoredProcedure);
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

        public bool DeleteSupportHR(int id)
        {
            try
            {
                var query = "delete from Support where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public IEnumerable<string> GetAllSupportHREmails()
        {
            try
            {
                //var query = "Select * from Support";
                var query = "Select EmailId from support";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query);
                    return results;
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
