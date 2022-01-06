using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class AccountController : Controller
    {
        private IAzureStorageHelper _azureStorageHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IProjectService _projectService;
        private IAccountService _accountService;
        private IConfiguration _configuration;
        private IInterviewerService _interviewerService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;

        public AccountController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IAccountService accountService, IConfiguration configuration, IInterviewerService interviewerService, IFileMimeTypeHelper fileMimeTypeHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _accountService = accountService;
            _configuration = configuration;
            _interviewerService = interviewerService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var accountModel = new AccountViewModel();
                    var accountList = _accountService.GetAccounts(emailid, profileName).ToList();
                    accountList.ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());
                    accountModel.AccountList = accountList;

                    if (TempData["SuccessErrorMessageAccount"] != null)
                    {
                        accountModel.Message = TempData["SuccessErrorMessageAccount"].ToString();
                        TempData.Remove("SuccessErrorMessageAccount");
                    }

                    if (HttpContext.Session.GetString("AccountFilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<AccountFilter>(HttpContext.Session.GetString("AccountFilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            accountModel.AccountList = accountModel.AccountList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();
                        }

                        filter.Location = filter.Location == null ? filter.Location = accountModel.AccountList.Select(x => x.Location).ToArray() : filter.Location = filter.Location;
                        filter.AccountId = filter.AccountId == null ? filter.AccountId = accountModel.AccountList.Select(x => x.Name).ToArray() : filter.AccountId = filter.AccountId;
                        filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = accountModel.AccountList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                        filter.ClientName = filter.ClientName == null ? filter.ClientName = accountModel.AccountList.Select(x => x.EndClient).ToArray() : filter.ClientName = filter.ClientName;

                        accountModel.AccountList = accountModel.AccountList.Where(c => filter.Location.Contains(c.Location)
                        && filter.AccountId.Contains(c.Name)
                        && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                        && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.EndClient) : c.EndClient == null)).ToList();

                        if (accountModel.AccountList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                        {
                            accountModel.AccountList = accountList;
                            accountModel.AccountList = accountModel.AccountList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();

                        }

                    }

                    if (profileName != null && profileName.Contains("Client"))
                    {
                        accountModel.IsClientUser = true;
                    }
                    return View(accountModel);
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
        public IActionResult Index(AccountFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("AccountFilters", JsonConvert.SerializeObject(filter));

                }
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var model = new AccountViewModel();
                    var accountList = _accountService.GetAccounts(emailid, profileName).ToList();
                    accountList.ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());
                    model.AccountList = accountList;

                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessage"].ToString();
                        TempData.Remove("SuccessErrorMessage");
                    }

                    filter = JsonConvert.DeserializeObject<AccountFilter>(HttpContext.Session.GetString("AccountFilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        model.AccountList = model.AccountList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();
                    }

                    filter.Location = filter.Location == null ? filter.Location = model.AccountList.Select(x => x.Location).ToArray() : filter.Location = filter.Location;
                    filter.AccountId = filter.AccountId == null ? filter.AccountId = model.AccountList.Select(x => x.Name).ToArray() : filter.AccountId = filter.AccountId;
                    filter.ArchiveStatus = filter.ArchiveStatus == null ? filter.ArchiveStatus = model.AccountList.Select(x => x.ArchiveStatus).ToArray() : filter.ArchiveStatus;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = model.AccountList.Select(x => x.EndClient).ToArray() : filter.ClientName = filter.ClientName;

                    model.AccountList = model.AccountList.Where(c => filter.Location.Contains(c.Location)
                    && filter.AccountId.Contains(c.Name)
                    && (filter.ArchiveStatus.Count() > 0 ? filter.ArchiveStatus.Contains(c.ArchiveStatus) : c.ArchiveStatus == null)
                    && (filter.ClientName.Count() > 0 ? filter.ClientName.Contains(c.EndClient) : c.EndClient == null)).ToList();

                    if (model.AccountList.Count() == 0 && filter.StartDate != null && filter.EndDate != null)
                    {
                        model.AccountList = accountList;
                        model.AccountList = model.AccountList.Where(t => t.CreatedOn.Date >= filter.StartDate && t.CreatedOn.Date <= filter.EndDate).ToList();
                    }
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        model.IsClientUser = true;
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

        [HttpGet]
        public async Task<IActionResult> ResetAccountFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("AccountFilters");
                    return RedirectToAction("Index");
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

        [Authorize]
        [HttpGet]
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var accountModel = new Account();
                accountModel = _accountService.GetAccount();

                return View(accountModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(Account model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_accountService.AddAccount(model).Result)
                    {
                        TempData["SuccessErrorMessageAccount"] = "Account has been added Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Failed to add Account!";
                        return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageProject"] = "Failed to add Account!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var accountModel = _accountService.GetAccountById(id);
                accountModel.ClientList = new SelectList(_projectService.GetClients(), "id", "Name");
                return View(accountModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Account model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    //var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = profileName.Replace("\"", "");
                    if (_accountService.UpdateAccount(model))
                    {
                        TempData["SuccessErrorMessageAccount"] = "Account has been edited Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageAccount"] = "Failed to edit Account!";
                        return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageAccount"] = "Failed to edit Account!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ArchiveAccount(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_accountService.ArchiveAccount(id).Result)
                        {
                            TempData["SuccessErrorMessageAccount"] = "Account has been archived Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageAccount"] = "Sorry! Archiving account failed.";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageAccount"] = "Sorry! Archiving account failed.";
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult UnArchiveAccount(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (_accountService.UnArchiveAccount(id).Result)
                        {
                            TempData["SuccessErrorMessageAccount"] = "Account has been restored Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageAccount"] = "Failed to restore Account!";
                            return RedirectToAction("Index");
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageAccount"] = "Failed to restore Account!";
                return RedirectToAction("Index");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFilteredProjects(int clientId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        var accountModel = new Account();
                        var projectList = _accountService.GetProjectsbyClientId(clientId);
                        accountModel.AccountProjectMappings = (IList<AccountProjectMapping>)(projectList);
                        if (accountModel.AccountProjectMappings.Count() == 0)
                        {
                            return new EmptyResult();
                        }

                        return PartialView("FilteredProjects", accountModel);
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
                return null;
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetEditFilteredProjects(int clientId, int accountId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        string projectList = string.Empty;
                        var accountModel = new Account();
                        var accountCluster = _accountService.GetAccountCluster(accountId);
                        if (accountCluster != null && accountCluster.Count() != 0)
                        {
                            accountCluster = accountCluster.Where(x => x.ClientId == clientId);
                            projectList = string.Join(",", from item in accountCluster select item.ProjectId);

                        }
                        var remainingProjects = _accountService.GetProjectbyClientId(clientId, projectList);


                        var objList = new List<AccountProjectMapping>();
                        objList.AddRange(accountCluster);
                        objList.AddRange(remainingProjects);

                        accountModel.AccountProjectMappings = (IList<AccountProjectMapping>)(objList);
                        if (accountModel.AccountProjectMappings.Count() == 0)
                        {
                            return new EmptyResult();
                        }

                        return PartialView("FilteredProjects", accountModel);
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
                return null;
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = new InterviewerProfileVM();
                var model1 = _accountService.FetchUserProfile(emailid);

                if (profileName == "Interviewer")
                {
                    model = await _interviewerService.GetInterviewerProfileByEmailId(emailid, profileName);
                    model.WhatsappNumber = model1.WhatsappNumber;
                    model.ImageURL = model1.ImageURL;
                }
                else
                {
                    model = model1;
                }
                if (TempData["MyProfileMessage"] != null)
                {
                    ViewBag.Message = TempData["MyProfileMessage"].ToString();
                    TempData.Remove("MyProfileMessage");
                }
                GetAllSelectList(model);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UserProfile(InterviewerProfileVM userProfile)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                string uploadedFileURL = string.Empty;
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                userProfile.EmailId = emailid;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (userProfile.ImageUpload != null)
                {
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(userProfile.ImageUpload);
                    if (fileMimeType == "image/ief" || fileMimeType == "image/jpeg"
                        || fileMimeType == "image/tiff")
                    {
                        await _azureStorageHelper.InsertBlob(userProfile.ImageUpload.FileName, "profileimages", userProfile.ImageUpload);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("profileimages");
                        var blob = containerRef.GetBlockBlobReference(userProfile.ImageUpload.FileName);
                        uploadedFileURL = blob.Uri.ToString();
                        userProfile.ImageURL = uploadedFileURL;
                    }
                   
                }
                if (profileName == "Interviewer")
                {
                    
                    if (userProfile.ResumeFile != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(userProfile.ResumeFile);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(userProfile.ResumeFile.FileName, "resumes", userProfile.ResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(userProfile.ResumeFile.FileName);
                            uploadedFileURL = blob.Uri.ToString();
                            userProfile.ResumeLink = uploadedFileURL;
                        }
                        
                    }
                    if (userProfile.CodedResumeFile != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(userProfile.CodedResumeFile);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(userProfile.CodedResumeFile.FileName.Replace(" ", "_"), "resumes", userProfile.CodedResumeFile);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(userProfile.CodedResumeFile.FileName.Replace(" ", "_"));
                            uploadedFileURL = blob.Uri.ToString();
                            userProfile.CodedResumeLink = uploadedFileURL;
                        }

                        

                    }
                    if (_interviewerService.AddInterviewerMyProfile(userProfile).Result)
                    {
                        if (!String.IsNullOrEmpty(userProfile.ImageURL))
                        {
                            HttpContext.Session.SetString("currentUserimage", userProfile.ImageURL);
                        }
                        TempData["MyProfileMessage"] = "Profile has been updated Successfully!";
                        return RedirectToAction("UserProfile");
                    }
                    else
                    {
                        TempData["MyProfileMessage"] = "Failed to update Profile!";
                        return RedirectToAction("UserProfile");
                    }
                }
                else
                {
                    if (_accountService.UpdateUserProfile(userProfile).Result)
                    {
                        if (!String.IsNullOrEmpty(userProfile.ImageURL))
                        {
                            HttpContext.Session.SetString("currentUserimage", userProfile.ImageURL);
                        }
                        TempData["MyProfileMessage"] = "Profile has been updated Successfully!";
                        return RedirectToAction("UserProfile");
                    }
                    else
                    {
                        TempData["MyProfileMessage"] = "Failed to update Profile!";
                        return RedirectToAction("UserProfile");
                    }
                }
            }
            return View();

        }

        [HttpGet]
        private void GetAllSelectList(InterviewerProfileVM model)
        {
            var Weekday = from Weekday s in Enum.GetValues(typeof(Weekday))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekDayList = new SelectList(Weekday, "ID", "Name");

            var Weekend = from Weekend s in Enum.GetValues(typeof(Weekend))
                          select new { ID = (int)s, Name = s.GetDescription() };
            model.WeekEndList = new SelectList(Weekend, "ID", "Name");
        }
    }
}