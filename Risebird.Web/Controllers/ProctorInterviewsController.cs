using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly HttpClient _httpClient;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IProctorInterviewsService _proctorInterviewsService;
        private readonly CancellationToken _cancellationToken;
        private readonly HttpClient _httpClient;


        public ProctorInterviewsController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IProctorInterviewsService proctorInterviewsService, HttpClientHelper factory)
        {
            _httpClient = factory.GetHttpClient();
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _proctorInterviewsService = proctorInterviewsService;
            _httpClient = factory.GetHttpClient();

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
                    model.VMList = (await _proctorInterviewsService.GetAllUserVMs(emailid, profileName)).ToList();
                    foreach (var vm in model.VMList)
                    {
                        if(vm.LastAction!=null && vm.LastAction!="Added")
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
                    if (TempData["SuccessErrorMessageProctor"] != null)
                    {
                        model.Message = TempData["SuccessErrorMessageProctor"].ToString();
                        TempData.Remove("SuccessErrorMessageProctor");
                    }
                    foreach (var vm in model.VMList)
                    {
                        if (vm.StatusDesc.ToLower()=="running" || vm.StatusDesc.ToLower() == "starting")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Green-White.png";

                        }
                        else if (vm.StatusDesc.ToLower() == "stopping" || vm.StatusDesc.ToLower()=="deallocating")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                        }
                        else if (vm.StatusDesc.ToLower() == "stopped" || vm.StatusDesc.ToLower() == "deallocated")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                        }
                    }
                    model.Profile = profileName;
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
                    var path = $"https://risebirdmicroservice.azurewebsites.net/api/StartStopVM";
                    var functionKey = "D1fJaqGva3a/p9biA7clekVxAyfOialCKpO3SHpgHCP3WdBa4lOBnA==";

                    var response = await _httpClient.SendRequestAsync(HttpMethod.Post, path, model, functionKey, "", _cancellationToken);
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

        public async Task<IActionResult> GetNonExistingVMs()
        {
            try
            {
                var model = new NonExistingVMViewModel();
                model.NonExistingVMList = ((IList<NonExistingVMDTO>)await _proctorInterviewsService.GetNonExistingVMs());
                return PartialView("GetNonExistingVMsPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddNonExistingVMs(NonExistingVMViewModel model)
        {
            try
            {
                var result = await _proctorInterviewsService.AddNonExistingVMs(model.NonExistingVMList.Where(x => x.IsSelected == true).ToList());
                TempData["SuccessErrorMessageProctor"] = result;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRDPFile(int vmId)
        {
            try
            {

                if (vmId!=0)
                {
                    
                    var path = $"https://risebirdmicroservice.azurewebsites.net/api/GetRDPFile";
                    var functionKey = "rE8wOZBltuOTe44IBcAY28zAydprjs/WjO8tONe4mb2HIOGwtO8onw==";

                    var response =await _httpClient.SendRequestAsync(HttpMethod.Get, path, vmId, functionKey, "", _cancellationToken);
                    if(response!=null)
                    {
                        var rdpfile = response.Content.ReadAsStringAsync().Result;
                        if (rdpfile != null)
                        {
                            rdpfile = rdpfile.Replace("\"", "").Replace("\\", "\"");
                            var file = JsonConvert.DeserializeObject<RdpFileModel>(rdpfile);
                            var filecontent = JsonConvert.DeserializeObject<List<byte>>(file.FileContent.ToString());
                            var obj = new RdpFileModel(file.FileName, filecontent);
                            return File(((List<byte>)obj.FileContent).ToArray<byte>(),
                               System.Net.Mime.MediaTypeNames.Application.Octet, obj.FileName);
                        }
                        else
                        {

                            TempData["SuccessErrorMessageProctor"] = "Failed!";
                            return RedirectToAction("Index");
                        }

                    }
                    else
                    {
                        TempData["SuccessErrorMessageProctor"] = "Failed!";
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
                TempData["SuccessErrorMessageProctor"] = "Failed!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string name, string rgName, int vmId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "Recruiter" || profileName == "CuratedUser")
                    {
                        var model = new VMResetPassword();
                        model.VmName = name;
                        model.ResourceGroupName = rgName;
                        model.VmId = vmId;
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(VMResetPassword model)
        {
            try
            {
                var result = await _proctorInterviewsService.ResetPassword(model);
                if(result.ToLower().Contains("success"))
                {
                    TempData["SuccessErrorMessageProctor"] = "Password reset successful for " + model.VmName;

                }
                else
                {
                    TempData["SuccessErrorMessageProctor"] = "Password reset failed for " + model.VmName;

                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditNamePassword(int id, string customName, string password, string vmName)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                       if(await _proctorInterviewsService.UpdateVMNamePassword(id, customName, password))
                        {
                            TempData["SuccessErrorMessageProctor"] = "Successfully saved data for " +vmName;
                            return Json(true);
                        }
                        else
                        {
                            TempData["SuccessErrorMessageProctor"] = "Failed to save the data for " + vmName;
                            return Json(false);

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
                TempData["SuccessErrorMessageProctor"] = "Failed to save the data for " + vmName;
                return RedirectToAction("SignOut", "Session");
            }
        }
    }
}
