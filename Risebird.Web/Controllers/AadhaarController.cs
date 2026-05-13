using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class AadhaarController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAadhaarService _aadhaarService;
        private readonly string apiendpoint;
        private readonly string client_code;
        private readonly string api_key;
        private readonly string salt;
        private readonly string user_name;
        private readonly string password;
        private readonly string passportUrl;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;
        private readonly IAadhaarRepository _aadhaarRepository;
        private readonly IInterviewerService _interviewerService;
        private readonly string employementapikey;
        private readonly string employementendpoint;
        private readonly string faceCompareApiendpoint;
        private readonly string faceCompare_api_key;
        private readonly string faceCompare_salt;

        public AadhaarController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IAadhaarService aadhaarService,
            IFileMimeTypeHelper fileMimeTypeHelper, IHttpRequestHelper httpRequestHelper, HttpClientHelper factory, IAadhaarRepository aadhaarRepository, IInterviewerService interviewerService)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            apiendpoint = _configuration["AadhaarApiDetails:endPoint"];
            client_code = _configuration["AadhaarApiDetails:client_code"];
            api_key = _configuration["AadhaarApiDetails:api_key"];
            salt = _configuration["AadhaarApiDetails:salt"];
            _aadhaarService = aadhaarService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            user_name = _configuration["CandidatePassportVerfication:username"];
            password = _configuration["CandidatePassportVerfication:password"];
            passportUrl = _configuration["CandidatePassportVerfication:endPoint"];
            _httpRequestHelper = httpRequestHelper;
            _httpClient = factory.GetHttpClient();
            _aadhaarRepository = aadhaarRepository;
            _interviewerService = interviewerService;
            employementapikey = _configuration["EmploymentVerfication:apikey"];
            employementendpoint = _configuration["EmploymentVerfication:endPoint"];
            faceCompareApiendpoint = _configuration["AadhaarApiDetails:faceCompareEndPoint"];
            faceCompare_api_key = _configuration["AadhaarApiDetails:faceCompareApiKey"];
            faceCompare_salt = _configuration["AadhaarApiDetails:faceCompareSalt"];
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> FetchAadhaarDetails(string MCQID, string candidateEmail)
        {
            var model = await _aadhaarService.FetchAadhaarDetails(MCQID, candidateEmail);
            if (model == null)
            {
                model = new AadhaarData();
            }
            return Json(model);
        }
        [HttpPost]
        public async Task<JsonResult> SendAadhaarNumber(string aadharnumber)
        {
            var model = new AadhaarVM();
            var hashstring = client_code + "|" + api_key + "|" + salt;
            model.request.aadhaar = aadharnumber;
            model.request.hash = getHashSha256(hashstring);
            model.headers.client_code = client_code;
            model.headers.sub_client_code = client_code;
            model.headers.function_code = "DEFAULT";
            model.headers.function_sub_code = "DEFAULT";
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };
            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "enterAadhaar", Method.Post, model, headers);

            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {
                return Json(new { message = convertedResponse.response_status.message });
            }
            return Json(new { message = convertedResponse.response_data.message, uuid = convertedResponse.response_data.uuid });
        }
        [HttpPost]
        public async Task<JsonResult> SubmitOTPNumber(string uuid, string otp, string datenow)
        {
            var model = new AadhaarVM();
            model.request.uuid = uuid;
            model.request.otp = otp;
            model.headers.client_code = client_code;
            model.headers.function_code = "DEFAULT";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "enterOtp", Method.Post, model, headers);
            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            return Json(convertedResponse.response_data);
        }
        public string getHashSha256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public async Task<JsonResult> SaveAadhaarDetails(string Name, string Address, string gender, string dob, string email, string doc_face)
        {
            var aadhaarKYCmodel = new AadhaarData();
            aadhaarKYCmodel.Name = Name;
            aadhaarKYCmodel.Address = Address;
            aadhaarKYCmodel.gender = gender;
            aadhaarKYCmodel.dob = dob;
            aadhaarKYCmodel.doc_face = doc_face;
            aadhaarKYCmodel.email = email;
            aadhaarKYCmodel.reference_id = "0";
            aadhaarKYCmodel.verified_using = "Interview";
            await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
            return Json("Aadhaar Details are verified successfully.");
        }
        public void SetHeaderValue(AadhaarHeader _header, string datenow, string saltstan)
        {
            _header.client_code = client_code;
            _header.sub_client_code = client_code;
            _header.channel_code = "ANDROID_SDK";
            _header.channel_version = "1";
            _header.stan = datenow;
            _header.client_ip = "";
            _header.transmission_datetime = Convert.ToInt64(datenow);
            _header.operation_mode = "SELF";
            _header.run_mode = "DEFAULT";
            _header.actor_type = "DEFAULT";
            _header.user_handle_type = "EMAIL";
            _header.user_handle_value = "talent@risebird.io";
            _header.location = "";
            _header.function_code = "DEFAULT";
            _header.function_sub_code = "DEFAULT";
        }
        #region US Candidate Verification
        public async Task<IActionResult> CandidateVerification(string uniqueid)
        {
            var candidateEmailid = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            var model = new candidatePassportVerificated();
            model.uniqueid = uniqueid;
            if (candidateEmailid == null)
            {
                model.status = "InvalidEmail";
            }
            else
            {
                model.email = candidateEmailid.EmailId;
            }
            if (TempData["SuccessAadhaarMessage"] != null)
            {
                TempData.Remove("SuccessAadhaarMessage");
                model.status = "Success";
            }
            return View(model);
        }
        [HttpPost]
        public async Task<JsonResult> Getshuftipro(verifyResponse _model)
        {
            var _model1 = JsonConvert.SerializeObject(_model);
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug, _model1);
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug, _model.reference);
            return Json("");
        }
        #endregion
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AadharDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        var adhardetail = await _aadhaarService.GetAadharDetails();
                        ViewBag.profile = profileName;
                        return View(adhardetail);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Interview");

            }
        }
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CandidatePassportVerificated(candidatePassportVerificated model)
        {
            if (model.PassportFront != null && model.PassportBack != null && model.CandidateImage != null)
            {
                var fileMimeTypefront = _fileMimeTypeHelper.GetFileMimeType(model.PassportFront);
                if (fileMimeTypefront == "image/jpg" || fileMimeTypefront == "image/jpeg" || fileMimeTypefront == "image/png")
                {
                    await _azureStorageHelper.InsertImageBlob(model.PassportFront.FileName, "passportdetails", model.PassportFront, fileMimeTypefront);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("passportdetails");
                    var blob = containerRef.GetBlockBlobReference(model.PassportFront.FileName);
                    string uploadedaudioFileURL = blob.Uri.ToString();
                    if (uploadedaudioFileURL == null)
                    {
                        ModelState.AddModelError(string.Empty, "Passport Front Upload failed.");
                    }
                    else
                    {
                        model.Passportfrontlink = uploadedaudioFileURL;
                    }
                }
                var fileMimeTypeback = _fileMimeTypeHelper.GetFileMimeType(model.PassportBack);
                if (fileMimeTypeback == "image/jpg" || fileMimeTypeback == "image/jpeg" || fileMimeTypeback == "image/png")
                {
                    await _azureStorageHelper.InsertImageBlob(model.PassportBack.FileName, "passportdetails", model.PassportBack, fileMimeTypefront);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("passportdetails");
                    var blob = containerRef.GetBlockBlobReference(model.PassportBack.FileName);
                    string uploadedaudioFileURL = blob.Uri.ToString();
                    if (uploadedaudioFileURL == null)
                    {
                        ModelState.AddModelError(string.Empty, "Passport Back Upload failed.");
                    }
                    else
                    {
                        model.Passportbacklink = uploadedaudioFileURL;
                    }
                }
                var fileMimeTypeimage = _fileMimeTypeHelper.GetFileMimeType(model.CandidateImage);
                if (fileMimeTypeimage == "image/jpg" || fileMimeTypeimage == "image/jpeg" || fileMimeTypeimage == "image/png")
                {
                    await _azureStorageHelper.InsertImageBlob(model.CandidateImage.FileName, "passportdetails", model.CandidateImage, fileMimeTypefront);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("passportdetails");
                    var blob = containerRef.GetBlockBlobReference(model.CandidateImage.FileName);
                    string uploadedaudioFileURL = blob.Uri.ToString();
                    if (uploadedaudioFileURL == null)
                    {
                        ModelState.AddModelError(string.Empty, "User Image Upload failed.");
                    }
                    else
                    {
                        model.CandidateImagelink = uploadedaudioFileURL;
                    }
                }
                if (String.IsNullOrWhiteSpace(model.CandidateImagelink) || String.IsNullOrWhiteSpace(model.Passportfrontlink) || String.IsNullOrWhiteSpace(model.Passportbacklink))
                {
                    return View("CandidateVerification", model);
                }
                var modelsave = new candidateDetailsresult();
                //Start: Passport Verification
                var modellist = new CandidatePassportRequest();
                modellist.username = user_name;
                modellist.password = password;
                //var client = new RestClient(passportUrl + "patrons/login");
                //var request = new RestRequest(passportUrl + "patrons/login", Method.Post);
                //request.AddHeader("content-type", "application/json");
                //request.AddJsonBody(modellist);
                //RestResponse response = await client.ExecuteAsync(request);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                };

                var response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "patrons/login", Method.Post, modellist, headers);



                var loginresponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(response.Content);
                var identityResonse = await Verification_Identityflow(model, loginresponse);
                var extractresponse = await Verification_Passport_extraction(model, identityResonse);
                var faceurl = await Verification_Passport_Faceextraction(model, identityResonse);
                var verification = await Verification_Passport_verification(model, extractresponse, identityResonse.id);
                //var resultset = await Verification_FaceMatch(model, loginresponse, faceurl.response.result.cropped);
                var resultset = await Verification_FaceMatch_veri5(model.Passportfrontlink, faceurl.response.result.cropped);
                //End: Passport Verification
                modelsave.name = extractresponse.response.result.name;
                modelsave.passportNumber = extractresponse.response.result.passportNumber;
                modelsave.nationality = extractresponse.response.result.nationality;
                modelsave.sex = extractresponse.response.result.sex;
                modelsave.birthDate = extractresponse.response.result.birthDate;
                modelsave.address = extractresponse.response.result.address;
                modelsave.email = model.email;
                modelsave.matchPercentage = resultset.ToString();
                modelsave.doc_face = ConvertImageURLToBase64(faceurl.response.result.cropped);
                modelsave.passport_image = ConvertImageURLToBase64(model.CandidateImagelink);
                //save in db
                await _aadhaarService.SaveCandidateVerificationDetails(modelsave);
                TempData["SuccessAadhaarMessage"] = verification.response.result.message;
                return RedirectToAction("CandidateVerification", new { uniqueid = model.uniqueid });
            }
            ModelState.AddModelError(string.Empty, "All files are mandatory to upload.");
            return RedirectToAction("CandidateVerification", new { uniqueid = model.uniqueid });
        }
        public async Task<CandidatePassportResponse> Verification_Identityflow(candidatePassportVerificated model, CandidatePassportResponse convertedResponse)
        {
            var modellist = new CandidatePassportRequest();
            modellist.type = "passport";
            modellist.email = "Prashantindoliya@gmail.com";
            modellist.callbackUrl = "https://portal.risebird.io/Aadhaar/Getshuftipro";
            modellist.images.Add(model.Passportfrontlink);
            modellist.images.Add(model.Passportbacklink);
            //var clientrequest = new RestClient(passportUrl + "patrons/" + convertedResponse.userId + "/identities");
            //var _request = new RestRequest(passportUrl + "patrons/" + convertedResponse.userId + "/identities", Method.Post);
            //_request.AddHeader("content-type", "application/json");
            //_request.AddHeader("Authorization", convertedResponse.id);
            //_request.AddJsonBody(modellist);
            //RestResponse _response = await clientrequest.ExecuteAsync(_request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", convertedResponse.id }
            };

            var _response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "patrons/" + convertedResponse.userId + "/identities", Method.Post, modellist, headers);

            var _convertedResponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(_response.Content);
            return _convertedResponse;
        }
        public async Task<CandidatePassportResponse> Verification_Passport_extraction(candidatePassportVerificated model, CandidatePassportResponse convertedResponse)
        {
            var modellist = new CandidatePassportRequest();
            modellist.service = "Identity";
            modellist.itemId = convertedResponse.id;
            modellist.task = "autoRecognition";
            modellist.essentials = new candidateverificationessentials();
            modellist.accessToken = convertedResponse.accessToken;
            //var clientrequest = new RestClient(passportUrl + "snoops");
            //var _request = new RestRequest(passportUrl + "snoops", Method.Post);
            //_request.AddHeader("content-type", "application/json");
            //_request.AddJsonBody(modellist);
            //RestResponse _response = await clientrequest.ExecuteAsync(_request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var _response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "snoops", Method.Post, modellist, headers);

            var _convertedResponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(_response.Content);
            return _convertedResponse;
        }
        public async Task<CandidatePassportResponse> Verification_Passport_Faceextraction(candidatePassportVerificated model, CandidatePassportResponse convertedResponse)
        {
            var modellist = new CandidatePassportRequest();
            modellist.service = "Identity";
            modellist.itemId = convertedResponse.id;
            modellist.task = "faceExtraction";
            modellist.essentials = new candidateverificationessentials();
            modellist.essentials.url = model.Passportfrontlink;
            modellist.accessToken = convertedResponse.accessToken;
            //var clientrequest = new RestClient(passportUrl + "snoops");
            //var _request = new RestRequest(passportUrl + "snoops", Method.Post);
            //_request.AddHeader("content-type", "application/json");
            //_request.AddJsonBody(modellist);
            //RestResponse _response = await clientrequest.ExecuteAsync(_request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var _response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "snoops", Method.Post, modellist, headers);

            var _convertedResponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(_response.Content);
            return _convertedResponse;
        }
        public async Task<CandidatePassportResponse> Verification_Passport_verification(candidatePassportVerificated model, CandidatePassportResponse convertedResponse, string itemid)
        {
            var modellist = new CandidatePassportRequest();
            modellist.service = "Identity";
            modellist.itemId = itemid;
            modellist.task = "verification";
            modellist.essentials = new candidateverificationessentials();
            modellist.essentials.fileNumber = convertedResponse.response.result.fileNumber;
            modellist.essentials.dob = convertedResponse.response.result.birthDate;
            modellist.essentials.name = convertedResponse.response.result.name;
            modellist.accessToken = convertedResponse.accessToken;
            //var clientrequest = new RestClient(passportUrl + "snoops");
            //var _request = new RestRequest(passportUrl + "snoops", Method.Post);
            //_request.AddHeader("content-type", "application/json");
            //_request.AddJsonBody(modellist);
            //RestResponse _response = await clientrequest.ExecuteAsync(_request);
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var _response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "snoops", Method.Post, modellist, headers);

            var _convertedResponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(_response.Content);
            return _convertedResponse;
        }
        public async Task<CandidatePassportResponse> Verification_FaceMatch(candidatePassportVerificated model, CandidatePassportResponse convertedResponse, string secondinamge)
        {
            var modellist = new CandidatePassportRequest();
            modellist.essentials = new candidateverificationessentials();
            modellist.essentials.firstImage = model.Passportfrontlink;
            modellist.essentials.secondImage = secondinamge;
            modellist.accessToken = convertedResponse.accessToken;
            //var clientrequest = new RestClient(passportUrl + "patrons/" + convertedResponse.userId + "/facematches");
            //var _request = new RestRequest(passportUrl + "patrons/" + convertedResponse.userId + "/facematches", Method.Post);
            //_request.AddHeader("content-type", "application/json");
            //_request.AddHeader("Authorization", convertedResponse.id);
            //_request.AddJsonBody(modellist);
            //RestResponse _response = await clientrequest.ExecuteAsync(_request);
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", convertedResponse.id },
                { "Content-Type", "application/json" }
            };

            var _response = await _httpRequestHelper.MakeHttpRequest(passportUrl + "patrons/" + convertedResponse.userId + "/facematches", Method.Post, modellist, headers);

            var _convertedResponse = JsonConvert.DeserializeObject<CandidatePassportResponse>(_response.Content);
            return _convertedResponse;
        }

        public string ConvertImageURLToBase64(String url)
        {
            StringBuilder _sb = new StringBuilder();
            Byte[] _byte = GetImage(url);
            _sb.Append(Convert.ToBase64String(_byte, 0, _byte.Length));
            return _sb.ToString();
        }
        private byte[] GetImage(string url)
        {
            Stream stream = null;
            byte[] buf;
            try
            {
                WebProxy myProxy = new WebProxy();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                stream = response.GetResponseStream();
                using (BinaryReader br = new BinaryReader(stream))
                {
                    int len = (int)(response.ContentLength);
                    buf = br.ReadBytes(len);
                    br.Close();
                }
                stream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                buf = null;
            }
            return (buf);
        }
        public async Task<int> Verification_FaceMatch_veri5(string imagefirst, string secondinamge, bool imageinbase64 = false)
        {
            var model = new AadhaarVM();
            var datenow = GetCurrentUnixTimestampMillis();
            SetHeaderValue(model.headers, datenow, faceCompare_salt);
            model.request.api_key = faceCompare_api_key;
            model.request.request_id = Guid.NewGuid().ToString();
            model.request.purpose = "VERIFY";
            if (!imageinbase64)
            {
                model.request.image_1 = ConvertImageURLToBase64(imagefirst);
                model.request.image_2 = ConvertImageURLToBase64(secondinamge);
            }
            else
            {
                model.request.image_1 = imagefirst;
                model.request.image_2 = secondinamge;
            }
            var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + faceCompare_api_key + "|" + faceCompare_salt;
            model.request.hash = getHashSha256(hashstring);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                //{ "Authorization", "Basic " + accesskey },
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(faceCompareApiendpoint + "faceCompare", Method.Post, model, headers);

            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {
                return 0;
            }

            var score = convertedResponse.response_data.score != null ? convertedResponse.response_data.score.Split(".")[0] : "0";
            return Convert.ToInt16(score);//convertedResponse.response_data.score
        }
        //public string GetCurrentUnixTimestampMillis()
        //{
        //    DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    DateTime localDateTime, univDateTime;
        //    localDateTime = DateTime.Now;
        //    univDateTime = localDateTime.ToUniversalTime();
        //    return ((univDateTime - UnixEpoch).TotalMilliseconds).ToString();
        //}
        public string GetCurrentUnixTimestampMillis()
        {
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime localDateTime, univDateTime;
            localDateTime = DateTime.Now;
            univDateTime = localDateTime.ToUniversalTime();
            var millis = (univDateTime - UnixEpoch).TotalMilliseconds.ToString();
            return millis.Split(".")[0];
        }

        #region Employment Verification
        public async Task<IActionResult> EmployementDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    //if (profileName == "Admin" || profileName == "SupportAdmin")
                    //{
                    var employmentdetails = await _aadhaarService.GetEmployementDetails();
                    ViewBag.profile = profileName;
                    return View(employmentdetails);
                    //}
                    //else
                    //{
                    //    return RedirectToAction("SignOut", "Session");
                    //}
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index", "Interview");
            }
        }
        [HttpPost]
        public async Task<JsonResult> VerifyEmployment(string candidateemail, string mobilenumber)
        {
            if (!string.IsNullOrWhiteSpace(mobilenumber))
            {
                var employementdetails = await _aadhaarService.CheckEmployementDetails(mobilenumber);
                if (employementdetails == 0)
                {
                    EmployementVerification(mobilenumber, candidateemail);
                    Task.Delay(100).Wait();
                    return Json("Employment verification is in process, please look in the list in couple of seconds.");
                }
                else
                {
                    return Json("Already verified, please find in the employment detail list");
                }
            }
            return Json("");
        }
        public async Task EmployementVerification(string mobile, string candidateEmail)
        {
            SignzyEmployeeRequest mobiledata = new SignzyEmployeeRequest();
            mobiledata.mobileNumber = mobile;
            mobiledata.CandidateEmail = candidateEmail;
            mobiledata.panNumber = "";
            var path = $"https://risebirdazuretimerfunctions.azurewebsites.net/api/SignzyEmploymentVerification";
            var functionKey = "LjeeKEjzQQqawcVuFtmM201YyePZnCvq53K5AaG9Fg2eAzFuVj3nAA==";
            await _httpClient.SendRequestAsync(HttpMethod.Post, path, mobiledata, functionKey, "", _cancellationToken);
        }
        [HttpPost]
        public async Task<JsonResult> EvVerification(string candidateemail, string mobilenumber, int? applicantId, bool isApplicantRenewed = false, string nameAsMentionedByApplicant = null, int? irfId = null, int? assessmentGroupId = null, string referredBy = null)
        {
            string loggedInUser = string.Empty;
            if (!isApplicantRenewed)
            {
                loggedInUser = HttpContext.Session.GetString("email")?.Trim();
                if (!string.IsNullOrEmpty(loggedInUser))
                {
                    loggedInUser = loggedInUser.Replace("\"", "");
                }
            }

            var employementdetails = new EmploymentDetailsDM();
            if (!string.IsNullOrWhiteSpace(mobilenumber))
            {
                employementdetails = await _aadhaarService.GetEmployementDetailsByMobileNumber(mobilenumber);
                if (employementdetails == null)
                {
                    employementdetails = new EmploymentDetailsDM();
                    //TODO: Update According to blacklist data pr deployement
                    var model = await EmployementVerificationDetails(candidateemail, mobilenumber);
                    if (string.IsNullOrWhiteSpace(model.message) && !string.IsNullOrEmpty(model.CandidateEmail) && (model.CandidateEmail.ToLower() == candidateemail.ToLower()))
                    {
                        var companyname = model.summary?.recentEmployerData.establishmentName;
                        var companystatus = await _aadhaarService.CheckCompanyStatus(companyname);
                        if (companystatus != null)
                        {
                            //var _dob = model.uanDetails1.FirstOrDefault()?.date_of_birth;
                            //DateTime? dob = !string.IsNullOrWhiteSpace(_dob) ? Convert.ToDateTime(_dob) : null;
                            //employementdetails.CandidateEmail = model.CandidateEmail;
                            //employementdetails.MobileNumber = model.MobileNumber;
                            //employementdetails.isVerified = true;
                            //if (model.employmentSummary != null)
                            //{
                            //    employementdetails.Isemployed = model.employmentSummary.is_employed;
                            //}
                            employementdetails.message = "This Interviwer Company has been blacklisted in our system.";
                        }
                        else
                        {
                            var _dob = model.uanDetails1.FirstOrDefault()?.date_of_birth;
                            DateTime? dob = !string.IsNullOrWhiteSpace(_dob) ? Convert.ToDateTime(_dob) : null;
                            employementdetails.CandidateEmail = model.CandidateEmail?.Trim();
                            employementdetails.MobileNumber = model.MobileNumber;
                            employementdetails.Name = model.uanDetails1?.FirstOrDefault().name;
                            employementdetails.isVerified = true;
                            employementdetails.CompanyName = companyname;
                            if (model.summary != null)
                            {
                                employementdetails.Isemployed = !model.summary.dateOfExitMarked;
                            }
                            // employementdetails.message = "For this employment Company is in black list in our system.";
                        }
                    }
                    else
                    {
                        if (model.message.Contains("Upstream"))
                        {
                            employementdetails.message = "Failed to verify , API didn't work, try after sometime";
                        }
                        //else if (model.CandidateEmail.ToLower() != candidateemail.ToLower())
                        //{
                        //    employementdetails.message = "The emailId specified does not match with the one associated with this mobile number.";
                        //}
                        else
                        {
                            employementdetails.message = "No employement records are found, so we don't process further.";
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(employementdetails.message))
                    {
                        employementdetails.message = "Employment records not found.";
                    }
                    //else if (string.IsNullOrEmpty(employementdetails.CandidateEmail) || employementdetails.CandidateEmail.ToLower() != candidateemail.ToLower())
                    //{
                    //    employementdetails.message = "The emailId specified does not match with the one associated with this mobile number.";
                    //}
                    else
                    {
                        var companystatus = await _aadhaarService.CheckCompanyStatus(employementdetails.CompanyName);
                        if (companystatus != null)
                        {
                            employementdetails.message = "Company in blacklist!";
                        }
                    }
                }
            }
            else
            {
                employementdetails.message = "Fail to verify,please check the details and try again.";
            }
            //if (employementdetails != null && string.IsNullOrEmpty(employementdetails.message) && !employementdetails.Isemployed)
            //{
            //    employementdetails.message = "Employment records not found.";
            //}
            employementdetails.CandidateEmail = employementdetails.CandidateEmail?.Trim();
            bool hasIrf = irfId.HasValue && irfId.Value > 0;
            bool hasAg = assessmentGroupId.HasValue && assessmentGroupId.Value > 0;
            if ((applicantId.HasValue || (isApplicantRenewed && (hasIrf || hasAg))) && employementdetails != null && employementdetails.message == null)
            {
                var model = new InterviewerDirectApplicationVM();
                string str = employementdetails.Name;
                if (str != null)
                {
                    int index = str.IndexOf(" ");
                    if (index > 0)
                    {
                        model.FirstName = str.Substring(0, index);
                        model.LastName = str.Substring(index + 1);
                    }
                    else
                    {
                        model.FirstName = employementdetails.Name;
                    }
                }
                model.isVerified = true;
                model.IsEmployed = employementdetails.Isemployed;
                model.CompanyName = employementdetails.CompanyName;
                model.DOB = employementdetails.DOB;
                if (applicantId.HasValue)
                {
                    model.id = applicantId.Value;
                    await _interviewerService.UpdateApplicantsEmploymentDetails(model, loggedInUser);
                }
                else if (isApplicantRenewed && (hasIrf || hasAg))
                {
                    model.EmailId = candidateemail;
                    model.Phone = mobilenumber;
                    model.ReferredBy = referredBy;
                    model.IRFId = hasIrf ? irfId.Value : 0;
                    model.AssessmentGroupId = hasAg ? assessmentGroupId.Value : 0;
                    model.NameAsMentionedByApplicant = nameAsMentionedByApplicant;
                    model.IsEmployed = true;
                    model = await _interviewerService.InsertApplicantsRenewed(model);
                    return Json(new { id = model.id, assessmentGroup = model.AssessmentGroup, message = employementdetails.message });
                }
            }
            return Json(employementdetails);
        }

        //public async Task<SignzyEmployeeResponse> EmployementVerificationDetails(string candidateemail, string mobilenumber)
        //{
        //    SignzyEmployeeRequest mobiledata = new SignzyEmployeeRequest();
        //    mobiledata.mobile = mobilenumber;
        //    mobiledata.CandidateEmail = candidateemail;
        //    mobiledata.panNumber = "";
        //    Dictionary<string, string> headers = new Dictionary<string, string>
        //        {
        //            { "Content-Type", "application/json" },
        //            { "Authorization", employementapikey }
        //        };
        //    var response = await _httpRequestHelper.MakeHttpRequest(employementendpoint, Method.Post, mobiledata, headers);
        //    var convertedResponse = JsonConvert.DeserializeObject<SignzyEmployeeResponse>(response.Content);
        //    convertedResponse.MobileNumber = mobiledata.mobile;
        //    convertedResponse.CandidateEmail = mobiledata.CandidateEmail;
        //    if (convertedResponse.error != null)
        //    {
        //        if (string.IsNullOrWhiteSpace(convertedResponse.message))
        //        {
        //            convertedResponse.message = convertedResponse.error.message;
        //        }
        //    }
        //    dynamic datavalue = JObject.Parse(response.Content);
        //    List<string>? namesList = convertedResponse.uanNumbers?.ToList();
        //    foreach (var data in datavalue.Children())
        //    {
        //        if (((JToken)data).Path == "uanDetails")
        //        {
        //            foreach (var dataset in data.Children())
        //            {
        //                foreach (var item in namesList)
        //                {
        //                    if (((JContainer)dataset).First.Path == "uanDetails." + item)
        //                    {
        //                        var datanew = ((JContainer)dataset).First;
        //                        var internalNodes = datanew.Children().GetEnumerator();
        //                        while (internalNodes.MoveNext())
        //                        {
        //                            UanDetails _uandeails = new UanDetails();
        //                            var mainFamilies = internalNodes.Current;
        //                            var nodedetails = mainFamilies.Children().GetEnumerator();
        //                            while (nodedetails.MoveNext())
        //                            {
        //                                _uandeails.uanNumber = item;
        //                                var mainFamilies1 = nodedetails.Current;
        //                                if (((JProperty)mainFamilies1).Name == "basic_details")
        //                                {
        //                                    _uandeails.date_of_birth = ((JProperty)mainFamilies1).Value["date_of_birth"].ToString();
        //                                    _uandeails.gender = ((JProperty)mainFamilies1).Value["gender"].ToString();
        //                                    _uandeails.name = ((JProperty)mainFamilies1).Value["name"].ToString();
        //                                }
        //                                else if (((JProperty)mainFamilies1).Name == "employment_details")
        //                                {
        //                                    _uandeails.establishment_name = ((JProperty)mainFamilies1).Value["establishment_name"].ToString();
        //                                    _uandeails.date_of_joining = ((JProperty)mainFamilies1).Value["date_of_joining"].ToString();
        //                                }
        //                            }
        //                            convertedResponse.uanDetails1.Add(_uandeails);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    //Save response data
        //    if (!string.IsNullOrEmpty(convertedResponse.employmentSummary?.recent_employer_data.matching_uan) || convertedResponse.message == "No record(s found")
        //    {
        //        await _aadhaarService.SaveEmploymentDetails(convertedResponse);
        //        var _interviewer = new InterviewerProfileVM();
        //        _interviewer.EmailId = convertedResponse.CandidateEmail;
        //        _interviewer.isVerified = true;
        //        if (convertedResponse.employmentSummary != null)
        //        {
        //            _interviewer.IsEmployed = convertedResponse.employmentSummary.is_employed;
        //            _interviewer.CompanyName = convertedResponse.employmentSummary.recent_employer_data.establishment_name;
        //        }
        //        else
        //        {
        //            _interviewer.IsEmployed = false;
        //            _interviewer.CompanyName = string.Empty;
        //        }
        //        _interviewer.EmploymentComment = convertedResponse.message;
        //        await _interviewerService.SaveInterviewerEmploymentVerification(_interviewer);
        //    }
        //    return convertedResponse;
        //}
        #endregion
        public async Task EVInterviewer()
        {
            var model = _interviewerService.GetInterviewers();
            int i = 0;
            //Start: Employment Verification
            //foreach (var item in model)
            //{
            //    try
            //    {
            //        if (!string.IsNullOrWhiteSpace(item.MobileNumber))
            //        {
            //            var mobilenumber = item.MobileNumber.Replace("+91", "").Replace("-", "");
            //            if (mobilenumber.Length > 10)
            //            {
            //                mobilenumber = mobilenumber.Substring(mobilenumber.Length - 10);
            //            }
            //            var result = await VerifyEmployment(item.EmailId, mobilenumber);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //    i++;
            //}
            //End: Employment Verification

            //Start : Employement Verification Mapping into InterivewerProfile
            //var employmentdetails = await _aadhaarService.GetEmployementDetails();
            //InterviewerProfileVM _interviewer;
            //foreach (var item in model)
            //{
            //    try
            //    {
            //        if (!string.IsNullOrWhiteSpace(item.MobileNumber))
            //        {
            //            var mobilenumber = item.MobileNumber.Replace("+91", "").Replace("-", "");
            //            if (mobilenumber.Length > 10)
            //            {
            //                mobilenumber = mobilenumber.Substring(mobilenumber.Length - 10);
            //            }
            //            var employmentverification = employmentdetails.Where(x => x.MobileNumber == mobilenumber).FirstOrDefault();
            //            _interviewer = new InterviewerProfileVM();
            //            _interviewer.EmailId = item.EmailId;
            //            if (employmentverification != null)
            //            {
            //                _interviewer.isVerified = true;
            //                _interviewer.IsEmployed = employmentverification.Isemployed;
            //                _interviewer.CompanyName = employmentverification.CompanyName;
            //                _interviewer.EmploymentComment = employmentverification.message;
            //            }
            //            else
            //            {
            //                _interviewer.isVerified = false;
            //            }
            //            var result = _interviewerService.SaveInterviewerEmploymentVerification(_interviewer);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //    i++;
            //}
            //End : Employement Verification Mapping into InterivewerProfile
        }
        [HttpPost]
        public async Task<JsonResult> GetInterviwerExists(string interviewer)
        {
            // var result = await _interviewerService.GetInterviwerExistsService(interviewer);
            var role = _interviewerService.GetUserRolebyEmailid(interviewer);

            if (!string.IsNullOrWhiteSpace(role))
            {
                return Json("This mail id already exists in our system as " + role + "  Please check add accordingly.");
            }
            else
            {
                return Json(null);
            }
        }
        public async Task<SignzyEmployeeResponse> EmployementVerificationDetails(string candidateemail, string mobilenumber)
        {
            SignzyEmployeeRequest mobiledata = new SignzyEmployeeRequest();
            mobiledata.mobileNumber = mobilenumber;
            mobiledata.CandidateEmail = candidateemail;
            mobiledata.panNumber = "";
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
                convertedResponse.message = resultset.error?.message;
                if (string.IsNullOrWhiteSpace(convertedResponse.message))
                {
                    convertedResponse.message = resultset.message;
                }
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
            //Save response data
            //if (!string.IsNullOrEmpty(convertedResponse.summary?.recentEmployerData.matchingUan) || convertedResponse.message == "No record(s found")
            if (convertedResponse?.uan?.Count() > 0 || convertedResponse.message == "No record(s found")
            {
                await _aadhaarService.SaveEmploymentDetails(convertedResponse);
                var _interviewer = new InterviewerProfileVM();
                _interviewer.EmailId = convertedResponse.CandidateEmail;
                _interviewer.isVerified = true;
                if (convertedResponse.summary != null)
                {
                    _interviewer.IsEmployed = !convertedResponse.summary.dateOfExitMarked;
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
            return convertedResponse;
        }

        private async Task<SignzyEmployeeResponse> TdsVerificationDetails(string candidateemail, string mobilenumber)
        {
            try
            {
                SignzyEmployeeRequest mobiledata = new SignzyEmployeeRequest();
                mobiledata.mobileNumber = mobilenumber;
                mobiledata.CandidateEmail = candidateemail;
                mobiledata.panNumber = "";
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
                    convertedResponse.message = resultset.error?.message;
                    if (string.IsNullOrWhiteSpace(convertedResponse.message))
                    {
                        convertedResponse.message = resultset.message;
                    }
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
                //Save response data
                if (convertedResponse?.uan?.Count() > 0 || convertedResponse.message == "No record(s found")
                {
                    await _aadhaarService.SaveEmploymentDetails(convertedResponse);
                    var _interviewer = new InterviewerProfileVM();
                    _interviewer.EmailId = convertedResponse.CandidateEmail;
                    _interviewer.isVerified = true;
                    if (convertedResponse.summary != null)
                    {
                        _interviewer.IsEmployed = !convertedResponse.summary.dateOfExitMarked;
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
                return convertedResponse;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveVerificationData(int interviewId, string capturedPhotoData, string aadhaarPhotoData, string Name, string Address, string gender, string dob, string email)
        {
            try
            {
                capturedPhotoData = capturedPhotoData.Replace("data:image/jpeg;base64,", "");
                var matchscore = await Verification_FaceMatch_veri5(capturedPhotoData, aadhaarPhotoData, true);
                var aadhaarKYCmodel = new AadhaarData();
                aadhaarKYCmodel.Name = Name;
                aadhaarKYCmodel.Address = Address;
                aadhaarKYCmodel.gender = gender;
                aadhaarKYCmodel.dob = dob;
                aadhaarKYCmodel.doc_face = aadhaarPhotoData;
                aadhaarKYCmodel.email = email;
                aadhaarKYCmodel.reference_id = interviewId.ToString();
                aadhaarKYCmodel.verified_using = "Interview";
                aadhaarKYCmodel.passport_image = capturedPhotoData;
                aadhaarKYCmodel.matchPercentage = matchscore.ToString();
                await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
                return Json(new { success = true, message = "Verification data saved successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveVerificationData: {ex.Message}");
                return Json(new { success = false, message = "Error saving verification data: " + ex.Message });
            }
        }
    }
}
