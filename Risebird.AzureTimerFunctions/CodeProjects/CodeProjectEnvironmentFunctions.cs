using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Net;

namespace Risebird.AzureTimerFunctions.CodeProjects
{
    /// <summary>
    /// The code project environment provisioner the web app calls (CodeProjects:ProvisionerUrl =
    /// https://{app}.azurewebsites.net/api/codeprojects, key in CodeProjects:ProvisionerKey, sent as
    /// x-functions-key). Contract: HttpCodeProjectEnvironmentProvisioner in Risebird.CommonHelper.
    /// </summary>
    public class CodeProjectEnvironmentFunctions
    {
        private readonly CodeProjectVmProvisioner _provisioner;
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly ILogger _log;

        public CodeProjectEnvironmentFunctions(ILoggerFactory loggerFactory, CodeProjectVmProvisioner provisioner, IApplicationErrorLogger errorLogger)
        {
            _provisioner = provisioner;
            _errorLogger = errorLogger;
            _log = loggerFactory.CreateLogger<CodeProjectEnvironmentFunctions>();
        }

        [Function("CodeProjectProvision")]
        public async Task<HttpResponseData> Provision(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "codeprojects/provision")] HttpRequestData req, CancellationToken ct)
        {
            var request = await ReadAsync<CodeProjectProvisionRequest>(req);
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId) || request.QuestionId <= 0)
                return await JsonAsync(req, new CodeProjectProvisionResult { Accepted = false, Error = "SessionId and QuestionId are required." }, HttpStatusCode.BadRequest);
            try
            {
                return await JsonAsync(req, await _provisioner.ProvisionAsync(request, ct));
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return await JsonAsync(req, new CodeProjectProvisionResult { Accepted = false, Error = "The environment could not be started." });
            }
        }

        [Function("CodeProjectStatus")]
        public async Task<HttpResponseData> Status(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "codeprojects/status")] HttpRequestData req, CancellationToken ct)
        {
            var handle = System.Web.HttpUtility.ParseQueryString(req.Url.Query)["handle"];
            if (string.IsNullOrWhiteSpace(handle))
                return await JsonAsync(req, new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Failed, Message = "handle is required." }, HttpStatusCode.BadRequest);
            try
            {
                return await JsonAsync(req, await _provisioner.GetStatusAsync(handle, ct));
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                // Transient: tell the caller to keep waiting rather than fail the candidate.
                return await JsonAsync(req, new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Provisioning, Message = "Waiting for the machine…" });
            }
        }

        [Function("CodeProjectCollect")]
        public async Task<HttpResponseData> Collect(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "codeprojects/collect")] HttpRequestData req, CancellationToken ct)
        {
            var body = await ReadAsync<HandleBody>(req);
            try
            {
                return await JsonAsync(req, await _provisioner.CollectAsync(body?.Handle, ct));
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return await JsonAsync(req, new CodeProjectWorkspaceSnapshot { Success = false, Error = "Could not read the candidate's workspace." });
            }
        }

        [Function("CodeProjectRelease")]
        public async Task<HttpResponseData> Release(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "codeprojects/release")] HttpRequestData req, CancellationToken ct)
        {
            var body = await ReadAsync<HandleBody>(req);
            try
            {
                await _provisioner.ReleaseAsync(body?.Handle, ct);
            }
            catch (Exception ex)
            {
                // The janitor retries anything left behind.
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return await JsonAsync(req, new { released = true });
        }

        /// <summary>Moves machines forward between status polls, so look-ahead VMs are ready before the candidate arrives.</summary>
        [Function("CodeProjectVmAdvance")]
        public async Task Advance([TimerTrigger("*/15 * * * * *")] TimerInfo timer, CancellationToken ct)
        {
            try { await _provisioner.AdvanceAllAsync(ct); }
            catch (Exception ex) { await _errorLogger.LogMessage(LogLevelInfo.Exception, ex); }
        }

        /// <summary>Deletes failed machines and any that outlived the interview (CodeProjects:Azure:MaxLifetimeMinutes).</summary>
        [Function("CodeProjectVmJanitor")]
        public async Task Janitor([TimerTrigger("0 */10 * * * *")] TimerInfo timer, CancellationToken ct)
        {
            _log.LogInformation($"CodeProjectVmJanitor executed at: {DateTime.UtcNow:o}");
            try { await _provisioner.ReleaseExpiredAsync(ct); }
            catch (Exception ex) { await _errorLogger.LogMessage(LogLevelInfo.Exception, ex); }
        }

        private sealed class HandleBody
        {
            public string Handle { get; set; }
        }

        private static async Task<T> ReadAsync<T>(HttpRequestData req) where T : class
        {
            var text = await new StreamReader(req.Body).ReadToEndAsync();
            try { return string.IsNullOrWhiteSpace(text) ? null : JsonConvert.DeserializeObject<T>(text); }
            catch (JsonException) { return null; }
        }

        /// <summary>camelCase, matching the web client's System.Text.Json web defaults.</summary>
        private static async Task<HttpResponseData> JsonAsync(HttpRequestData req, object value, HttpStatusCode status = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(status);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            }));
            return response;
        }
    }
}
