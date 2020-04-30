using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Data;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;

namespace Risebird.Repository
{
    public class InterviewerSkillsRepository : IInterviewerSkillsRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

        public InterviewerSkillsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<IEnumerable<InterviewerSkillsModel>> GetAllInterviewerSkills(string emailID, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<InterviewerSkillsModel>(con, "[dbo].[usp_GetAllInterviewerSkills]", parameters,
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
    }
}
