using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Risebird.Repository
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public SettingsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public Settings GetAllSettings()
        {
            try
            {
                var query = "Select * from Settings";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Settings>(query);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public bool UpdateSettings(Settings settings)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewerUnassignedMinutes", settings.InterviewerUnassignedMinutes);
                parameters.Add("@AutoCancellationMailMinutes", settings.AutoCancellationMailMinutes);
                parameters.Add("@ClientReportingMailMinutes", settings.ClientReportingMailMinutes);
                parameters.Add("@InterviewWindupReminderMinutes", settings.InterviewWindupReminderMinutes);
                parameters.Add("@InterviewReportReminderMinutes", settings.InterviewReportReminderMinutes);
                //parameters.Add("@QCCommentReminderMinutes", settings.QCCommentReminderMinutes);
                parameters.Add("@QCFormdeductionRate", settings.QCFormdeductionRate);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_UpdateSettings", parameters,
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

        public int GetInterviewUnassignedMinutes()
        {
            try
            {
                var query = "Select InterviewerUnassignedMinutes from Settings";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }

        }


    }
}
