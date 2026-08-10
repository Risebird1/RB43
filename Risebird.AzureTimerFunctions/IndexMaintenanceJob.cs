using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Daily index maintenance job. Calls usp_MaintainTableIndexes once per configured table.
    ///
    /// App settings (optional):
    ///   IndexMaintenance:Tables                  — comma-separated dbo table names (default: SentMailDetails)
    ///   IndexMaintenance:MinReorganizePercent    — default 5
    ///   IndexMaintenance:MinRebuildPercent       — default 30
    ///   IndexMaintenance:ForceFullMaintenance    — true/false; when true runs REORGANIZE ALL + REBUILD ALL
    ///
    /// Runs once daily at 4:00 AM UTC.
    /// CRON: "0 0 4 * * *"
    /// </summary>
    public class IndexMaintenanceJob
    {
        private const string DefaultTables = "SentMailDetails";
        private const int DefaultCommandTimeoutSeconds = 3600;

        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public IndexMaintenanceJob(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<IndexMaintenanceJob>();
        }

        [Function("IndexMaintenanceJob")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer)
        {
            _log.LogInformation("[IndexMaintenance] Job triggered at {UtcNow:u}", DateTime.UtcNow);

            var tables = (_configuration["IndexMaintenance:Tables"] ?? DefaultTables)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tables.Length == 0)
            {
                _log.LogWarning("[IndexMaintenance] No tables configured; skipping.");
                return;
            }

            byte minReorganize = byte.TryParse(_configuration["IndexMaintenance:MinReorganizePercent"], out byte reorganize)
                ? reorganize
                : (byte)5;

            byte minRebuild = byte.TryParse(_configuration["IndexMaintenance:MinRebuildPercent"], out byte rebuild)
                ? rebuild
                : (byte)30;

            bool forceFull = bool.TryParse(_configuration["IndexMaintenance:ForceFullMaintenance"], out bool force)
                && force;

            int commandTimeout = int.TryParse(_configuration["IndexMaintenance:CommandTimeoutSeconds"], out int timeout)
                ? timeout
                : DefaultCommandTimeoutSeconds;

            _log.LogInformation(
                "[IndexMaintenance] Tables: {Tables}; Reorganize >= {Reorganize}%; Rebuild >= {Rebuild}%; ForceFull={ForceFull}",
                string.Join(", ", tables),
                minReorganize,
                minRebuild,
                forceFull);

            try
            {
                await using var conn = new SqlConnection(_configuration["ConnectionString:DBConnectionString"]);
                await conn.OpenAsync();

                foreach (var tableName in tables)
                {
                    try
                    {
                        _log.LogInformation("[IndexMaintenance] Starting dbo.{TableName}", tableName);

                        var result = await conn.QuerySingleAsync<MaintenanceResult>(
                            "usp_MaintainTableIndexes",
                            new
                            {
                                TableName = tableName,
                                MinReorganizePercent = minReorganize,
                                MinRebuildPercent = minRebuild,
                                ForceFullMaintenance = forceFull
                            },
                            commandType: CommandType.StoredProcedure,
                            commandTimeout: commandTimeout);

                        _log.LogInformation(
                            "[IndexMaintenance] dbo.{TableName} complete — reorganized: {Reorganized}, rebuilt: {Rebuilt}, skipped: {Skipped}, forceFull: {ForceFull}",
                            result.TableName,
                            result.IndexesReorganized,
                            result.IndexesRebuilt,
                            result.IndexesSkipped,
                            result.ForceFullMaintenance);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "[IndexMaintenance] Failed for dbo.{TableName}", tableName);
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[IndexMaintenance] Job failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            _log.LogInformation("[IndexMaintenance] Job finished at {UtcNow:u}", DateTime.UtcNow);
        }

        private sealed class MaintenanceResult
        {
            public string TableName { get; set; }
            public int? IndexesReorganized { get; set; }
            public int? IndexesRebuilt { get; set; }
            public int? IndexesSkipped { get; set; }
            public DateTime StartedAtUtc { get; set; }
            public DateTime CompletedAtUtc { get; set; }
            public bool ForceFullMaintenance { get; set; }
        }
    }
}
