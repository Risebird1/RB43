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

        //[Authorize]
        //[HttpGet]
        //public IActionResult Add()
        //{
        //    if (HttpContext.Session.GetString("email") != null)
        //    {

        //        var accountModel = new Account();
        //        var irfList = _projectService.GetAllIRF();
        //        var objList = new List<ProjectIRFMapping>();
        //        foreach (var item in irfList)
        //        {

        //            var obj = new ProjectIRFMapping();
        //            obj.IRFId = item.id;
        //            obj.IRFName = item.IRFId;
        //            obj.IsAdded = false;
        //            //obj.id = 0;
        //            obj.PrimarySkills = item.JDTiltle.Split("|")[0];
        //            //obj.ProjectId = 0;
        //            obj.Client = item.ClientCompName;
        //            objList.Add(obj);
        //        }
        //        projectModel.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);

        //        ViewBag.Clients = new SelectList(_projectService.GetClients(), "id", "Name");
        //        return View(projectModel);
        //    }
        //    else
        //    {
        //        return RedirectToAction("SignOut", "Session");
        //    }
        //}
    }
}