using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    public class ProctorInterviewsController : Controller
    {

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IProctorInterviewsService _proctorInterviewsService;

        public ProctorInterviewsController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IProctorInterviewsService proctorInterviewsService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _proctorInterviewsService = proctorInterviewsService;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser")
                {
                    var model = new VirtualMachineVM();
                    model.VMList = (await _proctorInterviewsService.GetAllVMs()).ToList();
                    foreach (var vm in model.VMList)
                    {
                        vm.StatusDesc = vm.Status.HasValue ? ((VMState)vm.Status).GetDescription() : string.Empty;
                    }
                    //var irfList = _irfService.GetAllIRFs(emailid, profileName);
                    //irfList.ToList().ForEach(x => { x.ArchiveStatus = ((ArchiveStatus)Convert.ToInt16(x.IsDeleted)).GetDescription(); x.SubscriptionStatus = ((SubscriptionStatus)Convert.ToInt16(x.IsSubscribed)).GetDescription(); });
                    //foreach (var item in model.IRFList)
                    //{
                    //    if (item.IsPaused)
                    //    {
                    //        item.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                    //        item.IRFStatus = "Paused";
                    //    }
                    //    else if (item.IsDeleted)
                    //    {
                    //        item.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                    //        item.IRFStatus = "Archived";
                    //    }
                    //    else
                    //    {
                    //        item.ImageUrl = "/img/Cards_Png/01_Green-White.png";
                    //        item.IRFStatus = "Active";
                    //    }
                    //}


                    //if (TempData["SuccessErrorMessage"] != null)
                    //{
                    //    model.Message = TempData["SuccessErrorMessage"].ToString();
                    //    TempData.Remove("SuccessErrorMessage");
                    //}

                    //model.Profile = profileName;
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
    }
}
