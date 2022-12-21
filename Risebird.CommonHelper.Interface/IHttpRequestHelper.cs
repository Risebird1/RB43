using RestSharp;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IHttpRequestHelper
    {
        Task<RestResponse> MakeHttpRequest(string endPoint, Method method, object jsonBody = null, Dictionary<string, string> headers = null, List<HttpParameters> parameters = null);
    }
}
