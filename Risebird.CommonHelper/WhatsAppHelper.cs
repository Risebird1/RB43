using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class WhatsAppHelper : IWhatsAppHelper
    {
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IConfiguration _configuration;
        private readonly string accesskey;
        private readonly IUserService _userService;
        private readonly IWhatsappRepository _whatsappRepository;
        private readonly IHttpRequestHelper _httpRequestHelper;

        public WhatsAppHelper(IAzureStorageHelper azureStorageHelper, IConfiguration configuration, IApplicationErrorLogger errorLogger,
            IUserService userService, IWhatsappRepository whatsappRepository, IHttpRequestHelper httpRequestHelper)
        {
            //_errorLogger = errorLogger;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _errorLogger = errorLogger;
            accesskey = _configuration["WhatsApp:AccessKey"];
            _userService = userService;
            _whatsappRepository = whatsappRepository;
            _httpRequestHelper = httpRequestHelper;
        }
        public async Task<string> WhatsAppTrackUser(WhatsAppVM _whatsAppVM)
        {
            try
            {
                _whatsAppVM.Userid = "";
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Basic " + accesskey);
                headers.Add("Content-Type", "application/json");

                List<HttpParameters> parameters = new List<HttpParameters>
                {
                    new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"traits\": {\r\n        \"name\": \"" + _whatsAppVM.UserName + "\",\r\n        \"email\": \"" + _whatsAppVM.Emailid + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                };

                var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/users/", Method.Post, null, headers, parameters);

                if (response != null)
                {
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
                else
                {
                    return "Failed to Add User. Please check all fields and try again.";
                }

            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to Add User. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendEventNotification(WhatsAppVM _whatsAppVM)
        {
            try
            {
                _whatsAppVM.WhatsAppNumber = "8077988602";
                _whatsAppVM.CountryCode = "+91";
                _whatsAppVM.MessageValue = "same time";
                _whatsAppVM.Userid = "";
                _whatsAppVM.Event = "Interview Notification";
                //Fetch WhatsAPpNumber
                var dataResult = await _userService.FetchWhatsAppNumberByEmail(_whatsAppVM.Emailid);
                _whatsAppVM.WhatsAppNumber = dataResult.Item1;

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Authorization", "Basic " + accesskey },
                    { "Content-Type", "application/json" }
                };

                List<HttpParameters> parameters = new List<HttpParameters>
                {
                    new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"" + _whatsAppVM.Userid + "\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"Interview Notification\",\r\n    \"traits\": {\r\n        \"productName\": \"" + _whatsAppVM.MessageValue + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                };

                var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                if (obj != null)
                {
                    _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                }
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
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }

        public async Task<string> WhatsAppSendOnAssignation(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);

                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };
                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\": \"" + _whatsAppVM.InterviewDate + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);

                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };
                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\": \"" + _whatsAppVM.InterviewDate + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n        \"PrimarySkill\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\",\r\n        \"Role\":\"" + _whatsAppVM.MessageValue + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);



                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }

                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }

        public async Task<string> WhatsAppSendEInterviewer(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        var username = _whatsAppVM.UserName;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = username;

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + _whatsAppVM.UserName + "\",\r\n        \"InterviewerName\":\"" + dataResult.Item2 + "\"\r\n\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);



                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendApology(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendInterviewWaiting(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var Username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);
                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\",\r\n        \"Buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);

                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendReScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"Buttonlink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\"\r\n\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }


        public async Task<string> WhatsAppSendCancleReScheduling(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n\n    \"userId\": \"\",\r\n\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n\n    \"traits\": {\r\n\n        \"CandidateName\":\"" + _whatsAppVM.UserName + "\",\r\n\n        \"ClientName\":\"" + _whatsAppVM.Header + "\",\r\n\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\",\r\n\n        \"Role\":\"" + _whatsAppVM.InterviewDate + "\"\r\n\n    }\r\n\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendRatingNotification(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                var Username = _whatsAppVM.UserName;
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);
                        _whatsAppVM.UserName = Username;

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n\n    \"userId\": \"\",\r\n\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n\n    \"traits\": {\r\n\n        \"MessageValue\":\"" + _whatsAppVM.MessageValue + "\",\r\n\n        \"CandidateName\": \"" + _whatsAppVM.UserName + "\",\r\n\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n\n        \"Name\":\"" + dataResult.Item2 + "\"\r\n\n    }\r\n\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendFeedbackRemainder(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }

        public async Task<string> WhatsAppSendInterviewerFeedbackRemainder(WhatsAppVM _whatsAppVM)
        {
            try
            {
                if (_whatsAppVM != null)
                {
                    var Emailid = _whatsAppVM.Emailid?.Split(";");
                    if (Emailid != null)
                    {
                        foreach (var item in Emailid)
                        {
                            var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                            if (!string.IsNullOrEmpty(dataResult.Item1))
                            {
                                _whatsAppVM.UserName = dataResult.Item2;
                                _whatsAppVM.Emailid = item;
                                _whatsAppVM.CountryCode = "+91";
                                _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                                await WhatsAppTrackUser(_whatsAppVM);

                                Dictionary<string, string> headers = new Dictionary<string, string>
                                {
                                    { "Authorization", "Basic " + accesskey },
                                    { "Content-Type", "application/json" }
                                };

                                List<HttpParameters> parameters = new List<HttpParameters>
                                {
                                    new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"InterviewerName\":\"" + dataResult.Item2 + "\",\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                                };

                                var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                                var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                                if (obj != null)
                                {
                                    _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                                }
                            }
                        }
                        return "Message sent successfully.";

                    }
                    else
                    {
                        return "User EmailId missing!";
                    }
                }
                else
                {
                    return "User details missing!";
                }
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }

        public async Task<string> WhatsAppSendInterviewerCommonInterviewDetails(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                                {
                                    { "Authorization", "Basic " + accesskey },
                                    { "Content-Type", "application/json" }
                                };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"TotalCount\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\",\r\n        \"InterviewerName\":\"" + dataResult.Item2 + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };

                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);

                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendInterviewReminder(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                                {
                                    { "Authorization", "Basic " + accesskey },
                                    { "Content-Type", "application/json" }
                                };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\",\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"PrimarySkill\":\"" + _whatsAppVM.PrimarySkill + "\",\r\n        \"TimeZone\":\"" + _whatsAppVM.timezone + "\",\r\n        \"ClientName\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);

                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
        public async Task<string> WhatsAppSendClientCompleteInterview(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1.Replace("+91", "");
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                                {
                                    { "Authorization", "Basic " + accesskey },
                                    { "Content-Type", "application/json" }
                                };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"UserName\": \"" + _whatsAppVM.UserName + "\",\r\n        \"ButtonLink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);


                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }

        private async Task<bool> BridgeFuncSaveSentWhatsappMessage(WhatsAppVM _whatsAppVM)
        {
            try
            {
                if (_whatsAppVM != null)
                {
                    var obj = new SentWhatsAppMessageDetails();
                    obj.Recipient = _whatsAppVM.Emailid;
                    obj.CountryCode = _whatsAppVM.CountryCode;
                    obj.WhatsappNumber = _whatsAppVM.WhatsAppNumber;
                    obj.Message = _whatsAppVM.MessageValue;
                    obj.ButtonLink = _whatsAppVM.buttonlink;
                    obj.Event = _whatsAppVM.Event;
                    obj.InterviewDate = _whatsAppVM.InterviewDate;
                    obj.Header = _whatsAppVM.Header;
                    if (_whatsappRepository != null)
                    {
                        return await _whatsappRepository.SaveSentWhatsappMessageDetails(obj);
                    }
                    else
                    {
                        await _errorLogger.LogMessage(LogLevelInfo.Error, "dependency null exception");
                    }
                }
                return false;


            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<string> WhatsAppSendCandidateDashboardReminder(WhatsAppVM _whatsAppVM)
        {
            try
            {
                var Emailid = _whatsAppVM.Emailid.Split(";");
                foreach (var item in Emailid)
                {
                    var dataResult = await _userService.FetchWhatsAppNumberByEmail(item);
                    if (!string.IsNullOrEmpty(dataResult.Item1))
                    {
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        _whatsAppVM.CountryCode = "+91";
                        _whatsAppVM.WhatsAppNumber = dataResult.Item1;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                                {
                                    { "Authorization", "Basic " + accesskey },
                                    { "Content-Type", "application/json" }
                                };

                        List<HttpParameters> parameters = new List<HttpParameters>
                        {
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"CandidateName\":\"" + dataResult.Item2 + "\",\r\n        \"InterviewDate\":\"" + _whatsAppVM.InterviewDate + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, null, headers, parameters);

                        var obj = JsonConvert.DeserializeObject<WhatsAppVMResponse>(response.Content);
                        if (obj != null)
                        {
                            _ = await BridgeFuncSaveSentWhatsappMessage(_whatsAppVM);
                        }
                    }
                }
                return "Message sent successfully.";
            }
            catch (Exception ex)
            {
                await _errorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to send message. Please check all fields and try again.";
            }
        }
    }
}
