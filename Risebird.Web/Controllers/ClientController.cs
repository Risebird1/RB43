using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using Risebird.Entities.ViewModel;
using Risebird.Entities.Data;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public ClientController(IClientService clientService, IApplicationErrorLogger applicationErrorLogger)
        {
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IActionResult> List()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_clientService.GetClients());
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
                if (ModelState.IsValid)
                {
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
                Client clientToUpdate = _clientService.GetClient(id);
                
                if (clientToUpdate != null)
                    return View("ClientUpdate", clientToUpdate);
                else
                    return RedirectToAction("List");

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
                if (ModelState.IsValid)
                {
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
                    if (_clientService.DeleteClient(id))
                    {
                        TempData["SuccessErrorMessage"] = "Client is deleted Successfully";
                        return Json(new { url = Url.Action("List","Client")});
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Deleting Client is Failed";
                        return Json(new { url = Url.Action("List","Client")});
                    }             
                
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Deleting Client is Failed";
                return RedirectToAction("List");
            }
        }
    }
}