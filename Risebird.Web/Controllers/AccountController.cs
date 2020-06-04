using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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

namespace Risebird.Web.Controllers
{
    public class AccountController : Controller
    {
        private IAzureStorageHelper _azureStorageHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IProjectService _projectService;
        private IAccountService _accountService;
        private IConfiguration _configuration;

        public AccountController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IAccountService accountService, IConfiguration configuration)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _accountService = accountService;
            _configuration = configuration;
        }

        [Authorize]
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
                        TempData["SuccessErrorMessageAccount"] = "Account added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProject"] = "Adding Account has Failed";
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
                TempData["SuccessErrorMessageProject"] = "Adding Account has failed";
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
                        TempData["SuccessErrorMessageAccount"] = "Account edited Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageAccount"] = "Editing Account has Failed";
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
                TempData["SuccessErrorMessageAccount"] = "Editing Account has failed";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
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
                            TempData["SuccessErrorMessageAccount"] = "Account archived Successfully.";
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
                            TempData["SuccessErrorMessageAccount"] = "Account restored Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageAccount"] = "Account restore Failed";
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
                TempData["SuccessErrorMessageAccount"] = "Account restore Failed";
                return RedirectToAction("Index");
            }

        }

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
        [HttpPost]
        public JsonResult ChatBox()
        {
            var user = string.Empty;
            if (HttpContext.Session.GetString("email") == null)
            {

            }
            else
            {
                user = HttpContext.Session.GetString("email")?.Trim();
                user = user.Replace("\"", "");
            }
            var resultset = GetConversation(user, "", "Chat","");
            var _lst = resultset.Select(x => x.Sender.ToUpper()).Distinct();
            var _lstset = new List<Conversation>();
            foreach (var item in _lst)
            {
                var Isonline = resultset.Where(x => x.Sender.ToUpper() == item).Select(x => x.IsOnline).FirstOrDefault();
                var Whosend = resultset.Where(x => x.Sender.ToUpper() == item).Select(x => x.WhoSend).FirstOrDefault();
                _lstset.Add(new Conversation { Sender = item, IsOnline = Isonline,WhoSend= Whosend});
            }
            ViewBag.UserList = _lstset;
            ViewBag.Self = user.ToUpper();
            return Json(new { result = _lstset, Self = user.ToUpper() });
        }
        public List<Conversation> GetConversation(string userid, string contact, string status,string WhoSend)
        {
            string connectionString = _configuration["ConnectionString:DBConnectionString"];
            if (String.IsNullOrEmpty(userid))
            {
                if (HttpContext.Session.GetString("email")?.Trim() == null)
                {
                    userid = "";
                }
                else
                {
                    userid = HttpContext.Session.GetString("email")?.Trim();
                }
            }
            if (String.IsNullOrEmpty(contact))
            {
                contact = "";
            }
            userid = userid.Replace("\"", "");
            contact = contact.Replace("\"", "");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@id", userid);
                parameters.Add("@contact", contact);
                if (status == "Chat")
                {
                    var result = SqlMapper.Query<Conversation>(con, "usp_GetConversation", parameters,
                          commandType: CommandType.StoredProcedure);
                    foreach (var item in result)
                    {
                        item.Sender = item.Sender.ToUpper();
                        item.Reciver = item.Reciver.ToUpper();
                    }
                    return (List<Conversation>)result;
                }
                else
                {
                    parameters.Add("@WhoSend", WhoSend);
                    var result = SqlMapper.Query<Conversation>(con, "usp_GetConversationdetails", parameters,
                           commandType: CommandType.StoredProcedure);
                    foreach (var item in result)
                    {
                        item.Sender = item.Sender.ToUpper();
                        item.Reciver = item.Reciver.ToUpper();
                        if(item.Sender==userid.ToUpper())
                        {
                            item.self = 1;
                        }
                        else
                        {
                            item.self = 0;
                        }
                    }
                    return (List<Conversation>)result;
                }

            }
        }
        public void AddConversation(string userid, string contact, string message)
        {
            string connectionString = _configuration["ConnectionString:DBConnectionString"];
            if (String.IsNullOrEmpty(userid))
            {
                userid = HttpContext.Session.GetString("email")?.Trim();
            }
            userid = userid.Replace("\"", "");
            contact = contact.Replace("\"", "");
            string WhoSend = "U";
            if(contact.Contains("@"))
            {
                WhoSend = "U";
            }
            else
            {
                WhoSend = "G";
            }
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@id", userid);
                parameters.Add("@contact", contact);
                parameters.Add("@message", message);
                parameters.Add("@WhoSend", WhoSend);

                var result = SqlMapper.Query(con, "usp_AddConversation", parameters,
                        commandType: CommandType.StoredProcedure);
                // return result;
            }
        }

    }
}