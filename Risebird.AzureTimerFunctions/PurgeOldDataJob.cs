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
    /// Generic daily purge job. Delegates all DELETE statements to usp_PurgeOldData.
    ///
    /// Retention windows (configurable via app settings, with sensible defaults):
    ///   Purge:VapiPlivoRetentionDays  — Vapi / Plivo tables          (default: 30 days)
    ///   Purge:OpenAIRetentionDays     — OpenAI API log tables         (default: 60 days)
    ///
    /// Runs once daily at 2:00 AM UTC.
    /// CRON: "0 0 2 * * *"
    /// </summary>
    public class PurgeOldDataJob
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        private const int DefaultVapiPlivoRetentionDays = 30;
        private const int DefaultOpenAIRetentionDays    = 60;

        public PurgeOldDataJob(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<PurgeOldDataJob>();
        }

        [Function("PurgeOldDataJob")]
        public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            _log.LogInformation($"[Purge] Job triggered at: {DateTime.UtcNow:u}");

            int vapiPlivoDays = int.TryParse(_configuration["Purge:VapiPlivoRetentionDays"], out int vp)
                ? vp : DefaultVapiPlivoRetentionDays;

            int openAIDays = int.TryParse(_configuration["Purge:OpenAIRetentionDays"], out int oa)
                ? oa : DefaultOpenAIRetentionDays;

            _log.LogInformation(
                $"[Purge] Retention windows — Vapi/Plivo: {vapiPlivoDays} days, OpenAI: {openAIDays} days");

            try
            {
                using var conn = new SqlConnection(_configuration["ConnectionString:DBConnectionString"]);

                var result = await conn.QuerySingleAsync<PurgeResult>(
                    "usp_PurgeOldData",
                    new
                    {
                        VapiPlivoRetentionDays = vapiPlivoDays,
                        OpenAIRetentionDays    = openAIDays
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                _log.LogInformation(
                    $"[Purge] Done. Rows deleted:\n" +
                    $"  Vapi / Plivo  (cutoff: {result.VapiPlivoCutoff:yyyy-MM-dd}):\n" +
                    $"    VapiWebhookDebugLogs:      {result.VapiWebhookDebugLogs}\n" +
                    $"    VapiWebhookCheckpoints:    {result.VapiWebhookCheckpoints}\n" +
                    $"    PlivoWebhookDebugLogs:     {result.PlivoWebhookDebugLogs}\n" +
                    $"    PlivoCXWebhookDebugLogs:   {result.PlivoCXWebhookDebugLogs}\n" +
                    $"    PlivoCXWebhookCheckpoints: {result.PlivoCXWebhookCheckpoints}\n" +
                    $"    PlivoWhatsAppDebugLogs:    {result.PlivoWhatsAppDebugLogs}\n" +
                    $"    PlivoWhatsAppCheckpoints:  {result.PlivoWhatsAppCheckpoints}\n" +
                    $"    VapiCallLogs:              {result.VapiCallLogs}\n" +
                    $"    PlivoCallLogs:             {result.PlivoCallLogs}\n" +
                    $"    PlivoCXCallLogs:           {result.PlivoCXCallLogs}\n" +
                    $"    PlivoWhatsAppLogs:         {result.PlivoWhatsAppLogs}\n" +
                    $"  OpenAI / Azure cost  (cutoff: {result.OpenAICutoff:yyyy-MM-dd}):\n" +
                    $"    OpenAIApiLogs:             {result.OpenAIApiLogs}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[Purge] Job failed: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private sealed class PurgeResult
        {
            public DateTime VapiPlivoCutoff        { get; set; }
            public DateTime OpenAICutoff           { get; set; }
            // Vapi / Plivo
            public int VapiWebhookDebugLogs        { get; set; }
            public int VapiWebhookCheckpoints      { get; set; }
            public int PlivoWebhookDebugLogs       { get; set; }
            public int PlivoCXWebhookDebugLogs     { get; set; }
            public int PlivoCXWebhookCheckpoints   { get; set; }
            public int PlivoWhatsAppDebugLogs      { get; set; }
            public int PlivoWhatsAppCheckpoints    { get; set; }
            public int VapiCallLogs                { get; set; }
            public int PlivoCallLogs               { get; set; }
            public int PlivoCXCallLogs             { get; set; }
            public int PlivoWhatsAppLogs           { get; set; }
            // OpenAI / Azure cost
            public int OpenAIApiLogs               { get; set; }
        }
    }
}
