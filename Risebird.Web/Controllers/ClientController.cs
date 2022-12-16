using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    public class ClientController : Controller
    {
        private IInterviewerService _interviewerService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IUserRepository _userRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IMeetingService _meetingService;
        private readonly IUserService _userService;
        private static string Searchvalueproiprity;
        private static List<MeetingVM> _modelDetails;
        private readonly IIRFService _irfService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;

        public ClientController(IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IInterviewerService interviewerService, IUserRepository userRepository, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IMeetingService meetingService, IIRFService irfService, IUserService userService, IFileMimeTypeHelper fileMimeTypeHelper)
        {
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _interviewerService = interviewerService;
            _userRepository = userRepository;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _meetingService = meetingService;
            _irfService = irfService;
            _userService = userService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                        TempData.Remove("SuccessErrorMessage");
                    }
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var model = _clientService.GetClients(emailid, profileName);
                    ViewBag.LoggedEmail = emailid.ToLower();
                    ViewBag.profile = profileName;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddClient()
        {
            try
            {
                var model = new Client();
                model.IsMeetingNotificationSend = true;
                model.IsCandidateavailability = true;
                SetBillingTypeDropDown(model);
                return View("ClientAdd", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Adding Client is Failed";
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddClient(Client client)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        string uploadedFileURL = string.Empty;
                        string uploadedVideoURL = string.Empty;
                        if (client.LogoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(client.LogoFile);
                            if (fileMimeType == "image/ief" || fileMimeType == "image/jpeg" || fileMimeType == "image/tiff")
                            {
                                var fileName = client.LogoFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(fileName, "clientlogos", client.LogoFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientlogos");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                uploadedFileURL = blob.Uri.ToString();
                                client.LogoLink = uploadedFileURL;
                            }
                        }
                        if (client.VideoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(client.VideoFile);
                            if (fileMimeType == "video/mp4" || fileMimeType == "video/mpeg" || fileMimeType == "video/quicktime")
                            {
                                var fileName = client.VideoFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(fileName, "clientvaluevideosgifs", client.VideoFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientvaluevideosgifs");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                uploadedVideoURL = blob.Uri.ToString();
                                client.CompanyVideoLink = uploadedVideoURL;
                            }
                        }
                        if (await _clientService.AddClient(client))
                        {
                            TempData["SuccessErrorMessage"] = "Client has been added Successfully!";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Client.";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
                        SetBillingTypeDropDown(client);
                        return View("ClientAdd");
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
                TempData["SuccessErrorMessage"] = "Adding Client is Failed";
                return RedirectToAction("List");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditClient(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    Client clientToUpdate = await _clientService.GetClient(id);
                    SetBillingTypeDropDown(clientToUpdate);
                    if (clientToUpdate != null)
                        return View("ClientUpdate", clientToUpdate);
                    else
                        return RedirectToAction("List");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditClient(Client client)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        string uploadedFileURL = string.Empty;
                        string uploadedVideoURL = string.Empty;
                        if (client.LogoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(client.LogoFile);
                            if (fileMimeType == "image/ief" || fileMimeType == "image/jpeg" || fileMimeType == "image/tiff")
                            {
                                var fileName = client.LogoFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(fileName, "clientlogos", client.LogoFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientlogos");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                uploadedFileURL = blob.Uri.ToString();
                                client.LogoLink = uploadedFileURL;
                            }
                        }
                        if (client.VideoFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(client.VideoFile);
                            if (fileMimeType == "video/mp4" || fileMimeType == "video/mpeg" || fileMimeType == "video/quicktime")
                            {
                                var fileName = client.VideoFile.FileName.Replace(" ", "_");
                                await _azureStorageHelper.InsertBlob(fileName, "clientvaluevideosgifs", client.VideoFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientvaluevideosgifs");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                uploadedVideoURL = blob.Uri.ToString();
                                client.CompanyVideoLink = uploadedVideoURL;
                            }
                        }
                        if (await _clientService.UpdateClient(client))
                        {
                            TempData["SuccessErrorMessage"] = "Client has been updated Successfully!";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to update Client.";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
                        SetBillingTypeDropDown(client);
                        return View("ClientUpdate");
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
                TempData["SuccessErrorMessage"] = "Update of Client is Failed";
                return RedirectToAction("List");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteClient(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (await _clientService.DeleteClient(id))
                    {
                        TempData["SuccessErrorMessage"] = "Client has been deleted Successfully!";
                        return Json(new { url = Url.Action("List", "Client") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Failed to delete Client.";
                        return Json(new { url = Url.Action("List", "Client") });
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
                TempData["SuccessErrorMessage"] = "Deleting Client is Failed";
                return RedirectToAction("List");
            }
        }

        /// <summary>
        /// Fetch All Jd Records according to Role
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CreateJd()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.ClientId;
                        isClientUser = true;
                    }
                    //Fetch ClientJds
                    var result = _clientService.FetchClientJd(ClientId);
                    var resultset = _interviewerService.FetchAssessmentGroups();
                    //bind Dropdown values
                    var model = new ClientDetails();
                    model._clientjd = result.ToList();
                    model._clientjddetails = new ClientJdVM();
                    model._clientjddetails.AssessmentGrouplist = new SelectList(resultset, "id", "AssessmentGroup");
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    model._clientjddetails.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                    model._clientjddetails.ClientId = ClientId;
                    model._clientjddetails.isClientUser = isClientUser;
                    model._clientjddetails.iscreated = true;
                    return View(model);
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

        [HttpPost]
        public async Task<IActionResult> CreateJd(ClientDetails fillters)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.ClientId;
                        isClientUser = true;
                    }
                    //Fetch ClientJds
                    var result = _clientService.FetchClientJd(ClientId);
                    var resultset = _interviewerService.FetchAssessmentGroups();
                    fillters.fillter.AssessmentGroupName = fillters.fillter.AssessmentGroupName == null ? fillters.fillter.AssessmentGroupName = result.Select(x => x.AssessmentGroupName).ToArray() : fillters.fillter.AssessmentGroupName = fillters.fillter.AssessmentGroupName;
                    fillters.fillter.ClientEmail = fillters.fillter.ClientEmail == null ? fillters.fillter.ClientEmail = result.Select(x => x.ClientEmail).ToArray() : fillters.fillter.ClientEmail = fillters.fillter.ClientEmail;
                    result = result.Where(c =>
                           (fillters.fillter.AssessmentGroupName.Count() > 0 ? fillters.fillter.AssessmentGroupName.Contains(c.AssessmentGroupName) : c.AssessmentGroupName == string.Empty)
                          && (fillters.fillter.ClientEmail.Count() > 0 ? fillters.fillter.ClientEmail.Contains(c.ClientEmail) : c.ClientEmail == string.Empty)).ToList();
                    //bind Dropdown values
                    var model = new ClientDetails();
                    model._clientjd = result.ToList();
                    model._clientjddetails = new ClientJdVM();
                    model._clientjddetails.AssessmentGrouplist = new SelectList(resultset, "id", "AssessmentGroup");
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    model._clientjddetails.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                    model._clientjddetails.ClientId = ClientId;
                    model._clientjddetails.isClientUser = isClientUser;
                    model._clientjddetails.iscreated = true;
                    return View(model);
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

        [HttpGet]
        public async Task<IActionResult> ResetCreateJdFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    return RedirectToAction("CreateJd");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        /// <summary>
        /// Craete New Jd
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddCreateJd(ClientDetails model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    model._clientjddetails.CreatedBy = emailid;
                    if (profileName != "Client")
                    {
                        if (!string.IsNullOrEmpty(model._clientjddetails.ClientEmail))
                        {
                            var client = await _userRepository.GetClientUserDetails(model._clientjddetails.ClientEmail);
                            if (client != null)
                            {
                                model._clientjddetails.ClientId = client.ClientId;
                            }
                            else
                            {
                                model._clientjddetails.ClientId = 0;
                            }
                            model._clientjddetails.CreatedBy = model._clientjddetails.ClientEmail;
                        }
                    }
                    if (model._clientjddetails.JdFile != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model._clientjddetails.JdFile);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(model._clientjddetails.JdFile.FileName, "videos", model._clientjddetails.JdFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                            var blob = containerRef.GetBlockBlobReference(model._clientjddetails.JdFile.FileName);
                            string uploadedvideoFileURL = blob.Uri.ToString();
                            model._clientjddetails.Filelink = uploadedvideoFileURL;
                        }
                    }
                    var result = await _clientService.AddClientJd(model._clientjddetails);
                    if (result != "")
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Client Jd has been created Successfully!";
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Failed to create Client Jd.";
                    }
                    if (result == "Success")
                    {
                        return RedirectToAction("CreateJd");
                    }
                    else
                    {
                        return View("CreateJd", model);
                    }
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

        /// <summary>
        /// Add New Candidate For JD
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public IActionResult CandidateJDScore(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                CandidateCuratedScoreVM model = new CandidateCuratedScoreVM();
                var result = _clientService.GetCandidateJDScore(Convert.ToInt32(id));
                model.CandidateCuratedScores = result;
                model.IRFId = Convert.ToInt32(id);
                model.candidateProfile = "AddCandidate";
                //model.JDTitle = result.FirstOrDefault().JDTitle;
                if (TempData["SuccessErrorMessageInterviewer"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                    TempData.Remove("SuccessErrorMessageInterviewer");
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CandidateJDScore(CandidateCuratedScoreVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var newcandidatecount = model.CandidateCuratedScores.Where(x => x.IRFId == 0).Count();
                var result = _clientService.AddCandidateJDScore(model.IRFId, model.CandidateCuratedScores.ToList(), model.candidateProfile);
                if (result != "")
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate has been updated Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Failed to update Candidate.";
                }
                if (model.candidateProfile == "AddCandidate")
                {
                    if (result != "")
                    {
                        //Send an email
                        Mail _mail = new Mail
                        {
                            TO = result,
                            Subject = "New candidate(s) added into JD " + model.JDTitle
                        };
                        var resultset = await _notificationHelper.CandidateJDScoreNotification(_mail, newcandidatecount, model.JDTitle);
                    }
                    return RedirectToAction("CandidateJDScore", "Client", new { id = model.IRFId });
                }
                else
                {
                    return RedirectToAction("CandidateJDScorePriority", "Client", new { id = model.IRFId });
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// Edit existing JD
        /// </summary>
        /// <param name="assessmentid"></param>
        /// <returns></returns>
        /// 
        [HttpGet]
        public async Task<JsonResult> EditJd(int assessmentid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.ClientId;
                        isClientUser = true;
                    }
                    //Fetch ClientJds
                    var result = _clientService.FetchClientJd(ClientId);
                    var resultset = _interviewerService.FetchAssessmentGroups();
                    //bind Dropdown values
                    //var model = new ClientDetails();
                    //model._clientjd = result.ToList();
                    //model._clientjddetails = new ClientJdVM();
                    //model._clientjddetails.AssessmentGrouplist = new SelectList(resultset, "id", "AssessmentGroup");
                    //var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                    //               select new { ID = (int)s, Name = s.GetDescription() };
                    //model._clientjddetails.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                    //model._clientjddetails.ClientId = ClientId;
                    //model._clientjddetails.isClientUser = isClientUser;
                    //model._clientjddetails.iscreated = false;
                    var resultdetails = result.Where(x => x.id == assessmentid).FirstOrDefault();
                    resultdetails.StartDatestring = resultdetails.StartDate.ToString("yyyy-MM-dd");
                    resultdetails.EndDatestring = resultdetails.EndDate.ToString("yyyy-MM-dd");
                    resultdetails.isClientUser = isClientUser;
                    return Json(resultdetails);
                }
                else
                {
                    return Json("fail");
                }
            }
            else
            {
                return Json("fail");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CandidateRelatedJD(int interviewid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Client" || profileName == "Candidate")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.ClientId;
                        isClientUser = true;
                    }
                    //Fetch ClientJds
                    var result = _clientService.FetchClientJd(ClientId);
                    var resultset = _interviewerService.FetchAssessmentGroups();
                    //bind Dropdown values
                    var model = new ClientDetails();
                    model._clientjd = result.ToList();
                    model._clientjddetails = new ClientJdVM();
                    model._clientjddetails.AssessmentGrouplist = new SelectList(resultset, "id", "AssessmentGroup");
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    model._clientjddetails.Heuristicsscorelist = new SelectList(Heuristics, "ID", "Name");
                    model._clientjddetails.ClientId = ClientId;
                    model._clientjddetails.isClientUser = isClientUser;
                    return View("CreateJd", model);
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
        /// <summary>
        /// Add Priority for candidates
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CandidateJDScorePriority(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                CandidateCuratedScoreVM model = new CandidateCuratedScoreVM();
                var result = _clientService.GetCandidateJDScore(Convert.ToInt32(id));
                model.CandidateCuratedScores = result;
                model.IRFId = Convert.ToInt32(id);
                model.candidateProfile = "SetPriority";
                if (TempData["SuccessErrorMessageInterviewer"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterviewer"].ToString();
                    TempData.Remove("SuccessErrorMessageInterviewer");
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// Client Wallet Details
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> RisebirdPay()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                    isClientUser = true;
                    return View();
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

        [HttpPost]
        public async Task<JsonResult> WalletDetails(string action)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                    isClientUser = true;
                    var model = _clientService.FetchClientWallet(ClientId);
                    if (action == "CREDIT")
                    {
                        model = model.Where(x => x.Credit > 0).ToList();
                    }
                    else if (action == "DEBIT")
                    {
                        model = model.Where(x => x.Debit > 0).ToList();
                    }
                    foreach (var item in model)
                    {
                        DateTime _local = (DateTime)item.CreatedOn;
                        item.CreatedOnstring = _local.ToShortDateString();
                    }
                    return Json(model);
                }
                else
                {
                    return Json("SignOut");
                }
            }
            else
            {
                return Json("SignOut");
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddWalletPoints(int points, string comment)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                bool isClientUser = false;
                int ClientId = 0;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                    isClientUser = true;
                    var result = await _clientService.AddWalletPoints(ClientId, emailid, points, comment);
                    if (result == "Success")
                    {
                        return Json("Success");
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
                else
                {
                    return Json("SignOut");
                }
            }
            else
            {
                return Json("SignOut");
            }
        }

        [HttpGet]
        public IActionResult ResetCandidate(int Clientjdid, string candidateEmail)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = _clientService.ResetCandidate(Clientjdid, candidateEmail, emailid);
                if (result != "")
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate has been reset Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Failed to reset Candidate.";
                }
                return RedirectToAction("CandidateJDScore", "Client", new { id = Clientjdid });
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public JsonResult DeleteJd(int assessmentid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var result = _clientService.DeleteClientJd(assessmentid);
                    return Json(result);
                }
                else
                {
                    return Json("fail");
                }
            }
            else
            {
                return Json("fail");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AllMeetings(int? Page, string searchValue, string Emailid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Emailid = "";
                    ViewBag.emailid = emailid;
                    if (profileName == "Client")
                    {
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ViewBag.IsClientCancelInterview = client?.IsClientCancelInterview;
                        var role = await _clientService.GetClientRoleByEmailId(emailid);
                        ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                        var meetings = await _meetingService.GetClientMeetingList(emailid);
                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.IsClientQCRequired = client.IsQCRequired; x.IsClientRedoInterview = client.IsRedoInterview; return x; }).ToList();
                        if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") == null)
                        {
                            meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                        }

                        //ViewBag.IsSelected = false;

                        SetMeetingsProperties(meetings);

                        if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("AllPriorityClientMeetingsfilters"));
                            //Apply filter and get the filtered meetings back
                            meetings = ApplyAllMeetingsFilter(filter, meetings);
                        }

                        ViewBag.ClientHRList = meetings.Select(x => x.ClientHrContact).Distinct();
                        ViewBag.ClientNameList = meetings.Select(x => x.ClientName).Distinct();
                        ViewBag.IRFList = meetings.Select(x => x.IRFId).Distinct();
                        ViewBag.PrimarySkills = meetings.Select(x => x.JDTiltle).Distinct();
                        ViewBag.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).Distinct();
                        ViewBag.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).Distinct();
                        ViewBag.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).Distinct();
                        ViewBag.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).Distinct();
                        ViewBag.FinalSelection = meetings.Select(x => x.FinalSelectionStat).Distinct();
                        ViewBag.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).Distinct();
                        ViewBag.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).Distinct();

                        ViewBag.Searchvalue = searchValue;
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == " ") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == " ") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == " ") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                         || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == " ") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == " ")
                         || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == " ")).ToList();

                        }
                        ViewBag.TotalCount = meetings.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = meetings.Count / 10;
                        if (meetings.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page || 10 >= meetings.Count)
                            {
                                ViewBag.PageEndCount = meetings.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

                            }
                        }
                        if (meetings.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }

                        ViewBag.Date = DateTime.Now;
                        ViewBag.PageType = "Private";
                        var allmeetingdata = JsonConvert.SerializeObject(meetings);
                        HttpContext.Session.SetString("SearchAllMeetings", allmeetingdata);
                        return View(meetings.ToPagedList(Page ?? 1, 10));
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else if (!string.IsNullOrEmpty(Emailid))
                {
                    var emailid = Emailid;
                    ViewBag.Emailid = Emailid;
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ViewBag.IsClientCancelInterview = client?.IsClientCancelInterview;
                    var role = await _clientService.GetClientRoleByEmailId(emailid);
                    ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                    var meetings = await _meetingService.GetClientMeetingList(emailid);
                    meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                    if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") == null)
                    {
                        meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                    }
                    SetMeetingsProperties(meetings);
                    if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("AllPriorityClientMeetingsfilters"));
                        meetings = ApplyAllMeetingsFilter(filter, meetings);
                    }
                    ViewBag.ClientHRList = meetings.Select(x => x.ClientHrContact).Distinct();
                    ViewBag.ClientNameList = meetings.Select(x => x.ClientName).Distinct();
                    ViewBag.IRFList = meetings.Select(x => x.IRFId).Distinct();
                    ViewBag.PrimarySkills = meetings.Select(x => x.JDTiltle).Distinct();
                    ViewBag.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).Distinct();
                    ViewBag.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).Distinct();
                    ViewBag.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).Distinct();
                    ViewBag.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).Distinct();
                    ViewBag.FinalSelection = meetings.Select(x => x.FinalSelectionStat).Distinct();
                    ViewBag.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).Distinct();
                    ViewBag.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).Distinct();

                    ViewBag.Searchvalue = searchValue;
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == " ") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == " ") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == " ") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                     || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == " ") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == " ")
                     || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == " ")).ToList();
                    }
                    ViewBag.TotalCount = meetings.Count;
                    ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = meetings.Count / 10;
                    if (meetings.Count % 10 == 0)
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        if (a == Page || 10 >= meetings.Count)
                        {
                            ViewBag.PageEndCount = meetings.Count;
                        }
                        else
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                    }
                    if (meetings.Count == 0)
                    {
                        ViewBag.PageStartCount = 0;
                        ViewBag.PageEndCount = 0;
                    }
                    // _modelDetails = meetings;
                    ViewBag.Date = DateTime.Now;
                    ViewBag.PageType = "Private";
                    var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }
                    var allmeetingdata = JsonConvert.SerializeObject(meetings);
                    HttpContext.Session.SetString("SearchAllMeetings", allmeetingdata);
                    return View(meetings.ToPagedList(Page ?? 1, 10));
                    //return RedirectToAction("SignOut", "Session");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AllMeetings(InterviewFilter filter, int? Page, string searchValue, string Emailid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Emailid = "";
                    ViewBag.emailid = emailid;
                    if (profileName == "Client")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("AllPriorityClientMeetingsfilters", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("AllMeetings", new { Page = Page, searchValue = searchValue, Emailid = Emailid });
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else if (!string.IsNullOrEmpty(Emailid))
                {
                    return RedirectToAction("AllMeetings", new { Page = Page, searchValue = searchValue, Emailid = Emailid });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }
        }

        [HttpGet]
        private void SetMeetingsProperties(List<MeetingVM> meetings)
        {
            foreach (var m in meetings)
            {
                //if (m.InterviewStatus.HasValue)
                //{
                //    m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                //}
                if (m.L2InterviewStatus.HasValue)
                {
                    m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                }
                if (m.L2InterviewStatus.HasValue)
                {
                    m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                }
                if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                {
                    m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                }
                if (m.PreparationStatus.HasValue)
                {
                    m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                }
                if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                {
                    m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                }
                if (m.MeetingStatus.HasValue)
                {
                    m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                }
                if (m.IsApproved.HasValue && m.IsApproved != -1)
                {
                    m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                }
                if (m.IsSelected.HasValue && m.IsSelected != -1)
                {
                    m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                }
                if (m.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                {
                    if (m.interviewDate < DateTime.Now && m.MeetingStatusStatusDesc == MeetingStatus.Scheduled.GetDescription())
                    {
                        m.MeetingStatusStatusDesc = "Status Pending";

                    }
                    else if (m.MeetingStatus.HasValue && (m.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected || m.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || m.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine))
                    {
                        m.MeetingStatusStatusDesc = MeetingStatus.Completed.GetDescription();//"Completed"
                    }
                }
                else
                {
                    m.MeetingStatusStatusDesc = string.Empty;
                }
                if (m.InterviewStatus.HasValue)
                {
                    m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                }
                if (m.intervieweStatusStatDesc == null)
                {
                    m.intervieweStatusStatDesc = string.Empty;
                }
            }
            return;
        }

        [HttpGet]
        private List<MeetingVM> ApplyAllMeetingsFilter(InterviewFilter filter, List<MeetingVM> meetings)
        {
            if (filter.StartDate != null && filter.EndDate != null)
            {
                meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
            }

            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
            filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
            //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;


            meetings = meetings.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
            && filter.RFID.Contains(c.IRFId)
            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
            && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
            && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
            && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
            && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
            && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
            && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)).ToList();

            return meetings;
        }

        [HttpPost]
        public JsonResult AllPriorityClientMeetingsJson(string searchValue, string Emailid)
        {
            try
            {
                int Page = 1;

                //var emailid = HttpContext.Session.GetString("email")?.Trim();
                //emailid = emailid.Replace("\"", "");
                //ViewBag.Emailid = "";
                //var meetings = _meetingService.GetClientMeetingList(emailid);
                //meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                //if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") == null)
                //{
                //    meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                //}

                //SetMeetingsProperties(meetings);

                //if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") != null)
                //{
                //    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("AllPriorityClientMeetingsfilters"));
                //    if (filter.StartDate != null && filter.EndDate != null)
                //    {
                //        meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                //    }

                //    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                //    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                //    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                //    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                //    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                //    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                //    filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                //    filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                //    filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                //    filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                //    //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                //    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;


                //    meetings = meetings.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                //    && filter.RFID.Contains(c.IRFId)
                //    && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                //    && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                //    && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                //    && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                //    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                //    && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                //    && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                //    && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                //    && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)).ToList();


                //}
                var meetings = JsonConvert.DeserializeObject<List<MeetingVM>>(HttpContext.Session.GetString("SearchAllMeetings"));
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == " ") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == " ") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == " ") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == " ")
                     || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == " ") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == " ")
                     || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == " ")).ToList();
                }
                int totalcount = meetings.Count;
                int Pagestart = 0;
                int Pageend = 0;
                ViewBag.TotalCount = meetings.Count;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = meetings.Count / 10;
                if (meetings.Count % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    if (a == Page)
                    {
                        Pageend = meetings.Count;
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                }
                if (meetings.Count == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }
                return Json(new { result = meetings.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = meetings.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetAllPriorityClientMeetingsFilter(string Emailid)
        {
            try
            {
                HttpContext.Session.Remove("AllPriorityClientMeetingsfilters");
                return RedirectToAction("AllMeetings", "Client", new { @Page = 1, @searchValue = string.Empty, Emailid = Emailid });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllPriorityClientMeetings");
            }
        }


        /// <summary>
        /// Export selected candidates data to excel
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> AllPriorityExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("All Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_All_Candidate_Status_Report" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var roleId = await _clientService.GetClientRoleByEmailId(emailid);

                var selectedMeeting = new List<MeetingVM>();
                var meetings = await _meetingService.GetClientMeetingList(emailid);
                if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") == null)
                {
                    meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                }

                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();

                SetMeetingsProperties(meetings);

                if (HttpContext.Session.GetString("AllPriorityClientMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("AllPriorityClientMeetingsfilters"));
                    meetings = ApplyAllMeetingsFilter(filter, meetings);
                }

                Sheet.Cells["A1:N1"].Style.Font.Bold = true;
                Sheet.Cells["A1:N1"].Style.Font.Size = 12;
                Sheet.Cells["A1:N1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 12;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Candidate name";
                Sheet.Cells[1, 3].Value = "Candidate Email";
                Sheet.Cells[1, 4].Value = "Client HR";
                Sheet.Cells[1, 5].Value = "Confirmation Status";
                Sheet.Cells[1, 6].Value = "Meeting Status";
                if (roleId != (int)ClientRoleEnum.RestrictedIRFowner)
                {
                    Sheet.Cells[1, 7].Value = "Project Name";
                    Sheet.Cells[1, 8].Value = "Primary Skill";
                    Sheet.Cells[1, 9].Value = "Interview Status";
                    Sheet.Cells[1, 10].Value = "Video Link";
                    Sheet.Cells[1, 11].Value = "Interviewer Comment";
                }
                //Sheet.Cells[1, 11].Value = "Report Link";

                int i = 2;
                //string LastAction = null; ;
                meetings = meetings.OrderByDescending(x => x.interviewDate).ToList();
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.candidateName;
                        Sheet.Cells[i, 3].Value = item.candidateEmail;
                        Sheet.Cells[i, 4].Value = item.ClientHrContact;
                        Sheet.Cells[i, 5].Value = item.confirmationStatus;
                        if (!string.IsNullOrEmpty(item.MeetingStatusStatusDesc))
                        {
                            Sheet.Cells[i, 6].Value = item.MeetingStatusStatusDesc;
                        }
                        else
                        {
                            Sheet.Cells[i, 6].Value = string.Empty;
                        }
                        if (roleId != (int)ClientRoleEnum.RestrictedIRFowner)
                        {
                            Sheet.Cells[i, 7].Value = item.ClientName;
                            Sheet.Cells[i, 8].Value = item.JDTiltle.Split("|")[0];


                            //if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            //{
                            //    if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                            //    {
                            //        Sheet.Cells[i, 7].Value = "Status Pending";

                            //    }
                            //    else
                            //    {
                            //        Sheet.Cells[i, 7].Value = item.MeetingStatusStatusDesc;
                            //    }
                            //}
                            //else
                            //{
                            //    Sheet.Cells[i, 7].Value = string.Empty;
                            //}



                            //Sheet.Cells[i, 7].Value = item.MeetingStatusDescription;

                            //if ((item.InterviewStatus != 4 && item.InterviewStatus != 2) || (item.QCInterview > 0 && item.QCStatus == 1 && (item.InterviewStatus == 4 || item.InterviewStatus == 2)))
                            //{
                            //    Sheet.Cells[i, 8].Value = item.intervieweStatusStatDesc;
                            //}
                            //else if ((item.InterviewStatus == 4 || item.InterviewStatus == 2) && (item.QCStatus == null || item.QCInterview == 0 || item.QCStatus==2))
                            //{
                            //    Sheet.Cells[i, 8].Value = "QC Pending";

                            //}
                            //else
                            //{
                            //    Sheet.Cells[i, 8].Value = string.Empty;

                            //}

                            if (!string.IsNullOrEmpty(item.intervieweStatusStatDesc))
                            {
                                Sheet.Cells[i, 9].Value = item.intervieweStatusStatDesc;
                            }
                            else
                            {
                                Sheet.Cells[i, 9].Value = string.Empty;
                            }

                            //if ((item.InterviewStatus != 4 && item.InterviewStatus != 2) || (item.QCInterview > 0 && item.QCStatus == 1 && (item.InterviewStatus == 4 || item.InterviewStatus == 2)))
                            //{
                            //    Sheet.Cells[i, 8].Value = item.intervieweStatusStatDesc;
                            //}
                            //else
                            //{
                            //    Sheet.Cells[i, 8].Value = "";
                            //}

                            if (!string.IsNullOrEmpty(item.VideoLink) && (item.InterviewStatus != 4 && item.InterviewStatus != 2) || (item.QCInterview > 0 && item.QCStatus == 1 && (item.InterviewStatus == 4 || item.InterviewStatus == 2)))
                            {
                                try
                                {
                                    Sheet.Cells[i, 10].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                    Sheet.Cells[i, 10].Value = "Video Link";
                                    Sheet.Cells[i, 10].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 10].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 10].Value = "No Video";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 10].Value = "No Video";
                            }

                            if ((item.InterviewStatus != 4 && item.InterviewStatus != 2) || (item.QCInterview > 0 && item.QCStatus == 1 && (item.InterviewStatus == 4 || item.InterviewStatus == 2)))
                            {
                                Sheet.Cells[i, 11].Value = item.InterviewerComment;
                            }
                            else
                            {
                                Sheet.Cells[i, 11].Value = "";
                            }
                        }

                        //Sheet.Cells[i, 10].Value = item.InterviewerComment;
                        //if ((item.InterviewStatus != 4 || item.InterviewStatus != 2) || (item.QCInterview > 0 && item.QCStatus == 1 && (item.InterviewStatus == 4 || item.InterviewStatus == 2)))
                        //{
                        //    Sheet.Cells[i, 11].Hyperlink = new Uri("https://portal.risebird.io/Interview/AddReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true, UriKind.Absolute);

                        //    Sheet.Cells[i, 11].Value = "Report Link";
                        //    Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                        //    Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        //}
                        //else
                        //{
                        //    Sheet.Cells[i, 11].Value = "No Report";
                        //}
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Report(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        model.IsQCRequired = client.IsQCRequired;
                        model.IsRedoInterview = client.IsRedoInterview;
                        if (model.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                        }
                        ViewBag.IsClient = isClient;
                        ViewBag.profile = profileName;
                        ViewBag.IsClientSelected = isClientSelected;
                        model.PreviousMeetingStatus = model.MeetingStatus;
                        ViewBag.PageStatus = "Mainurl";
                        GetStatusList(model, irfId);
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        if (model.IsProctor)
                        {
                            return PartialView("ClientProctorPartialReport", model);
                        }
                        else
                        {
                            return PartialView("ClientPartialReport", model);
                        }
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
                return RedirectToAction("AllMeetings");
            }
        }

        [HttpGet]
        private void GetStatusList(InterviewReport model, int irfId = 0, int userId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_irfService.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_irfService.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };

            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");
            var questions = _meetingService.GetTechSkillComments(irfId, userId).Result;
            model.QuestionsList = new SelectList(questions, "Comment", "Comment");
            //model.QuestionsListData = JsonConvert.SerializeObject(questions.Select(x => (x.Comment.Replace("\r", "").Replace("\n", ""), x.SkillName)));
        }

        //[Authorize]
        //[HttpGet]
        //public IActionResult SelectedMeetings(int? Page, string searchValue)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Client")
        //            {

        //                var selectedMeeting = new List<MeetingVM>();
        //                var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");
        //                if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") == null)
        //                {
        //                    meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
        //                }

        //                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.MeetingStatusDescription = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); x.InterviewStatusDescription = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); return x; }).ToList();


        //                foreach (var m in meetings)
        //                {
        //                    if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
        //                    {
        //                        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
        //                    }
        //                    else
        //                    {
        //                        continue;
        //                    }

        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
        //                    }
        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
        //                    }
        //                    if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
        //                    {
        //                        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
        //                    }
        //                    if (m.PreparationStatus.HasValue)
        //                    {
        //                        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
        //                    }
        //                    if (m.OfferStatus.HasValue && m.OfferStatus != -1)
        //                    {
        //                        m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
        //                    }
        //                    if (m.MeetingStatus.HasValue)
        //                    {
        //                        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
        //                    }
        //                    if (m.QCStatus.HasValue)
        //                    {
        //                        m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
        //                    }
        //                    if (m.IsApproved.HasValue && m.IsApproved != -1)
        //                    {
        //                        m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
        //                    }
        //                    if (m.IsSelected.HasValue && m.IsSelected != -1)
        //                    {
        //                        m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
        //                    }
        //                    selectedMeeting.Add(m);
        //                }

        //                if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") != null)
        //                {
        //                    var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
        //                    if (filter.StartDate != null && filter.EndDate != null)
        //                    {
        //                        selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
        //                    }

        //                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
        //                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
        //                    filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
        //                    filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
        //                    filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
        //                    filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
        //                    filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
        //                    filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
        //                    filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

        //                    selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
        //                    && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
        //                    && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
        //                    && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
        //                     && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
        //                    && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
        //                    && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
        //                    && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
        //                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();


        //                }
        //                if (TempData["SuccessErrorMessageInterview"] != null)
        //                {
        //                    ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
        //                    TempData.Remove("SuccessErrorMessageInterview");
        //                }
        //                ViewBag.ClientHRList = _meetingService.GetClientHRContactList(emailid, selectedMeeting);
        //                ViewBag.ClientNameList = _meetingService.GetEndClientList(emailid, selectedMeeting);
        //                ViewBag.Searchvalue = searchValue;
        //                if (!string.IsNullOrEmpty(searchValue))
        //                {
        //                    searchValue = searchValue.ToUpper();
        //                    selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "")).ToList();

        //                }
        //                ViewBag.TotalCount = selectedMeeting.Count;

        //                ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
        //                int a = selectedMeeting.Count / 10;
        //                if (selectedMeeting.Count % 10 == 0)
        //                {
        //                    ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
        //                }
        //                else
        //                {
        //                    a = a + 1;
        //                    if (a == Page)
        //                    {
        //                        ViewBag.PageEndCount = selectedMeeting.Count;
        //                    }
        //                    else
        //                    {
        //                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

        //                    }

        //                }
        //                if (selectedMeeting.Count == 0)
        //                {
        //                    ViewBag.PageStartCount = 0;
        //                    ViewBag.PageEndCount = 0;
        //                }
        //                return View(selectedMeeting.ToPagedList(Page ?? 1, 10));
        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return RedirectToAction("AllMeetings");
        //    }

        //}

        //[Authorize]
        //[HttpPost]
        //public IActionResult SelectedMeetings(SelectedPriorityViewFilter filter, int? Page, string searchValue)
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            if (filter != null)
        //            {
        //                HttpContext.Session.SetString("SelectedPriorityClientMeetingsfilters", JsonConvert.SerializeObject(filter));
        //            }
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Client")
        //            {
        //                var selectedMeeting = new List<MeetingVM>();
        //                var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");

        //                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();


        //                foreach (var meeting in meetings)
        //                {
        //                    if (meeting.InterviewStatus != null)
        //                    {
        //                        meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
        //                    }
        //                    if (meeting.MeetingStatus != null)
        //                    {
        //                        meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
        //                    }
        //                }
        //                //ViewBag.IsSelected = true;
        //                foreach (var m in meetings)
        //                {
        //                    if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
        //                    {
        //                        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
        //                    }
        //                    else
        //                    {
        //                        continue;
        //                    }

        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
        //                    }
        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
        //                    }
        //                    if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
        //                    {
        //                        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
        //                    }
        //                    if (m.PreparationStatus.HasValue)
        //                    {
        //                        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
        //                    }
        //                    if (m.OfferStatus.HasValue && m.OfferStatus != -1)
        //                    {
        //                        m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
        //                    }
        //                    if (m.MeetingStatus.HasValue)
        //                    {
        //                        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
        //                    }
        //                    if (m.QCStatus.HasValue)
        //                    {
        //                        m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
        //                    }
        //                    if (m.IsApproved.HasValue && m.IsApproved != -1)
        //                    {
        //                        m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
        //                    }
        //                    if (m.IsSelected.HasValue && m.IsSelected != -1)
        //                    {
        //                        m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
        //                    }
        //                    selectedMeeting.Add(m);
        //                }

        //                if (filter.StartDate != null && filter.EndDate != null)
        //                {
        //                    selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
        //                }

        //                filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
        //                filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
        //                filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
        //                filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
        //                filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
        //                filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
        //                filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
        //                filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
        //                filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;


        //                selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
        //                && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
        //                && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
        //                && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
        //                && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
        //                    && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
        //                    && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
        //                    && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
        //                && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
        //                ViewBag.Searchvalue = searchValue;
        //                if (!string.IsNullOrEmpty(searchValue))
        //                {
        //                    searchValue = searchValue.ToUpper();
        //                    selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "")).ToList();

        //                }
        //                ViewBag.TotalCount = selectedMeeting.Count;

        //                ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
        //                int a = selectedMeeting.Count / 10;
        //                if (selectedMeeting.Count % 10 == 0)
        //                {
        //                    ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
        //                }
        //                else
        //                {
        //                    a = a + 1;
        //                    if (a == Page)
        //                    {
        //                        ViewBag.PageEndCount = selectedMeeting.Count;
        //                    }
        //                    else
        //                    {
        //                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));

        //                    }

        //                }
        //                if (selectedMeeting.Count == 0)
        //                {
        //                    ViewBag.PageStartCount = 0;
        //                    ViewBag.PageEndCount = 0;
        //                }
        //                return View(selectedMeeting.ToPagedList(Page ?? 1, 10));


        //            }
        //            else
        //            {
        //                return RedirectToAction("SignOut", "Session");
        //            }
        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return RedirectToAction("AllMeetings");
        //    }

        //}


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SelectedMeetings(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        var selectedMeeting = new List<MeetingVM>();
                        var meetings = new List<MeetingVM>();
                        //var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");
                        if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") == null)
                        {
                            meetings = (await _meetingService.GetClientSelectedMeetings(emailid)).ToList();
                        }
                        else
                        {
                            var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
                            meetings = (await _meetingService.GetClientSelectedMeetings(emailid, true, filter.StartDate, filter.EndDate)).ToList();
                        }

                        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.MeetingStatusDescription = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); x.InterviewStatusDescription = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); return x; }).ToList();

                        foreach (var m in meetings)
                        {
                            m.IsClientQCRequired = client.IsQCRequired;
                            m.IsClientRedoInterview = client.IsRedoInterview;
                            if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                            {
                                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                            }
                            else
                            {
                                continue;
                            }

                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                            }
                            if (m.L2InterviewStatus.HasValue)
                            {
                                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                            }
                            if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                            {
                                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                            }
                            if (m.PreparationStatus.HasValue)
                            {
                                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                            }
                            if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                            {
                                m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                            }
                            if (m.MeetingStatus.HasValue)
                            {
                                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                            }
                            if (m.IsApproved.HasValue && m.IsApproved != -1)
                            {
                                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                            }
                            if (m.IsSelected.HasValue && m.IsSelected != -1)
                            {
                                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                            }
                            selectedMeeting.Add(m);
                        }

                        if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                            filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

                            selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
                            && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                             && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                            && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                            && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                            && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        ViewBag.ClientHRList = _meetingService.GetClientHRContactList(emailid, selectedMeeting);
                        ViewBag.ClientNameList = _meetingService.GetEndClientList(emailid, selectedMeeting);
                        ViewBag.Searchvalue = searchValue;
                        if (!string.IsNullOrEmpty(searchValue))
                        {
                            searchValue = searchValue.ToUpper().Trim();
                            selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "") || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "")).ToList();
                        }
                        ViewBag.TotalCount = selectedMeeting.Count;

                        ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                        int a = selectedMeeting.Count / 10;
                        if (selectedMeeting.Count % 10 == 0)
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                        else
                        {
                            a = a + 1;
                            if (a == Page)
                            {
                                ViewBag.PageEndCount = selectedMeeting.Count;
                            }
                            else
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                            }
                        }
                        if (selectedMeeting.Count == 0)
                        {
                            ViewBag.PageStartCount = 0;
                            ViewBag.PageEndCount = 0;
                        }
                        var selectdata = JsonConvert.SerializeObject(selectedMeeting);
                        HttpContext.Session.SetString("SearchSelectedMeetings", selectdata);
                        return View(selectedMeeting.ToPagedList(Page ?? 1, 10));
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
                return RedirectToAction("AllMeetings");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SelectedMeetings(SelectedPriorityViewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("SelectedPriorityClientMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("SelectedMeetings", new { Page = Page, searchValue = searchValue });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetSelectedPriorityClientMeetingsfilters()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("SelectedPriorityClientMeetingsfilters");
                    return RedirectToAction("SelectedMeetings", "Client", new { @Page = 1, @searchValue = string.Empty });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SelectedPriorityClientMeetings");
            }
        }

        /// <summary>
        /// Export selected candidates data to excel
        /// </summary>
        /// <returns></returns>
        //[Authorize]
        //[HttpGet]
        //public FileContentResult SelectedPriorityExporttoExcel()
        //{
        //    string userEmailId = string.Empty;
        //    ExcelPackage Ep = new ExcelPackage();
        //    ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Selected Candidate Report");
        //    string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Selected_Candidate_Status_Report" + ".xlsx";
        //    try
        //    {
        //        var emailid = HttpContext.Session.GetString("email")?.Trim();
        //        emailid = emailid.Replace("\"", "");

        //        var selectedMeeting = new List<MeetingVM>();
        //        var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");
        //        if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") == null)
        //        {
        //            meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
        //        }
        //        meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
        //        foreach (var meeting in meetings)
        //        {
        //            if (meeting.InterviewStatus != null)
        //            {
        //                meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
        //            }
        //            if (meeting.MeetingStatus != null)
        //            {
        //                meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
        //            }
        //        }
        //        //ViewBag.IsSelected = true;
        //        foreach (var m in meetings)
        //        {
        //            if (m.InterviewStatus != null && m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
        //            {
        //                m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
        //            }
        //            else
        //            {
        //                continue;
        //            }

        //            if (m.L2InterviewStatus != null && m.L2InterviewStatus.HasValue)
        //            {
        //                m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
        //            }
        //            if (m.L2InterviewStatus != null && m.L2InterviewStatus.HasValue)
        //            {
        //                m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
        //            }
        //            if (m.JoiningStatus != null && m.JoiningStatus.HasValue)
        //            {
        //                m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
        //            }
        //            if (m.PreparationStatus != null && m.PreparationStatus.HasValue)
        //            {
        //                m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
        //            }
        //            if (m.OfferStatus != null && m.OfferStatus.HasValue)
        //            {
        //                m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
        //            }
        //            if (m.MeetingStatus != null && m.MeetingStatus.HasValue)
        //            {
        //                m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
        //            }
        //            if (m.QCStatus != null && m.QCStatus.HasValue)
        //            {
        //                m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
        //            }
        //            if (m.IsApproved != null && m.IsApproved.HasValue)
        //            {
        //                m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
        //            }
        //            if (m.IsSelected != null && m.IsSelected.HasValue)
        //            {
        //                m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
        //            }

        //            selectedMeeting.Add(m);
        //        }
        //        if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") != null)
        //        {
        //            var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
        //            if (filter.StartDate != null && filter.EndDate != null)
        //            {
        //                selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
        //            }

        //            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
        //            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
        //            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
        //            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
        //            filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
        //            filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
        //            filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
        //            filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
        //            filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

        //            selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
        //            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
        //            && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
        //            && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
        //             && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
        //            && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
        //            && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
        //            && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
        //            && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();

        //            if (selectedMeeting.Count == 0 && filter.StartDate != null && filter.EndDate != null)
        //            {
        //                meetings = _meetingService.GetClientMeetingList(emailid);//_meetingService.GetClientMeetingList("aneel.tirumalasetty@mphasis.com")
        //                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
        //                foreach (var meeting in meetings)
        //                {
        //                    if (meeting.InterviewStatus != null)
        //                    {
        //                        meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
        //                    }
        //                    if (meeting.MeetingStatus != null)
        //                    {
        //                        meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
        //                    }
        //                }
        //                //ViewBag.IsSelected = true;
        //                foreach (var m in meetings)
        //                {
        //                    if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
        //                    {
        //                        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
        //                    }
        //                    else
        //                    {
        //                        continue;
        //                    }

        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
        //                    }
        //                    if (m.L2InterviewStatus.HasValue)
        //                    {
        //                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
        //                    }
        //                    if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
        //                    {
        //                        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
        //                    }
        //                    if (m.PreparationStatus.HasValue)
        //                    {
        //                        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
        //                    }
        //                    if (m.OfferStatus.HasValue && m.OfferStatus != -1)
        //                    {
        //                        m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
        //                    }
        //                    if (m.MeetingStatus.HasValue)
        //                    {
        //                        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
        //                    }
        //                    if (m.QCStatus.HasValue)
        //                    {
        //                        m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
        //                    }
        //                    if (m.IsApproved.HasValue)
        //                    {
        //                        m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
        //                    }
        //                    if (m.IsSelected.HasValue)
        //                    {
        //                        m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
        //                    }
        //                    selectedMeeting.Add(m);
        //                }
        //                selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();

        //            }
        //        }


        //        Sheet.Cells["A1:P1"].Style.Font.Bold = true;
        //        Sheet.Cells["A1:P1"].Style.Font.Size = 12;
        //        Sheet.Cells["A1:P1"].Style.WrapText = true;
        //        //Sheet.Column(1).Width = 30;
        //        //Sheet.Column(2).Width = 30;
        //        int startColumn = 1; int endColumn = 11;
        //        for (int a = startColumn; a <= endColumn; a++)
        //        {
        //            Sheet.Column(a).Width = 30;
        //        }
        //        Sheet.Cells[1, 1].Value = "Interview Date";
        //        Sheet.Cells[1, 2].Value = "Candidate name";
        //        Sheet.Cells[1, 3].Value = "Client HR";
        //        Sheet.Cells[1, 4].Value = "End Client Name";
        //        Sheet.Cells[1, 5].Value = "Primary Skill";
        //        Sheet.Cells[1, 6].Value = "Video";
        //        //Sheet.Cells[1, 8].Value = "Report Link";
        //        Sheet.Cells[1, 7].Value = "Interviewer Comment";
        //        Sheet.Cells[1, 8].Value = "Approval Status";
        //        Sheet.Cells[1, 9].Value = "Approval Comments ";
        //        Sheet.Cells[1, 10].Value = "Final Selection Status";
        //        Sheet.Cells[1, 11].Value = "Final Selection Comments";

        //        //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
        //        int i = 2;
        //        //string LastAction = null; ;

        //        if (selectedMeeting.Count() > 0)
        //        {
        //            foreach (var item in selectedMeeting)
        //            {
        //                Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
        //                Sheet.Cells[i, 2].Value = item.candidateName;
        //                Sheet.Cells[i, 3].Value = item.ClientHrContact;
        //                Sheet.Cells[i, 4].Value = item.ClientName;
        //                Sheet.Cells[i, 5].Value = item.JDTiltle.Split("|")[0];
        //                if (!string.IsNullOrEmpty(item.VideoLink))
        //                {
        //                    try
        //                    {
        //                        Sheet.Cells[i, 6].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
        //                        Sheet.Cells[i, 6].Value = "Video Link";
        //                        Sheet.Cells[i, 6].Style.Font.UnderLine = true;
        //                        Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        Sheet.Cells[i, 6].Value = "No Video";
        //                    }
        //                }
        //                else
        //                {
        //                    Sheet.Cells[i, 6].Value = "No Video";
        //                }
        //                //Sheet.Cells[i, 8].Hyperlink = new Uri("https://portal.risebird.io/Interview/AddReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true, UriKind.Absolute);
        //                //Sheet.Cells[i, 8].Value = "Report Link";
        //                //Sheet.Cells[i, 8].Style.Font.UnderLine = true;
        //                //Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
        //                Sheet.Cells[i, 7].Value = item.InterviewerComment;
        //                Sheet.Cells[i, 8].Value = item.NextLevelApprovalStat;
        //                Sheet.Cells[i, 9].Value = item.ApprovalComments;
        //                Sheet.Cells[i, 10].Value = item.FinalSelectionStat;
        //                Sheet.Cells[i, 11].Value = item.FinalComments;

        //                i++;
        //            }
        //            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }
        //        else
        //        {
        //            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //            Rng.Value = "No data available in table !!!";
        //            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //        Rng.Value = "Data export failed !!!";
        //        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //    }

        //}

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> SelectedPriorityExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Selected Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Selected_Candidate_Status_Report" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                var selectedMeeting = new List<MeetingVM>();
                var meetings = new List<MeetingVM>();
                //var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");
                if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") == null)
                {
                    meetings = (await _meetingService.GetClientSelectedMeetings(emailid)).ToList();
                }
                else
                {
                    var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
                    meetings = (await _meetingService.GetClientSelectedMeetings(emailid, true, filter.StartDate, filter.EndDate)).ToList();
                }
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                foreach (var meeting in meetings)
                {
                    if (meeting.InterviewStatus != null)
                    {
                        meeting.InterviewStatusDescription = ((InterviewStatus)meeting.InterviewStatus).GetDescription();
                    }
                    if (meeting.MeetingStatus != null)
                    {
                        meeting.MeetingStatusDescription = ((MeetingStatus)meeting.MeetingStatus).GetDescription();
                    }
                }
                //ViewBag.IsSelected = true;
                foreach (var m in meetings)
                {
                    if (m.InterviewStatus != null && m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                    {
                        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                    }
                    else
                    {
                        continue;
                    }

                    if (m.L2InterviewStatus != null && m.L2InterviewStatus.HasValue)
                    {
                        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                    }
                    if (m.L2InterviewStatus != null && m.L2InterviewStatus.HasValue)
                    {
                        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                    }
                    if (m.JoiningStatus != null && m.JoiningStatus.HasValue)
                    {
                        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                    }
                    if (m.PreparationStatus != null && m.PreparationStatus.HasValue)
                    {
                        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                    }
                    if (m.OfferStatus != null && m.OfferStatus.HasValue)
                    {
                        m.OfferStatusStatDesc = ((PreparationStatus)(m.OfferStatus)).GetDescription();
                    }
                    if (m.MeetingStatus != null && m.MeetingStatus.HasValue)
                    {
                        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                    }
                    if (m.IsApproved != null && m.IsApproved.HasValue)
                    {
                        m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                    }
                    if (m.IsSelected != null && m.IsSelected.HasValue)
                    {
                        m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                    }

                    selectedMeeting.Add(m);
                }
                if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                    filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                    filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                    filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                    filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

                    selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                    && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
                    && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                     && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                    && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                    && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                    && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();

                }


                Sheet.Cells["A1:P1"].Style.Font.Bold = true;
                Sheet.Cells["A1:P1"].Style.Font.Size = 12;
                Sheet.Cells["A1:P1"].Style.WrapText = true;
                //Sheet.SelectedRange["G:G"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 11;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Candidate Name";
                Sheet.Cells[1, 3].Value = "Candidate Email";
                Sheet.Cells[1, 4].Value = "Client HR";
                Sheet.Cells[1, 5].Value = "Project Name";
                Sheet.Cells[1, 6].Value = "Primary Skill";
                Sheet.Cells[1, 7].Value = "Video";
                //Sheet.Cells[1, 8].Value = "Report Link";
                Sheet.Cells[1, 8].Value = "Interviewer Comment";
                Sheet.Cells[1, 9].Value = "Approval Status";
                Sheet.Cells[1, 10].Value = "Approval Comments ";
                Sheet.Cells[1, 11].Value = "Final Selection Status";
                Sheet.Cells[1, 12].Value = "Final Selection Comments";
                Sheet.Cells[1, 13].Value = "Executive Report URL";

                //ExcelHyperLink excelHyperLink = new ExcelHyperLink();
                int i = 2;
                //string LastAction = null; ;

                if (selectedMeeting.Count() > 0)
                {
                    foreach (var item in selectedMeeting)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.candidateName;
                        Sheet.Cells[i, 3].Value = item.candidateEmail;
                        Sheet.Cells[i, 4].Value = item.ClientHrContact;
                        Sheet.Cells[i, 5].Value = item.ClientName;
                        Sheet.Cells[i, 6].Value = item.JDTiltle.Split("|")[0];
                        if (!string.IsNullOrEmpty(item.VideoLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 7].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                Sheet.Cells[i, 7].Value = "Video Link";
                                Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 7].Value = "No Video";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "No Video";
                        }
                        //Sheet.Cells[i, 8].Hyperlink = new Uri("https://portal.risebird.io/Interview/AddReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true, UriKind.Absolute);
                        //Sheet.Cells[i, 8].Value = "Report Link";
                        //Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                        //Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        Sheet.Cells[i, 8].Value = item.InterviewerComment;
                        Sheet.Cells[i, 9].Value = item.NextLevelApprovalStat;
                        Sheet.Cells[i, 10].Value = item.ApprovalComments;
                        Sheet.Cells[i, 11].Value = item.FinalSelectionStat;
                        Sheet.Cells[i, 12].Value = item.FinalComments;

                        //var reporturl = Request.Host.ToString() + "/Client/ExecutiveReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true;
                        string reporturl = "https://portal.risebird.io/Client/ExecutiveReport?interviewId=" + item.imid + "&irfId=" + item.irfIntId + "&isClient=" + true;
                        Sheet.Cells[i, 13].Hyperlink = new Uri(reporturl, UriKind.Absolute);
                        Sheet.Cells[i, 13].Value = "Report Url";
                        Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                        Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        public JsonResult SelectedPriorityClientMeetingsJson(string searchValue)
        {
            try
            {
                int Page = 1;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //    profileName = profileName.Replace("\"", "");
                    //    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    //    emailid = emailid.Replace("\"", "");
                    //    var selectedMeeting = new List<MeetingVM>();
                    //    var meetings = _meetingService.GetClientMeetingList(emailid).Where(x => x.QCInterview > 0 && x.QCStatus == 1 && x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.confirmationStatus == "1");
                    //    meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); x.MeetingStatusDescription = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); x.InterviewStatusDescription = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); return x; }).ToList();
                    //    if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") == null)
                    //    {
                    //        meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                    //    }
                    //    foreach (var m in meetings)
                    //    {
                    //        if (m.InterviewStatus.HasValue && (m.InterviewStatus.Value == 2 || m.InterviewStatus.Value == 4))
                    //        {
                    //            m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                    //        }
                    //        else
                    //        {
                    //            continue;
                    //        }
                    //        if (m.L2InterviewStatus.HasValue)
                    //        {
                    //            m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                    //        }
                    //        if (m.L2InterviewStatus.HasValue)
                    //        {
                    //            m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                    //        }
                    //        if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                    //        {
                    //            m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                    //        }
                    //        if (m.PreparationStatus.HasValue)
                    //        {
                    //            m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                    //        }
                    //        if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                    //        {
                    //            m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                    //        }
                    //        if (m.MeetingStatus.HasValue)
                    //        {
                    //            m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                    //        }
                    //        if (m.QCStatus.HasValue)
                    //        {
                    //            m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                    //        }
                    //        if (m.IsApproved.HasValue && m.IsApproved != -1)
                    //        {
                    //            m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                    //        }
                    //        if (m.IsSelected.HasValue && m.IsSelected != -1)
                    //        {
                    //            m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                    //        }
                    //        selectedMeeting.Add(m);
                    //    }

                    //    if (HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters") != null)
                    //    {
                    //        var filter = JsonConvert.DeserializeObject<SelectedPriorityViewFilter>(HttpContext.Session.GetString("SelectedPriorityClientMeetingsfilters"));
                    //        if (filter.StartDate != null && filter.EndDate != null)
                    //        {
                    //            selectedMeeting = selectedMeeting.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    //        }
                    //        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = selectedMeeting.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    //        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = selectedMeeting.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    //        filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = selectedMeeting.Select(x => Convert.ToString(x.interviewDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                    //        filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = selectedMeeting.Select(x => x.candidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                    //        filter.ClientName = filter.ClientName == null ? filter.ClientName = selectedMeeting.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    //        filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                    //        filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                    //        filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                    //        filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;

                    //        selectedMeeting = selectedMeeting.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    //        && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                    //        && (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.interviewDate)) : c.interviewDate == null)
                    //        && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.candidateName) : c.candidateName == null)
                    //         && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                    //        && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                    //        && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                    //        && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)
                    //        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                    //    }

                    var selectedMeeting = JsonConvert.DeserializeObject<List<MeetingVM>>(HttpContext.Session.GetString("SearchSelectedMeetings"));
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        selectedMeeting = selectedMeeting.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == " ") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == " ") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == " ") || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == " ")).ToList();
                    }
                    int totalcount = selectedMeeting.Count;
                    int Pagestart = 0;
                    int Pageend = 0;

                    Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = selectedMeeting.Count / 10;
                    if (selectedMeeting.Count % 10 == 0)
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        if (a == Page)
                        {
                            Pageend = selectedMeeting.Count;
                        }
                        else
                        {
                            Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                    }
                    if (selectedMeeting.Count == 0)
                    {
                        Pagestart = 0;
                        Pageend = 0;
                    }
                    return Json(new { result = selectedMeeting.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = selectedMeeting.Count });
                }
                else
                {
                    return Json("failed");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyJoiningAudio(int candidateId, string CandidateName, int irfId, int meetingId)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Client")
                {
                    var model = await _meetingService.GetCandidateAudios(candidateId, irfId);
                    model.MeetingId = meetingId;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyJoiningAudio(VerifyCandidateAudio model)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Client")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg" || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(string.Empty, "Audio Upload failed.");
                                    return View("VerifyAudio", model);
                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }
                            }
                        }
                        var result = _meetingService.VerifyCandidateVoice(model.VoiceItUserId, model.AudioFileLink, model.PhraseDetail, model.MeetingId, "joining");
                        TempData["SuccessErrorMessageInterview"] = result;
                        return RedirectToAction("SelectedMeetings");
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
                TempData["SuccessErrorMessageInterview"] = "Audio verification failed.";
                return RedirectToAction("SelectedMeetings");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RejectedMeetings(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        if (TempData["SuccessErrorMessageQCForm"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageQCForm"].ToString();
                            TempData.Remove("SuccessErrorMessageQCForm");
                        }
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        ViewBag.IsQCRequired = client.IsQCRequired;
                        var roleid = await _clientService.GetClientRoleByEmailId(emailid);
                        if (roleid != 4)
                        {
                            var meetings = await _meetingService.GetClientMeetingList(emailid);
                            meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                            if (HttpContext.Session.GetString("RejectClientMeetingsfilters") == null)
                            {
                                meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                            }
                            SetMeetingsProperties(meetings);
                            meetings = meetings.Where(x => x.intervieweStatusStatDesc == "Rejected").ToList();
                            if (HttpContext.Session.GetString("RejectClientMeetingsfilters") != null)
                            {
                                var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RejectClientMeetingsfilters"));
                                //Apply filter and get the filtered meetings back
                                meetings = ApplyAllMeetingsFilter(filter, meetings);
                            }

                            ViewBag.ClientHRList = meetings.Select(x => x.ClientHrContact).Distinct();
                            ViewBag.ClientNameList = meetings.Select(x => x.ClientName).Distinct();
                            ViewBag.IRFList = meetings.Select(x => x.IRFId).Distinct();
                            ViewBag.PrimarySkills = meetings.Select(x => x.JDTiltle).Distinct();
                            ViewBag.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).Distinct();
                            ViewBag.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).Distinct();
                            ViewBag.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).Distinct();
                            ViewBag.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).Distinct();
                            ViewBag.FinalSelection = meetings.Select(x => x.FinalSelectionStat).Distinct();
                            ViewBag.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).Distinct();
                            ViewBag.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).Distinct();
                            if (string.IsNullOrEmpty(searchValue) && Page > 1)
                            {
                                searchValue = Searchvalueproiprity;
                            }
                            Searchvalueproiprity = searchValue;

                            ViewBag.Searchvalue = searchValue;
                            if (!string.IsNullOrEmpty(searchValue))
                            {
                                searchValue = searchValue.ToUpper().Trim();
                                meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == "") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == "") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == "") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == "")
                                || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == "") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == "")
                                || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == "")).ToList();
                            }
                            ViewBag.TotalCount = meetings.Count;
                            ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                            int a = meetings.Count / 10;
                            if (meetings.Count % 10 == 0)
                            {
                                ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                            }
                            else
                            {
                                a = a + 1;
                                if (a == Page || 10 >= meetings.Count)
                                {
                                    ViewBag.PageEndCount = meetings.Count;
                                }
                                else
                                {
                                    ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                                }
                            }
                            if (meetings.Count == 0)
                            {
                                ViewBag.PageStartCount = 0;
                                ViewBag.PageEndCount = 0;
                            }
                            _modelDetails = meetings;
                            ViewBag.Date = DateTime.Now;
                            foreach (var item in meetings)
                            {
                                item.IsClientQCRequired = client.IsQCRequired;
                                item.IsClientRedoInterview = client.IsRedoInterview;
                            }
                            var rejectdata = JsonConvert.SerializeObject(meetings);
                            HttpContext.Session.SetString("SearchRejectedMeetings", rejectdata);
                            return View(meetings.ToPagedList(Page ?? 1, 10));
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
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult RejectedMeetings(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("RejectClientMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("RejectedMeetings", new { Page = Page, searchValue = searchValue });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("RejectedMeetings");
            }
        }

        [HttpPost]
        public JsonResult RejectedClientMeetingsJson(string searchValue)
        {
            try
            {
                int Page = 1;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var emailid = HttpContext.Session.GetString("email")?.Trim();
                    //emailid = emailid.Replace("\"", "");
                    //var meetings = _meetingService.GetClientMeetingList(emailid);
                    //meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                    //if (HttpContext.Session.GetString("RejectClientMeetingsfilters") == null)
                    //{
                    //    meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                    //}
                    //foreach (var m in meetings)
                    //{
                    //    if (m.InterviewStatus.HasValue)
                    //    {
                    //        m.intervieweStatusStatDesc = ((InterviewStatus)(m.InterviewStatus)).GetDescription();
                    //    }
                    //    if (m.L2InterviewStatus.HasValue)
                    //    {
                    //        m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                    //    }
                    //    if (m.L2InterviewStatus.HasValue)
                    //    {
                    //        m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                    //    }
                    //    if (m.JoiningStatus.HasValue && m.JoiningStatus != -1)
                    //    {
                    //        m.JoiningStatusStatDesc = ((JoiningStatus)(m.JoiningStatus)).GetDescription();
                    //    }
                    //    if (m.PreparationStatus.HasValue)
                    //    {
                    //        m.PreparationStatusDesc = ((PreparationStatus)(m.PreparationStatus)).GetDescription();
                    //    }
                    //    if (m.OfferStatus.HasValue && m.OfferStatus != -1)
                    //    {
                    //        m.OfferStatusStatDesc = ((OfferStatus)(m.OfferStatus)).GetDescription();
                    //    }
                    //    if (m.MeetingStatus.HasValue)
                    //    {
                    //        m.MeetingStatusStatusDesc = ((MeetingStatus)(m.MeetingStatus)).GetDescription();
                    //    }

                    //    if (m.QCStatus.HasValue)
                    //    {
                    //        m.QCStatusStatusDesc = ((QCStatus)(m.QCStatus)).GetDescription();
                    //    }
                    //    if (m.IsApproved.HasValue && m.IsApproved != -1)
                    //    {
                    //        m.NextLevelApprovalStat = ((NextLevelApprovalStatus)(m.IsApproved)).GetDescription();
                    //    }
                    //    if (m.IsSelected.HasValue && m.IsSelected != -1)
                    //    {
                    //        m.FinalSelectionStat = ((FinalSelectionStatus)(m.IsSelected)).GetDescription();
                    //    }
                    //    if (m.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                    //    {
                    //        if (m.interviewDate < DateTime.Now && m.MeetingStatusStatusDesc == MeetingStatus.Scheduled.GetDescription())
                    //        {
                    //            m.MeetingStatusStatusDesc = "Status Pending";
                    //        }
                    //    }
                    //    if ((m.InterviewStatus == 4 || m.InterviewStatus == 2) && (m.QCStatus == null || m.QCInterview == 0))
                    //    {
                    //        m.intervieweStatusStatDesc = "QC Pending";
                    //    }
                    //}
                    //meetings = meetings.Where(x => x.intervieweStatusStatDesc == "Rejected").ToList();
                    //if (HttpContext.Session.GetString("RejectClientMeetingsfilters") != null)
                    //{
                    //    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RejectClientMeetingsfilters"));
                    //    if (filter.StartDate != null && filter.EndDate != null)
                    //    {
                    //        meetings = meetings.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                    //    }

                    //    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    //    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    //    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    //    filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                    //    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    //    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                    //    filter.ApprovalStatus = filter.ApprovalStatus == null ? filter.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).ToArray() : filter.ApprovalStatus = filter.ApprovalStatus;
                    //    filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = meetings.Select(x => x.FinalSelectionStat).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                    //    filter.JoiningStatus = filter.JoiningStatus == null ? filter.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).ToArray() : filter.JoiningStatus = filter.JoiningStatus;
                    //    filter.OfferStatus = filter.OfferStatus == null ? filter.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).ToArray() : filter.OfferStatus = filter.OfferStatus;
                    //    //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = meetings.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                    //    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;


                    //    meetings = meetings.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                    //    && filter.RFID.Contains(c.IRFId)
                    //    && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                    //    && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                    //    && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                    //    && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                    //    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                    //    && (filter.ApprovalStatus.Count() > 0 ? filter.ApprovalStatus.Contains(c.NextLevelApprovalStat) : c.NextLevelApprovalStat == string.Empty)
                    //    && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.FinalSelectionStat) : c.FinalSelectionStat == string.Empty)
                    //    && (filter.OfferStatus.Count() > 0 ? filter.OfferStatus.Contains(c.OfferStatusStatDesc) : c.OfferStatusStatDesc == string.Empty)
                    //    && (filter.JoiningStatus.Count() > 0 ? filter.JoiningStatus.Contains(c.JoiningStatusStatDesc) : c.JoiningStatusStatDesc == string.Empty)).ToList();
                    //}
                    var meetings = JsonConvert.DeserializeObject<List<MeetingVM>>(HttpContext.Session.GetString("SearchRejectedMeetings"));
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper().Trim();
                        meetings = meetings.Where(x => (!string.IsNullOrEmpty(x.candidateName) ? x.candidateName.ToUpper().Contains(searchValue) : x.candidateName == " ") || (!string.IsNullOrEmpty(x.candidateEmail) ? x.candidateEmail.ToUpper().Contains(searchValue) : x.candidateEmail == " ") || (!string.IsNullOrEmpty(x.confirmationStatus) ? x.confirmationStatus.ToUpper().Contains(searchValue) : x.confirmationStatus == " ") || (!string.IsNullOrEmpty(x.ClientHrContact) ? x.ClientHrContact.ToUpper().Contains(searchValue) : x.ClientHrContact == " ")
                         || (!string.IsNullOrEmpty(x.JDTiltle) ? x.JDTiltle.Split("|")[0].ToUpper().Contains(searchValue) : x.JDTiltle == " ") || (!string.IsNullOrEmpty(x.MeetingStatusStatusDesc) ? x.MeetingStatusStatusDesc.ToUpper().Contains(searchValue) : x.MeetingStatusStatusDesc == " ")
                         || (!string.IsNullOrEmpty(x.intervieweStatusStatDesc) ? x.intervieweStatusStatDesc.ToUpper().Contains(searchValue) : x.intervieweStatusStatDesc == " ")).ToList();
                    }
                    int totalcount = meetings.Count;
                    int Pagestart = 0;
                    int Pageend = 0;
                    ViewBag.TotalCount = meetings.Count;

                    Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = meetings.Count / 10;
                    if (meetings.Count % 10 == 0)
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        if (a == Page)
                        {
                            Pageend = meetings.Count;
                        }
                        else
                        {
                            Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }
                    }
                    if (meetings.Count == 0)
                    {
                        Pagestart = 0;
                        Pageend = 0;
                    }

                    return Json(new { result = meetings.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = meetings.Count });
                }
                else
                {
                    return Json("failed");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetRejectClientMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RejectClientMeetingsfilters");
                    return RedirectToAction("RejectedMeetings", "Client", new { @Page = 1, @searchValue = string.Empty });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllPriorityClientMeetings");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> RejectedPriorityExporttoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Rejected Candidate Report");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Rejected_Candidate_Status_Report" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var meetings = await _meetingService.GetClientMeetingList(emailid);
                meetings = meetings.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                if (HttpContext.Session.GetString("RejectClientMeetingsfilters") == null)
                {
                    meetings = meetings.Where(t => t.interviewDate.Date >= DateTime.Now.AddDays(-30)).ToList();
                }
                SetMeetingsProperties(meetings);
                meetings = meetings.Where(x => x.intervieweStatusStatDesc == "Rejected").ToList();
                if (HttpContext.Session.GetString("RejectClientMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RejectClientMeetingsfilters"));
                    meetings = ApplyAllMeetingsFilter(filter, meetings);
                }

                Sheet.Cells["A1:P1"].Style.Font.Bold = true;
                Sheet.Cells["A1:P1"].Style.Font.Size = 12;
                Sheet.Cells["A1:P1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 11;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Candidate Name";
                Sheet.Cells[1, 3].Value = "Candidate Email";
                Sheet.Cells[1, 4].Value = "Client HR";
                Sheet.Cells[1, 5].Value = "Project Name";
                Sheet.Cells[1, 6].Value = "Primary Skill";
                Sheet.Cells[1, 7].Value = "Video";
                Sheet.Cells[1, 8].Value = "Interviewer Comment";
                Sheet.Cells[1, 9].Value = "Approval Status";
                Sheet.Cells[1, 10].Value = "Approval Comments ";
                Sheet.Cells[1, 11].Value = "Final Selection Status";
                Sheet.Cells[1, 12].Value = "Final Selection Comments";
                int i = 2;
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.candidateName;
                        Sheet.Cells[i, 3].Value = item.candidateEmail;
                        Sheet.Cells[i, 4].Value = item.ClientHrContact;
                        Sheet.Cells[i, 5].Value = item.ClientName;
                        Sheet.Cells[i, 6].Value = item.JDTiltle.Split("|")[0];
                        if (!string.IsNullOrEmpty(item.VideoLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 7].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                Sheet.Cells[i, 7].Value = "Video Link";
                                Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 7].Value = "No Video";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "No Video";
                        }
                        Sheet.Cells[i, 8].Value = item.InterviewerComment;
                        Sheet.Cells[i, 9].Value = item.NextLevelApprovalStat;
                        Sheet.Cells[i, 10].Value = item.ApprovalComments;
                        Sheet.Cells[i, 11].Value = item.FinalSelectionStat;
                        Sheet.Cells[i, 12].Value = item.FinalComments;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.PreviousMeetingStatus = model.MeetingStatus;
                GetStatusList(model, irfId);
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                ViewBag.PageStatus = "Public";
                if (model.IsProctor)
                {
                    return PartialView("ClientProctorPartialReport", model);
                }
                else
                {
                    return PartialView("ClientPartialReport", model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NotScheduledMeetings(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    int ClientId = 0;
                    if (profileName == "Client" || profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        if (profileName == "Client")
                        {
                            var role = await _clientService.GetClientRoleByEmailId(emailid);
                            ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
                        }
                        var meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                        if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                            meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                                    && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        else
                        {
                            DateTime StartDate = DateTime.Now.AddMonths(-1);
                            DateTime EndDate = DateTime.Now.AddDays(1);
                            meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                        }
                        return View(meetings);
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> SendInvite(string irfId, string IRFName, string emailId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUserEmail = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUserEmail = loggedInUserEmail.Replace("\"", "");
                    if (profileName == "Client" || profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (await _irfService.SendInvite(Convert.ToInt32(irfId), emailId, loggedInUserEmail))
                        {
                            return Json("Invitation has been sent Successfully!");
                        }
                        else
                        {
                            return Json("Sorry! Could not send invite!");
                        }
                    }
                    else
                    {
                        return Json("Fail");
                    }
                }
                else
                {
                    return Json("Fail");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Sorry! Could not send invite!");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExecutiveReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {
                var model = await _meetingService.GetInteviewReport(interviewId, irfId);
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.PreviousMeetingStatus = model.MeetingStatus;
                GetStatusList(model, irfId);
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                ViewBag.PageStatus = "PublicExternal";
                ViewBag.PageReport = "";
                if (model.IsProctor)
                {
                    return PartialView("ClientProctorPartialReport", model);
                }
                else
                {
                    return PartialView("ClientPartialReport", model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ReScheduleMeetings()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    int ClientId = 0;
                    if (profileName == "Client" || profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        if (profileName == "Client")
                        {
                            var role = await _clientService.GetClientRoleByEmailId(emailid);
                            ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
                        }
                        var meetings = await _meetingService.GetClientReScheduledMeetingList(ClientId, emailid);
                        meetings = meetings.Select(x =>
                        {
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            return x;
                        }).ToList();
                        if (HttpContext.Session.GetString("ReScheduleMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("ReScheduleMeetingsfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                            meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                                    && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        else
                        {
                            DateTime StartDate = DateTime.Now.AddMonths(-1);
                            meetings = meetings.Where(t => t.dtCreatedOn >= StartDate).ToList();
                        }
                        return View(meetings);
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ReScheduleMeetings(InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("ReScheduleMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("ReScheduleMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetReScheduleMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("ReScheduleMeetingsfilters");
                    return RedirectToAction("ReScheduleMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotScheduledMeetings(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("NotScheduledMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("NotScheduledMeetings", new { Page = Page, searchValue = searchValue });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetNotScheduleMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("NotScheduledMeetingsfilters");
                    return RedirectToAction("NotScheduledMeetings");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult InstaAddReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {
                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                if (model.QuestionsCollection == null)
                {
                    ViewBag.QuestionsDiscussedCount = 0;
                }
                else
                {
                    ViewBag.QuestionsDiscussedCount = model.QuestionsCollection?.Count();
                }
                ViewBag.IsClient = isClient;
                ViewBag.IsClientSelected = isClientSelected;
                model.PreviousMeetingStatus = model.MeetingStatus;
                GetStatusList(model, irfId);
                var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                ViewBag.PageStatus = "PublicExternal";
                ViewBag.PageReport = "Insta";
                if (model.IsProctor)
                {
                    return PartialView("ClientProctorPartialReport", model);
                }
                else
                {
                    return PartialView("ClientPartialReport", model);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllMeetings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicAllMeetings()
        {
            int? Page = 1;
            var meetings = new List<MeetingVM>();
            ViewBag.PageType = "Public";
            var menus = _userService.GetMenuBasedOnUserEmailld("ExternalUser").GroupBy(p => p.functionName).Select(g => g.First()).ToList();
            if (menus != null)
            {
                menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
            }
            ViewBag.ClientHRList = meetings.Select(x => x.ClientHrContact).Distinct();
            ViewBag.ClientNameList = meetings.Select(x => x.ClientName).Distinct();
            ViewBag.IRFList = meetings.Select(x => x.IRFId).Distinct();
            ViewBag.PrimarySkills = meetings.Select(x => x.JDTiltle).Distinct();
            ViewBag.MeetingStatus = meetings.Select(x => x.MeetingStatusStatusDesc).Distinct();
            ViewBag.ConfirmationStatus = meetings.Select(x => x.confirmationStatus).Distinct();
            ViewBag.InterviewStatus = meetings.Select(x => x.intervieweStatusStatDesc).Distinct();
            ViewBag.ApprovalStatus = meetings.Select(x => x.NextLevelApprovalStat).Distinct();
            ViewBag.FinalSelection = meetings.Select(x => x.FinalSelectionStat).Distinct();
            ViewBag.JoiningStatus = meetings.Select(x => x.JoiningStatusStatDesc).Distinct();
            ViewBag.OfferStatus = meetings.Select(x => x.OfferStatusStatDesc).Distinct();
            return View("AllMeetings", meetings.ToPagedList(Page ?? 1, 10));
        }

        [HttpPost]
        public async Task<JsonResult> PublicAllMeetings(string Emailid)
        {
            var role = await _clientService.GetClientRoleByEmailId(Emailid);
            if (role > 0)
            {
                return Json("success");
            }
            else
            {
                return Json("Emailid don't have Client access.");
            }
        }

        [HttpPost]
        public async Task<JsonResult> SendPublicURLClientUser(string Emailid)
        {
            try
            {
                Guid uniqueid = Guid.NewGuid();
                var _uniqueid = await _clientService.PublicClientUrlSave(Emailid, uniqueid.ToString());
                string sendreporturl = "https://" + Request.Host.ToString() + "/Candidate/PublicAllMeetings?id=" + uniqueid;
                if (!string.IsNullOrEmpty(_uniqueid))
                {
                    sendreporturl = "https://" + Request.Host.ToString() + "/Candidate/PublicAllMeetings?id=" + _uniqueid;
                }
                Mail _mail1 = new Mail
                {
                    TO = Emailid,
                    CC = "talent@risebird.io",
                    Subject = "Candidate Status Update"
                };
                var emailsend1 = await _notificationHelper.SendPublicUrlToClient(_mail1, sendreporturl);
                if (emailsend1.Contains("successfully"))
                    return Json("Success");
                else
                    return Json("Failed");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }

        [HttpPost]
        public async Task<JsonResult> CopyPublicURLClientUser(string Emailid)
        {
            try
            {
                Guid uniqueid = Guid.NewGuid();
                var _uniqueid = await _clientService.PublicClientUrlSave(Emailid, uniqueid.ToString());
                string sendreporturl = "https://" + Request.Host.ToString() + "/Candidate/PublicAllMeetings?id=" + uniqueid;
                if (!string.IsNullOrEmpty(_uniqueid))
                {
                    sendreporturl = "https://" + Request.Host.ToString() + "/Candidate/PublicAllMeetings?id=" + _uniqueid;
                }
                return Json(sendreporturl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ReScheduleMeetingstoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                int ClientId = 0;
                if (profileName == "Client")
                {
                    var role = await _clientService.GetClientRoleByEmailId(emailid);
                    ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                }
                var meetings = await _meetingService.GetClientReScheduledMeetingList(ClientId, emailid);
                meetings = meetings.Select(x =>
                {
                    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                    return x;
                }).ToList();
                if (HttpContext.Session.GetString("ReScheduleMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("ReScheduleMeetingsfilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                    }
                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                        && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                        && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                }
                else
                {
                    DateTime StartDate = DateTime.Now.AddMonths(-1);
                    meetings = meetings.Where(t => t.dtCreatedOn >= StartDate).ToList();
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Interview Date";
                Sheet.Cells[1, 2].Value = "Status";
                Sheet.Cells[1, 3].Value = "JD Title";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "IRF ID";
                Sheet.Cells[1, 7].Value = "Resume";
                Sheet.Cells[1, 8].Value = "Client HR";
                Sheet.Cells[1, 9].Value = "Job Country";
                int i = 2;
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.dtCreatedOn.ToString("MM/dd/yyyy HH:mm");
                        if (item.confirmationStatus == "2")
                        {
                            Sheet.Cells[i, 2].Value = "Cancelled";
                        }
                        else
                        {
                            Sheet.Cells[i, 2].Value = item.MeetingStatusStatusDesc;
                        }
                        Sheet.Cells[i, 3].Value = item.JDTiltle;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.IRFId;
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 7].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 7].Value = "Resume";
                                Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 7].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "No Resume";
                        }
                        Sheet.Cells[i, 8].Value = item.ClientHrContact;
                        Sheet.Cells[i, 9].Value = item.JobCountry;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> NotScheduledMeetingstoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                int ClientId = 0;
                if (profileName == "Client")
                {
                    var role = await _clientService.GetClientRoleByEmailId(emailid);
                    ViewBag.ClientRole = role != 0 ? ((ClientRoleEnum)Convert.ToInt32(role)).GetDescription() : string.Empty;
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                }
                var meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                    }

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                    meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                            && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                }
                else
                {
                    DateTime StartDate = DateTime.Now.AddMonths(-1);
                    DateTime EndDate = DateTime.Now.AddDays(1);
                    meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Added Date";
                Sheet.Cells[1, 2].Value = "Client Name";
                Sheet.Cells[1, 3].Value = "JD Title";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "IRF ID";
                Sheet.Cells[1, 7].Value = "Resume";
                Sheet.Cells[1, 8].Value = "Client HR";
                Sheet.Cells[1, 9].Value = "Job Country";
                int i = 2;
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.dtCreatedOn.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.ClientName;
                        Sheet.Cells[i, 3].Value = item.JDTiltle;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.IRFId;
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 7].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 7].Value = "Resume";
                                Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 7].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "No Resume";
                        }
                        Sheet.Cells[i, 8].Value = item.ClientHrContact;
                        Sheet.Cells[i, 9].Value = item.JobCountry;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RecruiterReport()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ViewBag.ButtonAccess = false;
                    if (client.Role == "Multiple IRF owner")
                    {
                        ViewBag.ButtonAccess = true;
                    }
                    DateTime st = DateTime.Now.AddMonths(-1);
                    DateTime se = DateTime.Now;
                    if (HttpContext.Session.GetString("RecruiterReportfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RecruiterReportfilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            st = filter.StartDate.Value;
                            se = filter.EndDate.Value.AddDays(1);
                        }
                    }
                    var model = await _clientService.GetRecruiterReportByClient(emailid, st, se);
                    model = model.Where(x => x.ClientHrContact != null && x.ClientHrContact != "").ToList();
                    if (TempData["RecruiterSuccessErrorMessage"] != null)
                    {
                        ViewBag.Message = TempData["RecruiterSuccessErrorMessage"].ToString();
                        TempData.Remove("RecruiterSuccessErrorMessage");
                    }
                    return View(model);
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
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RecruiterReport(InterviewFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Client")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("RecruiterReportfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("RecruiterReport");
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
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetRecruiterReportFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RecruiterReportfilters");
                    return RedirectToAction("RecruiterReport");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("RecruiterReport");
            }

        }
        [Authorize]
        [HttpGet]
        public JsonResult DisableRecruiter(int userid, int clientid, int Irfid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        if (_clientService.UpdateRecruiterStatus(userid, clientid, Irfid, false))
                        {
                            TempData["RecruiterSuccessErrorMessage"] = "Recruiter has been disabled Successfully!";
                            return Json("success");
                        }
                        else
                        {
                            TempData["RecruiterSuccessErrorMessage"] = "Failed to disable Recruiter!";
                            return Json("Failed");
                        }
                    }
                    else
                    {
                        return Json("Failed");
                    }
                }
                else
                {
                    return Json("Failed");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["RecruiterSuccessErrorMessage"] = "Recruiter disable Failed";
                return Json("Failed");
            }
        }
        [Authorize]
        [HttpGet]
        public JsonResult ActiveRecruiter(int userid, int clientid, int Irfid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        if (_clientService.UpdateRecruiterStatus(userid, clientid, Irfid, true))
                        {
                            TempData["RecruiterSuccessErrorMessage"] = "Recruiter has been actived Successfully!";
                            return Json("success");
                        }
                        else
                        {
                            TempData["RecruiterSuccessErrorMessage"] = "Failed to active Recruiter!";
                            return Json("Failed");
                        }
                    }
                    else
                    {
                        return Json("Failed");
                    }
                }
                else
                {
                    return Json("Failed");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["RecruiterSuccessErrorMessage"] = "Recruiter active Failed";
                return Json("Failed");
            }
        }

        public void SetBillingTypeDropDown(Client model)
        {
            var _list = new List<SelectListItem>();
            var selectItem = new SelectListItem { Text = "Billing Owner", Value = Convert.ToString(1) };
            _list.Add(selectItem);
            var selectItem1 = new SelectListItem { Text = "Skill Channel", Value = Convert.ToString(2) };
            _list.Add(selectItem1);
            var selectItem2 = new SelectListItem { Text = "Client", Value = Convert.ToString(3) };
            _list.Add(selectItem2);
            model.BillingTypeDrop = _list;
            var interviewduration = from InterviewDurationEnum s in Enum.GetValues(typeof(InterviewDurationEnum))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewDurationList = new SelectList(interviewduration, "ID", "Name");
        }

        public async Task<IActionResult> GetClientCommentsForReport(int interviewId, bool allowClientAction = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");

                    ClientCommentsForReport model = await _clientService.GetClientCommentsForReport(interviewId, emailId, profileName);
                    //ClientFeedbackCommentVM model = await _clientService.GetClientCommentsList(interviewId, emailId);

                    if (model == null)
                    {
                        model = new ClientCommentsForReport();
                    }

                    model.Profile = profileName;
                    model.allowClientAction = allowClientAction;
                    return PartialView("GetClientCommentsForReport", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Error retrieving data! Please try again.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUpdateClientCommentsForReport(ClientCommentsForReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var emailId = HttpContext.Session.GetString("email")?.Trim();
                        emailId = emailId.Replace("\"", "");
                        if (ModelState.IsValid)
                        {
                            if (string.IsNullOrEmpty(model.CreatedBy))
                            {
                                model.CreatedBy = emailId;
                            }

                            await _clientService.AddUpdateClientCommentsForReport(model);
                        }
                        return PartialView("GetClientCommentsForReport", model);
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
                return Json(false);
            }
        }

        public async Task<IActionResult> GetClientAudioInstructionsFeedbacks(int interviewId, string Irfid, string profile = null, string currentUser = null)
        {
            try
            {
                string profileName;
                string emailId;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await CreateAudioInstructionsModal(interviewId, profileName, emailId);
                    model.irfid = Irfid;
                    //var model = new ClientAudioInstructionsWrapper();
                    ////model.ClientAudioInstructionsList = await _clientService.GetClientAudioForIRF(interviewId, emailId, profileName);
                    //model.ClientAudioInstructionsList = await _clientService.GetClientInstructionsAudioForReport(interviewId, emailId, profileName);

                    //model.Profile = profileName;
                    //model.InterviewId = interviewId;
                    return PartialView("GetClientAudioInstructionsFeedbacks", model);
                }
                else if (!string.IsNullOrEmpty(profile) && !string.IsNullOrEmpty(currentUser))
                {
                    profileName = profile;
                    emailId = currentUser.Trim('\'');
                    var model = await CreateAudioInstructionsModal(interviewId, profileName, emailId);
                    return PartialView("GetClientAudioInstructionsFeedbacks", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Error retrieving data! Please try again.");
            }
        }

        public async Task<ClientAudioInstructionsWrapper> CreateAudioInstructionsModal(int interviewId, string profileName, string emailId)
        {
            var model = new ClientAudioInstructionsWrapper();
            //model.ClientAudioInstructionsList = await _clientService.GetClientAudioForIRF(interviewId, emailId, profileName);
            model.ClientAudioInstructionsList = await _clientService.GetClientInstructionsAudioForReport(interviewId, emailId, profileName);

            model.Profile = profileName;
            model.InterviewId = interviewId;
            return model;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientAudioFeedbackDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (TempData["Successfeedbackdetails"] != null)
                        {
                            ViewBag.Message = TempData["Successfeedbackdetails"].ToString();
                            TempData.Remove("Successfeedbackdetails");
                        }

                        var feedbackdetails = await _clientService.Getclientaudiofeedbackdetails();
                        return View(feedbackdetails);

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
                return RedirectToAction("SignOut", "Session");
            }
        }
    }
}