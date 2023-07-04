using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
//using MCQ_Risebird.DataAccess;
//using MCQ_Risebird.Models;
using RestSharp;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Risebird.Repository.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper;
using Risebird.Entities.Enum;

namespace ScreenBackgroundJobs
{
    public class UploadAndVerifyCandidateImage
    {
        private readonly IConfiguration _configuration;

        //private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string client_code;
        private readonly string api_key;
        private readonly string salt;
        private readonly string apiendpoint;
        private static readonly RestClient client = new RestClient();
        public UploadAndVerifyCandidateImage(IConfiguration configuration, IAadhaarRepository aadhaarRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            //_applicationErrorLogger = applicationErrorLogger;
            _aadhaarRepository = aadhaarRepository;
            client_code = _configuration["AadhaarApiDetails:client_code"];
            api_key = _configuration["AadhaarApiDetails:api_key"];
            salt = _configuration["AadhaarApiDetails:salt"];
            apiendpoint = _configuration["AadhaarApiDetails:endPoint"];
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
                model.request.api_key = api_key;
                model.request.request_id = Guid.NewGuid().ToString();
                model.request.purpose = "VERIFY";
                model.request.image_1 = dto.imageFirst; //ConvertImageURLToBase64(dto.imageFirst);
                model.request.image_2 = dto.imageSecond; // ConvertImageURLToBase64(dto.imageSecond);
                var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + api_key + "|" + salt;
                model.request.hash = await getHashSha256(hashstring);

                Dictionary<string, string> headers = new Dictionary<string, string>
            {
                //{ "Authorization", "Basic " + accesskey },
                { "Content-Type", "application/json" }
            };

                //var response = await _httpRequestHelper.MakeHttpRequest(dto.apiendpoint + "getCaptcha", Method.Post, model, headers);
                var response = await MakeHttpRequest(apiendpoint + "getCaptcha", Method.Post, model, headers);

                var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
                if (convertedResponse.response_status.status.ToLower() == "fail")
                {
                    return 0;
                }
                return convertedResponse.response_data.score;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }
        public string GetCurrentUnixTimestampMillis()
        {
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime localDateTime, univDateTime;
            localDateTime = DateTime.Now;
            univDateTime = localDateTime.ToUniversalTime();
            return ((univDateTime - UnixEpoch).TotalMilliseconds).ToString();
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

        //public string ConvertImageURLToBase64(String url)
        //{
        //    StringBuilder _sb = new StringBuilder();
        //    Byte[] _byte = GetImage(url);
        //    _sb.Append(Convert.ToBase64String(_byte, 0, _byte.Length));
        //    return _sb.ToString();
        //}

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

        //private byte[] GetImage(string url)
        //{
        //    Stream stream = null;
        //    byte[] buf;
        //    try
        //    {
        //        WebProxy myProxy = new WebProxy();
        //        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        //        HttpWebResponse response = (HttpWebResponse)req.GetResponse();
        //        stream = response.GetResponseStream();
        //        using (BinaryReader br = new BinaryReader(stream))
        //        {
        //            int len = (int)(response.ContentLength);
        //            buf = br.ReadBytes(len);
        //            br.Close();
        //        }
        //        stream.Close();
        //        response.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        buf = null;
        //    }
        //    return (buf);
        //}
    }
}
