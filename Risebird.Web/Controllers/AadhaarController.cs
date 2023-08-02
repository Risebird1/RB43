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
using static OfficeOpenXml.ExcelErrorValue;

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

        public AadhaarController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IAadhaarService aadhaarService,
            IFileMimeTypeHelper fileMimeTypeHelper, IHttpRequestHelper httpRequestHelper, HttpClientHelper factory, IAadhaarRepository aadhaarRepository)
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
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> GenerateAadhaarCaptcha(string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.api_key = api_key;
            model.request.request_id = Guid.NewGuid().ToString();
            var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + api_key + "|" + salt;
            model.request.hash = getHashSha256(hashstring);
            //var client = new RestClient(apiendpoint + "getCaptcha");
            //var request = new RestRequest(apiendpoint + "getCaptcha", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "getCaptcha", Method.Post, model, headers);


            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {

            }
            return Json(new { image = convertedResponse.response_data.captcha, uuid = convertedResponse.response_data.uuid });
        }
        [HttpPost]
        public async Task<JsonResult> SendAadhaarNumber(string uuid, string aadharnumber, string aadharcaptcha, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.aadhaar = aadharnumber;
            model.request.captcha = aadharcaptcha;
            model.request.verification_type = "OTP";
            model.request.consent = "YES";
            //var client = new RestClient(apiendpoint + "enterAadhaar");
            //var request = new RestRequest(apiendpoint + "enterAadhaar", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "enterAadhaar", Method.Post, model, headers);


            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {
                return Json(convertedResponse.response_status.message);
            }
            return Json(convertedResponse.response_data.message);
        }
        [HttpPost]
        public async Task<JsonResult> SubmitOTPNumber(string uuid, string otp, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.otp = otp;
            model.request.share_code = "1234";
            //var client = new RestClient(apiendpoint + "enterOtp");
            //var request = new RestRequest(apiendpoint + "enterOtp", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "enterOtp", Method.Post, model, headers);


            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            return Json("");
        }
        [HttpPost]
        public async Task<JsonResult> FetchAadhaarKYC(string uuid, string datenow)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            model.request.api_key = api_key;
            var hashstring = model.headers.client_code + "|" + model.request.uuid + "|" + api_key + "|" + salt;
            model.request.hash = getHashSha256(hashstring);
            //var client = new RestClient(apiendpoint + "fetchKYCData");
            //var request = new RestRequest(apiendpoint + "fetchKYCData", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "fetchKYCData", Method.Post, model, headers);


            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse.response_status.status.ToLower() == "fail")
            {

            }
            return Json(convertedResponse.response_data);
        }

        public void SetHeaderValue(AadhaarHeader _header, string datenow)
        {
            _header.client_code = client_code;
            _header.sub_client_code = client_code;
            _header.channel_code = "ANDROID_SDK";
            _header.channel_version = "1";
            _header.stan = salt;
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
        [HttpPost]
        public async Task<JsonResult> FetchAadhaarDetails(string MCQID, string candidateEmail)
        {
            var model = await _aadhaarService.FetchAadhaarDetails(MCQID, candidateEmail);
            if (model == null)
            {
                model = await _aadhaarService.FetchInterviewCandidateDetails(candidateEmail);
                if (model == null)
                {
                    model = new AadhaarData();
                }
                else
                {
                    model.type = 1;
                }
            }
            return Json(model);
        }
        public async Task<IActionResult> AadhaarVerification(string uniqueid)
        {
            var candidateEmailid = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            var model = new AadhaarData();
            if (candidateEmailid == null)
            {

                model.status = "InvalidEmail";
            }

            else if (candidateEmailid.AadhaarVerified == true)
            {
                model.status = "Unauthorized";
            }
            else
            {
                model.email = candidateEmailid.EmailId;
            }

            return View(model);
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
        [HttpPost]
        public async Task<JsonResult> FetchInterviewAadhaarDetails(string candidateEmail)
        {
            var model = await _aadhaarService.FetchInterviewAadhaarDetails(candidateEmail);
            if (model == null)
            {
                model = await _aadhaarService.FetchInterviewCandidateDetails(candidateEmail);
                if (model == null)
                {
                    model = new AadhaarData();
                }
                else
                {
                    model.type = 1;
                }
            }
            return Json(model);
        }
        [HttpPost]
        public async Task<JsonResult> ReGenerateAadhaarCaptcha(string datenow, string uuid)
        {
            var model = new AadhaarVM();
            SetHeaderValue(model.headers, datenow);
            model.request.uuid = uuid;
            //var client = new RestClient(apiendpoint + "getNewCaptcha");
            //var request = new RestRequest(apiendpoint + "getNewCaptcha", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);

            Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "getNewCaptcha", Method.Post, model, headers);


            var convertedResponse = JsonConvert.DeserializeObject<AadhaarResponse>(response.Content);
            if (convertedResponse != null)
            {
                if (convertedResponse.response_status.status.ToLower() == "fail")
                {

                }
                return Json(new { image = convertedResponse.response_data.captcha, uuid = model.request.uuid });
            }
            return Json(new { image = "", uuid = model.request.uuid });
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
        public async Task<int> Verification_FaceMatch_veri5(string imagefirst, string secondinamge)
        {
            var model = new AadhaarVM();
            var datenow = GetCurrentUnixTimestampMillis();
            SetHeaderValue(model.headers, datenow);
            model.request.api_key = api_key;
            model.request.request_id = Guid.NewGuid().ToString();
            model.request.purpose = "VERIFY";
            model.request.image_1 = ConvertImageURLToBase64(imagefirst);
            model.request.image_2 = ConvertImageURLToBase64(secondinamge);
            var hashstring = model.headers.client_code + "|" + model.request.request_id + "|" + api_key + "|" + salt;
            model.request.hash = getHashSha256(hashstring);
            //var client = new RestClient(apiendpoint + "getCaptcha");
            //var request = new RestRequest(apiendpoint + "getCaptcha", Method.Post);
            //request.AddHeader("content-type", "application/json");
            //request.AddJsonBody(model);
            //RestResponse response = await client.ExecuteAsync(request);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                //{ "Authorization", "Basic " + accesskey },
                { "Content-Type", "application/json" }
            };

            var response = await _httpRequestHelper.MakeHttpRequest(apiendpoint + "faceCompare", Method.Post, model, headers);

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
            mobiledata.mobile = mobile;
            mobiledata.CandidateEmail = candidateEmail;
            var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/SignzyEmploymentVerification";
            var functionKey = "sjFBxP74_xSYOUDkT1IMl8roje5mvSeKDhux42HSFmunAzFuQ8_P3g==";
            await _httpClient.SendRequestAsync(HttpMethod.Post, path, mobiledata, functionKey, "", _cancellationToken);
        }
        #endregion
    }
}
