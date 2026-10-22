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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class CodeProjectSettingsRepository : ICodeProjectSettingsRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CodeProjectSettingsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public Dictionary<string, (string Value, bool IsSecret)> GetAll()
        {
            var result = new Dictionary<string, (string, bool)>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(_connectionString)) return result;
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    foreach (var row in DapperStoredProcedureHelper.Query<dynamic>(con, "usp_GetCodeProjectSettings"))
                        result[(string)row.Name] = ((string)row.Value, (bool)row.IsSecret);
                }
            }
            catch (SqlException)
            {
                // The table does not exist until setup has run; configuration alone applies until then.
            }
            return result;
        }

        public async Task Set(string name, string value, bool isSecret = false)
        {
            using (var con = new SqlConnection(_connectionString))
                await con.ExecuteAsync("usp_SetCodeProjectSetting", new { Name = name, Value = value, IsSecret = isSecret },
                    commandType: CommandType.StoredProcedure);
        }
    }

    public class CodeProjectSetupRepository : ICodeProjectSetupRepository
    {
        private static readonly Regex GoSeparator = new Regex(@"^\s*GO\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly string _connectionString;

        public CodeProjectSetupRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<int> StartOrResume(string startedBy, string initialStateJson)
        {
            using (var con = new SqlConnection(_connectionString))
                return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_StartCodeProjectInfraSetup",
                    new { StartedBy = startedBy, InitialStateJson = initialStateJson });
        }

        public async Task<CodeProjectSetupRun> GetLatest()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                    return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<CodeProjectSetupRun>(con, "usp_GetLatestCodeProjectInfraSetup");
            }
            catch (SqlException)
            {
                return null;   // not bootstrapped yet
            }
        }

        public async Task<bool> TryLease(int id, int leaseSeconds = 180)
        {
            using (var con = new SqlConnection(_connectionString))
                return await DapperStoredProcedureHelper.QueryFirstOrDefaultAsync<int>(con, "usp_LeaseCodeProjectInfraSetup",
                    new { Id = id, LeaseSeconds = leaseSeconds }) == 1;
        }

        public async Task Save(int id, string status, string stateJson, bool releaseLease = true)
        {
            using (var con = new SqlConnection(_connectionString))
                await con.ExecuteAsync("usp_SaveCodeProjectInfraSetup",
                    new { Id = id, Status = status, StateJson = stateJson, ReleaseLease = releaseLease },
                    commandType: CommandType.StoredProcedure);
        }

        public async Task ApplySqlScript(string script)
        {
            var batches = GoSeparator.Split(script.Replace("\r\n", "\n"))
                .Select(b => b.Trim())
                .Where(b => b.Length > 0 && !IsOnlyComments(b))
                .ToList();

            using (var con = new SqlConnection(_connectionString))
            {
                await con.OpenAsync();
                foreach (var batch in batches)
                {
                    using (var cmd = new SqlCommand(batch, con) { CommandTimeout = 300 })
                        await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> CodeProjectSchemaPresent()
        {
            // The last object each migration adds; all present means every script has run.
            const string sql = @"SELECT CASE WHEN
                    COL_LENGTH('dbo.SkillChannelSampleQuestions', 'IsCodeProjectQuestion') IS NOT NULL
                AND OBJECT_ID('dbo.usp_GetAIQuestionPoolForSets', 'P') IS NOT NULL
                AND COL_LENGTH('dbo.CodeProjectVmProvisioning', 'IntegrityFlagsJson') IS NOT NULL
                AND OBJECT_ID('dbo.usp_GetLiveCodeProjectVmHandles', 'P') IS NOT NULL
                AND OBJECT_ID('dbo.usp_UpsertStackImage', 'P') IS NOT NULL
                THEN 1 ELSE 0 END";
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                await con.OpenAsync();
                return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
            }
        }

        private static bool IsOnlyComments(string batch)
        {
            var withoutBlock = Regex.Replace(batch, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return withoutBlock.Split('\n').All(l => string.IsNullOrWhiteSpace(l) || l.TrimStart().StartsWith("--"));
        }

        public async Task UpsertStackImage(string code, string displayName, string toolchain, string galleryImageId, string osType)
        {
            using (var con = new SqlConnection(_connectionString))
                await con.ExecuteAsync("usp_UpsertStackImage",
                    new { Code = code, DisplayName = displayName, Toolchain = toolchain, GalleryImageId = galleryImageId, OsType = osType },
                    commandType: CommandType.StoredProcedure);
        }

        public string DescribeDatabase()
        {
            try
            {
                var b = new SqlConnectionStringBuilder(_connectionString);
                return $"{b.DataSource} / {b.InitialCatalog}";
            }
            catch (Exception)
            {
                return "(connection string not readable)";
            }
        }
    }
}
