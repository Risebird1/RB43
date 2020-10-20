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
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class RisebirdPayRepository : IRisebirdPayRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RisebirdPayRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<string> DeductWalletPoints(int ClientId, string emailid, int points, string comment)
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
                    await SqlMapper.QueryAsync(con, "[usp_DeductWalletPoints]", parameters,
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
    }
}
