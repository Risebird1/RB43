using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class HttpRequestHelper : IHttpRequestHelper
    {

        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IConfiguration _configuration;


        public HttpRequestHelper(IConfiguration configuration, IApplicationErrorLogger errorLogger)
        {
            //_errorLogger = errorLogger;
            _configuration = configuration;
            _errorLogger = errorLogger;
            //accesskey = _configuration["WhatsApp:AccessKey"];

        }

        public async Task<RestResponse> MakeHttpRequest(string endPoint, Method method, object jsonBody = null, Dictionary<string, string> headers = null, List<HttpParameters> parameters = null)
        {
            try
            {
                var client = new RestClient(endPoint);
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
                if( parameters != null)
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
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
