using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
//using MCQ_Risebird.DataAccess;
//using MCQ_Risebird.Models;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ScreenBackgroundJobs
{
    public class UploadAndVerifyCandidateImage
    {
        private readonly IConfiguration _configuration;

        //private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string client_code;
        //private readonly string api_key;
        //private readonly string salt;
        //private readonly string apiendpoint;
        private readonly string faceCompareApiendpoint;
        private readonly string faceCompare_api_key;
        private readonly string faceCompare_salt;
        private static readonly RestClient client = new RestClient();
        public UploadAndVerifyCandidateImage(IConfiguration configuration, IAadhaarRepository aadhaarRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            //_applicationErrorLogger = applicationErrorLogger;
            _aadhaarRepository = aadhaarRepository;
            client_code = _configuration["AadhaarApiDetails:client_code"];
            //api_key = _configuration["AadhaarApiDetails:api_key"];
            //salt = _configuration["AadhaarApiDetails:salt"];
            //apiendpoint = _configuration["AadhaarApiDetails:endPoint"];
            faceCompareApiendpoint = _configuration["AadhaarApiDetails:faceCompareEndPoint"];
            faceCompare_api_key = _configuration["AadhaarApiDetails:faceCompareApiKey"];
            faceCompare_salt = _configuration["AadhaarApiDetails:faceCompareSalt"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        [FunctionName("UploadAndVerifyCandidateImage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadAndVerifyCandidateImage")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"UploadAndVerifyCandidateImage : C# HTTP trigger function invoked at {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                log.LogInformation($"UploadAndVerifyCandidateImage : Event data recieved with request body :  {requestBody}");

                var data = JsonConvert.DeserializeObject<VerifyImageDTO>(requestBody);

                var score = await Verification_FaceMatch_veri5(data);
                log.LogInformation($"UploadAndVerifyCandidateImage : score api executed");

                await _aadhaarRepository.SaveFaceMatchScorePercentage(data.candidateEmail, score);

                log.LogInformation($"UploadAndVerifyCandidateImage : C# Http Trigger finished executing at {DateTime.Now}");

                return new OkObjectResult(true);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogError($"UploadAndVerifyCandidateImage :Error executing at: {DateTime.Now}, Message : {ex.Message}");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<int> Verification_FaceMatch_veri5(VerifyImageDTO dto)
        {
            try
            {
                var model = new AadhaarVM();
                var datenow = GetCurrentUnixTimestampMillis();
                SetHeaderValue(model.headers, datenow);
                model.request.api_key = faceCompare_api_key;
                model.request.request_id = Guid.NewGuid().ToString();
                model.request.purpose = "VERIFY";
                model.request.image_1 = dto.imageFirst; //ConvertImageURLToBase64(dto.imageFirst);
                model.request.image_2 = dto.imageSecond; // ConvertImageURLToBase64(dto.imageSecond);
                //var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + api_key + "|" + salt;
                var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + faceCompare_api_key + "|" + faceCompare_salt;
                model.request.hash = await getHashSha256(hashstring);

                Dictionary<string, string> headers = new Dictionary<string, string>
            {
                //{ "Authorization", "Basic " + accesskey },
                { "Content-Type", "application/json" }
            };

                //var response = await _httpRequestHelper.MakeHttpRequest(dto.apiendpoint + "getCaptcha", Method.Post, model, headers);
                var response = await MakeHttpRequest(faceCompareApiendpoint + "faceCompare", Method.Post, model, headers);

                var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
                if (convertedResponse.response_status.status.ToLower() == "fail")
                {
                    return 0;
                }
                var score = convertedResponse.response_data.score != null ? convertedResponse.response_data.score.Split(".")[0] : "0";
                return Convert.ToInt16(score);//convertedResponse.response_data.score
                //return Convert.ToInt16(convertedResponse.response_data.score);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }
        //public string GetCurrentUnixTimestampMillis()
        //{
        //    DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    DateTime localDateTime, univDateTime;
        //    localDateTime = DateTime.Now;
        //    univDateTime = localDateTime.ToUniversalTime();
        //    return ((univDateTime - UnixEpoch).TotalMilliseconds).ToString();
        //}

        public string GetCurrentUnixTimestampMillis()
        {
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime localDateTime, univDateTime;
            localDateTime = DateTime.Now;
            univDateTime = localDateTime.ToUniversalTime();
            var millis = (univDateTime - UnixEpoch).TotalMilliseconds.ToString();
            return millis.Split(".")[0];
        }

        //public void SetHeaderValue(AadhaarHeader _header, string datenow)
        //{
        //    _header.client_code = client_code;
        //    _header.sub_client_code = client_code;
        //    _header.channel_code = "ANDROID_SDK";
        //    _header.channel_version = "1";
        //    _header.stan = faceCompare_salt;
        //    _header.client_ip = "";
        //    _header.transmission_datetime = datenow;
        //    _header.operation_mode = "SELF";
        //    _header.run_mode = "DEFAULT";
        //    _header.actor_type = "DEFAULT";
        //    _header.user_handle_type = "EMAIL";
        //    _header.user_handle_value = "talent@risebird.io";
        //    _header.location = "";
        //    _header.function_code = "DEFAULT";
        //    _header.function_sub_code = "DEFAULT";
        //}
        public void SetHeaderValue(AadhaarHeader _header, string datenow)
        {
            _header.client_code = client_code;
            _header.sub_client_code = client_code;
            _header.channel_code = "";// "ANDROID_SDK";
            _header.channel_version = "";//"1";
            _header.stan = GenerateRandomAlphanumeric();// faceCompare_salt + Guid.NewGuid().ToString();
            _header.client_ip = "";
            _header.transmission_datetime = Convert.ToInt64(datenow);
            _header.operation_mode = "DEFAULT";//"SELF";
            _header.run_mode = "REAL";//"DEFAULT";
            _header.actor_type = "CUSTOMER";// "DEFAULT";
            _header.user_handle_type = "DEFAULT";// "EMAIL";
            _header.user_handle_value = "DEFAULT";// "talent@risebird.io";
            _header.location = "";
            _header.function_code = "DEFAULT";
            _header.function_sub_code = "DEFAULT";
        }

        private string GenerateRandomAlphanumeric()
        {
            Guid guid = Guid.NewGuid();
            string alphanumeric = Convert.ToBase64String(guid.ToByteArray());
            alphanumeric = alphanumeric.Replace("/", "").Replace("+", "");//.Substring(0, 10);
            return alphanumeric;
        }

        public async Task<string> getHashSha256(string rawData)
        {
            try
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<RestResponse> MakeHttpRequest(string endPoint, Method method, object jsonBody = null, Dictionary<string, string> headers = null, List<HttpParameters> parameters = null)
        {
            try
            {
                //var client = new RestClient();
                var request = new RestRequest(endPoint, method);
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        request.AddHeader(item.Key, item.Value);
                    }
                }
                if (jsonBody != null)
                {
                    request.AddJsonBody(jsonBody);
                }
                if (parameters != null)
                {
                    foreach (var item in parameters)
                    {
                        request.AddParameter(item.name, item.value, item.parameterType);

                    }
                }
                RestResponse response = await client.ExecuteAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

    }
}
