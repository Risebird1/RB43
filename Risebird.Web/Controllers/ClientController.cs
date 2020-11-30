using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private IInterviewerService _interviewerService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IUserRepository _userRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        public ClientController(IClientService clientService, IApplicationErrorLogger applicationErrorLogger, IInterviewerService interviewerService, IUserRepository userRepository, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper)
        {
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
            _interviewerService = interviewerService;
            _userRepository = userRepository;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
        }

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
                    return View(_clientService.GetClients());
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
                try
                {
                    var model = new Client();
                    return View("ClientAdd", model);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return RedirectToAction("List");
                }

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
                        if (client.LogoFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(client.LogoFile.FileName, "clientlogos", client.LogoFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientlogos");
                            var blob = containerRef.GetBlockBlobReference(client.LogoFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            client.LogoLink = uploadedFileURL;
                        }
                        if (_clientService.AddClient(client))
                        {
                            TempData["SuccessErrorMessage"] = "Client is added Successfully";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding Client is Failed";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
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
                    Client clientToUpdate = _clientService.GetClient(id);

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
                        if (client.LogoFile != null)
                        {
                            await _azureStorageHelper.InsertBlob(client.LogoFile.FileName, "clientlogos", client.LogoFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("clientlogos");
                            var blob = containerRef.GetBlockBlobReference(client.LogoFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            client.LogoLink = uploadedFileURL;
                        }
                        if (_clientService.UpdateClient(client))
                        {
                            TempData["SuccessErrorMessage"] = "Client is updated Successfully";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding Client is Failed";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
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

        public async Task<IActionResult> DeleteClient(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_clientService.DeleteClient(id))
                    {
                        TempData["SuccessErrorMessage"] = "Client is deleted Successfully";
                        return Json(new { url = Url.Action("List", "Client") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Deleting Client is Failed";
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
        public IActionResult CreateJd(ClientDetails fillters)
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
                        var client = _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.Result.ClientId;
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
        /// <summary>
        /// Craete New Jd
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult AddCreateJd(ClientDetails model)
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
                        if (!String.IsNullOrEmpty(model._clientjddetails.ClientEmail))
                        {
                            var client = _userRepository.GetClientUserDetails(model._clientjddetails.ClientEmail);
                            if (client.Result != null)
                            {
                                model._clientjddetails.ClientId = client.Result.ClientId;
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
                        _azureStorageHelper.InsertBlob(model._clientjddetails.JdFile.FileName, "videos", model._clientjddetails.JdFile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                        var blob = containerRef.GetBlockBlobReference(model._clientjddetails.JdFile.FileName);
                        string uploadedvideoFileURL = blob.Uri.ToString();
                        model._clientjddetails.Filelink = uploadedvideoFileURL;
                    }

                    var result = _clientService.AddClientJd(model._clientjddetails);
                    if (result != "")
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Client Jd is created Successfully";
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterviewer"] = "Client Jd creation is Failed";
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
        public IActionResult CandidateJDScore(CandidateCuratedScoreVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var newcandidatecount = model.CandidateCuratedScores.Where(x => x.IRFId == 0).Count();
                var result = _clientService.AddCandidateJDScore(model.IRFId, model.CandidateCuratedScores.ToList(), model.candidateProfile);
                if (result != "")
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate is Updated Successfully";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate update is Failed";
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
                        var resultset = _notificationHelper.CandidateJDScoreNotification(_mail, newcandidatecount, model.JDTitle);
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
        public JsonResult EditJd(int assessmentid)
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
                        var client = _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.Result.ClientId;
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
        public IActionResult CandidateRelatedJD(int interviewid)
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
                        var client = _userRepository.GetClientUserDetails(emailid);
                        ClientId = client.Result.ClientId;
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
        public IActionResult RisebirdPay()
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
                    var client = _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.Result.ClientId;
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
        public JsonResult WalletDetails(string action)
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
                    var client = _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.Result.ClientId;
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
        public JsonResult AddWalletPoints(int points, string comment)
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
                    var client = _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.Result.ClientId;
                    isClientUser = true;
                    var result = _clientService.AddWalletPoints(ClientId, emailid, points, comment);
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
        public IActionResult ResetCandidate(int Clientjdid, string candidateEmail)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var result = _clientService.ResetCandidate(Clientjdid, candidateEmail, emailid);
                if (result != "")
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate reset Successfully";
                }
                else
                {
                    TempData["SuccessErrorMessageInterviewer"] = "Candidate reset is Failed";
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
    }
}