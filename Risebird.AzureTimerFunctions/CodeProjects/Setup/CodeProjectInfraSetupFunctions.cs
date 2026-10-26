using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Net;

namespace Risebird.AzureTimerFunctions.CodeProjects.Setup
{
    /// <summary>
    /// One-click code project infrastructure setup.
    ///
    ///   POST /api/codeprojects/setup/start   start, or resume a failed/blocked run (runs the first steps immediately)
    ///   GET  /api/codeprojects/setup/status  progress of the latest run
    ///
    /// The admin dashboard's "Set up code project infrastructure" button calls these. Long steps
    /// (Bastion, VM image builds) are finished by the one-minute timer below, so keep the functions
    /// host running until the dashboard shows Completed (roughly 1.5–2.5 hours the first time).
    /// </summary>
    public class CodeProjectInfraSetupFunctions
    {
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private readonly CodeProjectInfraSetupService _setup;
        private readonly CodeProjectContainerRunner _runner;
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly ILogger _log;

        public CodeProjectInfraSetupFunctions(ILoggerFactory loggerFactory, CodeProjectInfraSetupService setup, CodeProjectContainerRunner runner, IApplicationErrorLogger errorLogger)
        {
            _setup = setup;
            _runner = runner;
            _errorLogger = errorLogger;
            _log = loggerFactory.CreateLogger<CodeProjectInfraSetupFunctions>();
        }

        [Function("CodeProjectSetupStart")]
        public async Task<HttpResponseData> Start(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "codeprojects/setup/start")] HttpRequestData req, CancellationToken ct)
        {
            var startedBy = System.Web.HttpUtility.ParseQueryString(req.Url.Query)["by"] ?? "functions";
            try
            {
                return await JsonAsync(req, await _setup.StartAsync(startedBy, ct));
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return await JsonAsync(req, new { status = "error", message = ex.Message }, HttpStatusCode.InternalServerError);
            }
        }

        [Function("CodeProjectSetupStatus")]
        public async Task<HttpResponseData> Status(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "codeprojects/setup/status")] HttpRequestData req)
        {
            try
            {
                return await JsonAsync(req, await _setup.GetStatusAsync());
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return await JsonAsync(req, new { status = "error", message = ex.Message }, HttpStatusCode.InternalServerError);
            }
        }

        [Function("CodeProjectInfraSetupTick")]
        public async Task Tick([TimerTrigger("0 */1 * * * *")] TimerInfo timer, CancellationToken ct)
        {
            try { await _setup.TickAsync(ct); }
            catch (Exception ex)
            {
                _log.LogError(ex, "Code project setup tick failed");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// 03:00 UTC on the 1st of each month: rebuilds the candidate images with the month's Windows
        /// security updates (a build costs one D4s_v5 for about two hours per stack).
        /// </summary>
        [Function("CodeProjectImageRefresh")]
        public async Task ImageRefresh([TimerTrigger("0 0 3 1 * *")] TimerInfo timer, CancellationToken ct)
        {
            try { _log.LogInformation("Code project image refresh: {Summary}", await _setup.RefreshImagesAsync(ct)); }
            catch (Exception ex)
            {
                _log.LogError(ex, "Code project image refresh failed");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// The container runner the web app calls for validation and grading
        /// (contract: HttpCodeProjectRunner in Risebird.CommonHelper).
        /// </summary>
        [Function("CodeProjectRun")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "codeprojects/run")] HttpRequestData req, CancellationToken ct)
        {
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                CodeProjectRunRequest request;
                try { request = JsonConvert.DeserializeObject<CodeProjectRunRequest>(body); }
                catch (JsonException) { request = null; }
                if (request == null || string.IsNullOrWhiteSpace(request.StackImageCode))
                    return await JsonAsync(req, new CodeProjectRunResult { Completed = false, Error = "stackImageCode and files are required." }, HttpStatusCode.BadRequest);

                return await JsonAsync(req, await _runner.RunAsync(request, ct));
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
            {
                // A readable error instead of a bare 500, so the author's panel says what went wrong.
                _log.LogError(ex, "Code project run failed");
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return await JsonAsync(req, new CodeProjectRunResult { Completed = false, Error = "Runner failure: " + ex.Message });
            }
        }

        private static async Task<HttpResponseData> JsonAsync(HttpRequestData req, object value, HttpStatusCode status = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(status);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonConvert.SerializeObject(value, Json));
            return response;
        }
    }
}
