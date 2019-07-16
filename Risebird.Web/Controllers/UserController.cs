using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private IUserService _userService;
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public UserController(IUserService userService, IClientService clientService,IApplicationErrorLogger applicationErrorLogger)
        {
            _userService = userService;
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public IActionResult Index()
        {
            try
            {
                return View(_userService.GetRisebirdUsers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult Interviewers()
        {
            try
            {
                return View(_userService.GetInterviewers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult ClientUsers()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_userService.GetClientUsers());
            }
            catch (Exception)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddClientUser()
        {
            try
            {
                var model = new ClientUserVM();
                model.Clients = new SelectList(_clientService.GetClients(), "id", "Name" );
                

                model.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                return View("ClientUserAdd", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("ClientUsers");
            }

        }

        [HttpPost]
        public async Task<IActionResult> AddClientUser(ClientUserVM clientUserVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_userService.AddClientUser(clientUserVM))
                    {
                        TempData["SuccessErrorMessage"] = "Client User is added Successfully";
                        return RedirectToAction("ClientUsers");                        
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Client User addition is Failed";
                        return RedirectToAction("ClientUsers");
                    }
                }

                else
                {

                    var model = new ClientUserVM();
                    model.Clients = new SelectList(_clientService.GetClients(), "id", "Name");


                    model.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                    return View("ClientUserAdd", model);                    
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Client User addition is Failed";
                return RedirectToAction("ClientUsers");
            }

        }

        public async Task<IActionResult> DeleteClientUser(int id)
        {
            try
            {
                if (_userService.DeleteClientUser(id))
                {
                    TempData["SuccessErrorMessage"] = "Client User is deleted Successfully";
                    return Json(new { url = Url.Action("ClientUsers", "User") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of Client User Failed";
                    return Json(new { url = Url.Action("ClientUsers", "User") });
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Deletion of Client User Failed";
                return RedirectToAction("ClientUsers");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditClientUser(int id)
        {
            try
            {
                ClientUserVM clientUserVM = _userService.GetClientUser(id);

                clientUserVM.Clients = new SelectList(_clientService.GetClients(), "id", "Name");
                clientUserVM.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");


                if (clientUserVM != null)
                    return View("ClientUserUpdate", clientUserVM);
                else
                    return RedirectToAction("ClientUsers");

            }

            catch(Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);              
                return RedirectToAction("ClientUsers");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditClientUser(ClientUserVM clientUserVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_userService.UpdateClientUser(clientUserVM))
                    {
                        TempData["SuccessErrorMessage"] = "Client User is updated Successfully";
                        return RedirectToAction("ClientUsers");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Client User is Failed";
                        return RedirectToAction("ClientUsers");
                    }
                }
                else
                {
                    clientUserVM.Clients = new SelectList(_clientService.GetClients(), "id", "Name");
                    clientUserVM.ClientRoles = new SelectList(_clientService.GetClientRoles(), "id", "Name");
                    return View("ClientUserUpdate",clientUserVM);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Client User is Failed"; ;
                return RedirectToAction("ClientUsers");
            }

        }
    }
}