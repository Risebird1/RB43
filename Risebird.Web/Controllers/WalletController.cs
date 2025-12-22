using Amazon.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Actions;

namespace Risebird.Web.Controllers
{
    public class WalletController : BaseController
    {
        private readonly IWalletService _walletService;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;

        public WalletController(IWalletService walletService, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger, IClientService clientService, IIRFService irfService)
        {
            _walletService = walletService;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
        }

        /// <summary>
        /// Display wallet details view
        /// </summary>
        /// <param name="billingType">1: Billing Owner (Client Users), 3: Client</param>
        /// <param name="clientId">Client ID (optional)</param>
        /// <param name="billingOwnerId">Billing Owner ID (optional)</param>
        /// <returns>Wallet details view</returns>
        [HttpGet]
        public async Task<IActionResult> Index(string id = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                int billingType = 0;
                int clientId = 0;
                int billingOwnerId = 0;
                try
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    // If user is Client, determine billing type and IDs
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        if (client != null)
                        {
                            // Get client billing type from Client table
                            var clientBillingType = await _clientService.GetClientBillingType(client.ClientId);
                            // Default to Client billing type if not specified
                            // If Client's BillingType is 1 (Billing Owner), use BillingOwner, otherwise use Client
                            if (clientBillingType == 1)
                            {
                                billingType = 1; // Billing Owner
                                billingOwnerId = client.UserId;
                                clientId = client.ClientId;
                            }
                            else
                            {
                                billingType = 3; // Client
                                clientId = client.ClientId;
                            }
                        }
                    }

                    ViewBag.BillingType = billingType;
                    ViewBag.ClientId = clientId;
                    ViewBag.BillingOwnerId = billingOwnerId;
                    ViewBag.ProfileName = profileName;
                    ViewBag.PageStatus = "";
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        ViewBag.PageStatus = "Public";
                    }
                    // Get current balance
                    var balance = await _walletService.GetWalletBalance(billingType, clientId, billingOwnerId);
                    ViewBag.CurrentBalance = balance;
                    return View();
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                    else
                    {
                        return RedirectToAction("PublicAllMeetings", "Candidate", new { id = id });
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return RedirectToAction("SignOut", "Session");
                }
                else
                {
                    return RedirectToAction("PublicAllMeetings", "Candidate", new { id = id });
                }
            }
        }

        /// <summary>
        /// Get wallet details for Client or Billing Owner
        /// </summary>
        /// <param name="billingType">1: Billing Owner (Client Users), 3: Client</param>
        /// <param name="clientId">Client ID (optional)</param>
        /// <param name="billingOwnerId">Billing Owner ID (optional)</param>
        /// <param name="action">Filter: 'CREDIT', 'DEBIT', or null for all</param>
        /// <returns>Wallet transaction list as partial view</returns>
        [HttpPost]
        public async Task<IActionResult> GetWalletDetails(int billingType, int? clientId = null, int? billingOwnerId = null, string actiontype = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                try
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    // Validate billing type
                    if (billingType != 1 && billingType != 3)
                    {
                        return PartialView("GetWalletDetails", new List<WalletVM>());
                    }

                    // If user is Client, get their ClientId
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = await _userRepository.GetClientUserDetails(emailid);
                        if (client != null && billingType == 3)
                        {
                            clientId = client.ClientId;
                        }
                        else if (client != null && billingType == 1)
                        {
                            billingOwnerId = client.UserId;
                        }
                    }

                    var model = await _walletService.GetWallet(billingType, clientId, billingOwnerId, actiontype);

                    if (model != null)
                    {
                        foreach (var item in model)
                        {
                            if (item.CreatedOn.HasValue)
                            {
                                DateTime _local = (DateTime)item.CreatedOn;
                                item.CreatedOnstring = _local.ToShortDateString();
                            }
                        }
                        return PartialView("GetWalletDetails", model);
                    }
                    else
                    {
                        return PartialView("GetWalletDetails", new List<WalletVM>());
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return PartialView("GetWalletDetails", new List<WalletVM>());
                }
            }
            else
            {
                return PartialView("GetWalletDetails", new List<WalletVM>());
            }
        }

        /// <summary>
        /// Get current wallet balance
        /// </summary>
        /// <param name="billingType">1: Billing Owner (Client Users), 3: Client</param>
        /// <param name="clientId">Client ID (optional)</param>
        /// <param name="billingOwnerId">Billing Owner ID (optional)</param>
        /// <returns>Current balance</returns>
        [HttpPost]
        public async Task<JsonResult> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                try
                {
                    // Validate billing type
                    if (billingType != 1 && billingType != 3)
                    {
                        return Json(new { success = false, message = "Invalid billing type. Use 1 for Billing Owner or 3 for Client." });
                    }

                    var balance = await _walletService.GetWalletBalance(billingType, clientId, billingOwnerId);
                    return Json(new { success = true, balance = balance });
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return Json(new { success = false, message = "An error occurred while fetching balance." });
                }
            }
            else
            {
                return Json(new { success = false, message = "SignOut" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> AddWalletBalance(int Clientid)
        {
            var clientBillingType = await _clientService.GetClientBillingType(Clientid);
            ViewBag.BillingType = clientBillingType;
            ViewBag.Clientid = Clientid;
            if (clientBillingType == 1)
            {
                //BillingType :BilingOwner
                ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(Clientid), "id", "EmailId");
            }
            else if (clientBillingType == 3)
            {
                //BillingType :Client
            }
            return PartialView();
        }
        [HttpPost]
        public async Task<JsonResult> AddWalletBalance(int Clientid, int billingType, int CreditAmount, int? billingOwnerId = null)
        {
            var emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
            }
            var result = await _walletService.AddClientWalletBalance(billingType, Clientid, CreditAmount, emailid, billingOwnerId);
            if (result)
            {
                return Json("Added Successfully");
            }
            else
            {
                return Json("Fail to added Credit amount, Please try again.");
            }
        }
    }
}

