using Risebird.CommonHelper.Interface;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class CodeInviteRequest : ICodeInviteRequest
    {
        public async Task<string> SendCodeInvitePostRequest(string json)
        {
            try
            {
                using (var httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                    using (var client = new HttpClient(httpClientHandler))
                    {
                        var requestUrl = $"https://api.code.risebird.io/interview/api/v1/invited-assignment";
                        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(requestUrl, requestContent);
                        var responseBody = await response.Content.ReadAsStringAsync();
                        return responseBody;
                    }
                }
            }
            catch (Exception)
            {
                return "";

            }
        }
    }
}
