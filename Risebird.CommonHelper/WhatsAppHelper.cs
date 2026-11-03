using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Sends WhatsApp messages through Interakt. Every message is an Interakt event; an Ongoing campaign in
    /// Interakt listens for the event name and sends one approved template, filling {{n}} from the traits.
    /// Code never names a template. See the WhatsApp (Interakt) developer hand-off for the event/template map.
    /// </summary>
    public class WhatsAppHelper : IWhatsAppHelper
    {
        private const string TrackUsersUrl = "https://api.interakt.ai/v1/public/track/users/";
        private const string TrackEventsUrl = "https://api.interakt.ai/v1/public/track/events/";
        private const string SendMessageUrl = "https://api.interakt.ai/v1/public/message/";

        /// <summary>
        /// An approved Interakt template and where each of its variables comes from. Body and Buttons list trait keys
        /// in template order ("name" = the recipient's name); Buttons[i] fills the {{1}} of URL button i.
        /// </summary>
        private sealed class TemplateSpec
        {
            public string Name;
            public string[] Body;
            public string[] Buttons;
        }

        // Events sent directly as approved templates (Send Template Message API), so no Interakt campaign is needed.
        // Template names can be overridden without a deploy: WhatsApp:Templates:<event without spaces/punctuation>,
        // e.g. WhatsApp:Templates:ScheduleInterview. Events not listed here still go to Track Events (campaign-driven).
        private static readonly Dictionary<string, TemplateSpec> Templates = new Dictionary<string, TemplateSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["Schedule Interview"] = new TemplateSpec { Name = "interview_invitation", Body = new[] { "name", "Client", "JdTitle" }, Buttons = new[] { "ScheduleLink", "UnsubscribeLink" } },
            ["Reschedule Interview"] = new TemplateSpec { Name = "reschedule_interview_candidate_iaas", Body = new[] { "name", "Client", "JdTitle" }, Buttons = new[] { "ScheduleLink", "UnsubscribeLink" } },
            ["Interview Reminder"] = new TemplateSpec { Name = "candidate_interview_reminder_iaas", Body = new[] { "CandidateName", "Client", "JdTitle", "InterviewDate", "TimeZone" }, Buttons = new[] { "buttonlink", "DashboardLink" } },
            ["Interviewer Waiting"] = new TemplateSpec { Name = "interviewer_waiting_for_candidate_iaas_cs", Body = new[] { "CandidateName" }, Buttons = new[] { "Buttonlink" } },
            ["Risebird: Interview Feedback"] = new TemplateSpec { Name = "candidate_rating_after_interview_iaas", Body = new[] { "CandidateName" }, Buttons = new[] { "buttonlink" } },
            ["Feedback Remainder"] = new TemplateSpec { Name = "candidate_rating_after_interview_iaas", Body = new[] { "CandidateName" }, Buttons = new[] { "buttonlink" } },
            ["Naukri Interviewer Invitation"] = new TemplateSpec { Name = "new_interviewer_onboarding_mag_iaas", Body = new[] { "Name", "MagName" }, Buttons = new[] { "RegisterLink", "UnsubscribeLink" } },
            ["Interviewer Common Interview"] = new TemplateSpec { Name = "open_interviews_skill_channel_", Body = new[] { "InterviewerName", "TotalCount" }, Buttons = new[] { "buttonlink", "UnsubscribeLink" } },
            ["Interviewer Confirmation"] = new TemplateSpec { Name = "interviewer_confirmation_joining_or_unassign_iaas", Body = new[] { "InterviewerName" }, Buttons = new[] { "buttonlink", "UnsubscribeLink" } },
            ["Candidate Waiting"] = new TemplateSpec { Name = "candidate_waiting_for_interviewer_iaas", Body = new[] { "InterviewerName", "InterviewId" }, Buttons = new[] { "Buttonlink", "UnsubscribeLink" } },
            ["Emergency Notification"] = new TemplateSpec { Name = "emergency_interview_message_to_other_interviewers_iaas", Body = new[] { "InterviewerName", "InterviewId", "JdTitle" }, Buttons = new[] { "ButtonLink", "UnsubscribeLink" } },
            ["Interviewer Feedback Remainder"] = new TemplateSpec { Name = "interviewer_feedback_reminder_iaas_", Body = new[] { "InterviewerName", "InterviewId", "InterviewDate" }, Buttons = new[] { "ReportLink", "UnsubscribeLink" } },
            ["Interviewer QC Report"] = new TemplateSpec { Name = "interviewer_quality_reporting_iaas", Body = new[] { "InterviewerName", "FlaggedCount" }, Buttons = new[] { "QualityLink", "UnsubscribeLink" } },
        };
        private const string DateFormat = "dd MMM yyyy, hh:mm tt";

        // Audiences for the WhatsApp-only opt-out (dbo.WhatsAppOptOut.Audience).
        public const string AudienceCandidate = "Candidate";
        public const string AudienceInterviewer = "Interviewer";
        public const string AudienceApplicant = "Applicant";

        public const string SkippedOptedOut = "Skipped: opted out";
        public const string SkippedLimitReached = "Skipped: limit reached";
        public const string SkippedNoNumber = "Skipped: no WhatsApp number";
        public const string SentMessage = "Message sent successfully.";
        public const string FailedMessage = "Failed to send message. Please check all fields and try again.";

        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IConfiguration _configuration;
        private readonly string accesskey;
        private readonly IUserService _userService;
        private readonly IWhatsappRepository _whatsappRepository;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private static readonly HashSet<string> KnownCountryCodes = new HashSet<string>
    {
        "1","7","20","27","30","31","32","33","34","36","39","40","41","43","44","45","46","47","48","49",
        "52","54","55","56","57","58","60","61","62","63","64","65","66","81","82","84","86","90","91",
        "92","93","94","95","98","211","212","213","216","218","220","221","222","223","224","225","226",
        "227","228","229","230","231","232","233","234","235","236","237","238","239","240","241","242",
        "243","244","245","246","248","249","250","251","252","253","254","255","256","257","258","260",
        "261","262","263","264","265","266","267","268","269","290","291","297","298","299","350","351",
        "352","353","354","355","356","357","358","359","370","371","372","373","374","375","376","377",
        "378","379","380","381","382","383","385","386","387","389","420","421","423","501","502","503",
        "504","505","506","507","508","509","590","591","592","593","594","595","596","597","598","599",
        "670","672","673","674","675","676","677","678","679","680","681","682","683","685","686","687",
        "688","689","690","691","692","850","852","853","855","856","880","886","960","961","962","963",
        "964","965","966","967","968","970","971","972","973","974","975","976","977","992","993","994",
        "995","996","998"
    };

        // Events that go to a candidate: opt-out is per IRF and the unsubscribe link is the candidate page.
        private static readonly HashSet<string> CandidateEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Schedule Interview", "Reschedule Interview", "Interview Reminder", "Interview Reminder Second",
            "Interviewer Waiting", "Risebird: Interview Feedback", "Feedback Remainder",
            "Candidate Dashboard Reminder", "Apology Notification", "Interview Reschedule Risebird",
            "Interview Reschedule Send Risebird"
        };

        public WhatsAppHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration, IApplicationErrorLogger errorLogger,
            IUserService userService, IWhatsappRepository whatsappRepository, IHttpRequestHelper httpRequestHelper)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _errorLogger = errorLogger;
            accesskey = _configuration["WhatsApp:AccessKey"];
            _userService = userService;
            _whatsappRepository = whatsappRepository;
            _httpRequestHelper = httpRequestHelper;
        }

        #region Shared send path

        private Dictionary<string, string> Headers()
        {
            return new Dictionary<string, string>
            {
                { "Authorization", "Basic " + accesskey },
                { "Content-Type", "application/json" }
            };
        }

        // Send limit per candidate per IRF for the stage this event belongs to; null = no limit.
        private int? SendLimitFor(string eventName)
        {
            if (string.Equals(eventName, "Schedule Interview", StringComparison.OrdinalIgnoreCase))
                return ConfigInt("WhatsApp:MaxNotScheduledSendsPerIrf", 3);
            if (string.Equals(eventName, "Reschedule Interview", StringComparison.OrdinalIgnoreCase))
                return ConfigInt("WhatsApp:MaxRescheduleSendsPerIrf", 3);
            return null;
        }

        private int ConfigInt(string key, int fallback)
        {
            return int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;
        }

        public static string AudienceFor(string eventName)
        {
            return CandidateEvents.Contains(eventName ?? "") ? AudienceCandidate : AudienceInterviewer;
        }

        /// <summary>Button URLs are https://portal.risebird.io/{{1}}: send the path only (no domain, no leading slash).</summary>
        public static string ToPath(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return "";
            link = link.Trim();
            if (Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return uri.PathAndQuery.TrimStart('/');
            return link.TrimStart('/');
        }

        public static string FormatDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            return DateTime.TryParse(value, out var date) ? FormatDate(date) : value;
        }

        public static string FormatDate(DateTime value)
        {
            return value.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        /// <param name="eventName">The message the link is in; saved with the opt-out as WhatsAppOptOut.TemplateEvent.</param>
        public static string CandidateUnsubscribeLink(string email, int irfId, string eventName = null)
        {
            return "Candidate/Unsubscribe?emailid=" + Uri.EscapeDataString(email ?? "") + "&irfid=" + irfId + SourceParam(eventName);
        }

        public static string InterviewerUnsubscribeLink(string uniqueId, string eventName = null)
        {
            return "Interviewer/Unsubscribe?id=" + Uri.EscapeDataString(uniqueId ?? "") + SourceParam(eventName);
        }

        private static string SourceParam(string eventName) =>
            string.IsNullOrWhiteSpace(eventName) ? "" : "&src=" + Uri.EscapeDataString(eventName.Trim());

        public static string FirstName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "there" : name.Trim();
        }

        private (string CountryCode, string Number) SplitPhone(string raw)
        {
            var phone = ExtractCountryCode(raw);
            return (string.IsNullOrWhiteSpace(phone.CountryCode) ? "+91" : phone.CountryCode, phone.MobileNumber);
        }

        private async Task TrackUser(string phoneNumber, string countryCode, string name, string email)
        {
            try
            {
                var bodyData = new
                {
                    userId = "",
                    phoneNumber,
                    countryCode,
                    traits = new Dictionary<string, object> { ["name"] = name ?? "", ["email"] = email ?? "" }
                };
                var response = await _httpRequestHelper.MakeHttpRequest(TrackUsersUrl, Method.Post, bodyData, Headers());
                if (!IsSuccess(response))
                {
                    await _errorLogger.LogMessage(LogLevelInfo.Error, "Interakt track/users failed for " + email + ": " + (response?.Content ?? "no response"));
                }
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private static bool IsSuccess(RestResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.Content))
                return false;
            try
            {
                var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                return obj != null && string.Equals(obj.result, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Sends one event to every address in vm.Emailid (';'-separated). For each recipient: look up the number,
        /// skip opted-out recipients and candidates at the send limit, track the user, post the event, log a failed
        /// response, and record the attempt with its Status.
        /// </summary>
        /// <param name="audience">Candidate / Interviewer for opt-out and the default unsubscribe link; null = neither.</param>
        /// <param name="buildTraits">Builds the event traits from the recipient's name (from UserList).</param>
        private async Task<string> SendToRecipients(WhatsAppVM vm, string audience, Func<string, Dictionary<string, object>> buildTraits)
        {
            try
            {
                if (vm == null)
                    return "User details missing!";
                if (string.IsNullOrWhiteSpace(vm.Emailid))
                    return "User EmailId missing!";

                var results = new List<string>();
                var recipients = vm.Emailid.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var email in recipients)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(email);
                    if (string.IsNullOrWhiteSpace(dataResult.Item1))
                    {
                        results.Add(SkippedNoNumber);
                        continue;
                    }
                    results.Add(await SendOne(vm, email, dataResult.Item1, dataResult.Item2?.Trim(), audience, buildTraits));
                }
                return Summarise(results);
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return FailedMessage;
            }
        }

        private static string Summarise(List<string> results)
        {
            if (results.Count == 0 || results.Any(x => x == SentMessage))
                return results.Count == 0 ? SkippedNoNumber : SentMessage;
            if (results.Any(x => x == FailedMessage))
                return FailedMessage;
            return results.First();
        }

        private async Task<string> SendOne(WhatsAppVM vm, string email, string rawPhone, string recipientName, string audience,
            Func<string, Dictionary<string, object>> buildTraits, int? prospectId = null)
        {
            var phone = SplitPhone(rawPhone);
            vm.Emailid = email;
            vm.CountryCode = phone.CountryCode;
            vm.WhatsAppNumber = phone.Number;

            if (audience == AudienceCandidate && vm.irfId <= 0 && vm.InterviewId > 0)
            {
                vm.irfId = await _whatsappRepository.FetchIrfIdByInterviewIdAsync(vm.InterviewId);
            }

            if (audience != null)
            {
                var optedOut = audience == AudienceApplicant
                    ? await _whatsappRepository.IsOptedOutAsync(audience, email, null, prospectId)
                    : await _whatsappRepository.IsOptedOutAsync(audience, email, audience == AudienceCandidate ? vm.irfId : (int?)null);
                if (optedOut)
                {
                    await SaveAttempt(vm, "Skipped");
                    return SkippedOptedOut;
                }
            }

            var limit = audience == AudienceCandidate ? SendLimitFor(vm.Event) : null;
            if (limit.HasValue && vm.irfId > 0 && await _whatsappRepository.CountSentAsync(email, vm.irfId, vm.Event) >= limit.Value)
            {
                await SaveAttempt(vm, "Skipped");
                return SkippedLimitReached;
            }

            var traits = buildTraits(recipientName) ?? new Dictionary<string, object>();
            if (!traits.ContainsKey("UnsubscribeLink"))
            {
                if (audience == AudienceCandidate && vm.irfId > 0)
                {
                    traits["UnsubscribeLink"] = CandidateUnsubscribeLink(email, vm.irfId, vm.Event);
                }
                else if (audience == AudienceInterviewer)
                {
                    var uniqueId = await _whatsappRepository.FetchUniqueIdByEmailAsync(email);
                    if (!string.IsNullOrEmpty(uniqueId))
                        traits["UnsubscribeLink"] = InterviewerUnsubscribeLink(uniqueId, vm.Event);
                }
            }

            // Buttons need a non-empty value: fall back to the unsubscribe page, which explains an incomplete link.
            if (!traits.ContainsKey("UnsubscribeLink"))
            {
                traits["UnsubscribeLink"] = audience == AudienceInterviewer ? "Interviewer/Unsubscribe?id=" : CandidateUnsubscribeLink(email, vm.irfId, vm.Event);
            }

            await TrackUser(phone.Number, phone.CountryCode, recipientName, email);

            RestResponse response;
            var template = TemplateFor(vm.Event);
            if (template != null)
            {
                var message = BuildTemplateMessage(template, traits, recipientName, phone, vm.Event, out var missing);
                if (missing != null)
                {
                    await _errorLogger.LogMessage(LogLevelInfo.Error, "WhatsApp '" + vm.Event + "' to " + email + " not sent: template " + template.Name + " has no value for " + missing);
                    await SaveAttempt(vm, "Failed");
                    return FailedMessage;
                }
                response = await _httpRequestHelper.MakeHttpRequest(SendMessageUrl, Method.Post, message, Headers());
            }
            else
            {
                var bodyData = new
                {
                    userId = "",
                    phoneNumber = phone.Number,
                    countryCode = phone.CountryCode,
                    @event = vm.Event,
                    traits
                };
                response = await _httpRequestHelper.MakeHttpRequest(TrackEventsUrl, Method.Post, bodyData, Headers());
            }
            if (IsSuccess(response))
            {
                await SaveAttempt(vm, "Sent");
                return SentMessage;
            }

            await _errorLogger.LogMessage(LogLevelInfo.Error, "Interakt event '" + vm.Event + "' failed for " + email + ": " + (response?.Content ?? response?.ErrorMessage ?? "no response"));
            await SaveAttempt(vm, "Failed");
            return FailedMessage;
        }

        private TemplateSpec TemplateFor(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName) || !Templates.TryGetValue(eventName, out var spec))
                return null;
            var overrideName = _configuration["WhatsApp:Templates:" + Regex.Replace(eventName, "[^A-Za-z0-9]", "")];
            return string.IsNullOrWhiteSpace(overrideName) ? spec : new TemplateSpec { Name = overrideName.Trim(), Body = spec.Body, Buttons = spec.Buttons };
        }

        // WhatsApp rejects template values with new lines, tabs or more than 4 spaces in a row.
        private static string CleanValue(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            text = Regex.Replace(text, @"[\r\n\t]+", " ");
            text = Regex.Replace(text, @" {2,}", " ");
            return text.Trim();
        }

        /// <summary>Request body for the Send Template Message API; <paramref name="missing"/> names the first empty variable.</summary>
        private static object BuildTemplateMessage(TemplateSpec template, Dictionary<string, object> traits, string recipientName,
            (string CountryCode, string Number) phone, string eventName, out string missing)
        {
            missing = null;
            string ValueOf(string key)
            {
                if (key == "name")
                    return FirstName(recipientName);
                return traits.TryGetValue(key, out var v) ? CleanValue(v) : "";
            }

            var bodyValues = new List<string>();
            foreach (var key in template.Body)
            {
                var value = ValueOf(key);
                if (value.Length == 0 && missing == null)
                    missing = key;
                bodyValues.Add(value);
            }
            var buttonValues = new Dictionary<string, string[]>();
            for (int i = 0; i < template.Buttons.Length; i++)
            {
                var value = ValueOf(template.Buttons[i]);
                if (value.Length == 0 && missing == null)
                    missing = template.Buttons[i];
                buttonValues[i.ToString(CultureInfo.InvariantCulture)] = new[] { value };
            }

            return new
            {
                countryCode = phone.CountryCode,
                phoneNumber = phone.Number,
                callbackData = eventName,
                type = "Template",
                template = new
                {
                    name = template.Name,
                    languageCode = "en",
                    bodyValues,
                    buttonValues
                }
            };
        }

        private async Task SaveAttempt(WhatsAppVM vm, string status)
        {
            var saved = await BridgeFuncSaveSentWhatsappMessage(vm, status);
            if (!saved)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Error, "Could not record WhatsApp '" + vm.Event + "' (" + status + ") for " + vm.Emailid);
            }
        }

        #endregion

        public async Task<string> WhatsAppTrackUser(WhatsAppVM _whatsAppVM)
        {
            try
            {
                await TrackUser(_whatsAppVM.WhatsAppNumber, _whatsAppVM.CountryCode, _whatsAppVM.UserName, _whatsAppVM.Emailid);
                return "User Added successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to Add User. Please check all fields and try again.";
            }
        }

        public async Task<string> WhatsAppSendEventNotification(WhatsAppVM _whatsAppVM)
        {
            _whatsAppVM.MessageValue = "same time";
            _whatsAppVM.Event = "Interview Notification";
            return await SendToRecipients(_whatsAppVM, null, name => new Dictionary<string, object>
            {
                ["productName"] = _whatsAppVM.MessageValue
            });
        }

        public async Task<string> WhatsAppSendOnAssignation(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, null, name => new Dictionary<string, object>
            {
                ["InterviewDate"] = _whatsAppVM.InterviewDate ?? ""
            });
        }

        /// <summary>
        /// C1 Schedule Interview / C2 Reschedule Interview. Counts toward the 3-per-IRF limit for its stage.
        /// </summary>
        public async Task<string> WhatsAppSendScheduling(WhatsAppVM _whatsAppVM)
        {
            if (string.IsNullOrWhiteSpace(_whatsAppVM?.buttonlink))
                return "Skipped: no scheduling link";
            var scheduleLink = ToPath(_whatsAppVM.buttonlink);
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["Client"] = _whatsAppVM.ClientName ?? "",
                ["JdTitle"] = _whatsAppVM.PrimarySkill ?? "",
                ["ScheduleLink"] = scheduleLink
            });
        }

        /// <summary>I5 Emergency Notification, to the emergency interviewer(s).</summary>
        public async Task<string> WhatsAppSendEInterviewer(WhatsAppVM _whatsAppVM)
        {
            var buttonLink = ToPath(_whatsAppVM?.buttonlink);
            if (string.IsNullOrEmpty(buttonLink))
                return "Skipped: no link";
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["InterviewerName"] = FirstName(name),
                ["InterviewId"] = _whatsAppVM.InterviewId.ToString(),
                ["JdTitle"] = _whatsAppVM.PrimarySkill ?? "",
                ["CandidateName"] = _whatsAppVM.CandidateName ?? _whatsAppVM.UserName ?? "",
                ["ButtonLink"] = buttonLink
            });
        }

        public async Task<string> WhatsAppSendApology(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name)
            });
        }

        /// <summary>
        /// C4 Interviewer Waiting (to the candidate) and I4 Candidate Waiting (to the interviewer). Same helper,
        /// different traits per event. Sent at 2 minutes and again at 5 minutes after start.
        /// </summary>
        public async Task<string> WhatsAppSendInterviewWaiting(WhatsAppVM _whatsAppVM)
        {
            var isCandidateWaiting = string.Equals(_whatsAppVM?.Event, "Candidate Waiting", StringComparison.OrdinalIgnoreCase);
            if (isCandidateWaiting)
            {
                var buttonLink = ToPath(_whatsAppVM.buttonlink);
                if (string.IsNullOrEmpty(buttonLink))
                    return "Skipped: no link";
                return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
                {
                    ["InterviewerName"] = FirstName(name),
                    ["InterviewId"] = _whatsAppVM.InterviewId.ToString(),
                    // The waiting candidate, not the recipient.
                    ["CandidateName"] = _whatsAppVM.CandidateName ?? "",
                    ["InterviewDate"] = FormatDate(_whatsAppVM.InterviewDate),
                    ["Buttonlink"] = buttonLink
                });
            }

            // The approved template (interviewer_waiting_for_candidate_iaas_cs) has one button, https://portal.risebird.io/{{1}},
            // so it can only open a portal page: the candidate's interview page, which carries the Join button.
            // MeetingLink and Buttonlink carry the same path so either mapping in the campaign works.
            var dashboardPath = ToPath(_whatsAppVM?.buttonlink);
            if (string.IsNullOrEmpty(dashboardPath))
                return "Skipped: no link";
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name),
                ["InterviewDate"] = FormatDate(_whatsAppVM.InterviewDate),
                ["MeetingLink"] = dashboardPath,
                ["Buttonlink"] = dashboardPath
            });
        }

        public async Task<string> WhatsAppSendReScheduling(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["Buttonlink"] = ToPath(_whatsAppVM.buttonlink),
                ["CandidateName"] = FirstName(name)
            });
        }

        public async Task<string> WhatsAppSendCancleReScheduling(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name),
                ["ClientName"] = _whatsAppVM.Header ?? "",
                ["ButtonLink"] = ToPath(_whatsAppVM.buttonlink),
                ["Role"] = _whatsAppVM.InterviewDate ?? ""
            });
        }

        public async Task<string> WhatsAppSendRatingNotification(WhatsAppVM _whatsAppVM)
        {
            var candidateName = _whatsAppVM?.UserName;
            return await SendToRecipients(_whatsAppVM, null, name => new Dictionary<string, object>
            {
                ["MessageValue"] = _whatsAppVM.MessageValue ?? "",
                ["CandidateName"] = candidateName ?? "",
                ["InterviewDate"] = _whatsAppVM.InterviewDate ?? "",
                ["Name"] = name ?? ""
            });
        }

        /// <summary>C5 Risebird: Interview Feedback and C6 Feedback Remainder, to the candidate.</summary>
        public async Task<string> WhatsAppSendFeedbackRemainder(WhatsAppVM _whatsAppVM)
        {
            var buttonLink = ToPath(_whatsAppVM?.buttonlink);
            if (string.IsNullOrEmpty(buttonLink))
                return "Skipped: no link";
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name),
                ["buttonlink"] = buttonLink
            });
        }

        /// <summary>I6 Interviewer Feedback Remainder: report not completed.</summary>
        public async Task<string> WhatsAppSendInterviewerFeedbackRemainder(WhatsAppVM _whatsAppVM)
        {
            var reportLink = ToPath(_whatsAppVM?.ReportLink);
            if (string.IsNullOrEmpty(reportLink))
                return "Skipped: no report link";
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["InterviewerName"] = FirstName(name),
                ["InterviewId"] = _whatsAppVM.InterviewId.ToString(),
                ["InterviewDate"] = FormatDate(_whatsAppVM.InterviewDate),
                ["CandidateName"] = _whatsAppVM.CandidateName ?? _whatsAppVM.MessageValue ?? "",
                ["ReportLink"] = reportLink
            });
        }

        /// <summary>I2 Interviewer Common Interview: open interviews in the skill channel. Nothing is sent for a count of 0.</summary>
        public async Task<string> WhatsAppSendInterviewerCommonInterviewDetails(WhatsAppVM _whatsAppVM)
        {
            if (!int.TryParse(_whatsAppVM?.MessageValue, out var totalCount) || totalCount <= 0)
                return "Skipped: no open interviews";
            var buttonLink = ToPath(_whatsAppVM.buttonlink);
            if (string.IsNullOrEmpty(buttonLink))
                return "Skipped: no link";
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["InterviewerName"] = FirstName(name),
                ["TotalCount"] = totalCount.ToString(),
                ["buttonlink"] = buttonLink
            });
        }

        /// <summary>C3 Interview Reminder: confirm availability (button 1) or reschedule / others (button 2).</summary>
        public async Task<string> WhatsAppSendInterviewReminder(WhatsAppVM _whatsAppVM)
        {
            if (string.IsNullOrWhiteSpace(_whatsAppVM?.buttonlink))
                return "Skipped: no link";
            // The approved template (candidate_interview_reminder_iaas) has button 1 as https://portal.risebird.io/{{1}},
            // so send the full path: Interview/CandidateMeetingsStatus?meetingid=...
            var yesLink = ToPath(_whatsAppVM.buttonlink);
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name),
                ["Client"] = _whatsAppVM.ClientName ?? _whatsAppVM.MessageValue ?? "",
                ["JdTitle"] = _whatsAppVM.PrimarySkill ?? "",
                ["InterviewDate"] = FormatDate(_whatsAppVM.InterviewDate),
                ["TimeZone"] = string.IsNullOrWhiteSpace(_whatsAppVM.timezone) ? "IST" : _whatsAppVM.timezone.Trim(),
                ["buttonlink"] = yesLink,
                ["DashboardLink"] = ToPath(_whatsAppVM.DashboardLink)
            });
        }

        public async Task<string> WhatsAppSendClientCompleteInterview(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, null, name => new Dictionary<string, object>
            {
                ["UserName"] = name ?? "",
                ["ButtonLink"] = ToPath(_whatsAppVM.buttonlink)
            });
        }

        private async Task<bool> BridgeFuncSaveSentWhatsappMessage(WhatsAppVM _whatsAppVM, string status = "Sent")
        {
            try
            {
                if (_whatsAppVM != null)
                {
                    var obj = new SentWhatsAppMessageDetails();
                    obj.Recipient = _whatsAppVM.Emailid;
                    obj.CountryCode = _whatsAppVM.CountryCode;
                    obj.WhatsappNumber = _whatsAppVM.WhatsAppNumber;
                    obj.Message = _whatsAppVM.MessageValue;
                    obj.ButtonLink = _whatsAppVM.buttonlink;
                    obj.Event = _whatsAppVM.Event;
                    obj.InterviewDate = _whatsAppVM.InterviewDate;
                    obj.Header = _whatsAppVM.Header;
                    obj.InterviewId = _whatsAppVM.InterviewId != 0 ? _whatsAppVM.InterviewId : null;
                    obj.IRFId = _whatsAppVM.irfId != 0 ? _whatsAppVM.irfId : null;
                    obj.SentBy = _whatsAppVM.SentBy;
                    obj.Status = status;
                    if (_whatsappRepository != null)
                    {
                        return await _whatsappRepository.SaveSentWhatsappMessageDetails(obj);
                    }
                    else
                    {
                        await _errorLogger.LogMessage(LogLevelInfo.Error, "dependency null exception");
                    }
                }
                return false;


            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> WhatsAppSendCandidateDashboardReminder(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, AudienceCandidate, name => new Dictionary<string, object>
            {
                ["CandidateName"] = FirstName(name),
                ["InterviewDate"] = _whatsAppVM.InterviewDate ?? "",
                ["buttonlink"] = _whatsAppVM.buttonlink ?? ""
            });
        }

        public async Task<string> WhatsAppSkillChannelInterviewerAvailbility(WhatsAppVM _whatsAppVM)
        {
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["Name"] = name ?? "",
                ["IRFName"] = _whatsAppVM.MessageValue ?? "",
                ["JDTitle"] = _whatsAppVM.PrimarySkill ?? "",
                ["PerSuccessRate"] = _whatsAppVM.PerSuccessRate ?? "",
                ["Interviewduration"] = _whatsAppVM.Interviewduration ?? "",
                ["buttonlink"] = ToPath(_whatsAppVM.buttonlink)
            });
        }

        /// <summary>
        /// I1 Naukri Interviewer Invitation. Applicants are not in UserList: the phone (vm.Phone), name (vm.UserName)
        /// and prospect id (vm.ProspectId) come from the prospect record.
        /// </summary>
        public async Task<string> WhatsAppSendNaukriInvitation(WhatsAppVM _whatsAppVM)
        {
            try
            {
                if (_whatsAppVM == null || string.IsNullOrWhiteSpace(_whatsAppVM.Phone))
                    return SkippedNoNumber;
                var registerLink = ToPath(_whatsAppVM.buttonlink);
                if (string.IsNullOrEmpty(registerLink))
                    return "Skipped: no registration link";
                _whatsAppVM.Event = "Naukri Interviewer Invitation";
                return await SendOne(_whatsAppVM, _whatsAppVM.Emailid, _whatsAppVM.Phone, _whatsAppVM.UserName, AudienceApplicant, name => new Dictionary<string, object>
                {
                    ["Name"] = FirstName(name),
                    ["MagName"] = _whatsAppVM.MessageValue ?? "",
                    ["RegisterLink"] = registerLink,
                    ["UnsubscribeLink"] = "NaukriOnboarding/ProspectResponseAction?id=" + _whatsAppVM.ProspectId + "&response=NotInterested"
                }, _whatsAppVM.ProspectId);
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return FailedMessage;
            }
        }

        /// <summary>I3 Interviewer Confirmation: confirm presence or unassign, for today's interviews.</summary>
        public async Task<string> WhatsAppSendInterviewerConfirmation(WhatsAppVM _whatsAppVM)
        {
            var buttonLink = ToPath(_whatsAppVM?.buttonlink);
            if (string.IsNullOrEmpty(buttonLink))
                return "Skipped: no link";
            _whatsAppVM.Event = "Interviewer Confirmation";
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["InterviewerName"] = FirstName(name),
                ["buttonlink"] = buttonLink
            });
        }

        /// <summary>
        /// I7a Payment Notification (Amount + Period) and I7b Advance Payment Notification (Amount only).
        /// vm.Event picks the template; amounts are formatted N0 (12,500).
        /// </summary>
        public async Task<string> WhatsAppSendPaymentNotification(WhatsAppVM _whatsAppVM)
        {
            if (_whatsAppVM == null)
                return "User details missing!";
            if (string.IsNullOrWhiteSpace(_whatsAppVM.Event))
                _whatsAppVM.Event = "Payment Notification";
            var amount = decimal.TryParse(_whatsAppVM.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value.ToString("N0", CultureInfo.GetCultureInfo("en-IN"))
                : _whatsAppVM.Amount ?? "";
            var isAdvance = string.Equals(_whatsAppVM.Event, "Advance Payment Notification", StringComparison.OrdinalIgnoreCase);
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name =>
            {
                var traits = new Dictionary<string, object>
                {
                    ["InterviewerName"] = FirstName(name),
                    ["Amount"] = amount
                };
                if (!isAdvance)
                    traits["Period"] = _whatsAppVM.Period ?? "";
                return traits;
            });
        }

        /// <summary>I8 Interviewer QC Report: flagged reports, links to My Quality. Nothing is sent for a count of 0.</summary>
        public async Task<string> WhatsAppSendInterviewerQCReport(WhatsAppVM _whatsAppVM)
        {
            if (_whatsAppVM == null || _whatsAppVM.FlaggedCount <= 0)
                return "Skipped: nothing flagged";
            _whatsAppVM.Event = "Interviewer QC Report";
            return await SendToRecipients(_whatsAppVM, AudienceInterviewer, name => new Dictionary<string, object>
            {
                ["InterviewerName"] = FirstName(name),
                ["FlaggedCount"] = _whatsAppVM.FlaggedCount.ToString(),
                // The approved template (interviewer_quality_reporting_iaas) has a dynamic first button: https://portal.risebird.io/{{1}}.
                ["QualityLink"] = "Interviewer/MyQuality"
            });
        }

        /// <summary>
        /// Extracts the country code (if any) and returns the clean mobile number.
        /// </summary>
        public (string CountryCode, string MobileNumber) ExtractCountryCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ("", "");

            // Remove spaces, dashes, parentheses, etc.
            string phone = Regex.Replace(input, @"[^\d\+]", "");

            // Case 1/2: starts with '+' or '00'. Match a known country code: a plain \d{1,3} split is greedy and
            // turned +919876543210 (how interviewer profiles store numbers) into +919 / 876543210.
            if (phone.StartsWith("+") || phone.StartsWith("00"))
            {
                var digits = phone.StartsWith("+") ? phone.Substring(1) : phone.Substring(2);
                if (Regex.IsMatch(digits, @"^\d+$"))
                {
                    var split = SplitKnownCountryCode(digits);
                    if (split.HasValue)
                        return split.Value;
                    var match = Regex.Match(digits, @"^(\d{1,3}?)(\d{6,14})$");
                    if (match.Success)
                        return ($"+{match.Groups[1].Value}", match.Groups[2].Value);
                }
            }

            // Case 3: Numeric and longer than 10 digits (try known country codes)
            if (Regex.IsMatch(phone, @"^\d+$"))
            {
                if (phone.Length <= 10)
                    return ("", phone); // Local number (no country code)

                var split = SplitKnownCountryCode(phone);
                if (split.HasValue)
                    return split.Value;
            }
            // Default: No country code detected
            return ("", phone.TrimStart('+'));
        }

        private static (string CountryCode, string MobileNumber)? SplitKnownCountryCode(string digits)
        {
            for (int len = 3; len >= 1; len--)
            {
                if (digits.Length > len)
                {
                    string prefix = digits.Substring(0, len);
                    if (KnownCountryCodes.Contains(prefix))
                    {
                        string remainder = digits.Substring(len);
                        if (remainder.Length >= 6 && remainder.Length <= 14)
                        {
                            return ($"+{prefix}", remainder);
                        }
                    }
                }
            }
            return null;
        }
    }
}
