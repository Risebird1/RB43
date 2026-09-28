using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Interviewer service agreement, e-signed through Zoho Sign (My Profile → Service Agreement).
    /// </summary>
    public class InterviewerAgreementController : Controller
    {
        private readonly IInterviewerAgreementService _agreementService;
        private readonly IInterviewerService _interviewerService;
        private readonly IZohoSignHelper _zohoSignHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ZohoSignOptions _signOptions;

        public InterviewerAgreementController(IInterviewerAgreementService agreementService, IInterviewerService interviewerService,
            IZohoSignHelper zohoSignHelper, IApplicationErrorLogger applicationErrorLogger, IOptions<ZohoSignOptions> signOptions)
        {
            _agreementService = agreementService;
            _interviewerService = interviewerService;
            _zohoSignHelper = zohoSignHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _signOptions = signOptions.Value;
        }

        [HttpGet]
        public async Task<JsonResult> Status()
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return Json(new { success = false, message = "Session expired. Please sign in again." });

                var agreement = await _agreementService.GetInterviewerAgreement(profileId.Value);
                // The profile exists, so null means the lookup itself failed (logged in the repository).
                if (agreement == null)
                    return Json(new { success = false, message = "Could not load the agreement status. Please try again later." });
                // Webhooks can be delayed; refresh from Zoho Sign while a request is in flight.
                if (agreement != null && (agreement.HasOpenRequest || agreement.AgreementStatus == InterviewerAgreementStatus.InterviewerSigned))
                    agreement = await _agreementService.SyncAgreementStatus(agreement.AgreementZohoRequestId) ?? agreement;

                return Json(new { success = true, agreement = ToStatusModel(agreement) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not load the agreement status." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Initiate()
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return Json(new { success = false, message = "Session expired. Please sign in again." });

                // Name, father's name and address are filled in on the agreement itself in Zoho Sign.
                var returnUrl = Url.Action(nameof(Return), "InterviewerAgreement", null, Request.Scheme);
                var (agreement, message) = await _agreementService.InitiateAgreement(profileId.Value, returnUrl);
                if (agreement == null)
                    return Json(new { success = false, message });

                // Embedded signing opens Zoho Sign from the portal; email signing (Standard plan) sends the
                // signing link to the interviewer's email.
                var embedded = _signOptions.UseEmbeddedSigning && agreement.HasOpenRequest;
                return Json(new
                {
                    success = agreement.HasOpenRequest,
                    message = message ?? (embedded
                        ? "Agreement generated."
                        : $"The agreement has been emailed to {agreement.EmailId} from Zoho Sign. Please open that email to fill in your details and sign."),
                    signUrl = embedded ? Url.Action(nameof(Sign)) : null,
                    agreement = ToStatusModel(agreement)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The agreement could not be generated right now. Please try again later." });
            }
        }

        /// <summary>
        /// Portal signing: the interviewer enters their details, draws a signature and accepts the
        /// agreement on the Service Agreement tab; the signed PDF is generated by the portal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)]
        public async Task<JsonResult> SignInPortal(string fullName, string fatherName, string addressLine1, string addressLine2,
            string city, string state, string pinCode, string signature, bool agree)
        {
            try
            {
                if (!_signOptions.IsPortalSigning)
                    return Json(new { success = false, message = "Portal signing is not enabled." });

                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return Json(new { success = false, message = "Session expired. Please sign in again." });

                fullName = CleanText(fullName);
                fatherName = CleanText(fatherName);
                addressLine1 = CleanText(addressLine1);
                addressLine2 = CleanText(addressLine2);
                city = CleanText(city);
                pinCode = CleanText(pinCode);
                state = IndianStates.Normalize(state);
                if (!agree)
                    return Json(new { success = false, message = "Please confirm that you have read and agree to the agreement." });
                if (!IsValidText(fullName, 2, 150))
                    return Json(new { success = false, message = "Please enter your full name (English letters, up to 150 characters)." });
                if (!IsValidText(fatherName, 2, 150))
                    return Json(new { success = false, message = "Please enter your father's / guardian's name (English letters, up to 150 characters)." });
                if (!IsValidText(addressLine1, 3, 80))
                    return Json(new { success = false, message = "Please enter your house / flat number and street (English letters, up to 80 characters)." });
                if (addressLine2.Length > 0 && !IsValidText(addressLine2, 2, 60))
                    return Json(new { success = false, message = "Area / locality must be in English letters, up to 60 characters." });
                if (!IsValidText(city, 2, 40))
                    return Json(new { success = false, message = "Please enter your city (English letters, up to 40 characters)." });
                if (state == null)
                    return Json(new { success = false, message = "Please select your state." });
                if (!System.Text.RegularExpressions.Regex.IsMatch(pinCode, @"^[1-9][0-9]{5}$"))
                    return Json(new { success = false, message = "Please enter a valid 6-digit PIN code." });

                // One line on the agreement: "House & street, Area, City, State - PIN".
                var address = $"{string.Join(", ", new[] { addressLine1, addressLine2, city }.Where(p => p.Length > 0))}, {state} - {pinCode}";

                var signatureImage = DecodeSignature(signature);
                if (signatureImage == null)
                    return Json(new { success = false, message = "Please draw your signature in the box." });

                var (agreement, message) = await _agreementService.SignInPortal(profileId.Value, new PortalSignatureStamp
                {
                    FullName = fullName,
                    FatherName = fatherName,
                    Address = address,
                    SignatureImage = signatureImage,
                    IpAddress = GetClientIp()
                });
                if (agreement == null)
                    return Json(new { success = false, message });

                return Json(new
                {
                    success = agreement.IsAgreementSigned,
                    message = message ?? "Thank you! Your service agreement is signed.",
                    agreement = ToStatusModel(agreement)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The agreement could not be signed right now. Please try again later." });
            }
        }

        private static string CleanText(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

        // The agreement is printed with a Latin (CP1252) font, so values must be in English letters.
        private static bool IsValidText(string value, int min, int max) =>
            value.Length >= min && value.Length <= max && value.All(c => c >= 32 && c <= 126);

        // Signature pad output: "data:image/png;base64,...". Returns null if it is not a PNG.
        private static byte[] DecodeSignature(string dataUrl)
        {
            const string prefix = "data:image/png;base64,";
            if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith(prefix, StringComparison.Ordinal) || dataUrl.Length > 1_500_000)
                return null;
            try
            {
                var bytes = Convert.FromBase64String(dataUrl.Substring(prefix.Length));
                var isPng = bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
                return isPng ? bytes : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        // Behind Azure App Service the client address comes in X-Forwarded-For.
        private string GetClientIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].ToString();
            var ip = !string.IsNullOrWhiteSpace(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : HttpContext.Connection.RemoteIpAddress?.ToString();
            // App Service appends the port, e.g. "1.2.3.4:5678".
            if (ip != null && ip.Count(c => c == ':') == 1)
                ip = ip.Split(':')[0];
            return string.IsNullOrEmpty(ip) ? "unknown" : ip;
        }

        /// <summary>Email signing: sends the Zoho Sign signing email again.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Resend()
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return Json(new { success = false, message = "Session expired. Please sign in again." });

                var sent = await _agreementService.ResendAgreementEmail(profileId.Value);
                return Json(new
                {
                    success = sent,
                    message = sent ? "The signing email has been sent again. Please check your inbox (and spam folder)." : "Could not resend the email. Please try again later."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not resend the email. Please try again later." });
            }
        }

        /// <summary>
        /// Sends the interviewer to Zoho Sign's signing page. Embed tokens are short-lived,
        /// so a fresh one is generated on every visit.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Sign()
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return RedirectToAction("SignOut", "Session");

                if (!_signOptions.UseEmbeddedSigning)
                {
                    TempData["AgreementMessage"] = "The agreement signing link has been sent to your email from Zoho Sign.";
                    return RedirectToAction("MyProfile", "Interviewer");
                }

                var signUrl = await _agreementService.GetEmbeddedSignUrl(profileId.Value, $"{Request.Scheme}://{Request.Host}");
                if (string.IsNullOrEmpty(signUrl))
                {
                    TempData["AgreementMessage"] = "The agreement is not available for signing. Please refresh My Profile and try again.";
                    return RedirectToAction("MyProfile", "Interviewer");
                }
                return Redirect(signUrl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["AgreementMessage"] = "Could not open the agreement for signing. Please try again later.";
                return RedirectToAction("MyProfile", "Interviewer");
            }
        }

        /// <summary>Zoho Sign redirects here after the interviewer signs, declines or chooses "sign later".</summary>
        [HttpGet]
        public async Task<IActionResult> Return()
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return RedirectToAction("SignOut", "Session");

                var agreement = await _agreementService.GetInterviewerAgreement(profileId.Value);
                if (!string.IsNullOrEmpty(agreement?.AgreementZohoRequestId))
                    agreement = await _agreementService.SyncAgreementStatus(agreement.AgreementZohoRequestId) ?? agreement;

                TempData["AgreementMessage"] = agreement?.AgreementStatus switch
                {
                    InterviewerAgreementStatus.Completed => "Thank you! Your service agreement is signed.",
                    InterviewerAgreementStatus.InterviewerSigned => "Thank you for signing. The agreement is awaiting Risebird's countersignature.",
                    InterviewerAgreementStatus.Declined => "You declined the service agreement.",
                    _ => "Your service agreement is not signed yet."
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return RedirectToAction("MyProfile", "Interviewer");
        }

        /// <summary>The agreement as it will be signed, shown inline on the Service Agreement tab.</summary>
        [HttpGet]
        public async Task<IActionResult> Document()
        {
            try
            {
                var profileName = GetSessionValue("profileName");
                if (GetSessionValue("email") == null || profileName == null)
                    return RedirectToAction("SignOut", "Session");
                if (profileName != "Interviewer" && profileName != "Admin" && profileName != "Operations")
                    return Forbid();

                var pdf = await _agreementService.GetAgreementDocumentForReview();
                if (pdf == null)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        $"Interviewer agreement document unavailable: source={_signOptions.DocumentSource}, container={_signOptions.DocumentBlobContainer}, " +
                        $"document={_signOptions.DocumentName}, layout={_signOptions.ResolvedLayoutName}, localFolder={_signOptions.LocalDocumentFolder}.",
                        GetSessionValue("email"));
                    return NotFound();
                }

                Response.Headers["Content-Disposition"] = "inline; filename=\"Risebird_Service_Agreement.pdf\"";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return NotFound();
            }
        }

        /// <summary>Signed agreement PDF. Interviewers get their own; Admin / Operations can pass a profile id.</summary>
        [HttpGet]
        public async Task<IActionResult> Download(int? interviewerProfileId = null)
        {
            try
            {
                var profileName = GetSessionValue("profileName");
                if (GetSessionValue("email") == null || profileName == null)
                    return RedirectToAction("SignOut", "Session");

                int? profileId;
                if (profileName == "Interviewer")
                    profileId = await GetSessionInterviewerProfileId();
                else if ((profileName == "Admin" || profileName == "Operations") && interviewerProfileId > 0)
                    profileId = interviewerProfileId;
                else
                    return Forbid();

                if (profileId == null)
                    return NotFound();

                var agreement = await _agreementService.GetInterviewerAgreement(profileId.Value);
                var pdf = await _agreementService.GetSignedDocument(agreement);
                if (pdf == null)
                    return NotFound();

                Response.Headers["Content-Disposition"] = $"inline; filename=\"Risebird_Service_Agreement_{profileId}.pdf\"";
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return NotFound();
            }
        }

        // ── Zoho Sign webhook (Zoho Sign → our server) ───────────────────────

        /// <summary>
        /// Zoho Sign webhook. The payload is only used to find the request id; the status is
        /// always re-read from the Zoho Sign API, so a forged call cannot mark anything signed.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                string rawBody;
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();

                if (!_zohoSignHelper.IsWebhookSignatureValid(rawBody, Request.Headers["X-ZS-WEBHOOK-SIGNATURE"].ToString()))
                    return Unauthorized(new { error = "Invalid signature." });

                string requestId = null;
                using (var doc = JsonDocument.Parse(rawBody))
                {
                    if (doc.RootElement.TryGetProperty("requests", out var requests)
                        && requests.TryGetProperty("request_id", out var id))
                    {
                        requestId = id.ValueKind == JsonValueKind.String ? id.GetString() : id.GetRawText();
                    }
                }
                if (string.IsNullOrEmpty(requestId))
                    return Ok();   // not a document event; acknowledge so Zoho doesn't retry

                await _agreementService.SyncAgreementStatus(requestId);
                return Ok();
            }
            catch (JsonException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<int?> GetSessionInterviewerProfileId()
        {
            var email = GetSessionValue("email");
            var profileName = GetSessionValue("profileName");
            if (email == null || profileName != "Interviewer")
                return null;

            var profile = await _interviewerService.GetInterviewerProfileByEmailId(email, profileName);
            return profile?.id > 0 ? profile.id : null;
        }

        private string GetSessionValue(string key) => HttpContext.Session.GetString(key)?.Trim().Replace("\"", "");

        private object ToStatusModel(InterviewerAgreementVM agreement)
        {
            if (agreement == null)
                return null;
            return new
            {
                status = agreement.AgreementStatus,
                isSigned = agreement.IsAgreementSigned,
                canSign = agreement.HasOpenRequest,
                mode = _signOptions.IsPortalSigning ? "Portal" : "ZohoSign",
                signByEmail = !_signOptions.UseEmbeddedSigning,
                email = agreement.EmailId,
                version = agreement.AgreementVersion,
                signerName = agreement.AgreementSignerName,
                fatherName = agreement.AgreementFatherName,
                address = agreement.AgreementAddress,
                sentOn = agreement.AgreementSentOn?.ToString("dd MMM yyyy, hh:mm tt"),
                signedOn = agreement.AgreementSignedOn?.ToString("dd MMM yyyy, hh:mm tt"),
                completedOn = agreement.AgreementCompletedOn?.ToString("dd MMM yyyy, hh:mm tt"),
                declineReason = agreement.AgreementDeclineReason
            };
        }
    }
}
