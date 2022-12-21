using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class CalendlyHelper : ICalendlyHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IIRFRepository _irfRepository;
        private readonly string _calendlyToken = string.Empty;
        private readonly string _calendlyOrganizationIdentifier = string.Empty;
        private readonly IHttpRequestHelper _httpRequestHelper;

        //private readonly string _calendlyUserUri = string.Empty;

        public CalendlyHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration,
            IIRFRepository irfRepository, IHttpRequestHelper httpRequestHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _irfRepository = irfRepository;
            _calendlyToken = _configuration["CalendlyDetails:Token"];
            _calendlyOrganizationIdentifier = _configuration["CalendlyDetails:OrganizationURI"];
            _httpRequestHelper = httpRequestHelper;
            //_calendlyUserUri = _configuration["CalendlyDetails:Current_User_URI"];
        }

        public List<IRF> irfList;
        public List<IRF> pendingIRFList;

        public void DumpExistingIRFResourceURIs()
        {
            //get the list of all the irfs along with calendly links
            irfList = _irfRepository.GetAllIRFs("talent@risebird.io", "Admin", null, null).Result.ToList();

            GetEventListAndDumpData();


        }

        //private void GetEventListAndDumpData(string endPoint = null)
        //{
        //    CalendlyEventsListResponse convertedResponse = new CalendlyEventsListResponse();
        //    if (string.IsNullOrEmpty(endPoint))
        //    {
        //        var client = new RestClient(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/event_types"));
        //        var request = new RestRequest(Method.GET);
        //        request.AddHeader("content-type", "application/json");
        //        request.AddHeader("authorization", "Bearer " + _calendlyToken);
        //        request.AddParameter("organization", _calendlyOrganizationIdentifier, ParameterType.QueryString);
        //        request.AddParameter("count", 100, ParameterType.QueryString);
        //        //request.AddParameter("user", _calendlyUserUri, ParameterType.QueryString);

        //        IRestResponse response = client.Execute(request);
        //        var content = response.Content;
        //        convertedResponse = JsonConvert.DeserializeObject<CalendlyEventsListResponse>(content);
        //    }
        //    else
        //    {
        //        var client = new RestClient(endPoint);
        //        var request = new RestRequest(Method.GET);
        //        request.AddHeader("content-type", "application/json");
        //        request.AddHeader("authorization", "Bearer " + _calendlyToken);

        //        IRestResponse response = client.Execute(request);
        //        var content = response.Content;
        //        convertedResponse = JsonConvert.DeserializeObject<CalendlyEventsListResponse>(content);
        //    }


        //    //create HTTP Request 
        //    if (convertedResponse != null)
        //    {
        //        DumpCalendlyResourceURI(convertedResponse);

        //    }

        //}

        //private void DumpCalendlyResourceURI(CalendlyEventsListResponse convertedResponse)
        //{
        //    DataTable calendlyDt = new DataTable();
        //    calendlyDt.Columns.Add("IRFId", typeof(int));
        //    calendlyDt.Columns.Add("ResourceUri", typeof(string));
        //    foreach (var irf in irfList)
        //    {
        //        if (convertedResponse.collection.Any(x => x.scheduling_url == irf.CalendlyLink))
        //        {
        //            calendlyDt.Rows.Add(irf.id, convertedResponse.collection.Where(x => x.scheduling_url == irf.CalendlyLink).FirstOrDefault().uri);
        //            irfList = irfList.Where(x => x != irf).ToList();

        //        }
        //    }
        //    if (calendlyDt != null && calendlyDt.Rows != null && calendlyDt.Rows.Count > 0)
        //    {
        //        var dumpResult = _irfRepository.DumpCalendlyResourceURI(calendlyDt);
        //    }
        //    if (!string.IsNullOrEmpty(convertedResponse.pagination.next_page))
        //    {
        //        GetEventListAndDumpData(convertedResponse.pagination.next_page);
        //    }
        //    return;
        //}

        public async Task UpdatePendingCalendlyResourceUriForIRF()
        {
            try
            {
                //get the list of all the irfs recently added/calendly link updated along with calendly links

                pendingIRFList = (await _irfRepository.GetPendingCalendlyResourceUriUpdateIRF())?.ToList();
                if (pendingIRFList != null && pendingIRFList.Count() > 0)
                {
                    GetEventListAndDumpData();
                }
                else
                    return;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

        }

        private void GetEventListAndDumpData(string endPoint = null)
        {
            CalendlyEventsListResponse convertedResponse = new CalendlyEventsListResponse();
            if (string.IsNullOrEmpty(endPoint))
            {
                //var client = new RestClient(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/event_types"));
                //var request = new RestRequest(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/event_types"), Method.Get);
                //request.AddHeader("content-type", "application/json");
                //request.AddHeader("authorization", "Bearer " + _calendlyToken);
                //request.AddParameter("organization", _calendlyOrganizationIdentifier, ParameterType.QueryString);
                //request.AddParameter("count", 100, ParameterType.QueryString);

                //RestResponse response = client.Execute(request);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer " + _calendlyToken },
                    { "Content-Type", "application/json" }
                };

                List<HttpParameters> parameters = new List<HttpParameters>();
                {
                    new HttpParameters() { name = "organization", value = _calendlyOrganizationIdentifier, parameterType = ParameterType.QueryString };
                    new HttpParameters() { name = "application/json", value = 100, parameterType = ParameterType.QueryString };
                }

                var response = _httpRequestHelper.MakeHttpRequest(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/event_types"), Method.Get, null, headers, parameters).Result;

                //var content = response.Content;
                convertedResponse = JsonConvert.DeserializeObject<CalendlyEventsListResponse>(response.Content);
            }
            else
            {
                //var client = new RestClient(endPoint);
                //var request = new RestRequest(endPoint, Method.Get);
                //request.AddHeader("content-type", "application/json");
                //request.AddHeader("authorization", "Bearer " + _calendlyToken);

                //RestResponse response = client.Execute(request);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer " + _calendlyToken },
                    { "Content-Type", "application/json" }
                };

                var response = _httpRequestHelper.MakeHttpRequest(endPoint, Method.Post, null, headers).Result;

                //var content = response.Content;
                convertedResponse = JsonConvert.DeserializeObject<CalendlyEventsListResponse>(response.Content);
            }


            //create HTTP Request 
            if (convertedResponse != null)
            {
                DumpCalendlyResourceURI(convertedResponse);

            }

        }
        private void DumpCalendlyResourceURI(CalendlyEventsListResponse convertedResponse)
        {
            DataTable calendlyDt = new DataTable();
            calendlyDt.Columns.Add("IRFId", typeof(int));
            calendlyDt.Columns.Add("ResourceUri", typeof(string));
            foreach (var irf in pendingIRFList)
            {
                if (convertedResponse.collection.Any(x => x.scheduling_url == irf.CalendlyLink))
                {
                    calendlyDt.Rows.Add(irf.id, convertedResponse.collection.Where(x => x.scheduling_url == irf.CalendlyLink).FirstOrDefault().uri);
                    pendingIRFList = pendingIRFList.Where(x => x != irf).ToList();

                }
            }
            if (calendlyDt != null && calendlyDt.Rows != null && calendlyDt.Rows.Count > 0)
            {
                var dumpResult = _irfRepository.DumpCalendlyResourceURI(calendlyDt);
            }
            if (!string.IsNullOrEmpty(convertedResponse.pagination.next_page) && pendingIRFList != null && pendingIRFList.Count() > 0)
            {
                GetEventListAndDumpData(convertedResponse.pagination.next_page);
            }
            return;
        }

        public async Task<string> GenerateOneTimeSchedulingLink(int irfId)
        {
            //get the resource uri from database
            try
            {
                string resourceUri = await _irfRepository.GetIRFCalendlyResourceUri(irfId);
                string singleUseLink = string.Empty;
                if (!string.IsNullOrEmpty(resourceUri))
                {
                    //get the scheduling link 
                    //var client = new RestClient(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/scheduling_links"));
                    //var request = new RestRequest(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/scheduling_links"), Method.Post);
                    //request.AddHeader("content-type", "application/json");
                    //request.AddHeader("authorization", "Bearer " + _calendlyToken);
                    var postData = new CalendlySchedulingLinkRequestBodyData { max_event_count = 1, owner = resourceUri, owner_type = "EventType" };
                    //request.AddParameter("application/json", JsonConvert.SerializeObject(postData),
                    //ParameterType.RequestBody);

                    //RestResponse response = await client.ExecuteAsync(request);

                    Dictionary<string, string> headers = new Dictionary<string, string>
                    {
                        { "Authorization", "Bearer " + _calendlyToken },
                        { "Content-Type", "application/json" }
                    };
                    List<HttpParameters> parameters = new List<HttpParameters>();
                    {
                        new HttpParameters() { name = "application/json", value = JsonConvert.SerializeObject(postData), parameterType = ParameterType.RequestBody };
                    }
                    var response = await _httpRequestHelper.MakeHttpRequest(string.Concat(_configuration["CalendlyDetails:endPoint"], $"/scheduling_links"), Method.Post, null, headers, parameters);


                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        var convertedResponse = JsonConvert.DeserializeObject<CalendlyCreateLinkResponseWrapper>(response.Content);
                        singleUseLink = convertedResponse.resource.booking_url;

                    }
                }
                return singleUseLink;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
