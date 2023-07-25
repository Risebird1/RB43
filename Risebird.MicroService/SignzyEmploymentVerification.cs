using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Entities.Data;
using System.Collections.Generic;
using Risebird.CommonHelper.Interface;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.Entities.ViewModel;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.OData.Edm;
using Risebird.Repository.Interface;
using Risebird.Repository;

namespace Risebird.MicroService
{
    public class SignzyEmploymentVerification
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly string employementapikey;
        private readonly string employementendpoint;
        public SignzyEmploymentVerification(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IAadhaarRepository aadhaarRepository)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _aadhaarRepository = aadhaarRepository;
            employementapikey = _configuration["EmploymentVerfication:apikey"];
            employementendpoint = _configuration["EmploymentVerfication:endPoint"];
        }

        [FunctionName("SignzyEmploymentVerification")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];
            //Get request body data
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with request body :  {requestBody}");
            var mobiledata = JsonConvert.DeserializeObject<SignzyEmployeeRequest>(requestBody);
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with request body :  {mobiledata}");
            Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", employementapikey }
                };
            var response = await _httpRequestHelper.MakeHttpRequest(employementendpoint, Method.Post, mobiledata, headers);
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with raw body :  {response}");
            var convertedResponse = JsonConvert.DeserializeObject<SignzyEmployeeResponse>(response.Content);
            convertedResponse.MobileNumber = mobiledata.mobile;
            convertedResponse.CandidateEmail = mobiledata.CandidateEmail;
            dynamic datavalue = JObject.Parse(response.Content);
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with response body :  {datavalue}");
            List<string> namesList = convertedResponse.uanNumbers.ToList();
            foreach (var data in datavalue.Children())
            {
                if (((JToken)data).Path == "uanDetails")
                {
                    foreach (var dataset in data.Children())
                    {
                        foreach (var item in namesList)
                        {
                            if (((JContainer)dataset).First.Path == "uanDetails." + item)
                            {
                                var datanew = ((JContainer)dataset).First;
                                var internalNodes = datanew.Children().GetEnumerator();
                                while (internalNodes.MoveNext())
                                {
                                    UanDetails _uandeails = new UanDetails();
                                    var mainFamilies = internalNodes.Current;
                                    var nodedetails = mainFamilies.Children().GetEnumerator();
                                    while (nodedetails.MoveNext())
                                    {
                                        _uandeails.uanNumber = item;
                                        var mainFamilies1 = nodedetails.Current;
                                        if (((JProperty)mainFamilies1).Name == "basic_details")
                                        {
                                            _uandeails.date_of_birth = ((JProperty)mainFamilies1).Value["date_of_birth"].ToString();
                                            _uandeails.gender = ((JProperty)mainFamilies1).Value["gender"].ToString();
                                            _uandeails.name = ((JProperty)mainFamilies1).Value["name"].ToString();
                                        }
                                        else if (((JProperty)mainFamilies1).Name == "employment_details")
                                        {
                                            _uandeails.establishment_name = ((JProperty)mainFamilies1).Value["establishment_name"].ToString();
                                            _uandeails.date_of_joining = ((JProperty)mainFamilies1).Value["date_of_joining"].ToString();
                                        }
                                    }
                                    convertedResponse.uanDetails1.Add(_uandeails);
                                }
                            }
                        }
                    }
                }
            }
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with final body :  {convertedResponse}");
            //Save response data
            await _aadhaarRepository.SaveEmploymentDetails(convertedResponse);
            return new OkObjectResult(true);
        }
    }
}
