using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Compiles and runs candidate code for coding interview questions in an external sandbox.
    ///
    /// Two backends are supported and selected via CodeExecution:Provider —
    ///   "Piston"  : https://github.com/engineer-man/piston  (self-host: docker run -p 2000:2000 --privileged ghcr.io/engineer-man/piston)
    ///   "Judge0"  : https://judge0.com                      (self-hosted, or RapidAPI with a key)
    ///
    /// CodeExecution:BaseUrl must be set for either provider; there is no working public
    /// default. Candidate source is sent to whatever BaseUrl points at, so use a
    /// self-hosted instance if interview code must not leave your infrastructure.
    /// </summary>
    public class CodeExecutionService : ICodeExecutionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private const string SettingsSection = "CodeExecution";
        private const int DefaultTimeoutSeconds = 25;
        private const int DefaultMaxOutputCharacters = 20000;

        public CodeExecutionService(
            HttpClient httpClient,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        private string Provider => (_configuration[$"{SettingsSection}:Provider"] ?? "Piston").Trim();

        // No default endpoint on purpose. The public Piston instance went whitelist-only in
        // Feb 2026, so falling back to it would fail every run; an unconfigured environment
        // should report the runner as unavailable instead.
        private string BaseUrl => (_configuration[$"{SettingsSection}:BaseUrl"] ?? string.Empty).Trim().TrimEnd('/');

        private string ApiKey => _configuration[$"{SettingsSection}:ApiKey"];

        private int TimeoutSeconds =>
            int.TryParse(_configuration[$"{SettingsSection}:TimeoutSeconds"], out var seconds) && seconds > 0
                ? seconds
                : DefaultTimeoutSeconds;

        private int MaxOutputCharacters =>
            int.TryParse(_configuration[$"{SettingsSection}:MaxOutputCharacters"], out var chars) && chars > 0
                ? chars
                : DefaultMaxOutputCharacters;

        public bool IsEnabled
        {
            get
            {
                var configured = _configuration[$"{SettingsSection}:Enabled"];
                if (bool.TryParse(configured, out var enabled) && !enabled)
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(BaseUrl))
                {
                    return false;
                }

                // A RapidApiHost means we are going through RapidAPI, which always needs a key.
                // Without one the runner would only ever return 401, so report it as unavailable
                // and let the IDE hide the Run button instead of showing candidates an error.
                var rapidApiHost = _configuration[$"{SettingsSection}:RapidApiHost"];
                if (!string.IsNullOrWhiteSpace(rapidApiHost) && string.IsNullOrWhiteSpace(ApiKey))
                {
                    return false;
                }

                return true;
            }
        }

        public async Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request)
        {
            if (!IsEnabled)
            {
                return Fail("Code execution is not enabled for this environment.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.SourceCode))
            {
                return Fail("Write some code before running it.");
            }

            var language = CodeLanguageCatalog.Find(request.LanguageId);
            if (language == null)
            {
                return Fail("Select a supported language before running your code.");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

                var result = string.Equals(Provider, "Judge0", StringComparison.OrdinalIgnoreCase)
                    ? await ExecuteOnJudge0Async(language, request, cts.Token)
                    : await ExecuteOnPistonAsync(language, request, cts.Token);

                stopwatch.Stop();
                result.DurationMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);

                // Fall back to the version we asked for when the backend does not report one,
                // so a code session records something rather than nothing.
                if (string.IsNullOrWhiteSpace(result.LanguageVersionUsed))
                {
                    result.LanguageVersionUsed = language.PistonVersion;
                }

                result.Stdout = Truncate(result.Stdout);
                result.Stderr = Truncate(result.Stderr);
                result.CompileOutput = Truncate(result.CompileOutput);
                result.CombinedOutput = Truncate(BuildCombinedOutput(result));
                return result;
            }
            catch (OperationCanceledException)
            {
                // Deliberately DidNotRun rather than a failure. Our own timeout can fire because
                // the candidate wrote an infinite loop, but equally because the runner was queued
                // or slow — and we cannot tell which from here. Recording it as a failure would
                // put a verdict about the candidate into a client report on the strength of a
                // guess, so the ambiguous case resolves the way that does not blame them.
                return Fail($"Your program did not finish within {TimeoutSeconds} seconds. Check for an infinite loop and try again.");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Fail("The code runner is temporarily unavailable. Your code is safe — you can still submit it.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Piston                                                             //
        // ------------------------------------------------------------------ //

        private async Task<CodeExecutionResult> ExecuteOnPistonAsync(CodeLanguageOption language, CodeExecutionRequest request, CancellationToken token)
        {
            // "*" lets Piston pick whichever runtime version the instance actually has installed.
            // Pinning exact versions breaks silently every time the instance upgrades a runtime.
            var payload = new
            {
                language = language.PistonLanguage,
                version = "*",
                files = new[]
                {
                    new { name = language.FileName, content = request.SourceCode }
                },
                stdin = request.Stdin ?? string.Empty
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/execute")
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                message.Headers.TryAddWithoutValidation("Authorization", ApiKey);
            }

            var response = await _httpClient.SendAsync(message, token);
            var body = await response.Content.ReadAsStringAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"Piston execute failed [{(int)response.StatusCode}] against {BaseUrl}: {body}"));

                switch ((int)response.StatusCode)
                {
                    // The public emkc.org instance became whitelist-only in Feb 2026 and now
                    // rejects every unauthenticated request, so this means "not configured yet".
                    case 401:
                    case 403:
                        return Fail("The code runner is not configured for this environment. Your code is safe — write your solution and submit it.");
                    case 429:
                        return Fail("The code runner is busy right now. Wait a few seconds and run again.");
                    case 400:
                        return Fail($"The code runner rejected this request ({ExtractPistonMessage(body)}). Your code is safe — you can still submit it.");
                    default:
                        return Fail("The code runner returned an error. Your code is safe — you can still submit it.");
                }
            }

            var json = JObject.Parse(body);
            var run = json["run"];
            var compile = json["compile"];

            var compileOutput = compile?["output"]?.ToString();
            var compileCode = compile?["code"]?.Value<int?>();

            // We ask for version "*", so this is the only place the version that actually ran
            // can be learned.
            var versionUsed = json["version"]?.ToString();

            // A non-zero compile exit code means the program never ran.
            //
            // DidNotRun is NOT set here: a compile error is the candidate's code failing to
            // compile, which is evidence about them and publishes verbatim. DidNotRun is only
            // for our runner being unable to give an answer at all.
            if (compileCode.HasValue && compileCode.Value != 0)
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    Status = "Compilation Error",
                    CompileOutput = compileOutput,
                    ExitCode = compileCode,
                    LanguageVersionUsed = versionUsed
                };
            }

            var exitCode = run?["code"]?.Value<int?>();
            var stderr = run?["stderr"]?.ToString();

            return new CodeExecutionResult
            {
                Success = !exitCode.HasValue || exitCode.Value == 0,
                Status = (!exitCode.HasValue || exitCode.Value == 0) ? "Success" : $"Exited with code {exitCode.Value}",
                Stdout = run?["stdout"]?.ToString(),
                Stderr = stderr,
                CompileOutput = compileOutput,
                ExitCode = exitCode,
                LanguageVersionUsed = versionUsed
            };
        }

        // ------------------------------------------------------------------ //
        //  Judge0                                                             //
        // ------------------------------------------------------------------ //

        // Judge0 status ids: 1 = In Queue, 2 = Processing, 3 = Accepted, anything higher is a
        // compile or runtime failure. https://ce.judge0.com/#statuses-and-languages
        private const int Judge0StatusAccepted = 3;

        // Metered plans count every poll, so the first wait is long enough that most short
        // programs are already judged by the time we ask, keeping a typical run at two requests.
        private const int Judge0FirstPollDelayMilliseconds = 900;
        private const int Judge0PollIntervalMilliseconds = 400;

        /// <summary>
        /// Creates an async submission and polls it to completion. The official Judge0 (which is
        /// what RapidAPI hosts) has the synchronous wait=true feature disabled and answers such
        /// requests with "wait not allowed", so the token flow is the only one that works there.
        /// Everything is base64 encoded, which is what keeps a program emitting non-printable
        /// bytes from breaking the response serialization.
        /// </summary>
        private async Task<CodeExecutionResult> ExecuteOnJudge0Async(CodeLanguageOption language, CodeExecutionRequest request, CancellationToken token)
        {
            var payload = new
            {
                language_id = language.Judge0Id,
                source_code = ToBase64(request.SourceCode),
                stdin = ToBase64(request.Stdin ?? string.Empty)
            };

            using var createMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/submissions?base64_encoded=true&wait=false")
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            AddJudge0AuthHeaders(createMessage);

            var createResponse = await _httpClient.SendAsync(createMessage, token);
            var createBody = await createResponse.Content.ReadAsStringAsync(token);

            if (!createResponse.IsSuccessStatusCode)
            {
                return await Judge0FailureAsync(createResponse, createBody, "create");
            }

            var submissionToken = JObject.Parse(createBody)["token"]?.ToString();
            if (string.IsNullOrWhiteSpace(submissionToken))
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"Judge0 returned no submission token: {createBody}"));
                return Fail("The code runner did not accept your submission. Your code is safe — you can still submit it.");
            }

            // Poll until the judge finishes or the overall request timeout cancels us
            var pollDelay = Judge0FirstPollDelayMilliseconds;
            while (true)
            {
                await Task.Delay(pollDelay, token);
                pollDelay = Judge0PollIntervalMilliseconds;

                using var pollMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/submissions/{submissionToken}?base64_encoded=true");
                AddJudge0AuthHeaders(pollMessage);

                var pollResponse = await _httpClient.SendAsync(pollMessage, token);
                var pollBody = await pollResponse.Content.ReadAsStringAsync(token);

                if (!pollResponse.IsSuccessStatusCode)
                {
                    return await Judge0FailureAsync(pollResponse, pollBody, "poll");
                }

                var json = JObject.Parse(pollBody);
                var statusId = json["status"]?["id"]?.Value<int?>() ?? 0;

                if (statusId <= 2)
                {
                    continue;
                }

                var statusText = json["status"]?["description"]?.ToString() ?? "Unknown";
                var judgeMessage = DecodeBase64OrRaw(json["message"]?.ToString());

                return new CodeExecutionResult
                {
                    Success = statusId == Judge0StatusAccepted,
                    Status = statusText,
                    Stdout = DecodeBase64OrRaw(json["stdout"]?.ToString()),
                    Stderr = DecodeBase64OrRaw(json["stderr"]?.ToString()),
                    CompileOutput = DecodeBase64OrRaw(json["compile_output"]?.ToString()),
                    ExitCode = json["exit_code"]?.Value<int?>(),
                    // Judge0 echoes the language it ran under only when asked for the full
                    // submission fields; the name carries the version, e.g. "Python (3.8.1)".
                    LanguageVersionUsed = json["language"]?["name"]?.ToString(),
                    Message = statusId == Judge0StatusAccepted ? null : (judgeMessage ?? statusText)
                };
            }
        }

        private async Task<CodeExecutionResult> Judge0FailureAsync(HttpResponseMessage response, string body, string stage)
        {
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                new Exception($"Judge0 {stage} failed [{(int)response.StatusCode}] against {BaseUrl}: {body}"));

            switch ((int)response.StatusCode)
            {
                case 401:
                case 403:
                    return Fail("The code runner rejected our credentials. Your code is safe — write your solution and submit it.");
                case 429:
                    return Fail("The code runner has hit its request limit. Wait a moment and run again, or submit your code as it is.");
                default:
                    return Fail("The code runner returned an error. Your code is safe — you can still submit it.");
            }
        }

        private static string ToBase64(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

        /// <summary>
        /// Judge0 base64 encodes the output streams. Not every field is encoded on every host,
        /// so anything that is not valid base64 is passed through untouched.
        /// </summary>
        private static string DecodeBase64OrRaw(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value.Trim()));
            }
            catch (FormatException)
            {
                return value;
            }
        }

        private void AddJudge0AuthHeaders(HttpRequestMessage message)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                return;
            }

            var rapidApiHost = _configuration[$"{SettingsSection}:RapidApiHost"];
            if (!string.IsNullOrWhiteSpace(rapidApiHost))
            {
                message.Headers.TryAddWithoutValidation("X-RapidAPI-Key", ApiKey);
                message.Headers.TryAddWithoutValidation("X-RapidAPI-Host", rapidApiHost);
            }
            else
            {
                message.Headers.TryAddWithoutValidation("X-Auth-Token", ApiKey);
            }
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                            //
        // ------------------------------------------------------------------ //

        /// <summary>Pulls the "message" field out of a Piston error body, falling back to the raw body.</summary>
        private static string ExtractPistonMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "no detail returned";

            try
            {
                var message = JObject.Parse(body)["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message)) return message;
            }
            catch (JsonException)
            {
                // Not JSON; the raw body is the best detail available
            }

            return body.Length > 300 ? body.Substring(0, 300) : body;
        }

        private static string BuildCombinedOutput(CodeExecutionResult result)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(result.CompileOutput))
            {
                builder.AppendLine(result.CompileOutput.TrimEnd());
            }
            if (!string.IsNullOrWhiteSpace(result.Stdout))
            {
                builder.AppendLine(result.Stdout.TrimEnd());
            }
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                builder.AppendLine(result.Stderr.TrimEnd());
            }

            var combined = builder.ToString().TrimEnd();
            if (combined.Length > 0)
            {
                return combined;
            }

            // A refused or failed run carries its explanation in Message and produces no streams.
            // Without this the candidate would only ever see "(no output)" and no reason why.
            return string.IsNullOrWhiteSpace(result.Message) ? "(no output)" : result.Message;
        }

        private string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= MaxOutputCharacters)
            {
                return value;
            }
            return value.Substring(0, MaxOutputCharacters) + "\n... output truncated ...";
        }

        /// <summary>
        /// Every caller of this is a case where the candidate's program never ran: the runner is
        /// not configured, refused us, rate-limited us, timed out, or there was nothing runnable
        /// to send. So DidNotRun is set here rather than at each call site — a new failure path
        /// added later is then classified correctly by default instead of silently arriving in a
        /// client report as the candidate's code having failed.
        ///
        /// Status stays "Error" because the AI interview IDE already displays that string.
        /// </summary>
        private static CodeExecutionResult Fail(string message) => new CodeExecutionResult
        {
            Success = false,
            DidNotRun = true,
            Status = "Error",
            Message = message,
            CombinedOutput = message
        };
    }
}
