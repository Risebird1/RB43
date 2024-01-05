using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
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

        public async Task<Tuple<string, string>> UploadAndParseResume(string fileUrl = null, string skillChannelName = null)
        {
            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];
                var collectionId = _configuration["AffIndaResumeParser:collectionId"];
                string result = string.Empty;
                string identifier = string.Empty;
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

                    var responseObjTypeDefinition = new
                    {
                        meta = new { identifier = string.Empty }
                    };


                    var responseObj = JsonConvert.DeserializeAnonymousType(body, responseObjTypeDefinition);
                    //use the identifier recieved for the document above

                    identifier = responseObj.meta.identifier;

                    await IndexADocument(responseObj.meta.identifier, skillChannelName);

                    result = body;
                }

                return new Tuple<string, string>(result, identifier);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
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

        public async Task<string> UploadResumeForRedaction(string fileUrl = null)
        {
            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];
                var collectionId = _configuration["AffIndaResumeParser:redactorCollectionId"];
                string result = string.Empty;

                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api.affinda.com/v2/redacted_resumes"),
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

                        new StringContent(fileUrl)
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "url",
                                }
                            }
                        },
                        new StringContent(collectionId)
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "collection",
                                }
                            }
                        },
                        new StringContent("true")
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "redactHeadshot",
                                }
                            }
                        },
                        new StringContent("true")
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "redactPersonalDetails",
                                }
                            }
                        },
                        new StringContent("true")
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "redactReferees",
                                }
                            }
                        },
                        new StringContent("true")
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "redactPdfMetadata",
                                }
                            }
                        }
                    }
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    //response.StatusCode
                    var responseObjTypeDefinition = new
                    { 
                        data = new { redactedPdf  = string.Empty}
                    };

                    var body = await response.Content.ReadAsStringAsync();

                    var responseObj = JsonConvert.DeserializeAnonymousType(body, responseObjTypeDefinition);
                    //result = body;
                    result = responseObj.data.redactedPdf;
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

        /// <summary>
        /// Creates an index into which Resumes will be uploaded to enable a search through the collection using AffinDa Search and Match
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<bool> CreateIndex(string indexName)//indexName is the same as IRF Name
        {
            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];
                string result = string.Empty;

                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api.affinda.com/v3/index"),
                    Headers =
                {
                    { "accept", "application/json" },
                    { "authorization", $"Bearer {apiKey}" },
                },
                    Content = new MultipartFormDataContent
                    {
                        
                        new StringContent(indexName)
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "name",
                                }
                            }
                        }
                    }
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return true;
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> IndexADocument(string documentIdentifier, string indexName)//indexName is the same as IRF Name
        {
            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];
                string result = string.Empty;

                var requestUri = $"https://api.affinda.com/v3/index/{indexName}/documents";
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Headers =
                {
                    { "accept", "application/json" },
                    { "authorization", $"Bearer {apiKey}" },
                },
                    Content = new MultipartFormDataContent
                    {

                        new StringContent(documentIdentifier)
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "document",
                                }
                            }
                        }
                    }
                };
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return true;
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<string> GenerateEmbeddableResumeSearchUrl(string[] indicesArray)
        {

            try
            {
                var apiKey = _configuration["AffIndaResumeParser:apiKey"];

                var options = new RestClientOptions("https://api.affinda.com/v3/resume_search/embed");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Bearer {apiKey}");

                var requestObj = new
                {
                    configOverride = new { indices = indicesArray, allowPdfDownload = true },
                };

                var jsonRequestBody = JsonConvert.SerializeObject(requestObj);

                //request.AddJsonBody("{\"configOverride\":{\"indices\":[\"TestIndex_2\"]}}", false);
                request.AddJsonBody(jsonRequestBody);
                var response = await client.PostAsync(request);

                var responseObjTypeDefinition = new
                {
                    url = string.Empty
                };

                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JsonConvert.DeserializeAnonymousType(response.Content, responseObjTypeDefinition);
                    return responseObj.url;
                }
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            
        }
    }
}
