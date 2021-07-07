using System;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper
{
    public class WhatsAppHelper : IWhatsAppHelper
    {
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IConfiguration _configuration;
        private readonly string accesskey;
        private IUserService _userService;

        public WhatsAppHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration, IApplicationErrorLogger errorLogger, IUserService userService)
        {
            //_errorLogger = errorLogger;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _errorLogger = errorLogger;
            accesskey = _configuration["WhatsApp:AccessKey"];
            _userService = userService;
        }
        public string WhatsAppTrackUser(WhatsAppVM _whatsAppVM)
        {
            try
            {
                _whatsAppVM.Userid = "";
                //var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(_whatsAppVM.Emailid);
                //_whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                var client = new RestClient("https://api.interakt.ai/v1/public/track/users/");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic " + accesskey);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"traits\": {\r\n        \"name\": \"" + _whatsAppVM.UserName + "\",\r\n        \"email\": \"" + _whatsAppVM.Emailid + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                if (obj.result == "true")
                {
                    return "User Added successfully.";
                }
                else
                {
                    return "Failed to Add User. Please check all fields and try again.";
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to Add User. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendEventNotification(WhatsAppVM _whatsAppVM)
        {
            try
            {
                _whatsAppVM.WhatsAppNumber = "8077988602";
                _whatsAppVM.CountryCode = "+91";
                _whatsAppVM.MessageValue = "same time";
                _whatsAppVM.Userid = "";
                _whatsAppVM.Event = "Interview Notification";
                //Fetch WhatsAPpNumber
                var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(_whatsAppVM.Emailid);
                _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic " + accesskey);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "{\r\n    \"userId\": \"" + _whatsAppVM.Userid + "\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"Interview Notification\",\r\n    \"traits\": {\r\n        \"productName\": \"" + _whatsAppVM.MessageValue + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                if (obj.result == "true")
                {
                    return "Message sent successfully.";
                }
                else
                {
                    return "Failed to send message. Please check all fields and try again.";
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendOnAssignation(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    var name = _userService.FetchNameByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\": \"" + _whatsAppVM.InterviewDate + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    var name = _userService.FetchNameByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\": \"" + _whatsAppVM.InterviewDate + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n        \"PrimarySkill\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + name + "\",\r\n        \"Role\":\"" + _whatsAppVM.MessageValue + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendEInterviewer(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var username = _whatsAppVM.UserName;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = username;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + _whatsAppVM.UserName + "\",\r\n        \"InterviewerName\":\"" + name + "\"\r\n\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendApology(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + name + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendInterviewWaiting(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var Username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        if (_whatsAppVM.Event.Contains("Candidate"))
                        {
                            name = _whatsAppVM.UserName;
                        }
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        if (_whatsAppVM.Event.Contains("Interviewer"))
                        {
                            name = Username;
                        }
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"CandidateName\":\"" + name + "\",\r\n        \"Buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendReScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    var name = _userService.FetchNameByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\": \"" + _whatsAppVM.InterviewDate + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n        \"PrimarySkill\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"Buttonlink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + name + "\"\r\n\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendCandidateReminder(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"interviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"CandidateName\":\"" + name + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"Role\":\"" + _whatsAppVM.Header + "\",\r\n        \"Buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendClientCandidateConfirmation(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = username;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"Name\": \"" + _whatsAppVM.UserName + "\",\r\n        \"Skill\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"Status\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\",\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendCancleReScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        //request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\": \"" + _whatsAppVM.UserName + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n        \"Role\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n    }\r\n}", ParameterType.RequestBody);
                        request.AddParameter("application/json", "{\r\n\n    \"userId\": \"\",\r\n\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n\n    \"traits\": {\r\n\n        \"CandidateName\":\"" + _whatsAppVM.UserName + "\",\r\n\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n\n        \"Role\":\"" + _whatsAppVM.InterviewDate + "\"\r\n\n    }\r\n\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendRatingNotification(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var Username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.UserName = name;
                        _whatsAppVM.Emailid = item;
                        WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = Username;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"MessageValue\": \"" + _whatsAppVM.MessageValue + "\",\r\n        \"CandidateName\":\"" + _whatsAppVM.UserName + "\",\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"Name\":\"" + name + "\",\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public string WhatsAppSendClientCandidateConfirmation(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var WhatsAppnumber = _userService.FetchWhatsAppNumberByEmail(item);
                    if (!String.IsNullOrEmpty(WhatsAppnumber))
                    {
                        var name = _userService.FetchNameByEmail(item);
                        _whatsAppVM.UserName = name;
                        WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = username;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = WhatsAppnumber;
                        var client = new RestClient("https://api.interakt.ai/v1/public/track/events/");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Authorization", "Basic " + accesskey);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddParameter("application/json", "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"Name\": \"" + _whatsAppVM.UserName + "\",\r\n        \"Skill\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"Status\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\",\r\n    }\r\n}", ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
    }
}
