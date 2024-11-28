using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class SignzyEmploymentVerification
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IInterviewerService _interviewerService;
        private readonly string employementapikey;
        private readonly string employementendpoint;
        private readonly ILogger log;
        public SignzyEmploymentVerification(ILoggerFactory loggerFactory, IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IAadhaarRepository aadhaarRepository, IInterviewerService interviewerService)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _aadhaarRepository = aadhaarRepository;
            employementapikey = _configuration["EmploymentVerfication:apikey"];
            employementendpoint = _configuration["EmploymentVerfication:endPoint"];
            _interviewerService = interviewerService;
            log = loggerFactory.CreateLogger<SignzyEmploymentVerification>();
        }

        [Function("SignzyEmploymentVerification")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
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
            var resultset = JsonConvert.DeserializeObject<SignzyEVResponseResult>(response.Content);
            var convertedResponse = resultset.result?.result;
            if (convertedResponse == null)
            {
                convertedResponse = new SignzyEmployeeResponse();
                convertedResponse.message = resultset.message;
            }
            convertedResponse.CandidateEmail = mobiledata.CandidateEmail;
            convertedResponse.MobileNumber = mobiledata.mobileNumber;
            List<string>? namesList = convertedResponse.uan?.ToList();
            dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
            var children = stuff.Children().GetEnumerator();
            while (children.MoveNext())
            {
                var mainDetails = children.Current;
                if (((JProperty)mainDetails).Name == "result")
                {
                    foreach (var data in mainDetails.Children())
                    {
                        var internalNodes = data.Children().GetEnumerator();
                        while (internalNodes.MoveNext())
                        {
                            var mainFamilies1 = internalNodes.Current;
                            if (((Newtonsoft.Json.Linq.JProperty)((JToken)mainFamilies1)).Name == "result")
                            {
                                var currentmainDetails = internalNodes.Current;
                                foreach (var internernodeParticipent in currentmainDetails.Children())
                                {
                                    var internalNodes1 = internernodeParticipent.Children().GetEnumerator();
                                    while (internalNodes1.MoveNext())
                                    {
                                        var mainFamilies2 = internalNodes1.Current;
                                        if (((Newtonsoft.Json.Linq.JProperty)mainFamilies2).Name == "uanDetails")
                                        {
                                            foreach (var dataset in mainFamilies2.Children())
                                            {
                                                foreach (var item in namesList)
                                                {
                                                    if (((JContainer)dataset).First.Path.Contains("uanDetails." + item))
                                                    {
                                                        var datanew = ((JContainer)dataset).First;
                                                        var internalNodes5 = datanew.Children().GetEnumerator();
                                                        while (internalNodes5.MoveNext())
                                                        {
                                                            UanDetails _uandeails = new UanDetails();
                                                            var mainFamilies = internalNodes5.Current;
                                                            var nodedetails = mainFamilies.Children().GetEnumerator();
                                                            while (nodedetails.MoveNext())
                                                            {
                                                                _uandeails.uanNumber = item;
                                                                var mainFamilies5 = nodedetails.Current;
                                                                if (((JProperty)mainFamilies5).Name == "basicDetails")
                                                                {
                                                                    _uandeails.date_of_birth = ((JProperty)mainFamilies5).Value["dateOfBirth"].ToString();
                                                                    _uandeails.gender = ((JProperty)mainFamilies5).Value["gender"].ToString();
                                                                    _uandeails.name = ((JProperty)mainFamilies5).Value["name"].ToString();
                                                                }
                                                                else if (((JProperty)mainFamilies5).Name == "employmentDetails")
                                                                {
                                                                    _uandeails.establishment_name = ((JProperty)mainFamilies5).Value["establishmentName"].ToString();
                                                                    _uandeails.date_of_joining = ((JProperty)mainFamilies5).Value["dateOfJoining"].ToString();
                                                                }
                                                            }
                                                            convertedResponse.uanDetails1.Add(_uandeails);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with final body :  {convertedResponse}");
            //Save response data
            if (!string.IsNullOrEmpty(convertedResponse.summary?.recentEmployerData.matchingUan) || convertedResponse.message == "No record(s found")
            {
                await _aadhaarRepository.SaveEmploymentDetails(convertedResponse);
                var _interviewer = new InterviewerProfileVM();
                _interviewer.EmailId = convertedResponse.CandidateEmail;
                _interviewer.isVerified = true;
                if (convertedResponse.summary != null)
                {
                    _interviewer.IsEmployed = convertedResponse.summary.dateOfExitMarked;
                    _interviewer.CompanyName = convertedResponse.summary.recentEmployerData.establishmentName;
                }
                else
                {
                    _interviewer.IsEmployed = false;
                    _interviewer.CompanyName = string.Empty;
                }
                _interviewer.EmploymentComment = convertedResponse.message;
                await _interviewerService.SaveInterviewerEmploymentVerification(_interviewer);
            }
            return new OkObjectResult(true);
        }
    }
}
