using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class AuthenticationService
    {
        private readonly AuthenticationOptions _options;
        private HttpClient _httpClient;
        public AuthenticationService()
        {
            
        }

        /// <summary>
        /// Get Access token
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAccessTokenAsync()
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", System.Environment.GetEnvironmentVariable($"ClientId").ToString()),
                new KeyValuePair<string, string>("client_secret", System.Environment.GetEnvironmentVariable($"ClientSecret").ToString()),
                new KeyValuePair<string, string>("scope",  System.Environment.GetEnvironmentVariable($"Scope").ToString()),
                new KeyValuePair<string, string>("grant_type", System.Environment.GetEnvironmentVariable($"GrantType").ToString())
                //new KeyValuePair<string, string>("resource", System.Environment.GetEnvironmentVariable($"Resource").ToString())
            };
            var client = new HttpClient();
            var tenantId = System.Environment.GetEnvironmentVariable($"TenantId").ToString();
           //  var requestUrl = $"{System.Environment.GetEnvironmentVariable($"Endpoint").ToString()}{System.Environment.GetEnvironmentVariable($"TenantId").ToString()}/oauth2/token";
            var requestUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var requestContent = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(requestUrl, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic tokenResponse = JsonConvert.DeserializeObject(responseBody);
             //return tokenResponse?.access_token;
            return "eyJ0eXAiOiJKV1QiLCJub25jZSI6IjktbTlxd05JMXVjSzhlMVR2MUpjRnUzdGhoWXNjblQ0MktFRWxnVXhoTVkiLCJhbGciOiJSUzI1NiIsIng1dCI6Imh1Tjk1SXZQZmVocTM0R3pCRFoxR1hHaXJuTSIsImtpZCI6Imh1Tjk1SXZQZmVocTM0R3pCRFoxR1hHaXJuTSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8xZTRiNTk1Yi1kMjBhLTQ5ZGQtODNjNS0xOWNhM2NjZDg3ZDEvIiwiaWF0IjoxNTk1Nzk2NzYyLCJuYmYiOjE1OTU3OTY3NjIsImV4cCI6MTU5NTgwMDY2MiwiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFVUUF1LzhRQUFBQVFYa0dKMldaaHMvaXF5dDlYYUxpR2MwTzZDTGhib0EwRGxZcmtDUVo4SFN1RHBHczUwRENMRmh4dFlNbEFmMmtFUVJGUC9LU3JUMGdtWThaK2hTQy9RPT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcF9kaXNwbGF5bmFtZSI6IkdyYXBoIGV4cGxvcmVyIChvZmZpY2lhbCBzaXRlKSIsImFwcGlkIjoiZGU4YmM4YjUtZDlmOS00OGIxLWE4YWQtYjc0OGRhNzI1MDY0IiwiYXBwaWRhY3IiOiIwIiwiZmFtaWx5X25hbWUiOiJTRVRIIiwiZ2l2ZW5fbmFtZSI6IkFTSFVUT1NIIiwiaXBhZGRyIjoiNDAuNzEuNDQuMjUxIiwibmFtZSI6IkFTSFVUT1NIIFNFVEgiLCJvaWQiOiIyODZiOTAwNS1iNjk4LTQ3YmUtYTE5NC05OGU0MWYzNmE0MDYiLCJwbGF0ZiI6IjMiLCJwdWlkIjoiMTAwMzIwMDBEMTQzNDk0RCIsInNjcCI6Ik9ubGluZU1lZXRpbmdzLlJlYWQgT25saW5lTWVldGluZ3MuUmVhZFdyaXRlIG9wZW5pZCBwcm9maWxlIFVzZXIuRXhwb3J0LkFsbCBVc2VyLlJlYWQgZW1haWwiLCJzaWduaW5fc3RhdGUiOlsia21zaSJdLCJzdWIiOiJFMFRuUmlMeDA1Y2I2UERjcVlYaXVzM2VuVEY4NWNJSzlZci1BTTdocXRFIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6IkFTIiwidGlkIjoiMWU0YjU5NWItZDIwYS00OWRkLTgzYzUtMTljYTNjY2Q4N2QxIiwidW5pcXVlX25hbWUiOiJUQUxFTlRAUklTRUJJUkQxLm9ubWljcm9zb2Z0LmNvbSIsInVwbiI6IlRBTEVOVEBSSVNFQklSRDEub25taWNyb3NvZnQuY29tIiwidXRpIjoiUVRheWt3UjJiRTZyNC1LbUQyaENBQSIsInZlciI6IjEuMCIsIndpZHMiOlsiNjJlOTAzOTQtNjlmNS00MjM3LTkxOTAtMDEyMTc3MTQ1ZTEwIl0sInhtc19zdCI6eyJzdWIiOiJ2QmNqYnFYM3NmbDJPZEJHdE1aT016RnlOTHF6djFnWnN6b2h2ajFvZHFvIn0sInhtc190Y2R0IjoxNTk1MTY0MjA1fQ.mmL_JLBQP0Mi2hhSnkGwFwAgLE_EOQTxbdiSnuVM2hGVXQbYn4nDJNdaETxPnCn3CZ1Jdx3R9daXeslMYQ2l5W-xyvNWzz7vveQxCmRL0Ogbb4WBwnHn4QtO2SNfGkPF3643DTQvy9zrXGDU2sblTieXl9RbRQxZaW9zVj8Xq7TyPRqe1cqog89h_L0uqwdNPDTt4IW3owafZmOLxiGbY-4qbXE38U9fhzBP7m1du08iD6fk5IHas_LNLA9Z2qg76k8oLuDdpQ0FDIEk1IaND5yW859w4EpUyKauWy-d80pwOvBwNA56FiR_dDRGVf8Rk4w30ihSLRS1KoNdf1k3Pw";
        }

        /// <summary>
        /// Auth object
        /// </summary>
        /// <returns></returns>
        public async Task<HttpClient> CreateAuthorizedHttpClient()
        {
            if (_httpClient != null)
            {
                return _httpClient;
            }

            var token = await GetAccessTokenAsync();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            return _httpClient;
        }
    }
}
