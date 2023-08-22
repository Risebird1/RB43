using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AffIndaResumeParserHelper : IAffIndaResumeParserHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;


        public AffIndaResumeParserHelper(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<string> UploadAndParseResume(string fileUrl = null)
        {
            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];
                var collectionId = _configuration["AffIndaResumeParser:collectionId"];
                string result = string.Empty;

                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api.affinda.com/v3/documents"),
                    Headers =
                {
                    { "accept", "application/json" },
                    { "authorization", $"Bearer {apiKey}" },
                },
                    Content = new MultipartFormDataContent
                {
                    new StringContent("true")
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "wait",
                            }
                        }
                    },

                    new StringContent(fileUrl)//"WUOHbNXO"
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "url",
                            }
                        }
                    },
                    new StringContent(collectionId)//"WUOHbNXO"
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "collection",
                            }
                        }
                    },
                },
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    //response.StatusCode
                    var body = await response.Content.ReadAsStringAsync();
                    result = body;
                }

                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }

            //RestResponse response = client.Execute(request);

            //Dictionary<string, string> headers = new Dictionary<string, string>
            //    {
            //        { "Authorization", "Bearer " + apiKey },
            //        //{ "Content-Type", "multipart/form-data" },
            //        { "Content-Type", "application/json" },
            //        { "accept", "application/json" }
            //    };

            //List<HttpParameters> parameters = new List<HttpParameters>
            //    {
            //        new HttpParameters() { name = "file", value = fileStream, parameterType = ParameterType.RequestBody },
            //        new HttpParameters() { name = "application/json", value = 100, parameterType = ParameterType.QueryString }
            //    };

            //var response = _httpRequestHelper.MakeHttpRequest("https://api.calendly.com/event_types", Method.Get, null, headers, parameters).Result;

        }
    }
}
