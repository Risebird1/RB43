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
    /// Daily cleanup job that removes Vapi and Plivo related rows older than 30 days.
    /// Delegates all DELETE statements to usp_DeleteOldVapiPlivoData.
    ///
    /// Runs once daily at 2:00 AM UTC.
    /// CRON: "0 0 2 * * *"
    /// </summary>
    public class DeleteOldVapiPlivoData
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        private const int RetentionDays = 30;

        public DeleteOldVapiPlivoData(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<DeleteOldVapiPlivoData>();
        }

        [Function("DeleteOldVapiPlivoData")]
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[VapiPlivo-Cleanup] Job triggered at: {DateTime.UtcNow:u}");

            try
            {
                using var conn = new SqlConnection(_configuration["ConnectionString:DBConnectionString"]);

                var result = await conn.QuerySingleAsync<CleanupResult>(
                    "usp_DeleteOldVapiPlivoData",
                    new { RetentionDays },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                _log.LogInformation(
                    $"[VapiPlivo-Cleanup] Done. Rows deleted (cutoff: {result.Cutoff:yyyy-MM-dd}):\n" +
                    $"  Debug/Checkpoint tables:\n" +
                    $"    VapiWebhookDebugLogs:      {result.VapiWebhookDebugLogs}\n" +
                    $"    VapiWebhookCheckpoints:    {result.VapiWebhookCheckpoints}\n" +
                    $"    PlivoWebhookDebugLogs:     {result.PlivoWebhookDebugLogs}\n" +
                    $"    PlivoCXWebhookDebugLogs:   {result.PlivoCXWebhookDebugLogs}\n" +
                    $"    PlivoCXWebhookCheckpoints: {result.PlivoCXWebhookCheckpoints}\n" +
                    $"    PlivoWhatsAppDebugLogs:    {result.PlivoWhatsAppDebugLogs}\n" +
                    $"    PlivoWhatsAppCheckpoints:  {result.PlivoWhatsAppCheckpoints}\n" +
                    $"  Call/message log tables:\n" +
                    $"    VapiCallLogs:              {result.VapiCallLogs}\n" +
                    $"    PlivoCallLogs:             {result.PlivoCallLogs}\n" +
                    $"    PlivoCXCallLogs:           {result.PlivoCXCallLogs}\n" +
                    $"    PlivoWhatsAppLogs:         {result.PlivoWhatsAppLogs}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[VapiPlivo-Cleanup] Job failed: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private sealed class CleanupResult
        {
            public DateTime Cutoff                { get; set; }
            public int VapiWebhookDebugLogs       { get; set; }
            public int VapiWebhookCheckpoints     { get; set; }
            public int PlivoWebhookDebugLogs      { get; set; }
            public int PlivoCXWebhookDebugLogs    { get; set; }
            public int PlivoCXWebhookCheckpoints  { get; set; }
            public int PlivoWhatsAppDebugLogs     { get; set; }
            public int PlivoWhatsAppCheckpoints   { get; set; }
            public int VapiCallLogs               { get; set; }
            public int PlivoCallLogs              { get; set; }
            public int PlivoCXCallLogs            { get; set; }
            public int PlivoWhatsAppLogs          { get; set; }
        }
    }
}
