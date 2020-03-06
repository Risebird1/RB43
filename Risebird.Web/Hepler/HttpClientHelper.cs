using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Risebird.Web.Hepler
{
    public class HttpClientHelper
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// This constructor configures the base address of the <see cref="HttpClient"/> instance member with the 
        /// value of the WebApiUrl if this value is found in configuration. This constructor also configures the 
        /// <see cref="HttpClient"/> instance member with an instance of <see cref="HttpClientLogHandler"/> 
        /// which requires an <see cref="IHttpContextAccessor"/> and a <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="contextAccessor"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        public HttpClientHelper(IHttpContextAccessor contextAccessor, IOptions<AzureAdB2COptions> options)
        {
            _httpClient = new HttpClient();

           
        }

        /// <summary>
        /// This method exposes the configured <see cref="HttpClient"/> member.
        /// </summary>
        /// <returns>An instance of <see cref="HttpClient"/> configured to perform logging</returns>
        public HttpClient GetHttpClient()
        {
            return _httpClient;
        }
    }
}
