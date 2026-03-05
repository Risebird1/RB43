using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Controller for Plivo AI interview booking calls.
    ///
    /// Call flow:
    ///   1. InitiateInterviewCall  → saves PlivoCallLog, fires outbound call via Plivo REST API
    ///   2. AnswerXml/{logId}      → Plivo fetches this when call is answered; returns PlivoXML
    ///                               with bidirectional Stream pointing to AudioStream
    ///   3. AudioStream/{logId}    → Plivo WebSocket; bridges audio to OpenAI Realtime API;
    ///                               handles submitInterviewSlot function call → books interview
    ///   4. HangupCallback         → Plivo POSTs when call ends; updates call log
    ///   5. StreamStatus           → Plivo POSTs stream connect/disconnect events
    /// </summary>
    [Authorize]
    public class PlivoInterviewCallController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IPlivoService _plivoService;
        private readonly IPlivoCallRepository _plivoCallRepository;
        private readonly IApplicationErrorLogger _logger;
        private readonly IPhoneNumberHelper _phoneNumberHelper;
        private readonly string _connectionString;

        public PlivoInterviewCallController(
            IConfiguration configuration,
            IPlivoService plivoService,
            IPlivoCallRepository plivoCallRepository,
            IApplicationErrorLogger applicationErrorLogger,
            IPhoneNumberHelper phoneNumberHelper)
        {
            _configuration       = configuration;
            _plivoService        = plivoService;
            _plivoCallRepository = plivoCallRepository;
            _logger              = applicationErrorLogger;
            _phoneNumberHelper   = phoneNumberHelper;
            _connectionString    = _configuration["ConnectionString:DBConnectionString"];
        }

        // ─── Dashboard ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var recentCalls = await _plivoCallRepository.GetRecentCallLogsAsync(100);
                return View(recentCalls);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new List<PlivoCallLog>());
            }
        }

        // ─── Assistant configuration (the "create assistant" equivalent) ─────────

        /// <summary>
        /// View / edit the active assistant config — the instructions, voice, and model
        /// that drive every Plivo AI call. Analogous to Vapi's assistant dashboard.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SetupAssistant()
        {
            try
            {
                var configs = await _plivoCallRepository.GetAllAssistantConfigsAsync();
                ViewBag.AllConfigs = configs;

                var active = configs.FirstOrDefault(c => c.IsActive) ?? configs.FirstOrDefault();
                if (active == null)
                    return View(new PlivoAssistantSetupVM());

                return View(new PlivoAssistantSetupVM
                {
                    Id                = active.Id,
                    AssistantName     = active.AssistantName,
                    Instructions      = active.Instructions,
                    Voice             = active.Voice,
                    Model             = active.Model,
                    Temperature       = active.Temperature,
                    MaxResponseTokens = active.MaxResponseTokens,
                    VadThreshold      = active.VadThreshold,
                    SilenceDurationMs = active.SilenceDurationMs
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new PlivoAssistantSetupVM());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupAssistant(PlivoAssistantSetupVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AllConfigs = await _plivoCallRepository.GetAllAssistantConfigsAsync();
                return View(vm);
            }

            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown";
                bool success;

                if (vm.Id.HasValue && vm.Id > 0)
                {
                    var existing = await _plivoCallRepository.GetAssistantConfigByNameAsync(vm.AssistantName);
                    if (existing == null)
                    {
                        TempData["Error"] = "Assistant config not found.";
                        return RedirectToAction("SetupAssistant");
                    }

                    existing.AssistantName     = vm.AssistantName;
                    existing.Instructions      = vm.Instructions;
                    existing.Voice             = vm.Voice;
                    existing.Model             = vm.Model;
                    existing.Temperature       = vm.Temperature;
                    existing.MaxResponseTokens = vm.MaxResponseTokens;
                    existing.VadThreshold      = vm.VadThreshold;
                    existing.SilenceDurationMs = vm.SilenceDurationMs;
                    existing.UpdatedBy         = userEmail;
                    success = await _plivoCallRepository.UpdateAssistantConfigAsync(existing);
                }
                else
                {
                    var config = new PlivoAssistantConfig
                    {
                        AssistantName     = vm.AssistantName,
                        Instructions      = vm.Instructions,
                        Voice             = vm.Voice,
                        Model             = vm.Model,
                        Temperature       = vm.Temperature,
                        MaxResponseTokens = vm.MaxResponseTokens,
                        VadThreshold      = vm.VadThreshold,
                        SilenceDurationMs = vm.SilenceDurationMs,
                        IsActive          = true,
                        CreatedBy         = userEmail
                    };
                    int newId = await _plivoCallRepository.SaveAssistantConfigAsync(config);
                    success = newId > 0;
                    if (success)
                        await _plivoCallRepository.SetActiveAssistantAsync(newId);
                }

                TempData[success ? "Success" : "Error"] = success
                    ? "Assistant configuration saved. All new Plivo calls will use these instructions."
                    : "Failed to save configuration.";
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["Error"] = "An error occurred while saving.";
            }

            return RedirectToAction("SetupAssistant");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActiveAssistant(int id)
        {
            await _plivoCallRepository.SetActiveAssistantAsync(id);
            TempData["Success"] = "Active assistant updated.";
            return RedirectToAction("SetupAssistant");
        }

        // ─── Manual call trigger ─────────────────────────────────────────────────

        /// <summary>
        /// Initiates a Plivo AI call to a candidate.
        /// Saves the call log first (to get a DB id) then fires the Plivo call with
        /// answer_url = /PlivoInterviewCall/AnswerXml/{logId}
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> InitiateInterviewCall(
            int interviewId,
            int candidateId,
            string candidateName,
            string candidateEmail,
            string candidatePhone,
            string jobTitle,
            string irfId,
            string clientName = null,
            DateTime? preferredDateTime = null)
        {
            try
            {
                if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidatePhone, out var phone, out var phoneError))
                    return Json(new { success = false, message = $"Invalid phone: {phoneError}" });

                var baseUrl    = _configuration["HostDetails:baseUrl"];
                var fromNumber = _configuration["Plivo:FromNumber"];

                // 1. Save initial call log to obtain the DB id used in the answer_url
                var callLog = new PlivoCallLog
                {
                    RecipientPhone = phone,
                    RecipientName  = candidateName,
                    RecipientEmail = candidateEmail,
                    FromNumber     = fromNumber,
                    InterviewId    = interviewId > 0 ? interviewId : (int?)null,
                    IRFId          = int.TryParse(irfId, out int parsedIrfId) ? parsedIrfId : (int?)null,
                    RecipientId    = candidateId > 0 ? candidateId : (int?)null,
                    CallStatus     = "queued",
                    ScheduledDateTime = preferredDateTime,
                    AIModel        = "gpt-4o-realtime-preview",
                    CreatedAt      = DateTime.UtcNow,
                    CreatedBy      = User.Identity.Name
                };

                var logId = await _plivoCallRepository.SaveCallLogAsync(callLog);

                // 2. Build URLs
                var answerUrl  = $"{baseUrl}/PlivoInterviewCall/AnswerXml/{logId}";
                var hangupUrl  = $"{baseUrl}/PlivoInterviewCall/HangupCallback";
                var ringUrl    = $"{baseUrl}/PlivoInterviewCall/RingCallback";

                // 3. Fire the call
                var callRequest = new PlivoOutboundCallRequest
                {
                    To          = phone,
                    From        = fromNumber,
                    AnswerUrl   = answerUrl,
                    AnswerMethod = "GET",
                    HangupUrl   = hangupUrl,
                    RingUrl     = ringUrl,
                    TimeLimit   = 300
                };

                var response = await _plivoService.InitiateCallAsync(callRequest, candidateId > 0 ? candidateId : (int?)null);

                // 4. Persist the RequestUUID
                callLog.Id = logId;
                callLog.PlivoRequestUUID = response.RequestUUID;
                callLog.UpdatedAt = DateTime.UtcNow;
                await _plivoCallRepository.UpdateCallLogAsync(callLog);

                return Json(new
                {
                    success       = true,
                    logId,
                    requestUUID   = response.RequestUUID,
                    message       = "Call initiated successfully"
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Plivo answer URL ─────────────────────────────────────────────────────

        /// <summary>
        /// Plivo calls this URL (GET) when the outbound call is answered.
        /// Returns PlivoXML that instructs Plivo to open a bidirectional WebSocket stream.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ContentResult> AnswerXml(
            int logId,
            string CallUUID      = null,   // Plivo sends these as query/form params
            string CallStatus    = null,
            string From          = null,
            string To            = null,
            string Direction     = null,
            string RequestUUID   = null)
        {
            try
            {
                // Update call log with the real Plivo CallUUID
                var callLog = await _plivoCallRepository.GetCallLogByIdAsync(logId);
                if (callLog != null && !string.IsNullOrEmpty(CallUUID))
                {
                    callLog.PlivoCallUUID = CallUUID;
                    callLog.CallStatus    = "in-progress";
                    callLog.CallStartedAt = DateTime.UtcNow;
                    callLog.UpdatedAt     = DateTime.UtcNow;
                    await _plivoCallRepository.UpdateCallLogAsync(callLog);
                }

                var baseUrl        = _configuration["HostDetails:baseUrl"];
                // Convert http(s) → ws(s) for the WebSocket URL
                var wsBase         = baseUrl.Replace("https://", "wss://").Replace("http://", "ws://");
                var webSocketUrl   = $"{wsBase}/PlivoInterviewCall/AudioStream/{logId}";
                var statusUrl      = $"{baseUrl}/PlivoInterviewCall/StreamStatus";

                var xml = _plivoService.BuildAnswerXml(webSocketUrl, statusUrl);
                return Content(xml, "text/xml");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                // Return a safe fallback XML
                return Content(
                    @"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Speak>Sorry, there was an error. Goodbye.</Speak></Response>",
                    "text/xml");
            }
        }

        // ─── WebSocket audio bridge ───────────────────────────────────────────────

        /// <summary>
        /// WebSocket endpoint for Plivo's bidirectional audio stream.
        /// Bridges audio between Plivo and OpenAI Realtime API.
        /// Handles the submitInterviewSlot function call to book the interview.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task AudioStream(int logId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var plivoWs = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                // Load call context from DB
                var callLog = await _plivoCallRepository.GetCallLogByIdAsync(logId);
                if (callLog == null)
                {
                    await plivoWs.CloseAsync(WebSocketCloseStatus.InternalServerError, "Call log not found", CancellationToken.None);
                    return;
                }

                await RunAiBridgeAsync(plivoWs, callLog);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
            finally
            {
                if (plivoWs.State == WebSocketState.Open)
                    await plivoWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            }
        }

        /// <summary>
        /// Core audio bridge: Plivo ↔ OpenAI Realtime API.
        /// Loads the active assistant config from DB to drive the session.
        /// </summary>
        private async Task RunAiBridgeAsync(WebSocket plivoWs, PlivoCallLog callLog)
        {
            var cts = new CancellationTokenSource();

            // Load assistant config from DB (falls back to appsettings model if none found)
            var assistantConfig = await _plivoCallRepository.GetActiveAssistantConfigAsync();

            var openAiApiKey = _configuration["OpenAI:ApiKey"];
            var model        = assistantConfig?.Model ?? _configuration["Plivo:OpenAIRealtimeModel"] ?? "gpt-4o-realtime-preview";
            var openAiWsUri  = new Uri($"wss://api.openai.com/v1/realtime?model={model}");

            using var openAiWs = new ClientWebSocket();
            openAiWs.Options.SetRequestHeader("Authorization", $"Bearer {openAiApiKey}");
            openAiWs.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            await openAiWs.ConnectAsync(openAiWsUri, cts.Token);

            // Resolve candidate/job context for token interpolation
            string jobTitle    = null;
            string clientName  = null;

            if (callLog.IRFId.HasValue && !string.IsNullOrEmpty(callLog.RecipientEmail))
            {
                try
                {
                    var details = await _plivoCallRepository.GetCandidateDetailsForScheduleAsync(
                        callLog.IRFId.Value, callLog.RecipientEmail);
                    if (details != null)
                    {
                        jobTitle   = string.Empty;
                        clientName = details.ClientHRAttemptContact;
                    }
                }
                catch { /* non-critical */ }
            }

            // Build final instructions — from DB template if available, else code fallback
            string systemPrompt;
            if (assistantConfig != null)
                systemPrompt = InterpolatePrompt(assistantConfig.Instructions,
                    callLog.RecipientName, jobTitle, callLog.IRFId?.ToString(),
                    callLog.RecipientEmail, clientName);
            else
                systemPrompt = BuildSystemPrompt(
                    callLog.RecipientName, jobTitle, callLog.IRFId?.ToString(),
                    callLog.RecipientEmail, clientName);

            // Configure OpenAI session using DB-driven config
            await SendOpenAISessionConfig(openAiWs, systemPrompt, assistantConfig, cts.Token);

            // Run two concurrent loops
            var plivoToOai = ForwardPlivoAudioToOpenAIAsync(plivoWs, openAiWs, callLog, cts);
            var oaiToPlivo = ForwardOpenAIAudioToPlivoAsync(openAiWs, plivoWs, callLog, cts);

            await Task.WhenAny(plivoToOai, oaiToPlivo);
            cts.Cancel();

            // Collect transcript and persist
            try
            {
                callLog.UpdatedAt = DateTime.UtcNow;
                await _plivoCallRepository.UpdateCallLogAsync(callLog);
            }
            catch { }
        }

        /// <summary>
        /// Reads Plivo stream events, converts mulaw → PCM24k, forwards to OpenAI
        /// </summary>
        private async Task ForwardPlivoAudioToOpenAIAsync(
            WebSocket plivoWs,
            ClientWebSocket openAiWs,
            PlivoCallLog callLog,
            CancellationTokenSource cts)
        {
            var buffer = new byte[4096];
            try
            {
                while (!cts.IsCancellationRequested &&
                       plivoWs.State == WebSocketState.Open)
                {
                    var result = await plivoWs.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var evt  = JsonConvert.DeserializeObject<PlivoStreamEvent>(json);
                    if (evt == null) continue;

                    switch (evt.Event)
                    {
                        case "start":
                            // Plivo sends the real CallUUID here — update the DB record
                            if (!string.IsNullOrEmpty(evt.Start?.CallId) &&
                                string.IsNullOrEmpty(callLog.PlivoCallUUID))
                            {
                                callLog.PlivoCallUUID = evt.Start.CallId;
                                callLog.CallStartedAt = DateTime.UtcNow;
                                callLog.CallStatus    = "in-progress";
                                callLog.UpdatedAt     = DateTime.UtcNow;
                                await _plivoCallRepository.UpdateCallLogAsync(callLog);
                            }
                            break;

                        case "media":
                            if (evt.Media?.Payload != null &&
                                openAiWs.State == WebSocketState.Open)
                            {
                                // Decode base64 mulaw, convert to PCM24k, forward to OpenAI
                                var mulawBytes = Convert.FromBase64String(evt.Media.Payload);
                                var pcm24k     = PlivoAudioConverter.MulawToPcm24k(mulawBytes);
                                var pcm24kB64  = Convert.ToBase64String(pcm24k);

                                var appendEvt = new OAIInputAudioBufferAppendEvent { Audio = pcm24kB64 };
                                await SendJsonToWebSocketAsync(openAiWs, appendEvt, cts.Token);
                            }
                            break;

                        case "stop":
                            return;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Reads OpenAI Realtime events: forwards audio to Plivo, handles tool calls
        /// </summary>
        private async Task ForwardOpenAIAudioToPlivoAsync(
            ClientWebSocket openAiWs,
            WebSocket plivoWs,
            PlivoCallLog callLog,
            CancellationTokenSource cts)
        {
            var buffer          = new byte[65536];
            var transcriptParts = new System.Text.StringBuilder();

            try
            {
                while (!cts.IsCancellationRequested &&
                       openAiWs.State == WebSocketState.Open)
                {
                    // OpenAI messages can be large — accumulate until end of message
                    var ms = new System.IO.MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await openAiWs.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    var evt  = JsonConvert.DeserializeObject<OAIRealtimeEvent>(json);
                    if (evt == null) continue;

                    switch (evt.Type)
                    {
                        case "response.audio.delta":
                            // Stream AI audio back to Plivo caller
                            if (!string.IsNullOrEmpty(evt.Delta) &&
                                plivoWs.State == WebSocketState.Open)
                            {
                                var pcm24k     = Convert.FromBase64String(evt.Delta);
                                var mulawBytes = PlivoAudioConverter.Pcm24kToMulaw(pcm24k);
                                if (mulawBytes.Length > 0)
                                {
                                    var playEvent = new PlivoPlayAudioEvent
                                    {
                                        Media = new PlivoPlayAudioMedia
                                        {
                                            Payload = Convert.ToBase64String(mulawBytes)
                                        }
                                    };
                                    await SendJsonToWebSocketAsync(plivoWs, playEvent, cts.Token);
                                }
                            }
                            break;

                        case "response.audio_transcript.delta":
                            if (!string.IsNullOrEmpty(evt.Transcript))
                                transcriptParts.Append(evt.Transcript);
                            break;

                        case "response.audio_transcript.done":
                            // A complete AI utterance — accumulate into log
                            if (!string.IsNullOrEmpty(evt.Transcript))
                                transcriptParts.AppendLine($"[AI]: {evt.Transcript}");
                            break;

                        case "conversation.item.input_audio_transcription.completed":
                            // Caller utterance transcribed
                            if (!string.IsNullOrEmpty(evt.Transcript))
                                transcriptParts.AppendLine($"[Caller]: {evt.Transcript}");
                            break;

                        case "response.function_call_arguments.done":
                            // Handle tool calls — submitInterviewSlot is the key one
                            await HandleOpenAIFunctionCall(
                                openAiWs, plivoWs, evt, callLog, transcriptParts, cts.Token);
                            break;

                        case "error":
                            await _logger.LogMessage(LogLevelInfo.Error,
                                new Exception($"OpenAI Realtime error for call {callLog.Id}: {json}"));
                            break;

                        case "session.created":
                            // Session ready — log it
                            break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
            finally
            {
                // Persist transcript
                callLog.Transcript = transcriptParts.ToString();
                callLog.UpdatedAt  = DateTime.UtcNow;
                try { await _plivoCallRepository.UpdateCallLogAsync(callLog); } catch { }
            }
        }

        /// <summary>
        /// Handles OpenAI function calls — primarily submitInterviewSlot
        /// </summary>
        private async Task HandleOpenAIFunctionCall(
            ClientWebSocket openAiWs,
            WebSocket plivoWs,
            OAIRealtimeEvent evt,
            PlivoCallLog callLog,
            System.Text.StringBuilder transcriptParts,
            CancellationToken ct)
        {
            string functionResult;

            try
            {
                if (evt.Name == "submitInterviewSlot")
                {
                    functionResult = await HandleInterviewSlotSubmission(evt, callLog, transcriptParts);
                }
                else
                {
                    functionResult = JsonConvert.SerializeObject(new { success = false, message = $"Unknown function: {evt.Name}" });
                }
            }
            catch (Exception ex)
            {
                functionResult = JsonConvert.SerializeObject(new { success = false, message = ex.Message });
            }

            // Return function result back to OpenAI so it can continue speaking
            var itemCreateEvt = new OAIConversationItemCreateEvent
            {
                Item = new OAIConversationItem
                {
                    Type   = "function_call_output",
                    CallId = evt.CallId,
                    Output = functionResult
                }
            };
            await SendJsonToWebSocketAsync(openAiWs, itemCreateEvt, ct);

            // Trigger the next response
            var responseCreateEvt = new OAIResponseCreateEvent();
            await SendJsonToWebSocketAsync(openAiWs, responseCreateEvt, ct);
        }

        /// <summary>
        /// Mirrors the Vapi HandleInterviewSlotSubmission logic — parses the function arguments,
        /// validates, saves the booking, and calls the Schedule API
        /// </summary>
        private async Task<string> HandleInterviewSlotSubmission(
            OAIRealtimeEvent evt,
            PlivoCallLog callLog,
            System.Text.StringBuilder transcriptParts)
        {
            try
            {
                if (string.IsNullOrEmpty(evt.Arguments))
                    return JsonConvert.SerializeObject(new { success = false, message = "No arguments provided" });

                var args = JsonConvert.DeserializeObject<Dictionary<string, object>>(evt.Arguments);

                var candidateName     = args.ContainsKey("candidateName")     ? args["candidateName"]?.ToString() : null;
                var candidateEmail    = args.ContainsKey("candidateEmail")    ? args["candidateEmail"]?.ToString() : null;
                var interviewDate     = args.ContainsKey("interviewDate")     ? args["interviewDate"]?.ToString() : null;
                var interviewTime     = args.ContainsKey("interviewTime")     ? args["interviewTime"]?.ToString() : null;
                var timezone          = args.ContainsKey("timezone")          ? args["timezone"]?.ToString() : "IST";
                var candidateConfirmed = args.ContainsKey("candidateConfirmed") && Convert.ToBoolean(args["candidateConfirmed"]);
                var irfId             = args.ContainsKey("irfId")             ? args["irfId"]?.ToString() : null;

                // Basic validation
                if (string.IsNullOrWhiteSpace(candidateName) || candidateName.Contains("@"))
                    return JsonConvert.SerializeObject(new { success = false, message = "Please ask the candidate for their full name." });

                if (!candidateConfirmed)
                    return JsonConvert.SerializeObject(new { success = false, message = "Candidate has not explicitly confirmed. Please get confirmation first." });

                if (string.IsNullOrWhiteSpace(interviewDate) || string.IsNullOrWhiteSpace(interviewTime))
                    return JsonConvert.SerializeObject(new { success = false, message = "Both interview date and time are required." });

                // Normalise natural language dates
                var dateLower = interviewDate.ToLower().Trim();
                if (dateLower == "tomorrow")
                    interviewDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                else if (dateLower == "day after tomorrow" || dateLower == "dayaftertomorrow")
                    interviewDate = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");

                // Parse date/time
                var dateTimeString = $"{interviewDate} {interviewTime}";
                string[] formats = { "yyyy-MM-dd hh:mm tt", "yyyy-MM-dd h:mm tt", "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm" };
                DateTime? parsedDateTime = null;
                foreach (var fmt in formats)
                {
                    if (DateTime.TryParseExact(dateTimeString, fmt,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        parsedDateTime = dt;
                        break;
                    }
                }

                if (!parsedDateTime.HasValue)
                    return JsonConvert.SerializeObject(new { success = false, message = "Could not parse the date/time. Please re-confirm with AM or PM." });

                if (parsedDateTime.Value < DateTime.Now)
                    return JsonConvert.SerializeObject(new { success = false, message = "The time slot is in the past. Please provide a valid future time." });

                if (parsedDateTime.Value > DateTime.Now.AddDays(3))
                    return JsonConvert.SerializeObject(new { success = false, message = "Interviews can only be scheduled within the next 2 days." });

                // Persist booking info on the call log
                callLog.InterviewBooked            = true;
                callLog.BookedInterviewDateTime    = parsedDateTime.Value;
                callLog.BookingConfirmationDetails = JsonConvert.SerializeObject(new
                {
                    candidateName,
                    candidateEmail,
                    interviewDate,
                    interviewTime,
                    timezone,
                    irfId,
                    confirmedAt = DateTime.UtcNow
                });
                callLog.UpdatedAt = DateTime.UtcNow;
                await _plivoCallRepository.UpdateCallLogAsync(callLog);

                transcriptParts.AppendLine($"[BOOKING]: {candidateName} confirmed for {interviewDate} {interviewTime} {timezone}");

                // Guard: don't double-book if already confirmed in InterviewMeetings
                if (!string.IsNullOrEmpty(candidateEmail) && !string.IsNullOrEmpty(irfId) &&
                    int.TryParse(irfId, out int irfIdInt))
                {
                    var hasConfirmed = await _plivoCallRepository.HasConfirmedInterviewAsync(candidateEmail, irfIdInt);

                    if (!hasConfirmed)
                    {
                        await Task.Delay(2000);
                        try
                        {
                            var candidateDetails = await _plivoCallRepository.GetCandidateDetailsForScheduleAsync(irfIdInt, candidateEmail);
                            if (candidateDetails != null)
                            {
                                var dto = new ScheduleCandidatesRequest
                                {
                                    skillChannelId = candidateDetails.IRFId,
                                    requestedBy    = candidateDetails.ClientHRAttemptContact
                                };
                                var roundedDate = parsedDateTime.Value.RoundToNearSlot(candidateDetails.duration);
                                dto.CandidateList.Add(new ScheduleCandidateList
                                {
                                    EmailId       = candidateEmail,
                                    Interviewdate = roundedDate,
                                    ResumeLink    = candidateDetails.ResumeLink,
                                    Phone         = candidateDetails.Phone,
                                    Name          = candidateDetails.CandidateName
                                });

                                var client  = new RestClient("https://api.portal.risebird.io/api/Candidates/Schedule");
                                var request = new RestRequest("https://api.portal.risebird.io/api/Candidates/Schedule", Method.Post);
                                request.AddHeader("content-type", "application/json");
                                request.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                                request.AddHeader("tenant-id", candidateDetails.tenantId);
                                request.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);
                                await client.ExecuteAsync(request);
                            }
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success         = true,
                    message         = $"Perfect! Your interview is confirmed for {interviewDate} at {interviewTime} {timezone}. You'll receive a confirmation email shortly.",
                    interviewDateTime = parsedDateTime.Value.ToString("yyyy-MM-dd HH:mm"),
                    candidateName,
                    candidateEmail,
                    timezone,
                    irfId
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return JsonConvert.SerializeObject(new { success = false, message = $"Internal error: {ex.Message}" });
            }
        }

        // ─── OpenAI session configuration ────────────────────────────────────────

        private async Task SendOpenAISessionConfig(
            ClientWebSocket openAiWs,
            string systemPrompt,
            PlivoAssistantConfig config,
            CancellationToken ct)
        {
            double temperature      = config?.Temperature      ?? 0.8;
            int    maxTokens        = config?.MaxResponseTokens ?? 500;
            double vadThreshold     = config?.VadThreshold     ?? 0.5;
            int    silenceDurationMs = config?.SilenceDurationMs ?? 500;
            string voice            = config?.Voice            ?? "alloy";

            // Always include the core interview-booking tool
            var tools = new List<OAITool>
            {
                new OAITool
                {
                    Type        = "function",
                    Name        = "submitInterviewSlot",
                    Description = "Submit the confirmed interview slot after candidate explicitly confirms. ONLY call this after receiving explicit confirmation.",
                    Parameters  = new OAIToolParameters
                    {
                        Properties = new Dictionary<string, OAIToolProperty>
                        {
                            ["candidateName"]      = new OAIToolProperty { Type = "string",  Description = "Full name of the candidate as confirmed during the call." },
                            ["candidateEmail"]     = new OAIToolProperty { Type = "string",  Description = "Email address of the candidate." },
                            ["interviewDate"]      = new OAIToolProperty { Type = "string",  Description = "Date of interview: 'tomorrow' or 'day after tomorrow'." },
                            ["interviewTime"]      = new OAIToolProperty { Type = "string",  Description = "Time in HH:MM AM/PM format (e.g. '10:00 AM', '02:30 PM')." },
                            ["timezone"]           = new OAIToolProperty { Type = "string",  Description = "Always IST.", Enum = new List<string> { "IST" } },
                            ["candidateConfirmed"] = new OAIToolProperty { Type = "boolean", Description = "True only if candidate explicitly confirmed." },
                            ["irfId"]              = new OAIToolProperty { Type = "string",  Description = "IRF ID for this interview request." }
                        },
                        Required = new List<string>
                        {
                            "candidateName", "candidateEmail", "interviewDate",
                            "interviewTime", "timezone", "candidateConfirmed", "irfId"
                        }
                    }
                }
            };

            var sessionUpdate = new OAISessionUpdateEvent
            {
                Session = new OAISessionConfig
                {
                    Instructions            = systemPrompt,
                    Voice                   = voice,
                    InputAudioFormat        = "pcm16",
                    OutputAudioFormat       = "pcm16",
                    InputAudioTranscription = new OAITranscriptionConfig { Model = "whisper-1" },
                    TurnDetection           = new OAITurnDetectionConfig
                    {
                        Type              = "server_vad",
                        Threshold         = vadThreshold,
                        PrefixPaddingMs   = 300,
                        SilenceDurationMs = silenceDurationMs
                    },
                    Tools                   = tools,
                    ToolChoice              = "auto",
                    Temperature             = temperature,
                    MaxResponseOutputTokens = maxTokens
                }
            };

            await SendJsonToWebSocketAsync(openAiWs, sessionUpdate, ct);
        }

        /// <summary>
        /// Replaces {{token}} placeholders in a DB-stored instructions template
        /// </summary>
        private static string InterpolatePrompt(
            string template,
            string candidateName,
            string jobTitle,
            string irfId,
            string candidateEmail,
            string clientName)
        {
            var today = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
            return template
                .Replace("{{candidateName}}",  candidateName  ?? "the candidate")
                .Replace("{{jobTitle}}",        jobTitle       ?? "the position")
                .Replace("{{irfId}}",           irfId          ?? "N/A")
                .Replace("{{candidateEmail}}",  candidateEmail ?? "")
                .Replace("{{clientName}}",      clientName     ?? "the client")
                .Replace("{{todayDate}}",       today);
        }

        // ─── Plivo callbacks ──────────────────────────────────────────────────────

        /// <summary>
        /// Plivo POSTs to this URL when the call ends
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> HangupCallback(
            string CallUUID      = null,
            string CallStatus    = null,
            string Duration      = null,
            string HangupCause   = null,
            string BillDuration  = null,
            string TotalAmount   = null,
            string RequestUUID   = null)
        {
            try
            {
                var callLog = !string.IsNullOrEmpty(CallUUID)
                    ? await _plivoCallRepository.GetCallLogByCallUUIDAsync(CallUUID)
                    : !string.IsNullOrEmpty(RequestUUID)
                        ? await _plivoCallRepository.GetCallLogByRequestUUIDAsync(RequestUUID)
                        : null;

                if (callLog != null)
                {
                    callLog.CallStatus      = CallStatus ?? callLog.CallStatus;
                    callLog.CallEndedAt     = DateTime.UtcNow;
                    callLog.HangupCause     = HangupCause;
                    callLog.DurationSeconds = int.TryParse(Duration, out int dur) ? dur : callLog.DurationSeconds;
                    if (decimal.TryParse(TotalAmount, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal cost))
                        callLog.CallCost = cost;
                    callLog.UpdatedAt = DateTime.UtcNow;
                    await _plivoCallRepository.UpdateCallLogAsync(callLog);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Ok(); // Always return 200 to Plivo
            }
        }

        /// <summary>
        /// Plivo POSTs to this URL when the call starts ringing
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RingCallback(
            string CallUUID    = null,
            string CallStatus  = null,
            string RequestUUID = null)
        {
            try
            {
                var callLog = !string.IsNullOrEmpty(CallUUID)
                    ? await _plivoCallRepository.GetCallLogByCallUUIDAsync(CallUUID)
                    : !string.IsNullOrEmpty(RequestUUID)
                        ? await _plivoCallRepository.GetCallLogByRequestUUIDAsync(RequestUUID)
                        : null;

                if (callLog != null)
                {
                    callLog.PlivoCallUUID = CallUUID ?? callLog.PlivoCallUUID;
                    callLog.CallStatus    = CallStatus ?? "ringing";
                    callLog.UpdatedAt     = DateTime.UtcNow;
                    await _plivoCallRepository.UpdateCallLogAsync(callLog);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Ok();
            }
        }

        /// <summary>
        /// Plivo POSTs stream connection/disconnection events here
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public IActionResult StreamStatus()
        {
            // Log stream status events for diagnostics
            return Ok();
        }

        // ─── Management endpoints ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetCallStatus(string callUUID)
        {
            try
            {
                var localData   = await _plivoCallRepository.GetCallLogByCallUUIDAsync(callUUID);
                PlivoCallDetails apiData = null;
                try { apiData = await _plivoService.GetCallDetailsAsync(callUUID); } catch { }

                return Json(new { success = true, apiData, localData });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInterviewCalls(int interviewId)
        {
            try
            {
                var calls = await _plivoCallRepository.GetCallLogsByInterviewIdAsync(interviewId);
                return Json(new { success = true, calls });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(
            string callUUID,
            bool isBooked,
            DateTime? bookedDateTime,
            string confirmationDetails)
        {
            try
            {
                var result = await _plivoCallRepository.UpdateBookingStatusAsync(
                    callUUID, isBooked, bookedDateTime, confirmationDetails);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult WebhookTest()
        {
            return Json(new
            {
                status    = "ok",
                message   = "Plivo webhook endpoint is accessible",
                timestamp = DateTime.UtcNow,
                answerUrl = $"{Request.Scheme}://{Request.Host}/PlivoInterviewCall/AnswerXml/{{logId}}",
                wsUrl     = $"wss://{Request.Host}/PlivoInterviewCall/AudioStream/{{logId}}"
            });
        }

        [HttpGet]
        public async Task<IActionResult> WebhookDebugLogs(int count = 20)
        {
            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(_connectionString);
                var logs = await conn.QueryAsync(
                    "SELECT TOP (@Count) * FROM PlivoWebhookDebugLogs ORDER BY RequestTimestamp DESC",
                    new { Count = count });
                return Json(new { success = true, logs });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static async Task SendJsonToWebSocketAsync(WebSocket ws, object payload, CancellationToken ct)
        {
            if (ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        private static async Task SendJsonToWebSocketAsync(ClientWebSocket ws, object payload, CancellationToken ct)
        {
            if (ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        /// <summary>
        /// Generates the system prompt injected at runtime — identical to the Vapi version
        /// </summary>
        private static string BuildSystemPrompt(
            string candidateName,
            string jobTitle,
            string irfId,
            string candidateEmail,
            string clientName)
        {
            candidateName  ??= "the candidate";
            jobTitle       ??= "the position";
            irfId          ??= "N/A";
            candidateEmail ??= "";
            clientName     ??= "the client";

            var dayOfWeek = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            return $@"You are an outbound interview scheduling assistant from Risebird Interviews calling on behalf of {clientName} for urgent hiring.

IDENTITY & PURPOSE:
- You represent Risebird Interviews on behalf of {clientName}
- This is URGENT and time-sensitive hiring - interviews MUST be scheduled within the next TWO days ONLY
- Your goal: Politely convey urgency and secure a confirmed interview slot within the next two days

CURRENT DATE INFORMATION (CRITICAL):
- Today is: {dayOfWeek}
- You can ONLY offer: TOMORROW or DAY AFTER TOMORROW
- When speaking to candidate, use the words tomorrow or day after tomorrow ONLY
- NEVER read out specific dates like numbers
- IRF ID for this interview: {irfId}
- Candidate email for confirmation: {candidateEmail}

CRITICAL RULES:
- Interviews MUST be within the next TWO days from today (tomorrow or day after tomorrow ONLY)
- Do NOT allow scheduling beyond 2 days under any circumstances
- When speaking dates: Always say tomorrow or day after tomorrow - NEVER read out specific date numbers
- If candidate requests later dates: ""I apologize, but due to the urgent nature of this hiring process, we can only accommodate slots within the next two days. Would either tomorrow or the day after work for you?""
- If candidate insists on dates beyond 2 days: Politely explain this is not possible and end call gracefully
- ALL times must be confirmed in IST (Indian Standard Time)

CALL FLOW:
1. Greet and confirm identity
   - ""Am I speaking with {candidateName}?""
   - Wait for confirmation (yes/no)
   - If wrong person: ""I apologize for the confusion. Could you please let {candidateName} know we called? Thank you."" Then end call.

2. State purpose with urgency
   - ""I'm calling on behalf of {clientName} regarding an interview opportunity for the {jobTitle} position.""
   - ""The scope of this call is limited to scheduling your interview slot. For any further details about the role or process, please check your email.""
   - ""This is time-sensitive and we're looking to schedule interviews within the next two days.""
   - ""Would you be available tomorrow or the day after tomorrow?""
   - Wait for response

3. Get specific time
   - ""What time would work best for you?""
   - If they say just ""morning"" or ""afternoon"", ask: ""What specific time would be most convenient? Please specify with AM or PM.""
   - Wait for specific time (e.g., ""10:00 AM"", ""02:30 PM"")

4. Confirm details explicitly
   - ""Thank you! Let me confirm the day and time - that's [tomorrow/day after tomorrow] at [TIME] IST. Is that correct?""
   - Wait for explicit confirmation (""yes"", ""correct"", ""that's right"")
   - IMPORTANT: When confirming, say tomorrow or day after tomorrow, do NOT read out date numbers

5. Submit booking (ONLY after explicit confirmation)
   - Call submitInterviewSlot function with:
     * candidateName: {candidateName}
     * candidateEmail: {candidateEmail}
     * interviewDate: Use tomorrow or day after tomorrow as the value
     * interviewTime: Time in HH:MM AM/PM format (e.g., ""10:00 AM"", ""02:30 PM"")
     * timezone: ""IST""
     * candidateConfirmed: true (ONLY if candidate said yes/confirmed/correct)
     * irfId: {irfId}

6. End professionally with gratitude and DISCONNECT
   - ""Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. We appreciate your time and look forward to speaking with you. Good luck!""
   - After you say this sentence, END THE CALL IMMEDIATELY. Do not say anything else.

OBJECTION HANDLING (BE POLITE BUT PERSISTENT):
- If busy now: ""I completely understand. This will only take 2 minutes to find a suitable time. May we proceed?""
- If asks to call back: ""I appreciate that, and since I have you on the line and this is time-sensitive, could we quickly lock in a time that works for you? It will just take a moment.""
- If uncertain about job: ""I understand. You had applied for this position, and the company is moving quickly with qualified candidates. Would you like to secure an interview slot?""
- If wants different date beyond 2 days: ""I appreciate your preference, but unfortunately, due to the urgent hiring timeline, we can only offer slots within the next two days - either tomorrow or the day after. Would either of those work for you?""
- If asks for role/process/salary/JD details: ""The scope of this call is limited to scheduling your interview slot. For all further details, please check your email. Now, shall we find a time that works for you?""

VALIDATION RULES:
- Date MUST be either tomorrow or day after tomorrow - NO EXCEPTIONS
- Time MUST include AM or PM (e.g., ""10:00 AM"", not just ""10:00"")
- Candidate MUST explicitly confirm the final details
- Do NOT submit booking without explicit confirmation
- ALWAYS include the IRF ID and candidate email when submitting the slot

NEVER:
- Accept dates beyond 2 days from today
- Submit booking without explicit confirmation
- Use 24-hour time format (always use AM/PM)
- Forget to specify timezone as IST
- Read out specific date numbers when speaking";
        }
    }
}
