using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private ISupportService _supportService;
        private IUserService _userService;
        private readonly IDashboardService _dashboardService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;

        public SupportController(ISupportService supportService, IApplicationErrorLogger applicationErrorLogger, IUserService userService, IDashboardService dashboardService, IFileMimeTypeHelper fileMimeTypeHelper, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper)
        {
            _supportService = supportService;
            _applicationErrorLogger = applicationErrorLogger;
            _userService = userService;
            _dashboardService = dashboardService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string controllerName = null, string actMethod = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid?.Replace("\"", "");
                    if (HttpContext.Session.GetString("NavigationShow") == "true")
                    {
                        await _userService.UpdateUserSigInDetails(emailid);
                        HttpContext.Session.SetString("NavigationShow", "false");
                    }

                    var model = new SupportList();
                    model.Supports = await _supportService.GetAllSupportHR(emailid);
                    ViewBag.profile = profileName;
                    if (TempData["SuccessErrorMessageSupport"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageSupport"].ToString();
                        TempData.Remove("SuccessErrorMessageSupport");
                    }

                    if (controllerName != null && actMethod != null)
                    {
                        model.BackController = controllerName;
                        model.BackMethod = actMethod;
                    }
                    return View(model);

                }
                else
                {

                    var model = new SupportList();
                    model.Supports = await _supportService.GetAllSupportHR();

                    if (TempData["SuccessErrorMessageSupport"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageSupport"].ToString();
                        TempData.Remove("SuccessErrorMessageSupport");
                    }

                    if (controllerName != null && actMethod != null)
                    {
                        model.BackController = controllerName;
                        model.BackMethod = actMethod;
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> SuportIndex()
        {
            try
            {
                string emailId = null;
                if (HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId?.Replace("\"", "");
                }

                var model = new SupportList();
                model.Supports = await _supportService.GetAllSupportHR(emailId);
                return PartialView("SupportIndex", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddSupport()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var model = new Support();
                        return View(model);

                    }
                    else
                    {
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
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddSupport(Support hrDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (await _supportService.AddSupportHR(hrDetails))
                    {
                        TempData["SuccessErrorMessageSupport"] = "Support HR has been added Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageSupport"] = "Sorry! Could not add Support HR!";
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
                TempData["SuccessErrorMessageSupport"] = "Sorry! Could not add Support HR!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var hrSupport = _supportService.GetSupportHR(id);
                        return View(hrSupport);
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

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(Support hrDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (await _supportService.UpdateSupportHR(hrDetails))
                    {
                        TempData["SuccessErrorMessageSupport"] = "Support HR has been updated Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageSupport"] = "Sorry! Could not update Support HR!";
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
                TempData["SuccessErrorMessageSupport"] = "Sorry! Could not update Support HR!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (await _supportService.DeleteSupportHR(id))
                        {
                            //TempData["SuccessErrorMessageSupport"] = "Support HR deleted successfully.";
                            return Json(new { url = Url.Action("Index", "Support") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageSupport"] = "Sorry! Could not delete Support HR!";
                            return Json(new { url = Url.Action("Index", "Support") });
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
                //TempData["SuccessErrorMessageSupport"] = "Sorry! Could not delete Support HR!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Alertmessage()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        if (TempData["SuccessAlertMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessAlertMessage"].ToString();
                            TempData.Remove("SuccessAlertMessage");
                        }

                        var AlterMessage = await _dashboardService.FetchAlertMessageList();
                        foreach (var item in AlterMessage)
                        {
                            item.Announcement = System.Web.HttpUtility.HtmlDecode(item.Announcement);
                        }
                        return View(AlterMessage);

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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditAlertMessage(int Alertid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var AlterMessage = await _dashboardService.FetchAlertMessageList();
                    var model = AlterMessage?.Where(x => x.ID == Alertid)?.FirstOrDefault();
                    if (model != null)
                    {
                        model.Announcement = System.Web.HttpUtility.HtmlDecode(model.Announcement);
                    }
                    return PartialView(model);
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
        [Authorize]
        public async Task<IActionResult> EditAlertMessage(AlterMessage model)
        {
            model.Announcement = System.Web.HttpUtility.HtmlEncode(model.Announcement);
            var result = await _dashboardService.AddAlertMessage(model);
            if (result == "Success")
            {
                if (model.ID > 0)
                {
                    TempData["SuccessAlertMessage"] = "Announcement edited Successfully.";
                }
                else
                {
                    TempData["SuccessAlertMessage"] = "Announcement added Successfully.";
                }
            }
            else
            {
                if (model.ID > 0)
                {
                    TempData["SuccessAlertMessage"] = "Failed to edit the Announcement!";
                }
                else
                {
                    TempData["SuccessAlertMessage"] = "Failed to add the Announcement!";
                }
            }
            return RedirectToAction("Alertmessage");
        }
        #region Raise Tickets
        public async Task<IActionResult> RaiseBugRequest(int? interviewId = null, int? irfId = null, int? penaltyAmount = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");

                var model = new RaiseBugsVM();
                if (profileName == ProfileNames.Interviewer || profileName == ProfileNames.Client)
                {
                    model.RequestType = "Operation";
                }
                else
                {
                    model.RequestType = "Technical";
                }
                model.RequestedBy = emailId;
                model.Requester = profileName;
                if (interviewId.HasValue)
                {
                    //model.Comment = $"Reclaim penalty request. Interview ID: {interviewId.Value}.";
                    //if (irfId.HasValue && irfId.Value > 0)
                    //{
                    //    model.Comment += $" IRF ID: {irfId.Value}.";
                    //}
                    //if (penaltyAmount.HasValue && penaltyAmount.Value != 0)
                    //{
                    //    model.Comment += $" Late report penalty amount: {penaltyAmount.Value}.";
                    //}
                    //model.Comment += " ";
                    model.interviewId = interviewId.Value;
                    model.RequestType = "Payment";
                }
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RaiseBugRequest(RaiseBugsVM model)
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
            profileName = profileName.Replace("\"", "");
            if (model.Attachment != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.Attachment);
                if (fileMimeType == "image/jpg" || fileMimeType == "image/png" || fileMimeType == "image/jpeg")
                {
                    var fileName = model.Attachment.FileName;
                    long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    fileName = unixTime.ToString() + "_" + fileName;
                    await _azureStorageHelper.InsertBlob(fileName, "audios", model.Attachment);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                    var blob = containerRef.GetBlockBlobReference(fileName);
                    string uploadedaudioFileURL = blob.Uri.ToString();
                    model.AttachmentURL = uploadedaudioFileURL;
                }
            }
            var result = await _supportService.SaveRaisedIssueDetails(model);
            if (result)
            {
                if (profileName == ProfileNames.SupportAdmin || profileName == ProfileNames.SeniorHR || profileName == ProfileNames.HR || profileName == ProfileNames.QualityController || profileName == ProfileNames.Finance || profileName == ProfileNames.InterviewerManagement || profileName == ProfileNames.SeniorInterviewerManagement || profileName == ProfileNames.Recruiter)
                {
                    await _notificationHelper.SendTechSupportMail(model.Comment, DateTime.Now, model.RequestedBy);
                }
                return Json("Request raised successfully.");
            }
            return Json("Request is not raised, Please try again.");
        }
        public async Task<IActionResult> RaisedRequests(string type, int returnBack)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    type = "OpenPayment";
                }
                ViewBag.Type = type;
                ViewBag.ReturnBack = returnBack;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = await _supportService.GetRaisedIssueDetails(profileName);
                if (type == "Open")
                {
                    model = model.Where(x => x.Status == "Open").ToList();
                    model = model.Where(x => x.RequestType != "Payment").ToList();
                }
                else if (type == "OpenPayment")
                {
                    model = model.Where(x => x.Status == "Open").ToList();
                    model = model.Where(x => x.RequestType == "Payment").ToList();
                }
                else
                {
                    model = model.Where(x => x.Status != "Open").ToList();
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateRaisedRequestsStatus(int requestid, string comment)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = new RaiseBugsVM();
                model.id = requestid;
                model.RequestedBy = emailId;
                model.Requester = profileName;
                model.Comment = comment;
                var result = await _supportService.UpdateRaisedRequestStatus(model);
                return Json("Request is closed successfully.");
            }
            else
            {
                return Json("Please try again.");
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetMyOpenTicketCount()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { openCount = 0, totalCount = 0 });

            var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var tickets = await _supportService.GetMyRaisedIssues(emailId);
            if (tickets == null) return Json(new { openCount = 0, totalCount = 0 });

            var list = tickets.ToList();
            return Json(new { openCount = list.Count(x => x.Status == "Open"), totalCount = list.Count });
        }

        [HttpGet]
        public async Task<IActionResult> MyTickets(string status = null)
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("SignOut", "Session");

            var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");

            var tickets = await _supportService.GetMyRaisedIssues(emailId);
            if (tickets == null) tickets = new System.Collections.Generic.List<RaiseBugsVM>();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Open", System.StringComparison.OrdinalIgnoreCase))
                    tickets = tickets.Where(x => x.Status == "Open").ToList();
                else if (status.Equals("Close", System.StringComparison.OrdinalIgnoreCase))
                    tickets = tickets.Where(x => x.Status == "Close").ToList();
            }

            ViewBag.FilterStatus = status ?? "All";
            ViewBag.Profile = profileName;
            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> TicketDetails(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("SignOut", "Session");

            var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");

            var ticket = await _supportService.GetRaisedIssueById(id);
            if (ticket == null)
                return RedirectToAction("MyTickets");

            // Security: only Admin/TechSupport or the original requester may view the detail.
            if (ticket.RequestedBy != emailId &&
                profileName != ProfileNames.Admin &&
                profileName != "TechSupport" &&
                profileName != ProfileNames.SupportAdmin)
            {
                return RedirectToAction("MyTickets");
            }

            var history = await _supportService.GetRaisedIssueHistory(id);
            ticket.History = history?.ToList() ?? new System.Collections.Generic.List<RaisedIssueHistoryVM>();

            ViewBag.Profile = profileName;
            return View(ticket);
        }

        [HttpPost]
        public async Task<JsonResult> ReopenTicket(int ticketId, string comment)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "A comment is required to reopen a ticket." });

            var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");

            var result = await _supportService.ReopenRaisedIssue(ticketId, comment, emailId, profileName);
            if (result)
                return Json(new { success = true, message = "Ticket has been reopened successfully." });

            return Json(new { success = false, message = "Could not reopen the ticket. Please try again." });
        }
        #endregion
    }
}