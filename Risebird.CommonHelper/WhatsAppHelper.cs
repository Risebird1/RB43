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
using System.Text.RegularExpressions;

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
        private static readonly HashSet<string> KnownCountryCodes = new HashSet<string>
    {
        "1","7","20","27","30","31","32","33","34","36","39","40","41","43","44","45","46","47","48","49",
        "52","54","55","56","57","58","60","61","62","63","64","65","66","81","82","84","86","90","91",
        "92","93","94","95","98","211","212","213","216","218","220","221","222","223","224","225","226",
        "227","228","229","230","231","232","233","234","235","236","237","238","239","240","241","242",
        "243","244","245","246","248","249","250","251","252","253","254","255","256","257","258","260",
        "261","262","263","264","265","266","267","268","269","290","291","297","298","299","350","351",
        "352","353","354","355","356","357","358","359","370","371","372","373","374","375","376","377",
        "378","379","380","381","382","383","385","386","387","389","420","421","423","501","502","503",
        "504","505","506","507","508","509","590","591","592","593","594","595","596","597","598","599",
        "670","672","673","674","675","676","677","678","679","680","681","682","683","685","686","687",
        "688","689","690","691","692","850","852","853","855","856","880","886","960","961","962","963",
        "964","965","966","967","968","970","971","972","973","974","975","976","977","992","993","994",
        "995","996","998"
    };
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
                        var phonenumber = ExtractCountryCode(dataResult.Item1);
                        if (string.IsNullOrWhiteSpace(phonenumber.Item1))
                        {
                            _whatsAppVM.CountryCode = "+91";
                        }
                        else
                        {
                            _whatsAppVM.CountryCode = phonenumber.Item1;
                        }
                        _whatsAppVM.WhatsAppNumber = phonenumber.Item2;
                        _whatsAppVM.UserName = dataResult.Item2;
                        _whatsAppVM.Emailid = item;
                        await WhatsAppTrackUser(_whatsAppVM);

                        Dictionary<string, string> headers = new Dictionary<string, string>
                        {
                            { "Authorization", "Basic " + accesskey },
                            { "Content-Type", "application/json" }
                        };
                        var traits = new Dictionary<string, object>();
                        traits["Client"] = _whatsAppVM.ClientName;
                        traits["JdTitle"] = _whatsAppVM.PrimarySkill;
                        traits["ScheduleLink"] = _whatsAppVM.buttonlink;
                        traits["UnsubscribeLink"] = _whatsAppVM.UnsubscribeLink;

                        var bodyData = new
                        {
                            phoneNumber = _whatsAppVM.WhatsAppNumber,
                            countryCode = _whatsAppVM.CountryCode,
                            @event = _whatsAppVM.Event,
                            traits = traits
                        };
                        var response = await _httpRequestHelper.MakeHttpRequest("https://api.interakt.ai/v1/public/track/events/", Method.Post, bodyData, headers);
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
                    obj.InterviewId = _whatsAppVM.InterviewId != 0 ? _whatsAppVM.InterviewId : null;
                    obj.IRFId = _whatsAppVM.irfId != 0 ? _whatsAppVM.irfId : null;
                    obj.SentBy = _whatsAppVM.SentBy;
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
        public async Task<string> WhatsAppSkillChannelInterviewerAvailbility(WhatsAppVM _whatsAppVM)
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
                            new HttpParameters() { name = "application/json", value = "{\r\n    \"userId\": \"\",\r\n    \"phoneNumber\": \"" + _whatsAppVM.WhatsAppNumber + "\",\r\n    \"countryCode\": \"" + _whatsAppVM.CountryCode + "\",\r\n    \"event\": \"" + _whatsAppVM.Event + "\",\r\n    \"traits\": {\r\n        \"Name\":\"" + dataResult.Item2 + "\",\r\n        \"IRFName\":\"" + _whatsAppVM.MessageValue + "\",\r\n        \"JDTitle\":\"" + _whatsAppVM.PrimarySkill + "\",\r\n        \"PerSuccessRate\":\"" + _whatsAppVM.PerSuccessRate + "\",\r\n        \"Interviewduration\":\"" + _whatsAppVM.Interviewduration + "\",\r\n        \"buttonlink\":\"" + _whatsAppVM.buttonlink + "\"\r\n    }\r\n}", parameterType = ParameterType.RequestBody }
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



        /// <summary>
        /// Extracts the country code (if any) and returns the clean mobile number.
        /// </summary>
        public (string CountryCode, string MobileNumber) ExtractCountryCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ("", "");

            // Remove spaces, dashes, parentheses, etc.
            string phone = Regex.Replace(input, @"[^\d\+]", "");

            // Case 1: Starts with '+'
            if (phone.StartsWith("+"))
            {
                var match = Regex.Match(phone, @"^\+(\d{1,3})(\d+)$");
                if (match.Success)
                {
                    return ($"+{match.Groups[1].Value}", match.Groups[2].Value);
                }
            }

            // Case 2: Starts with '00'
            if (phone.StartsWith("00"))
            {
                var match = Regex.Match(phone, @"^00(\d{1,3})(\d+)$");
                if (match.Success)
                {
                    return ($"+{match.Groups[1].Value}", match.Groups[2].Value);
                }
            }

            // Case 3: Numeric and longer than 10 digits (try known country codes)
            if (Regex.IsMatch(phone, @"^\d+$"))
            {
                if (phone.Length <= 10)
                    return ("", phone); // Local number (no country code)

                for (int len = 3; len >= 1; len--)
                {
                    if (phone.Length > len)
                    {
                        string prefix = phone.Substring(0, len);
                        if (KnownCountryCodes.Contains(prefix))
                        {
                            string remainder = phone.Substring(len);
                            if (remainder.Length >= 6 && remainder.Length <= 14)
                            {
                                return ($"+{prefix}", remainder);
                            }
                        }
                    }
                }
            }
            // Default: No country code detected
            return ("", phone);
        }
    }
}
