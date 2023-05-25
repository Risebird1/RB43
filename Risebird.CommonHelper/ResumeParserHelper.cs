using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.CommonHelper
{
    public class ResumeParserHelper : IResumeParserHelper//Rchilli is the service Provider
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly string userKey;
        private readonly string version;
        private readonly string subUserId;
        private readonly string resumeParserServiceUrl;

        public ResumeParserHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            userKey = _configuration["ResumeParserCredentials:userKey"];
            version = _configuration["ResumeParserCredentials:version"];
            subUserId = _configuration["ResumeParserCredentials:subUserId"];
            resumeParserServiceUrl = _configuration["ResumeParserCredentials:resumeParserServiceUrl"];

        }
        public string OutPutJson { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        //public string ServiceUrl { get; set; }
        public bool isError { get; set; }

        public async Task<RchilliMapFields> ParseResume(MemoryStream fileStream, string fileName)
        {
            try
            {
                //FileInfo file = new FileInfo(fileStream);
                //var fileBytes = File.ReadAllBytes(@"d:\pele.jpg");
                byte[] fileBytes = fileStream.ToArray();

                string encodedFile = Convert.ToBase64String(fileBytes);

                //byte[] DataFile = converttoBase64(file);
                OutPutJson = await CallResumeParserApi(encodedFile, fileName);
                if (OutPutJson.Contains("\"error\":"))
                {
                    isError = true;
                    ErrorClass errorObj = JsonConvert.DeserializeObject<ErrorClass>(OutPutJson);
                    Error error = errorObj.error;
                    ErrorCode = error.errorcode.ToString();
                    ErrorMessage = error.errormsg;

                }
                else
                {
                    RchilliMapFields obj = JsonConvert.DeserializeObject<RchilliMapFields>(OutPutJson);
                    return obj;
                }

            }
            catch (Exception ex)
            {
                ErrorCode = "5001";
                ErrorMessage = ex.Message;
            }
            return new RchilliMapFields();


        }
        private async Task<string> CallResumeParserApi(string base64string, string fileName)
        {
            //System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
            //String strRequest = "{\"filedata\":\"" + base64string + "\",\"filename\":\"" + fileName + "\",\"userkey\":\"" + userKey + "\",\"version\":\"" + version + "\",\"subuserid\":\"" + subUserId + "\"}";
            //byte[] byteArray = Encoding.UTF8.GetBytes(strRequest);
            //HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(ServiceUrl);
            //httpRequest.Credentials = CredentialCache.DefaultCredentials;
            //httpRequest.Method = "POST";
            //httpRequest.ContentType = "application/json";
            //httpRequest.ContentLength = byteArray.Length;
            //httpRequest.Timeout = 300000;
            //Stream dataStream = httpRequest.GetRequestStream();
            //dataStream.Write(byteArray, 0, byteArray.Length);
            //dataStream.Close();

            //HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            //StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            //string jsonData = streamReader.ReadToEnd();
            //return jsonData;

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{resumeParserServiceUrl}/Rchilli/parseResumeBinary");
            string formattedContent = string.Format("{\n\t\"filedata\":\"{0}\",\n\t\"filename\":\"{1}\",\n\t\"userkey\":\"{2}\",\n\t\"version\":\"{3}\",\n\t\"subuserid\":\"{4}\"\n}", base64string, fileName, userKey, version, subUserId);
            //var content = new StringContent("{\n\t\"filedata\":\"{{base64data}}\",\n\t\"filename\":\"{{filename}}\",\n\t\"userkey\":\"{{userkey}}\",\n\t\"version\":\"8.0.0\",\n\t\"subuserid\":\"{{subuserid}}\"\n}", null, "application/json");
            var content = new StringContent(formattedContent, null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            //Console.WriteLine(await response.Content.ReadAsStringAsync());
            return await response.Content.ReadAsStringAsync();

        }


        public byte[] converttoBase64(FileInfo fno)
        {

            try
            {
                Int64 numofbyte = fno.Length;
                FileStream fs = new FileStream(fno.FullName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                byte[] DataFile = br.ReadBytes(Convert.ToInt32(numofbyte));
                fs.Close();
                fs.Dispose();
                return DataFile;
            }
            catch (Exception ex)
            {
                isError = true;
                ErrorCode = "5000";
                ErrorMessage = "Exception occured" + ex.Message;
            }
            byte[] error1 = new byte[1];
            error1[0] = (byte)' ';
            return error1;
        }
    }
}
