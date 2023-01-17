using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        private IProfileService _profileService;
        private readonly IVmHelper _vmhelper;

        public ProctorInterviewsController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IProctorInterviewsService proctorInterviewsService, HttpClientHelper factory, IProfileService profileService, IVmHelper vmhelper)
        {
            _httpClient = factory.GetHttpClient();
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _proctorInterviewsService = proctorInterviewsService;
            _profileService = profileService;
            _vmhelper = vmhelper;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (emailid == "interview@risebird.io")
                {
                    profileName = "Admin";
                }
                if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "Candidate" || profileName == "ResumeEvaluator" || profileName == "Client" || profileName == "QualityController" || profileName == "CuratedUser" || profileName == "ExternalUser" || profileName == "Recruiter")
                {
                    var model = new VirtualMachineVM();
                    var vmList = (await _proctorInterviewsService.GetAllUserVMs(emailid, profileName)).ToList();
                    foreach (var vm in vmList)
                    {
                        if (vm.LastAction != null && vm.LastAction != "Added")
                        {
                            if (vm.LastAction.ToLower() == "start")
                            {
                                vm.StatusDesc = "Starting";
                            }
                            else if (vm.LastAction.ToLower() == "deallocate")
                            {
                                vm.StatusDesc = "Deallocating";
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
                    foreach (var vm in vmList)
                    {
                        if (vm.StatusDesc.ToLower() == "running" || vm.StatusDesc.ToLower() == "starting")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Green-White.png";

                        }
                        else if (vm.StatusDesc.ToLower() == "stopping" || vm.StatusDesc.ToLower() == "deallocating")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Grey-White.png";
                        }
                        else if (vm.StatusDesc.ToLower() == "stopped" || vm.StatusDesc.ToLower() == "deallocated")
                        {
                            vm.ImageUrl = "/img/Cards_Png/01_Black-White.png";
                        }
                    }
                    model.VMList = vmList.Where(x => x.Status.HasValue && x.Status == (int)VMState.running).ToList();
                    model.VMList.AddRange(vmList.Where(x => !x.Status.HasValue || x.Status != (int)VMState.running).ToList());
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
        public async Task<bool> StartStopVM(int vmId, string name, string rgName, string action)
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
                    var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/StartStopVM";
                    var functionKey = "dzI10CMPKBEqBJchDE1jQ/klyimLhWISbH7q0aDZZHtI8K8sBu8THg==";

                    var response = await _httpClient.SendRequestAsync(HttpMethod.Post, path, model, functionKey, "", _cancellationToken);
                    if (response.Content.ReadAsStringAsync().Result.ToLower().Contains("success"))
                    {
                        //save the email of the performer
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        await _proctorInterviewsService.UpdateActionPerformed(vmId, emailid, action);
                        TempData["SuccessErrorMessageProctor"] = $"Successfully performed {action} action for {name}!";
                        return true;
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProctor"] = $"Failed to perform {action} action for {name}!";

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

        [HttpGet]
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

                if (vmId != 0)
                {

                    var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/GetRDPFile";
                    var functionKey = "lkxa2k0TT1WseCpCIAONXwvh3q5GIx9cvO3h68x1N3jSePNdcwH50A==";

                    var response = await _httpClient.SendRequestAsync(HttpMethod.Get, path, vmId, functionKey, "", _cancellationToken);
                    if (response != null)
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
                    if (emailid == "interview@risebird.io")
                    {
                        profileName = "Admin";
                    }
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
                if (result.ToLower().Contains("success"))
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
        public async Task<IActionResult> EditNamePassword(int id, string customName, string password, string vmName, string userName)
        {

            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    emailid = emailid.Replace("\"", "");
                    if (emailid == "interview@risebird.io")
                    {
                        profileName = "Admin";
                    }
                    if (profileName == "Admin")
                    {
                        if (await _proctorInterviewsService.UpdateVMNamePassword(id, customName, password, userName))
                        {
                            TempData["SuccessErrorMessageProctor"] = "Successfully saved data for " + vmName;
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

        [Authorize]
        [HttpGet]
        public IActionResult VMUsers(int vmId, string vmName)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var model = new VMUsersMapping
                {
                    VmId = vmId,
                    VmName = vmName,
                    UsersList = (IList<TransVMUser>)(_proctorInterviewsService.GetUserList(vmId, (int)ProfileEnum.HR)),
                    ProfileList = new SelectList(_profileService.GetProfile().Where(x => x.id != (int)ProfileEnum.Admin), "id", "Name", (int)ProfileEnum.HR)
                };
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public IActionResult GetUserList(int profileId, int vmId)
        {
            if (HttpContext.Session.GetString("email") != null)
            {

                var model = new VMUsersMapping
                {
                    UsersList = (IList<TransVMUser>)(_proctorInterviewsService.GetUserList(vmId, profileId))
                };
                return PartialView("VMUsersPartial", model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateVMUsers(int[] userList, int vmId, string vmName, int profileId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (await _proctorInterviewsService.UpdateVMUsers(userList, vmId, profileId))
                    {
                        TempData["SuccessErrorMessageProctor"] = "Users for the VM " + vmName + " updated successfully!";
                        return Json(true);
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProctor"] = "Saving users for the VM " + vmName + " failed!";
                        return Json(false);
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
                TempData["SuccessErrorMessageProctor"] = "Saving users for the VM " + vmName + " failed!";
                return Json(false);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProctorVMQuestions(int vmId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ProctorQuestionsViewModel model = new ProctorQuestionsViewModel();
                    model.VmId = vmId;
                    model.QuestionList = (IList<ProctorVMQuestions>)(await _proctorInterviewsService.GetVMQuestions(vmId));
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
                return RedirectToAction("SignOut", "Session");

            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProctorVMQuestions(ProctorQuestionsViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (await _proctorInterviewsService.UpdateVMQuestions(model))
                    {
                        TempData["SuccessErrorMessageProctor"] = "Questions has been updated Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageProctor"] = "Failed to save the Questions!";
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
                TempData["SuccessErrorMessageProctor"] = "Failed to save the Questions!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CandidateQuestions(int vmId, string vmName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    //profileName = "Candidate";
                    profileName.Replace("\"", "");

                    ProctorQuestionsViewModel model = new ProctorQuestionsViewModel();
                    model.VmId = vmId;
                    model.QuestionList = (IList<ProctorVMQuestions>)(await _proctorInterviewsService.GetVMQuestions(vmId));
                    foreach (var item in model.QuestionList)
                    {
                        item.LevelDesc = ((QuestionLevel)item.Level).GetDescription();
                    }
                    if (model.QuestionList != null)
                    {
                        model.TotalQuestions = model.QuestionList.Count();
                    }
                    model.Profile = profileName;

                    if (HttpContext.Session.GetString("CurrentQuestion") != null)
                    {
                        model.CurrentQuestionIndex = Convert.ToInt32(HttpContext.Session.GetString("CurrentQuestion"));
                    }
                    //HttpContext.Session.Remove("CurrentQuestion");
                    model.VMName = vmName;

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
                return RedirectToAction("SignOut", "Session");

            }
        }

        [HttpGet]
        public async Task<IActionResult> SetCurrentQuestion(string newIndex)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (HttpContext.Session.GetString("CurrentQuestion") != null)
                    {
                        HttpContext.Session.Remove("CurrentQuestion");

                    }

                    HttpContext.Session.SetString("CurrentQuestion", newIndex);

                    return Json(true);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RemoveVM(int vmId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (await _proctorInterviewsService.RemoveVM(vmId))
                    {
                        return Json(true);
                    }
                    else
                    {
                        return Json(true);
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
        [HttpGet]
        public async Task<IActionResult> CreateNewVM()
        {
            try
            {
                var model = new VMProperties();
                var locations = await _vmhelper.Fetchavailablelocation();
                var _locations = JsonConvert.DeserializeObject<VMServerProperty>(locations);
                if (_locations.value.Count > 0)
                {
                    var _locationlist = _locations.value.Select(x => new SelectListItem() { Text = x.name, Value = x.name });
                    _locationlist = _locationlist.OrderBy(x => x.Text);
                    model.LocationsList = new SelectList(_locationlist, "Value", "Text");
                }
                model.VMCount = 1;
                return PartialView(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateNewVM(VMProperties model)
        {
            try
            {
                var path = $"https://risebirdmicroservicededicated.azurewebsites.net/api/GetVMList";
                var functionKey = "fa19fIch6k8mPgiWRkbjvYSpBf26kJffV8jUaXFyWCk2yOXbofkRDA==";
                var response = await _httpClient.SendRequestAsync(HttpMethod.Get, path, null, functionKey, "", _cancellationToken);
                var result = response.Content.ReadAsStringAsync().Result;
                result = result.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\"", "").Replace("\\", "\"");
                var vmsInCloudPortal = JsonConvert.DeserializeObject<VMServerProperty>(result);
                if (!vmsInCloudPortal.value.Select(x => x.name).Contains(model.Name))
                {
                    var responsevmdetails = "";
                    for (int i = 0; i < model.VMCount; i++)
                    {
                        model.Name = model.Name + "_" + i;
                        model.ResourceGroupName = model.Name;
                        var responsevm = await _vmhelper.CreateVmMachine(model);
                        responsevmdetails = responsevm;
                        if (responsevm == "Success")
                        {
                            var resultset = await _proctorInterviewsService.AddNewVmDetails(model);
                        }
                    }
                    TempData["SuccessErrorMessageProctor"] = responsevmdetails;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["SuccessErrorMessageProctor"] = "VM with name " + model.Name + " is already present.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
