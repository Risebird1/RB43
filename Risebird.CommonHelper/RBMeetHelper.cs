using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class RBMeetHelper : IRBMeetHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;


        public RBMeetHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<RBMeetCreateMeetingResponse> CreateMeeting(string agentname, string visitorName, DateTime datetime)
        {
            var _model = new RBMeetCreateMeetingObject();
            _model.agentname = agentname;
            _model.visitorName = visitorName;
            _model.datetime = datetime.ToString();
            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room");
            var request = new RestRequest(Method.POST);
            request.AddHeader("authorization", _configuration["RBMeetingDetails:apiKey"]);//"v7sy3J8cAFdEJp5S"
            request.AddBody(_model);
            //request.AddJsonBody(_model);
            IRestResponse response = _meeting.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<RBMeetCreateMeetingResponse>(content);
            if (convertedResponse != null)
            {
                return convertedResponse;
            }
            return new RBMeetCreateMeetingResponse();
        }
    }
}
