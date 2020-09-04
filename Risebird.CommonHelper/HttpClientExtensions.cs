using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> SendRequestAsync(this HttpClient httpClient, HttpMethod method,
            string path, object requestBody, string functionKey, string accountEmail, CancellationToken ct)
        {
            var request = new HttpRequestMessage(method, path);

            if (requestBody != null)
            {
                var json = JsonConvert.SerializeObject(requestBody, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;
            }
            request.Headers.Add("x-functions-key", functionKey);
            request.Headers.Add("accountEmail", accountEmail);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.SendAsync(request, ct);
            return response;
        }
    }
}
