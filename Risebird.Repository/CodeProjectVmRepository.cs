using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// Unlike most repositories, writes here throw on SqlException: the provisioner must not
    /// believe a step was recorded when it was not, or it would leak an Azure resource.
    /// </summary>
    public class CodeProjectVmRepository : ICodeProjectVmRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeProjectVmRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<string> Add(CodeProjectVm vm)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<string>(con, "usp_AddCodeProjectVm", new
                {
                    vm.Handle,
                    vm.SessionId,
                    vm.QuestionId,
                    vm.ResourceGroupName,
                    vm.Location,
                    vm.VmName,
                    vm.NicName,
                    vm.StackImageCode,
                    vm.GalleryImageId,
                    vm.OsType,
                    vm.ProjectZipUrl,
                    vm.ReadOnlyPathsJson,
                    vm.CandidateUserName,
                    vm.CandidatePasswordProtected,
                    vm.AdminPasswordProtected
                });
            }
        }

        public async Task<CodeProjectVm> Get(string handle)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectVm>(con, "usp_GetCodeProjectVm", new { Handle = handle });
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> TryLease(string handle, int leaseSeconds = 60)
        {
            using (var con = new SqlConnection(_connectionString))
                return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_LeaseCodeProjectVm",
                    new { Handle = handle, LeaseSeconds = leaseSeconds }) == 1;
        }

        public async Task Update(string handle, string step, string message, int? vmPropertyId = null, string connectUrl = null,
            bool clearProjectZipUrl = false, bool releaseLease = true)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                await con.ExecuteAsync("usp_UpdateCodeProjectVm", new
                {
                    Handle = handle,
                    Step = step,
                    Message = message != null && message.Length > 1000 ? message.Substring(0, 1000) : message,
                    VMPropertyId = vmPropertyId,
                    ConnectUrl = connectUrl,
                    ClearProjectZipUrl = clearProjectZipUrl,
                    ReleaseLease = releaseLease
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<List<string>> GetInProgress()
        {
            using (var con = new SqlConnection(_connectionString))
                return (await DapperStoredProcedureHelper.QueryAsync<string>(con, "usp_GetInProgressCodeProjectVms")).ToList();
        }

        public async Task<List<string>> GetExpired(int maxLifetimeMinutes, int failedGraceMinutes)
        {
            using (var con = new SqlConnection(_connectionString))
                return (await DapperStoredProcedureHelper.QueryAsync<string>(con, "usp_GetExpiredCodeProjectVms",
                    new { MaxLifetimeMinutes = maxLifetimeMinutes, FailedGraceMinutes = failedGraceMinutes })).ToList();
        }

        public async Task<int> AddVMProperty(string name, string resourceGroupName, string osType, string location, string userName)
        {
            using (var con = new SqlConnection(_connectionString))
                return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_AddCodeProjectVMProperty", new
                {
                    Name = name,
                    ResourceGroupName = resourceGroupName,
                    OSType = osType,
                    VmLocation = location,
                    UserName = userName
                });
        }
    }
}
