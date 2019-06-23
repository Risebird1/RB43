using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class UserRepository: IUserRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId); // Normal parametersmeters 

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Menu>(con, "[dbo].[sp_GetMenuBasedOnEmailId]", parameters,
                        commandType: StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
               // _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

    }
}
