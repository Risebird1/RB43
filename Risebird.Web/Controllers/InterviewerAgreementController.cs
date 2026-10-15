using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Interviewer service agreement (My Profile → SERVICE AGREEMENT): the interviewer reads the agreement,
    /// fills in their details and signs; the details and signature are saved on their profile.
    /// </summary>
    public class InterviewerAgreementController : Controller
    {
        private readonly IInterviewerAgreementService _agreementService;
        private readonly IInterviewerService _interviewerService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewerAgreementController(IInterviewerAgreementService agreementService, IInterviewerService interviewerService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _agreementService = agreementService;
            _interviewerService = interviewerService;
            _applicationErrorLogger = applicationErrorLogger;
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
                if (agreement == null)
                    return Json(new { success = false, message = "Could not load the agreement status. Please try again later." });

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
        [RequestSizeLimit(8_000_000)]
        public async Task<JsonResult> Sign(string fullName, string fatherName, string addressLine1, string addressLine2,
            string city, string state, string pinCode, string signature, string screenshot, bool agree)
        {
            try
            {
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
                    return Json(new { success = false, message = "Please enter your full name (up to 150 characters)." });
                if (!IsValidText(fatherName, 2, 150))
                    return Json(new { success = false, message = "Please enter your father's / guardian's name (up to 150 characters)." });
                if (!IsValidText(addressLine1, 3, 80))
                    return Json(new { success = false, message = "Please enter your house / flat number and street (up to 80 characters)." });
                if (addressLine2.Length > 0 && !IsValidText(addressLine2, 2, 60))
                    return Json(new { success = false, message = "Area / locality can be up to 60 characters." });
                if (!IsValidText(city, 2, 40))
                    return Json(new { success = false, message = "Please enter your city (up to 40 characters)." });
                if (state == null)
                    return Json(new { success = false, message = "Please select your state." });
                if (!Regex.IsMatch(pinCode, @"^[1-9][0-9]{5}$"))
                    return Json(new { success = false, message = "Please enter a valid 6-digit PIN code." });

                var signatureImage = DecodeSignature(signature);
                if (signatureImage == null)
                    return Json(new { success = false, message = "Please draw or upload your signature." });

                var address = $"{string.Join(", ", new[] { addressLine1, addressLine2, city }.Where(p => p.Length > 0))}, {state} - {pinCode}";
                var error = await _agreementService.SignAgreement(profileId.Value, new InterviewerAgreementSignRequest
                {
                    SignerName = fullName,
                    FatherName = fatherName,
                    Address = address,
                    SignatureImage = signatureImage,
                    // Proof screenshot of the signing page: optional, signing never depends on it.
                    ProofImage = DecodeScreenshot(screenshot)
                });
                if (error != null)
                    return Json(new { success = false, message = error });

                var agreement = await _agreementService.GetInterviewerAgreement(profileId.Value);
                return Json(new { success = true, message = "Thank you! Your service agreement is signed.", agreement = ToStatusModel(agreement) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "The agreement could not be signed right now. Please try again later." });
            }
        }

        /// <summary>The signed-in interviewer's own signature image.</summary>
        [HttpGet]
        public Task<IActionResult> Signature() => AgreementImage(proof: false);

        /// <summary>The signed-in interviewer's own proof screenshot of the signing page.</summary>
        [HttpGet]
        public Task<IActionResult> Proof() => AgreementImage(proof: true);

        private async Task<IActionResult> AgreementImage(bool proof)
        {
            try
            {
                var profileId = await GetSessionInterviewerProfileId();
                if (profileId == null)
                    return Unauthorized();

                var image = await _agreementService.GetAgreementImage(profileId.Value, proof);
                if (image == null)
                    return NotFound();
                return File(image.Value.Content, image.Value.ContentType);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return NotFound();
            }
        }

        private static string CleanText(string value) => Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

        private static bool IsValidText(string value, int min, int max) => value.Length >= min && value.Length <= max;

        // Signature from the page: "data:image/png;base64,...". Returns null if it is not a PNG.
        private static byte[] DecodeSignature(string dataUrl) =>
            DecodeImage(dataUrl, "data:image/png;base64,", 1_500_000, b => b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47);

        // Proof screenshot of the page: "data:image/jpeg;base64,...". Returns null if missing or not a JPEG.
        private static byte[] DecodeScreenshot(string dataUrl) =>
            DecodeImage(dataUrl, "data:image/jpeg;base64,", 6_000_000, b => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF);

        private static byte[] DecodeImage(string dataUrl, string prefix, int maxLength, Func<byte[], bool> hasSignature)
        {
            if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith(prefix, StringComparison.Ordinal) || dataUrl.Length > maxLength)
                return null;
            try
            {
                var bytes = Convert.FromBase64String(dataUrl.Substring(prefix.Length));
                return bytes.Length > 8 && hasSignature(bytes) ? bytes : null;
            }
            catch (FormatException)
            {
                return null;
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

        private static object ToStatusModel(InterviewerAgreementVM agreement) => agreement == null ? null : new
        {
            isSigned = agreement.IsAgreementSigned,
            signerName = agreement.AgreementSignerName,
            fatherName = agreement.AgreementFatherName,
            address = agreement.AgreementAddress,
            version = agreement.AgreementVersion,
            signedOn = agreement.AgreementSignedOn?.ToString("dd MMM yyyy, hh:mm tt"),
            hasSignature = !string.IsNullOrEmpty(agreement.AgreementSignatureUrl),
            hasProof = !string.IsNullOrEmpty(agreement.AgreementProofUrl)
        };
    }
}
