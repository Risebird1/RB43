using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using RestSharp;
using System.Text;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using Newtonsoft.Json;

namespace Risebird.Web.Controllers
{
    public class AadhaarController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string apiendpoint;
        private readonly string client_code;
        private readonly string api_key;
        private readonly string salt;

        public AadhaarController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            apiendpoint = _configuration["AadhaarApiDetails:endPoint"];
            client_code = _configuration["AadhaarApiDetails:client_code"];
            api_key = _configuration["AadhaarApiDetails:api_key"];
            salt = _configuration["AadhaarApiDetails:salt"];
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
    }
}
