using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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
        private readonly CancellationToken _cancellationToken;


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
                        if(vm.LastAction!=null)
                        {
                            if(vm.LastAction.ToLower()=="start")
                            {
                                vm.StatusDesc = "Starting";
                            }
                            else if(vm.LastAction.ToLower() == "poweroff")
                            {
                                vm.StatusDesc = "Stopping";
                            }

                        }
                        else
                        {
                            vm.StatusDesc = vm.Status.HasValue ? ((VMState)vm.Status).GetDescription() : string.Empty;

                        }
                    }
                    
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

        [HttpPost]
        public async Task<bool> StartStopVM(int vmId,string name, string rgName, string action)
        {
            try
            {

                if (name != null && rgName != null && action != null)
                {
                    var model = new VMActionDTO();
                    model.VMPropertyId = vmId;
                    model.Name = name;
                    model.ResourceGroupName = rgName;
                    model.ActionCommand = action;
                    var path = $"http://localhost:7071/api/StartStopVM";
                    //var path = $"https://risebirdcognitiveservice.azurewebsites.net/api/SentimentAnalysis";
                    //var functionKey = "jiNbkIadJuGB/ms6U4KIWBmFdCTh6DLLku8MrEaaNCLjquJAkZSHbw==";

                    HttpClient httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                    var response = await httpClient.SendRequestAsync(HttpMethod.Post, path, model, "", "", _cancellationToken);
                    // return response.Content.ReadAsStringAsync().Result;
                    if(response.Content.ReadAsStringAsync().Result.ToLower().Contains("success"))
                    {
                        return true;
                    }

                }
                return false;

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

    }
}
