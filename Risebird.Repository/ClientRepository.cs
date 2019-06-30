using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class ClientRepository: IClientRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        public ClientRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public IEnumerable<Client> GetClients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Client>(
                        con, "select * from Client", null, commandType: Text);
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
