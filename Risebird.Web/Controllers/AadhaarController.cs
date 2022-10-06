using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class AadhaarController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAadhaarService _aadhaarService;
        private readonly string apiendpoint;
        private readonly string client_code;
        private readonly string api_key;
        private readonly string salt;

        public AadhaarController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper, IApplicationErrorLogger applicationErrorLogger, IAadhaarService aadhaarService)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            apiendpoint = _configuration["AadhaarApiDetails:endPoint"];
            client_code = _configuration["AadhaarApiDetails:client_code"];
            api_key = _configuration["AadhaarApiDetails:api_key"];
            salt = _configuration["AadhaarApiDetails:salt"];
            _aadhaarService = aadhaarService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> GenerateAadhaarCaptcha(string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.api_key = api_key;
            model.request.request_id = Guid.NewGuid().ToString();
            var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + api_key + "|" + salt;
            model.request.hash = getHashSha256(hashstring);
            var client = new RestClient(apiendpoint + "getCaptcha");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {

            }
            return Json(new { image = convertedResponse.response_data.captcha, uuid = convertedResponse.response_data.uuid });
        }
        [HttpPost]
        public async Task<JsonResult> SendAadhaarNumber(string uuid, string aadharnumber, string aadharcaptcha, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.aadhaar = aadharnumber;
            model.request.captcha = aadharcaptcha;
            model.request.verification_type = "OTP";
            model.request.consent = "YES";
            var client = new RestClient(apiendpoint + "enterAadhaar");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {
                return Json(convertedResponse.response_status.message);
            }
            return Json(convertedResponse.response_data.message);
        }
        [HttpPost]
        public async Task<JsonResult> SubmitOTPNumber(string uuid, string otp, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.otp = otp;
            model.request.share_code = "1234";
            var client = new RestClient(apiendpoint + "enterOtp");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            return Json("");
        }
        [HttpPost]
        public async Task<JsonResult> FetchAadhaarKYC(string uuid, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.api_key = api_key;
            var hashstring = model.headers.client_code + "|" + model.request.uuid + "|" + api_key + "|" + salt;
            model.request.hash = getHashSha256(hashstring);
            var client = new RestClient(apiendpoint + "fetchKYCData");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {

            }
            return Json(convertedResponse.response_data);
        }

        public void SetHeaderValue(AadhaarHeader _header, string datenow)
        {
            _header.client_code = client_code;
            _header.sub_client_code = client_code;
            _header.channel_code = "ANDROID_SDK";
            _header.channel_version = "1";
            _header.stan = salt;
            _header.client_ip = "";
            _header.transmission_datetime = datenow;
            _header.operation_mode = "SELF";
            _header.run_mode = "DEFAULT";
            _header.actor_type = "DEFAULT";
            _header.user_handle_type = "EMAIL";
            _header.user_handle_value = "talent@risebird.io";
            _header.location = "";
            _header.function_code = "DEFAULT";
            _header.function_sub_code = "DEFAULT";
        }
        public string getHashSha256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        [HttpPost]
        public async Task<JsonResult> FetchAadhaarDetails(string MCQID, string candidateEmail)
        {
            var model = await _aadhaarService.FetchAadhaarDetails(MCQID, candidateEmail);
            if (model == null)
            {
                model = new AadhaarData();
            }
            return Json(model);
        }
        public async Task<IActionResult> AadhaarVerification(string uniqueid)
        {
            var candidateEmailid = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            var model = new AadhaarData();
            if (candidateEmailid == null)
            {

                model.status = "InvalidEmail";
            }

            else if (candidateEmailid.AadhaarVerified == true)
            {
                model.status = "Unauthorized";
            }
            else
            {
                model.email = candidateEmailid.EmailId;
            }

            return View(model);
        }
        public async Task<JsonResult> SaveAadhaarDetails(string Name, string Address, string gender, string dob, string email, string doc_face)
        {
            var aadhaarKYCmodel = new AadhaarData();
            aadhaarKYCmodel.Name = Name;
            aadhaarKYCmodel.Address = Address;
            aadhaarKYCmodel.gender = gender;
            aadhaarKYCmodel.dob = dob;
            aadhaarKYCmodel.doc_face = doc_face;
            aadhaarKYCmodel.email = email;
            aadhaarKYCmodel.reference_id = "0";
            aadhaarKYCmodel.verified_using = "Interview";
            await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
            return Json("Aadhaar Details are verified successfully.");
        }
        [HttpPost]
        public async Task<JsonResult> FetchInterviewAadhaarDetails(string candidateEmail)
        {
            var model = await _aadhaarService.FetchInterviewAadhaarDetails(candidateEmail);
            if (model == null)
            {
                model = new AadhaarData();
            }
            return Json(model);
        }
        [HttpPost]
        public async Task<JsonResult> ReGenerateAadhaarCaptcha(string datenow, string uuid)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            var client = new RestClient(apiendpoint + "getNewCaptcha");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse != null)
            {
                if (convertedResponse.response_status.status.ToLower() == "fail")
                {

                }
                return Json(new { image = convertedResponse.response_data.captcha, uuid = model.request.uuid });
            }
            return Json(new { image = "", uuid = model.request.uuid });
        }
        #region US Candidate Verification
        public async Task<IActionResult> CandidateVerification(string uniqueid)
        {
            var candidateEmailid = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            var model = new AadhaarData();
            if (candidateEmailid == null)
            {
                model.status = "InvalidEmail";
            }
            else
            {
                model.email = candidateEmailid.EmailId;
            }
            return View(model);
        }
        public async Task<JsonResult> FetchVerificationURL(string Name, string PhoneNumber, string Emailid)
        {
            Random generator = new Random();
            int r = generator.Next(10000, 100000);
            candidateVerification _list = new candidateVerification();
            _list.reference = Guid.NewGuid().ToString();
            _list.callback_url = "https://portal.risebird.io/Aadhaar/Getshuftipro";
            _list.email = Emailid;
            _list.country = "GB";
            _list.language = "EN";
            _list.verification_mode = "any";

            _list.document.proof = "";
            _list.document.supported_types = new string[] { "id_card", "driving_license", "passport" };

            _list.document.name.first_name = "";
            _list.document.name.middle_name = "";
            _list.document.name.last_name = "";

            _list.phone.phone_number = PhoneNumber;
            _list.phone.random_code = r.ToString();
            _list.phone.text = "Your verification code is " + r.ToString();
            var client = new RestClient("https://api.shuftipro.com/");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic " + "MDA4M2YwODYwMDU1MmYzOWQ4MjZhZWM2NDUzMmM5NDU2MWE5ZDhlM2YyYWRhMGQ1MGFlZGFkYjdhMzUyYTVmZTpHdW16QjJpTDVDTzhGcUM4d2ExdnFHMGd4U21mVkY2SQ==");
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(_list);
            IRestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<verifyResponse>(response.Content);
            return Json(convertedResponse.verification_url);
        }
        [HttpPost]
        public async Task<JsonResult> Getshuftipro(Object _model)
        {
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Info, _model.ToString());
            return Json("");
        }
        #endregion

       [Authorize]
       [HttpGet]
        public async Task<IActionResult> AadharDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin"|| profileName == "SupportAdmin")
                    {                     
                        var adhardetail = await _aadhaarService.GetAadharDetails();
                        return View(adhardetail);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Interview");

            }
        }
    }
}
