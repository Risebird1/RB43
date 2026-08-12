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
    ///   "Piston"  : https://github.com/engineer-man/piston  (public instance needs no key; self-host for private code)
    ///   "Judge0"  : https://judge0.com                      (self-hosted, or RapidAPI with a key)
    ///
    /// Candidate source is sent to whatever BaseUrl is configured, so point this at a
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

        private string BaseUrl => (_configuration[$"{SettingsSection}:BaseUrl"] ?? "https://emkc.org/api/v2/piston").TrimEnd('/');

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
                return !string.IsNullOrWhiteSpace(BaseUrl);
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

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

                var result = string.Equals(Provider, "Judge0", StringComparison.OrdinalIgnoreCase)
                    ? await ExecuteOnJudge0Async(language, request, cts.Token)
                    : await ExecuteOnPistonAsync(language, request, cts.Token);

                result.Stdout = Truncate(result.Stdout);
                result.Stderr = Truncate(result.Stderr);
                result.CompileOutput = Truncate(result.CompileOutput);
                result.CombinedOutput = Truncate(BuildCombinedOutput(result));
                return result;
            }
            catch (OperationCanceledException)
            {
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
            var payload = new
            {
                language = language.PistonLanguage,
                version = string.IsNullOrWhiteSpace(language.PistonVersion) ? "*" : language.PistonVersion,
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
                    new Exception($"Piston execute failed [{(int)response.StatusCode}]: {body}"));

                // 429 is common on the shared public instance
                return (int)response.StatusCode == 429
                    ? Fail("The code runner is busy right now. Wait a few seconds and run again.")
                    : Fail("The code runner returned an error. Your code is safe — you can still submit it.");
            }

            var json = JObject.Parse(body);
            var run = json["run"];
            var compile = json["compile"];

            var compileOutput = compile?["output"]?.ToString();
            var compileCode = compile?["code"]?.Value<int?>();

            // A non-zero compile exit code means the program never ran
            if (compileCode.HasValue && compileCode.Value != 0)
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    Status = "Compilation Error",
                    CompileOutput = compileOutput,
                    ExitCode = compileCode
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
                ExitCode = exitCode
            };
        }

        // ------------------------------------------------------------------ //
        //  Judge0                                                             //
        // ------------------------------------------------------------------ //

        private async Task<CodeExecutionResult> ExecuteOnJudge0Async(CodeLanguageOption language, CodeExecutionRequest request, CancellationToken token)
        {
            var payload = new
            {
                language_id = language.Judge0Id,
                source_code = request.SourceCode,
                stdin = request.Stdin ?? string.Empty
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/submissions?base64_encoded=false&wait=true")
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            AddJudge0AuthHeaders(message);

            var response = await _httpClient.SendAsync(message, token);
            var body = await response.Content.ReadAsStringAsync(token);

            if (!response.IsSuccessStatusCode)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"Judge0 execute failed [{(int)response.StatusCode}]: {body}"));
                return Fail("The code runner returned an error. Your code is safe — you can still submit it.");
            }

            var json = JObject.Parse(body);
            var statusId = json["status"]?["id"]?.Value<int?>() ?? 0;
            var statusText = json["status"]?["description"]?.ToString() ?? "Unknown";

            return new CodeExecutionResult
            {
                Success = statusId == 3, // 3 == Accepted in Judge0
                Status = statusText,
                Stdout = json["stdout"]?.ToString(),
                Stderr = json["stderr"]?.ToString(),
                CompileOutput = json["compile_output"]?.ToString(),
                ExitCode = json["exit_code"]?.Value<int?>()
            };
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
            return combined.Length == 0 ? "(no output)" : combined;
        }

        private string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= MaxOutputCharacters)
            {
                return value;
            }
            return value.Substring(0, MaxOutputCharacters) + "\n... output truncated ...";
        }

        private static CodeExecutionResult Fail(string message) => new CodeExecutionResult
        {
            Success = false,
            Status = "Error",
            Message = message,
            CombinedOutput = message
        };
    }
}
