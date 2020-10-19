using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Update;
using Risebird.Business.Interface;

namespace Risebird.Web.Controllers
{
    public class RisebirdPayController : Controller
    {
        private IRisebirdPayService _risebirdpayservice;
        private IClientService _clientService;
        public RisebirdPayController(IRisebirdPayService risebirdpayservice, IClientService clientService)
        {
            _risebirdpayservice = risebirdpayservice;
            _clientService = clientService;
        }
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Detailed View
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult ClientDetails(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Clientid = id;
                    return View();
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
        public JsonResult WalletDetails(string action, int ClientId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var model = _clientService.FetchClientWallet(ClientId);
                    if (action == "CREDIT")
                    {
                        model = model.Where(x => x.Credit > 0).ToList();
                    }
                    else if (action == "DEBIT")
                    {
                        model = model.Where(x => x.Debit > 0).ToList();
                    }
                    return Json(model);
                }
                else
                {
                    return Json("SignOut");
                }
            }
            else
            {
                return Json("SignOut");
            }
        }

        /// <summary>
        /// Add Points to client wallet
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddWalletPoints(int points, string comment, int ClientId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var result = _clientService.AddWalletPoints(ClientId, emailid, points, comment);
                    if (result == "Success")
                    {
                        return Json("Success");
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
                else
                {
                    return Json("SignOut");
                }
            }
            else
            {
                return Json("SignOut");
            }
        }
        [HttpPost]
        public JsonResult DeductWalletPoints(int points, string comment, int ClientId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var result = _risebirdpayservice.DeductWalletPoints(ClientId, emailid, points, comment);
                    if (result == "Success")
                    {
                        return Json("Success");
                    }
                    else
                    {
                        return Json("failed");
                    }
                }
                else
                {
                    return Json("SignOut");
                }
            }
            else
            {
                return Json("SignOut");
            }
        }
    }
}
