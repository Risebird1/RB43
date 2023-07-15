using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AffIndaResumeParserHelper : IAffIndaResumeParserHelper
    {
        private readonly IConfiguration _configuration;

        public AffIndaResumeParserHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadAndParseResume(string fileUrl = null)
        {
            var apiKey = _configuration["AffIndaResumeParser:apiKey"];
            var collectionId = _configuration["AffIndaResumeParser:collectionId"];
            string result = string.Empty;
            //string fileNameWithExtension = string.Empty;
            //if(!string.IsNullOrEmpty(fileUrl))
            //{
            //    var subName = fileUrl.Split("/").Last();
            //    if(subName.Contains("?"))
            //    {
            //        fileNameWithExtension = subName.Split("?").First();
            //    }
            //    else
            //    {
            //        fileNameWithExtension = subName;
            //    }
            //}
            //string extension = "." + fileNameWithExtension.Split(".").Last();
            //HttpClient myWebClient = new HttpClient();
            //Stream myStream = await myWebClient.GetStreamAsync(fileUrl);
            //byte[] fileBytes = await myWebClient.GetByteArrayAsync(fileUrl);
            //string encodedFile = Convert.ToBase64String(fileBytes);
            //var fileInfo = new FileInfo(file.FileName);
            //string contentType = string.Empty;

            //switch (extension)
            //{
            //    case ".pdf":
            //        contentType = "application/pdf";
            //        break;
            //    case ".txt":
            //        contentType = "text/plain";
            //        break;
            //    case ".doc":
            //        contentType = "application/msword";
            //        break;
            //    case ".docx":
            //        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            //        break;
            //    case ".dotx":
            //        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
            //        break;
            //    default:
            //        contentType = "application/pdf";
            //        break;
            //}
            //string fileNameWithExtension = fileInfo.Name + fileInfo.Extension;
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
                    //new StringContent($"data:{contentType};name={fileNameWithExtension};base64,{encodedFile}")
                    //{
                    //    Headers =
                    //    {
                    //        //ContentType = new MediaTypeHeaderValue("application/pdf"),
                    //        ContentType = new MediaTypeHeaderValue(contentType),
                    //        ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    //        {
                    //            Name = "file",
                    //            //FileName = "Neeta_Asodekar.pdf",
                    //            FileName = fileNameWithExtension
                    //        }
                    //    }
                    //},
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
                var body = await response.Content.ReadAsStringAsync();
                result = body;
            }

            return result;

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
