using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class MeetingInviteRepository : IMeetingInviteRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string dbconnectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public MeetingInviteRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            dbconnectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        /// <summary>
        /// Check if any Microsoft Team account is avialble and fetch account email
        /// </summary>
        /// <param name="starttime"></param>
        /// <param name="endtime"></param>
        /// <returns></returns>

        public async Task<string> CheckMeetingAccount(DateTime starttime, DateTime endtime)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startdate", starttime);
                    parameters.Add("@enddate", endtime);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_CheckOnlineMettings", parameters,
                            commandType: CommandType.StoredProcedure);
                    if (result != null && result.Count() > 0)
                    {
                        return (result.FirstOrDefault());
                    }
                    else
                        return "";
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }

        /// <summary>
        /// Insert MeetingInvite details
        /// </summary>
        /// <param name="starttime"></param>
        /// <param name="endtime"></param>
        /// <param name="Emailaddress"></param>
        /// <param name="Accountemail"></param>
        /// <param name="Linkurl"></param>
        /// <param name="whom"></param>
        /// <param name="Subject"></param>
        /// <returns></returns>
        public async Task<string> InsertMeetingInvite(DateTime starttime, DateTime endtime, string Emailaddress, string Accountemail, string Linkurl, string whom, string Subject)
        {
            try
            {
                string connectionString = dbconnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startdate", starttime);
                    parameters.Add("@enddate", endtime);
                    parameters.Add("@CreatedBy", Emailaddress);
                    parameters.Add("@AccountEmail", Accountemail);
                    parameters.Add("@LinkUrl", Linkurl);
                    parameters.Add("@AttendesMails", whom);
                    parameters.Add("@Subject", Subject);

                    var result = await SqlMapper.QueryAsync(con, "usp_InsertOnlinemeeting", parameters,
                            commandType: CommandType.StoredProcedure);
                    return "sucess";
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }

    }
}
