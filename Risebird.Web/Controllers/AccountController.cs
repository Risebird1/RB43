using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AccountController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IAccountService accountService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _accountService = accountService;
            
        }
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
                    var accountList = _accountService.GetAllAccounts().ToList();
                    accountList.ForEach(x => x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription());
                    if (profileName != null && profileName.Contains("Client"))
                    {
                        ViewBag.IsClientUser = true;
                        //accountList.ToList().ForEach(c => c.IsClientUser = true);
                    }
                    else
                    {
                        ViewBag.IsClientUser = false;
                    }
                    if (TempData["SuccessErrorMessageAccount"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageAccount"].ToString();
                        TempData.Remove("SuccessErrorMessageAccount");
                    }
                    return View(accountList);
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
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var accountModel = new Account();
                accountModel = _accountService.GetAccount();
                //var irfList = _projectService.GetAllIRF();
                //var objList = new List<ProjectIRFMapping>();
                //foreach (var item in irfList)
                //{

                //    var obj = new ProjectIRFMapping();
                //    obj.IRFId = item.id;
                //    obj.IRFName = item.IRFId;
                //    obj.IsAdded = false;
                //    //obj.id = 0;
                //    obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                //    //obj.ProjectId = 0;
                //    obj.Client = item.ClientCompName;
                //    objList.Add(obj);
                //}
                //projectModel.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);

                //ViewBag.Clients = new SelectList(_projectService.GetClients(), "id", "Name");
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
                //ViewBag.Clients = new SelectList(_projectService.GetClients(), "id", "Name");
                //ViewBag.id = id;
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
    }
}