using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AzureFaceApi : IAzureFaceApi
    {
        private readonly IConfiguration _configuration;

        private readonly string subscriptionKey;// = "8ee1181083bc4035b5a803421ef73f40";
        private readonly string uriBase;// = "https://facedetectionapigroup.cognitiveservices.azure.com/face/v1.0/detect";
        private readonly IHttpClientFactory _httpClientFactory;

        public AzureFaceApi(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            subscriptionKey = _configuration["AzureFaceApi:subscriptionKey"];
            uriBase = _configuration["AzureFaceApi:uriBase"];
        }
        public string FaceDetectionResult(string imageFilePath)
        {
            if (File.Exists(imageFilePath))
            {
                try
                {
                    var result = MakeAnalysisRequest(imageFilePath);
                    return result.Result;
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }

        async Task<string> MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);

                string contentString = await response.Content.ReadAsStringAsync();
                var result = JsonPrettyPrint(contentString);
                return result;

            }
        }
        byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }
        string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
