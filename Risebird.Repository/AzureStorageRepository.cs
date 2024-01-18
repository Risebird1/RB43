
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Risebird.Repository.Interface;


namespace Risebird.Repository
{
    public class AzureStorageRepository: IAzureStorageRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly string employmentconnectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public AzureStorageRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            employmentconnectionString = _configuration["ConnectionString:EmploymentVerificationConnection"];
        }

        public async Task<bool> SaveAzurStorageDetails(AzureStorageInfoModelVM model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    foreach (var container in model.BlobContainers)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@Container",container.Name);
                        parameters.Add("@MemorySpace", container.Size);
                        //parameters.Add("@Price", container.Price);
                        parameters.Add("@CreatedAt", DateTime.Now);
                        await SqlMapper.QueryAsync(con, "usp_SaveAzureStoreDetails", parameters,
                                commandType: CommandType.StoredProcedure);
                       
                    }
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
    }
}
