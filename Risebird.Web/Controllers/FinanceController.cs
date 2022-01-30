using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private static string SearchValue;
        private IInterviewerService _interviewerService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IProjectService _projectService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;


        public FinanceController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IInterviewerService interviewerService, IWhatsAppHelper whatsAppHelper, IConfiguration configuration, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IFileMimeTypeHelper fileMimeTypeHelper)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _interviewerService = interviewerService;
            _whatsAppHelper = whatsAppHelper;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _projectService = projectService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }
        /// <summary>
        /// Interviewer or QC check payment status and request for payment with details
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> RBPayment()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Interviewer" || profileName == "Quality Controller")
                {
                    var model = await _meetingService.RBDetails(emailid);
                    model = model.Select(x => { x.InterviewStatusDetails = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusDetails = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                    var sum = model.Where(x => x.Status == "Submitted" || x.Status == "Pending").Sum(x => x.Points) - model.Where(x => x.Status == "Advance").Sum(x => x.Points);
                    if (sum > 0)
                    {
                        ViewBag.submit = "true";
                    }
                    else
                    {
                        ViewBag.submit = "false";
                    }
                    foreach (var item in model)
                    {
                        //var _onemoth = item.Date.AddMonths(1).DayOfWeek;
                        //int diff = (7 + (_onemoth - DayOfWeek.Monday)) % 7;
                        //var _firstdate = item.Date.AddMonths(1).AddDays(-1 * diff).Date;
                        //var _lastdate = _firstdate.AddDays(6);
                        //item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        if (1 <= item.Date.AddMonths(1).Day && item.Date.AddMonths(1).Day <= 7)
                        {
                            var _firstdate = item.Date.AddMonths(1).AddDays(-(item.Date.AddMonths(1).Day - 1));
                            var _lastdate = item.Date.AddMonths(1).AddDays(7 - item.Date.AddMonths(1).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (7 < item.Date.AddMonths(1).Day && item.Date.AddMonths(1).Day <= 14)
                        {
                            var _firstdate = item.Date.AddMonths(1).AddDays(-(item.Date.AddMonths(1).Day - 7));
                            var _lastdate = item.Date.AddMonths(1).AddDays(14 - item.Date.AddMonths(1).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (14 < item.Date.AddMonths(1).Day && item.Date.AddMonths(1).Day <= 21)
                        {
                            var _firstdate = item.Date.AddMonths(1).AddDays(-(item.Date.AddMonths(1).Day - 14));
                            var _lastdate = item.Date.AddMonths(1).AddDays(21 - item.Date.AddMonths(1).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (21 < item.Date.AddMonths(1).Day && item.Date.AddMonths(1).Day <= 31)
                        {
                            var _firstdate = item.Date.AddMonths(1).AddDays(-(item.Date.AddMonths(1).Day - 21));
                            var _lastdate = item.Date.AddMonths(2).AddDays(-(item.Date.AddMonths(1).Day + 1));
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatusDetails != "No Show(Interviewer)")
                        {
                            item.MeetingStatusDetails = "Unassign Meeting within 1/2 hr";
                            item.InterviewStatusDetails = "";
                            item.VideoDuration = 0;
                        }
                    }
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
        /// <summary>
        /// Interviewer or QC can export the details in excel sheet for future process
        /// </summary>
        /// <returns></returns>

        [Authorize]
        [HttpGet]
        public FileContentResult ExportRBDetails()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payable Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_RBDetails.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer" || profileName == "Quality Controller")
                    {

                        var model = _meetingService.RBDetails(emailid);
                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        //Sheet.Column(1).Width = 30;
                        //Sheet.Column(2).Width = 30;
                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 30;
                        }
                        Sheet.Cells[1, 1].Value = "Payment For";
                        Sheet.Cells[1, 2].Value = "Interview Id";
                        Sheet.Cells[1, 3].Value = "Type";
                        Sheet.Cells[1, 4].Value = "Rate";
                        Sheet.Cells[1, 5].Value = "Amount";
                        Sheet.Cells[1, 6].Value = "Video Link";
                        Sheet.Cells[1, 7].Value = "Video Duration";
                        Sheet.Cells[1, 8].Value = "Date";
                        Sheet.Cells[1, 9].Value = "Status";
                        Sheet.Cells[1, 10].Value = "Location";
                        Sheet.Cells[1, 11].Value = "Meeting Status";

                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var item in model.Result)
                            {
                                int _location = 0;
                                Sheet.Cells[i, 1].Value = item.Comment;
                                Sheet.Cells[i, 2].Value = item.InterviewId;
                                Sheet.Cells[i, 3].Value = item.Type;
                                Sheet.Cells[i, 4].Value = item.Rate;
                                Sheet.Cells[i, 5].Value = item.Points;
                                Sheet.Cells[i, 8].Value = item.Date;
                                Sheet.Cells[i, 9].Value = item.Status;
                                if (String.IsNullOrEmpty(item.PreferredLocation))
                                {
                                    Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                }
                                else if (int.TryParse(item.PreferredLocation, out _location))
                                {
                                    if (_location == 1)
                                    {
                                        Sheet.Cells[i, 10].Value = "India";
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = "OutSide India";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                }
                                if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5)
                                {
                                    item.MeetingStatusDetails = "Unassign Meeting within 1/2 hr";
                                    item.InterviewStatusDetails = "";
                                }
                                else
                                {
                                    Sheet.Cells[i, 11].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                                }
                                if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5)
                                {
                                    Sheet.Cells[i, 6].Value = "No Video";
                                    Sheet.Cells[i, 7].Value = "NA";
                                }
                                else if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    Sheet.Cells[i, 6].Value = "No Video";
                                    Sheet.Cells[i, 7].Value = "NA";
                                }
                                else if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 6].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                        Sheet.Cells[i, 6].Value = "Video";
                                        Sheet.Cells[i, 6].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                        Sheet.Cells[i, 7].Value = (item.VideoDuration.HasValue && item.VideoDuration.Value > 0) ? item.VideoDuration.Value.ToString() : string.Empty;
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 6].Value = "No Video";
                                        Sheet.Cells[i, 7].Value = "NA";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 6].Value = "No Video";
                                    Sheet.Cells[i, 7].Value = "NA";
                                }
                                i++;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }

                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
        /// <summary>
        /// Interviewer or QC share payment details with Finance team for releasing payment
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> ShareWithFN()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Interviewer" || profileName == "Quality Controller")
                {

                    var model = await _meetingService.RBUpdateStatusDetails(emailid);
                    //Send a mail to FN
                    Mail _mail = new Mail
                    {
                        TO = "finance@risebird.io",
                        Subject = "Interviewer Submitted Payment details"
                    };
                    var emailsend = _notificationHelper.RBPayNotification(_mail, emailid);
                    return RedirectToAction("RBPayment");
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
        /// <summary>
        /// Finance Team or Admin can see all coming request for payment
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> FNPayment(int? Page, string searchValue, string year)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = await _meetingService.FetchFNDetails(emailid);
                    var totalAmount = model.Sum(x => x.Points);
                    foreach (var item in model)
                    {
                        item.Points = (item.Points - Convert.ToInt32(Math.Round(item.Points * .1)));
                    }
                    var payableAmount = model.Sum(x => x.Points);
                    var Tds = totalAmount - payableAmount;
                    ViewBag.totalAmount = totalAmount;
                    ViewBag.payableAmount = payableAmount;
                    ViewBag.Tds = Tds;
                    ViewBag.setvalueforfunction = 1;
                    ViewBag.Searchvalue = "";
                    ViewBag.TotalCount = model.Count;

                    ViewBag.PageStartCount = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                    int a = model.Count / 10;
                    if (model.Count % 10 == 0)
                    {
                        ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                    else
                    {
                        a = a + 1;
                        Page = Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page);
                        if (a == Page)
                        {
                            ViewBag.PageEndCount = model.Count;
                        }
                        else
                        {
                            ViewBag.PageEndCount = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                        }

                    }
                    if (model.Count == 0)
                    {
                        ViewBag.PageStartCount = 0;
                        ViewBag.PageEndCount = 0;
                    }
                    HttpContext.Session.SetString("financemodel", JsonConvert.SerializeObject(model));
                    if (TempData["PaymentMessage"] != null)
                    {
                        ViewBag.Message = TempData["PaymentMessage"].ToString();
                        TempData.Remove("PaymentMessage");

                    }
                    List<string> _listyear = new List<string>();
                    var currentyear = DateTime.Now.Year;
                    var counter = currentyear - 2020;
                    for (int i = 0; i < counter; i++)
                    {
                        _listyear.Add(DateTime.Now.AddYears(-i).Year.ToString());
                    }
                    var selectListforsize = new List<SelectListItem>();
                    if (String.IsNullOrEmpty(year))
                    {
                        year = currentyear.ToString();
                    }
                    foreach (var item in _listyear)
                    {
                        bool selected = false;
                        if (item == year)
                        {
                            selected = true;
                        }
                        var selectItem = new SelectListItem(item, Convert.ToString(item), selected);
                        selectListforsize.Add(selectItem);
                    }
                    ViewBag.TdsYears = selectListforsize;
                    return View(model.ToPagedList(Page ?? 1, 10));

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
        /// <summary>
        /// Finance Team or Admin can view detailed view 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="flag"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> GetInterviewerPaymentDetails(int id, int flag, DateTime st, DateTime se)
        {
            var model = await _meetingService.RBInterviewerDetails(id, flag);
            int totalAmount = model.Sum(x => x.Points);
            if (flag == 2)
            {
                var model1 = model.Where(x => x.Status == "Advance").ToList();
                model = model.Where(x => x.Date >= st && x.Date <= se.AddDays(1)).ToList();
                totalAmount = model.Where(x => x.Status != "Advance").Sum(x => x.Points) - model1.Sum(x => x.Points);
                model = model.Union(model1).ToList();
            }
            int payableAmount = 0;
            int Tds = 0;
            if (totalAmount > 0)
            {
                payableAmount = (totalAmount - Convert.ToInt32(Math.Round(totalAmount * .1)));
                Tds = totalAmount - payableAmount;
            }
            foreach (var item in model)
            {
                item.MeetingStatusDetails = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                item.InterviewStatusDetails = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
            }
            return Json(new { result = model, totalAmount = totalAmount, payableAmount = payableAmount, tds = Tds });
        }
        /// <summary>
        /// Admin or Finance Team may add points in interviewer or QC account
        /// </summary>
        /// <param name="id"></param>
        /// <param name="Points"></param>
        /// <param name="Comment"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> AddPointsforInterviewer(int id, int Points, string Comment, DateTime PointDate)
        {
            var model = await _meetingService.RBAddPoints(id, Points, Comment, PointDate);
            return Json(model);
        }
        /// <summary>
        /// Finance Team can raise query againsted the payment
        /// </summary>
        /// <param name="emailid"></param>
        /// <param name="query"></param>
        /// <returns></returns>

        [HttpPost]
        public JsonResult SendQuerybyFN(string emailid, string query)
        {
            //send a mail to interviewer
            Mail _mail = new Mail
            {
                TO = emailid,
                CC = "finance@risebird.io",
                Subject = "RiseBird: Finance Team has query regarding you payment request"
            };
            var emailsend = _notificationHelper.RBFNQueryNotification(_mail, query);
            if (emailsend == "Mail sent successfully.")
            {
                return Json("Success");
            }
            else
            {
                return Json("");
            }
        }
        /// <summary>
        /// Finance Team may approve and confirm the payment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> ConfirmPayment(int id, int index, string email, string amount, DateTime st, DateTime se)
        {

            var resultset = await _meetingService.RBInterviewerDetails(id, index);
            if (index == 2)
            {
                var model1 = resultset.Where(x => x.Status == "Advance").ToList();
                resultset = resultset.Where(x => x.Date >= st && x.Date <= se.AddDays(1)).ToList();
                resultset = resultset.Union(model1).ToList();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("<table cellspacing='0' width='100%'><thead><tr><th>Date</th><th>Payment for</th><th>Type</th><th>Status</th><th>Total Amount</th><th>Payable Amount</th></tr></thead><tbody>");
            foreach (var item in resultset)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + item.Date.ToString() + "</td>");
                sb.Append("<td>" + item.Comment + "</td>");
                sb.Append("<td>" + item.Type + "</td>");
                if (item.Status == "Advance")
                {
                    sb.Append("<td>" + item.Status + "</td>");
                }
                else
                {
                    sb.Append("<td>Paid</td>");
                }
                sb.Append("<td>" + item.Points + "</td>");
                if (item.Status != "Advance")
                {
                    sb.Append("<td>" + Math.Round(item.Points - item.Points * .1) + "</td>");
                }
                else
                {
                    sb.Append("<td>" + item.Points + "</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("<tbody></table>");

            var commentvalue = st.ToString() + " to " + se.ToString();
            var _Amount = Convert.ToInt32(resultset.Where(x => x.Status != "Advance").Sum(x => x.Points * .9) - resultset.Where(x => x.Status == "Advance").Sum(x => x.Points));
            amount = _Amount.ToString();
            var totalAmount = resultset.Where(x => x.Status != "Advance").Sum(x => x.Points) - resultset.Where(x => x.Status == "Advance").Sum(x => x.Points);
            var paymenthistory = await _meetingService.InsertPaymentHistory(email, Convert.ToInt32(amount), totalAmount, "Done", commentvalue);
            if (String.IsNullOrEmpty(paymenthistory))
                paymenthistory = "0";
            var model = await _meetingService.RBConfirmPayment(id, index, st, se.AddDays(1), Convert.ToInt32(paymenthistory));
            //Start:Send email to Interviewer
            Mail _mail = new Mail
            {
                TO = email,
                CC = "finance@risebird.io",
                Subject = "RiseBird: Finance Team has done payment"
            };
            var emailsend = _notificationHelper.RBFNPaymentConfirmationNotification(_mail, amount, sb);
            //End:Send email to Interviewer
            WhatsAppVM _whatsApp = new WhatsAppVM();
            _whatsApp.Emailid = email;
            _whatsApp.InterviewDate = amount;
            _whatsApp.Event = "Payment Notification";
            var result = _whatsAppHelper.WhatsAppSendOnAssignation(_whatsApp);
            if (emailsend == "Mail sent successfully.")
            {

            }
            if (model == "Success")
            {
                return Json("Payment has been done  Successfully!");
                // TempData["PaymentMessage"] = "Payment paid Successfully";
            }
            else
            {
                return Json("Failed to pay the payment");
                //TempData["PaymentMessage"] = "Fail To pay the payment";
            }

            //return RedirectToAction("FNPayment");
        }
        /// <summary>
        /// Search functionlity implement
        /// </summary>
        /// <param name="searchValue"></param>
        /// <returns></returns>

        [HttpPost]
        public JsonResult FNPaymentJson(string searchValue)
        {
            try
            {
                int Page = 1;
                var model = JsonConvert.DeserializeObject<List<RBInterviewDetails>>(HttpContext.Session.GetString("financemodel"));
                SearchValue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!String.IsNullOrEmpty(x.Comment) ? x.Comment.ToUpper().Contains(searchValue) : x.Comment == "")).ToList();
                }
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = model.Count / 10;
                if (model.Count % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    Page = Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page);
                    if (a == Page)
                    {
                        Pageend = model.Count;
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                }
                if (model.Count == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }
                return Json(new { result = model.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count });
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }
        /// <summary>
        /// To Fetch All Interviewer Emailid based on Typed string in text box or used for auto filled textbox
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> FetchInterviewer(string term)
        {
            var model = await _meetingService.FetchInterviewerList(term);
            return Json(model);
        }
        /// <summary>
        /// Finance Team may done payment in advance for interviewer
        /// </summary>
        /// <param name="Interviewer"></param>
        /// <param name="Comment"></param>
        /// <param name="Amount"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> AdvancePayment(string Interviewer, string Comment, int Amount)
        {
            var paymenthistory = await _meetingService.InsertPaymentHistory(Interviewer, Amount, Amount, "Advance", "");
            if (String.IsNullOrEmpty(paymenthistory))
            {
                paymenthistory = "0";
            }
            var model = await _meetingService.InsertAdvancePayment(Interviewer, Comment, Amount, Convert.ToInt32(paymenthistory));
            StringBuilder sb = new StringBuilder();
            var result = "";
            if (model == "Success")
            {
                result = "Advance Payment has been done Successfully!";
                Mail _mail = new Mail
                {
                    TO = Interviewer,
                    CC = "finance@risebird.io",
                    Subject = "RiseBird: Finance Team done advance payment"
                };
                var emailsend = _notificationHelper.RBFNPaymentConfirmationNotification(_mail, Amount.ToString(), sb);
                WhatsAppVM _whatsApp = new WhatsAppVM();
                _whatsApp.Emailid = Interviewer;
                _whatsApp.InterviewDate = Amount.ToString();
                _whatsApp.Event = "Payment Notification";
                _whatsAppHelper.WhatsAppSendOnAssignation(_whatsApp);
            }
            else if (model == "Failed")
            {
                result = "Interviewer Email-id is wrong.";
            }
            else
            {
                result = "Failed to done the advance payment.";
            }
            return Json(result);
        }

        /// <summary>
        /// Finace Team may be find data on date basis and Pending and Submitted 
        /// </summary>
        /// <param name="st"></param>
        /// <param name="se"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> FNGenerateInvoiceJson(int? PageSet, DateTime st, DateTime se)
        {
            try
            {
                int Page = 0;
                if (PageSet == null || PageSet == 0)
                    Page = 1;
                else
                    Page = Convert.ToInt32(PageSet);
                var model = await _meetingService.FNGenerateInvoiceDetails(st, se.AddDays(1));
                var _totalpaidamount = await _meetingService.FNFetchToalAmountPaid(st, se.AddDays(1));
                var totalpaidamount1 = Convert.ToInt32((from ad in _totalpaidamount
                                                        select ad.Points).ToList().Sum());
                var totalpaidamount = Convert.ToInt32((from ad in _totalpaidamount
                                                       select Math.Round(ad.Points * .9)).ToList().Sum());
                var totalamountforstatus = await _meetingService.FNFetchToalAmountStatus(st, se.AddDays(1));
                var totalqcamount = await _meetingService.FNFetchToalQCAmountStatus(st, se.AddDays(1));
                //int totalinterviewamount = totalamountforstatus.Where(x => x.MeetingStatus == 4).Sum(x => x.Points * 90 / 100);
                var totalinterviewamount = Convert.ToInt32((from ad in totalamountforstatus
                                                            where ad.MeetingStatus == 4
                                                            select Math.Round(ad.Points * .9)).ToList().Sum());
                var totalNoShowamount = Convert.ToInt32((from ad in totalamountforstatus
                                                         where ad.MeetingStatus == 1
                                                         select Math.Round(ad.Points * .9)).ToList().Sum());
                var totaldroppedamount = Convert.ToInt32((from ad in totalamountforstatus
                                                          where ad.MeetingStatus == 3
                                                          select Math.Round(ad.Points * .9)).ToList().Sum());
                //int totalNoShowamount = totalamountforstatus.Where(x => x.MeetingStatus == 1).Sum(x => x.Points * 90 / 100);
                //int totaldroppedamount = totalamountforstatus.Where(x => x.MeetingStatus == 3).Sum(x => x.Points * 90 / 100);
                //var pendingAmount = model.Sum(x => x.Points);
                var pendingAmount = Convert.ToInt32((from ad in model
                                                     select Math.Round(ad.Points * .9)).ToList().Sum());
                var totalAmount = totalpaidamount1 + model.Sum(x => x.Points);
                //totalpaidamount = Convert.ToInt32(Math.Round(totalpaidamount * .9));
                //pendingAmount = Convert.ToInt32(Math.Round(pendingAmount * .9));
                foreach (var item in model)
                {
                    item.Points = Convert.ToInt32(Math.Round(item.Points * .9));
                }
                //var payableAmount = model.Sum(x => x.Points);
                var payableAmount = totalpaidamount + pendingAmount;
                var Tds = totalAmount - payableAmount;
                //int qcamount = totalqcamount.Sum(x => x.Points * 90 / 100);
                int qcamount = Convert.ToInt32((from ad in totalqcamount
                                                select Math.Round(ad.Points * .9)).ToList().Sum());
                int totalcount = model.Count;
                int Pagestart = 0;
                int Pageend = 0;

                Pagestart = 10 * ((Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page)) - 1) + 1;
                int a = model.Count / 10;
                if (model.Count % 10 == 0)
                {
                    Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                }
                else
                {
                    a = a + 1;
                    Page = Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page);
                    if (a == Page)
                    {
                        Pageend = model.Count;
                    }
                    else
                    {
                        Pageend = 10 * (Convert.ToInt32(Page) == 0 ? 1 : Convert.ToInt32(Page));
                    }
                }
                if (model.Count == 0)
                {
                    Pagestart = 0;
                    Pageend = 0;
                }
                HttpContext.Session.SetString("financemodel", JsonConvert.SerializeObject(model));
                return Json(new { result = model.Skip((Page - 1) * 10).Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count, currentpage = Page, totalAmount = totalAmount, payableAmount = payableAmount, tds = Tds, totalpaidamount = totalpaidamount, totalinterviewamount = totalinterviewamount, totaldroppedamount = totaldroppedamount, totalNoShowamount = totalNoShowamount, pendingAmount = pendingAmount, qcamount = qcamount });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }
        /// <summary>
        /// Finance Team may Export data in Excel sheet
        /// </summary>
        /// <param name="st"></param>
        /// <param name="se"></param>
        /// <returns></returns>

        [Authorize]
        [HttpGet]
        public FileContentResult ExportGenerateFNPayment(DateTime st, DateTime se)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payable Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_AccountDetails.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        //var model = _meetingService.FetchAllFNDetail();
                        var model = _meetingService.FNAllGeneratedInvoiceDetails(st, se.AddDays(1));
                        var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        //Sheet.Column(1).Width = 30;
                        //Sheet.Column(2).Width = 30;
                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 25;
                        }
                        Sheet.Cells[1, 1].Value = "IRFID";
                        Sheet.Cells[1, 2].Value = "Date";
                        Sheet.Cells[1, 3].Value = "Candidate Email";
                        Sheet.Cells[1, 4].Value = "Candidate Name";
                        Sheet.Cells[1, 5].Value = "Primary Skill";
                        Sheet.Cells[1, 6].Value = "Client Name";
                        Sheet.Cells[1, 7].Value = "Payment For";
                        Sheet.Cells[1, 8].Value = "Interviewer";
                        Sheet.Cells[1, 9].Value = "Type";
                        Sheet.Cells[1, 10].Value = "Country";
                        Sheet.Cells[1, 11].Value = "Meeting Status";
                        Sheet.Cells[1, 12].Value = "Interview Status";
                        Sheet.Cells[1, 13].Value = "Video Link";
                        Sheet.Cells[1, 14].Value = "Video Duration";
                        Sheet.Cells[1, 15].Value = "Rate";
                        Sheet.Cells[1, 16].Value = "Amount";
                        Sheet.Cells[1, 17].Value = "Channel Owner";
                        Sheet.Cells[1, 18].Value = "Emergency";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var interviewer in Interviewers)
                            {
                                Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
                                Sheet.Cells[i, 7].Style.Font.Size = 14;
                                i++;
                                foreach (var item in model.Result.Where(x => x.InterviewerEmailid == interviewer).ToList())
                                {
                                    int _location = 0;
                                    Sheet.Cells[i, 1].Value = item.IRFId;
                                    Sheet.Cells[i, 2].Value = item.Date.ToString();
                                    Sheet.Cells[i, 3].Value = item.candidateEmail;
                                    Sheet.Cells[i, 4].Value = item.candidateName;
                                    Sheet.Cells[i, 5].Value = item.PrimarySkillInvite;
                                    Sheet.Cells[i, 6].Value = item.ClientName;
                                    Sheet.Cells[i, 7].Value = item.Comment;
                                    Sheet.Cells[i, 8].Value = item.InterviewerEmailid;
                                    Sheet.Cells[i, 9].Value = item.Type;
                                    if (String.IsNullOrEmpty(item.PreferredLocation))
                                    {
                                        Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                    }
                                    else if (int.TryParse(item.PreferredLocation, out _location))
                                    {
                                        if (_location == 1)
                                        {
                                            Sheet.Cells[i, 10].Value = "India";
                                        }
                                        else
                                        {
                                            Sheet.Cells[i, 10].Value = "OutSide India";
                                        }
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                    }
                                    if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5)
                                    {
                                        Sheet.Cells[i, 11].Value = "Unassign Meeting within 1/2 hr";
                                        Sheet.Cells[i, 12].Value = "";
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 11].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                                        Sheet.Cells[i, 12].Value = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
                                    }
                                    if (!string.IsNullOrEmpty(item.VideoLink))
                                    {
                                        try
                                        {
                                            Sheet.Cells[i, 13].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                            Sheet.Cells[i, 13].Value = "Video";
                                            Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                                            Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                            Sheet.Cells[i, 14].Value = (item.VideoDuration.HasValue && item.VideoDuration.Value > 0) ? item.VideoDuration.Value.ToString() : string.Empty;
                                        }
                                        catch (Exception)
                                        {
                                            Sheet.Cells[i, 13].Value = "No Video";
                                            Sheet.Cells[i, 14].Value = "NA";
                                        }
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 13].Value = "No Video";
                                        Sheet.Cells[i, 14].Value = "NA";
                                    }
                                    Sheet.Cells[i, 15].Value = item.Rate;
                                    Sheet.Cells[i, 16].Value = item.Points;
                                    if (item.Points == 0)
                                    {
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    }
                                    Sheet.Cells[i, 17].Value = item.ChannelOwner;
                                    Sheet.Cells[i, 18].Value = item.IsEmergency == true ? "Y" : "N";
                                    i++;
                                }
                                Sheet.Cells[i, 15].Value = "Sub Total";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum());
                                i++;
                                Sheet.Cells[i, 15].Value = "10% TDS";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                i++;
                                Sheet.Cells[i, 15].Value = "Total";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
                                i = i + 2;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }

                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
        /// <summary>
        /// Finance Team may Export data in Excel sheet
        /// </summary>
        /// <returns></returns>

        [Authorize]
        [HttpGet]
        public FileContentResult ExportFNPayment()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payable Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_AccountDetails.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        var model = _meetingService.FetchAllFNDetail();
                        var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;

                        //Sheet.Column(1).Width = 30;mmmmm
                        //Sheet.Column(2).Width = 30;
                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 25;
                        }
                        Sheet.Cells[1, 1].Value = "IRFID";
                        Sheet.Cells[1, 2].Value = "Date";
                        Sheet.Cells[1, 3].Value = "Candidate Email";
                        Sheet.Cells[1, 4].Value = "Candidate Name";
                        Sheet.Cells[1, 5].Value = "Primary Skill";
                        Sheet.Cells[1, 6].Value = "Client Name";
                        Sheet.Cells[1, 7].Value = "Payment For";
                        Sheet.Cells[1, 8].Value = "Interviewer";
                        Sheet.Cells[1, 9].Value = "Type";
                        Sheet.Cells[1, 10].Value = "Country";
                        Sheet.Cells[1, 11].Value = "Meeting Status";
                        Sheet.Cells[1, 12].Value = "Interview Status";
                        Sheet.Cells[1, 13].Value = "Video Link";
                        Sheet.Cells[1, 14].Value = "Video Duration";
                        Sheet.Cells[1, 15].Value = "Rate";
                        Sheet.Cells[1, 16].Value = "Amount";
                        Sheet.Cells[1, 17].Value = "Channel Owner";
                        Sheet.Cells[1, 18].Value = "Emergency";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var interviewer in Interviewers)
                            {
                                Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
                                Sheet.Cells[i, 7].Style.Font.Size = 14;
                                i++;
                                foreach (var item in model.Result.Where(x => x.InterviewerEmailid == interviewer).ToList())
                                {
                                    int _location = 0;
                                    Sheet.Cells[i, 1].Value = item.IRFId;
                                    Sheet.Cells[i, 2].Value = item.Date.ToString();
                                    Sheet.Cells[i, 3].Value = item.candidateEmail;
                                    Sheet.Cells[i, 4].Value = item.candidateName;
                                    Sheet.Cells[i, 5].Value = item.PrimarySkillInvite;
                                    Sheet.Cells[i, 6].Value = item.ClientName;
                                    Sheet.Cells[i, 7].Value = item.Comment;
                                    Sheet.Cells[i, 8].Value = item.InterviewerEmailid;
                                    Sheet.Cells[i, 9].Value = item.Type;
                                    if (String.IsNullOrEmpty(item.PreferredLocation))
                                    {
                                        Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                    }
                                    else if (int.TryParse(item.PreferredLocation, out _location))
                                    {
                                        if (_location == 1)
                                        {
                                            Sheet.Cells[i, 10].Value = "India";
                                        }
                                        else
                                        {
                                            Sheet.Cells[i, 10].Value = "OutSide India";
                                        }
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 10].Value = item.PreferredLocation;
                                    }
                                    if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5)
                                    {
                                        Sheet.Cells[i, 11].Value = "Unassign Meeting within 1/2 hr";
                                        Sheet.Cells[i, 12].Value = "";
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 11].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                                        Sheet.Cells[i, 12].Value = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
                                    }
                                    if (!string.IsNullOrEmpty(item.VideoLink))
                                    {
                                        try
                                        {
                                            Sheet.Cells[i, 13].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                            Sheet.Cells[i, 13].Value = "Video";
                                            Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                                            Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                            Sheet.Cells[i, 14].Value = (item.VideoDuration.HasValue && item.VideoDuration.Value > 0) ? item.VideoDuration.Value.ToString() : string.Empty;
                                        }
                                        catch (Exception)
                                        {
                                            Sheet.Cells[i, 13].Value = "No Video";
                                            Sheet.Cells[i, 14].Value = "NA";
                                        }
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 13].Value = "No Video";
                                        Sheet.Cells[i, 14].Value = "NA";
                                    }
                                    Sheet.Cells[i, 15].Value = item.Rate;
                                    Sheet.Cells[i, 16].Value = item.Points;
                                    if (item.Points == 0)
                                    {
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    }
                                    Sheet.Cells[i, 17].Value = item.ChannelOwner;
                                    Sheet.Cells[i, 18].Value = item.IsEmergency == true ? "Y" : "N";
                                    i++;
                                }
                                Sheet.Cells[i, 15].Value = "Sub Total";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum());
                                i++;
                                Sheet.Cells[i, 15].Value = "10% TDS";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                i++;
                                Sheet.Cells[i, 15].Value = "Total";
                                Sheet.Cells[i, 16].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
                                i = i + 2;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }

                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
        /// <summary>
        /// Finance Team may be find History of payment that is done by finance team to interviewer
        /// </summary>
        /// <returns></returns>

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PaymentHistory()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = await _meetingService.FetchFNPaymentHistory();
                    if (model != null)
                        model = model.OrderByDescending(x => x.Date).ToList();
                    var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                    foreach (var item in model)
                    {
                        if (!String.IsNullOrEmpty(item.AssessmentGroupId))
                        {
                            var AssessmentGroup = item.AssessmentGroupId.Split(",");
                            string groupdetail = "";
                            foreach (var group in AssessmentGroup)
                            {
                                int groupid = Convert.ToInt32(group);
                                if (String.IsNullOrEmpty(groupdetail))
                                {
                                    groupdetail = AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                }
                                else
                                {
                                    groupdetail = groupdetail + ", " + AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                }
                            }
                            item.AssessmentGroupId = groupdetail;
                        }
                    }
                    if (HttpContext.Session.GetString("PaymentHistoryfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("PaymentHistoryfilters"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model = model.Where(t => t.Date >= filter.StartDate && t.Date <= filter.EndDate).ToList();
                        }
                        filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.InterviewerEmailid).ToArray() : filter.HREmail = filter.HREmail;
                        model = model.Where(x => filter.HREmail.Contains(x.InterviewerEmailid)).ToList();
                    }
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PaymentHistory(CandidateTimelineFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = await _meetingService.FetchFNPaymentHistory();
                    var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                    foreach (var item in model)
                    {
                        if (!String.IsNullOrEmpty(item.AssessmentGroupId))
                        {
                            var AssessmentGroup = item.AssessmentGroupId.Split(",");
                            string groupdetail = "";
                            foreach (var group in AssessmentGroup)
                            {
                                int groupid = Convert.ToInt32(group);
                                if (String.IsNullOrEmpty(groupdetail))
                                {
                                    groupdetail = AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                }
                                else
                                {
                                    groupdetail = groupdetail + ", " + AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                }
                            }
                            item.AssessmentGroupId = groupdetail;
                        }
                    }
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("PaymentHistoryfilters", JsonConvert.SerializeObject(filter));
                    }
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        model = model.Where(t => t.Date >= filter.StartDate && t.Date <= filter.EndDate).ToList();
                    }
                    filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.InterviewerEmailid).ToArray() : filter.HREmail = filter.HREmail;
                    model = model.Where(x => filter.HREmail.Contains(x.InterviewerEmailid)).ToList();
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

        [HttpGet]
        public IActionResult ResetFinaceHistoryFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("PaymentHistoryfilters");
                    return RedirectToAction("PaymentHistory");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public IActionResult PaymentDescription()
        {
            return View();

        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportPaymentHistory()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payable Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_PaymentHistory.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        var model = await _meetingService.FetchFNPaymentHistory();
                        var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                        foreach (var item in model)
                        {
                            if (!String.IsNullOrEmpty(item.AssessmentGroupId))
                            {
                                var AssessmentGroup = item.AssessmentGroupId.Split(",");
                                string groupdetail = "";
                                foreach (var group in AssessmentGroup)
                                {
                                    int groupid = Convert.ToInt32(group);
                                    if (String.IsNullOrEmpty(groupdetail))
                                    {
                                        groupdetail = AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                    }
                                    else
                                    {
                                        groupdetail = groupdetail + ", " + AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                                    }
                                }
                                item.AssessmentGroupId = groupdetail;
                            }
                        }
                        if (HttpContext.Session.GetString("PaymentHistoryfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("PaymentHistoryfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                model = model.Where(t => t.Date >= filter.StartDate && t.Date <= filter.EndDate).ToList();
                            }
                            filter.HREmail = filter.HREmail == null ? filter.HREmail = model.Select(x => x.InterviewerEmailid).ToArray() : filter.HREmail = filter.HREmail;
                            model = model.Where(x => filter.HREmail.Contains(x.InterviewerEmailid)).ToList();
                        }

                        int startColumn = 1; int endColumn = 15;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 15;
                        }
                        Sheet.Cells[1, 1].Value = "Interviewer";
                        Sheet.Cells[1, 2].Value = "Email-Id";
                        Sheet.Cells[1, 3].Value = "Total Amount";
                        Sheet.Cells[1, 4].Value = "TDS deduction";
                        Sheet.Cells[1, 5].Value = "Amount";
                        Sheet.Cells[1, 6].Value = "Payment Status";
                        Sheet.Cells[1, 7].Value = "Payment Date";
                        int i = 2;
                        if (model.Count() > 0)
                        {
                            foreach (var item in model)
                            {
                                Sheet.Cells[i, 1].Value = item.FirstName;
                                Sheet.Cells[i, 2].Value = item.InterviewerEmailid;
                                Sheet.Cells[i, 5].Value = item.Points;
                                if (item.Status == "Advance")
                                {
                                    Sheet.Cells[i, 3].Value = item.Points;
                                    Sheet.Cells[i, 4].Value = 0;
                                }
                                else
                                {
                                    Sheet.Cells[i, 3].Value = item.TotalAmount;
                                    Sheet.Cells[i, 4].Value = item.TotalAmount - item.Points;
                                }
                                Sheet.Cells[i, 6].Value = item.Status;
                                Sheet.Cells[i, 7].Value = item.Date.ToString();
                                i++;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "No data available in table !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InvoiceDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = new List<InvoiceDetailsVM>();
                    string Startdate = string.Empty;
                    string Enddate = string.Empty;
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    var Previousmonth = DateTime.Now.AddMonths(-1).Month;
                    var Previousyear = DateTime.Now.AddMonths(-1).Year;
                    Startdate = Previousyear + "-" + Previousmonth + "-01";
                    Enddate = currentYear + "-" + currentMonth + "-01";
                    if (HttpContext.Session.GetString("InvoiceFilter") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InvoiceFilter>(HttpContext.Session.GetString("InvoiceFilter"));
                        if (filter.Month != null && filter.Year != null)
                        {
                            Previousmonth = Convert.ToInt32(filter.Month);
                            Previousyear = Convert.ToInt32(filter.Year);
                            currentMonth = (Previousmonth + 1) > 12 ? 1 : Previousmonth + 1;
                            currentYear = (Previousmonth + 1) > 12 ? Previousyear + 1 : Previousyear;
                            Startdate = Previousyear + "-" + Previousmonth + "-01";
                            Enddate = currentYear + "-" + currentMonth + "-01";
                        }
                        model = await _meetingService.FetchFNInvoiceDetails(Startdate, Enddate);
                        model = model.OrderBy(x => x.Name).ToList();
                        filter.PaymentStatus = filter.PaymentStatus == null ? filter.PaymentStatus = model.Select(x => x.PaymentStatus.ToString()).ToArray() : filter.PaymentStatus = filter.PaymentStatus;
                        filter.Client = filter.Client == null ? filter.Client = model.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                        model = model.Where(c => filter.PaymentStatus.Contains(c.PaymentStatus.ToString()) && filter.Client.Contains(c.Name)).ToList();
                    }
                    else
                    {
                        model = await _meetingService.FetchFNInvoiceDetails(Startdate, Enddate);
                        model = model.OrderBy(x => x.Name).ToList();
                    }
                    ViewBag.Startdate = Startdate;
                    ViewBag.Enddate = Enddate;
                    System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                    string strMonthName = mfi.GetMonthName(Previousmonth).ToString();
                    ViewBag.DateResult = strMonthName + " " + Previousyear;
                    ViewBag.ClientList = _projectService.GetClients().Select(x => x.Name).Distinct();
                    if (TempData["SuccessErrorMessageCandidate"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                        TempData.Remove("SuccessErrorMessageCandidate");
                    }
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> InvoiceDetails(InvoiceFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    var Previousmonth = DateTime.Now.AddMonths(-1).Month;
                    var Previousyear = DateTime.Now.AddMonths(-1).Year;
                    var Startdate = Previousyear + "-" + Previousmonth + "-01";
                    var Enddate = currentYear + "-" + currentMonth + "-01";
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("InvoiceFilter", JsonConvert.SerializeObject(filter));
                    }
                    if (filter.Month != null && filter.Year != null)
                    {
                        Previousmonth = Convert.ToInt32(filter.Month);
                        Previousyear = Convert.ToInt32(filter.Year);
                        currentMonth = (Previousmonth + 1) > 12 ? 1 : Previousmonth + 1;
                        currentYear = (Previousmonth + 1) > 12 ? Previousyear + 1 : Previousyear;
                        Startdate = Previousyear + "-" + Previousmonth + "-01";
                        Enddate = currentYear + "-" + currentMonth + "-01";
                    }
                    var model = await _meetingService.FetchFNInvoiceDetails(Startdate, Enddate);
                    model = model.OrderBy(x => x.Name).ToList();
                    filter.PaymentStatus = filter.PaymentStatus == null ? filter.PaymentStatus = model.Select(x => x.PaymentStatus.ToString()).ToArray() : filter.PaymentStatus = filter.PaymentStatus;
                    filter.Client = filter.Client == null ? filter.Client = model.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                    model = model.Where(c => filter.PaymentStatus.Contains(c.PaymentStatus.ToString()) && filter.Client.Contains(c.Name)).ToList();
                    ViewBag.Startdate = Startdate;
                    ViewBag.Enddate = Enddate;
                    System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                    string strMonthName = mfi.GetMonthName(Previousmonth).ToString();
                    ViewBag.DateResult = strMonthName + " " + Previousyear;
                    ViewBag.ClientList = _projectService.GetClients().Select(x => x.Name).Distinct();
                    if (TempData["SuccessErrorMessageCandidate"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                        TempData.Remove("SuccessErrorMessageCandidate");
                    }
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

        [HttpGet]
        public async Task<IActionResult> ResetInvoiceDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InvoiceFilter");
                    return RedirectToAction("InvoiceDetails");
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
        public async Task<IActionResult> GetInvoiceDetails(int id, string startdate, string enddate, string Clientname, string jobcountry)
        {
            var model = await _meetingService.GetInvoiceDetailsById(id);
            if (model == null)
            {
                model = new InvoiceDetailsVM();
            }
            model.startdate = startdate;
            model.enddate = enddate;
            model.Name = Clientname;
            model.JobCountry = jobcountry;
            var meetingStatuses = from PaymentStatus s in Enum.GetValues(typeof(PaymentStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };
            model.PaymentStatusList = new SelectList(meetingStatuses, "ID", "Name");
            return PartialView(model);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> GetInvoiceDetails(InvoiceDetailsVM model)
        {
            if (model.fileupload != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.fileupload);

                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        || fileMimeType == "application/pdf")
                {
                    var uploadedFileURL = string.Empty;
                    await _azureStorageHelper.InsertBlob(model.fileupload.FileName.Replace(" ", ""), "invoicefiles", model.fileupload);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("invoicefiles");
                    var blob = containerRef.GetBlockBlobReference(model.fileupload.FileName.Replace(" ", ""));
                    uploadedFileURL = blob.Uri.ToString();
                    model.filelink = uploadedFileURL;
                }

            }
            var result = await _meetingService.SaveInvoiceDetails(model);
            TempData["SuccessErrorMessageCandidate"] = result;
            return RedirectToAction("InvoiceDetails");
        }

        [HttpPost]
        public async Task<JsonResult> GetPaymentHistoryDetailsView(int id)
        {
            var model = await _meetingService.GetPaymentHistoryDetailsView(id);
            //int totalAmount = model.Sum(x => x.Points);
            //int payableAmount = 0;
            //int Tds = 0;
            //if (totalAmount > 0)
            //{
            //    payableAmount = (totalAmount - Convert.ToInt32(Math.Round(totalAmount * .1)));
            //    Tds = totalAmount - payableAmount;
            //}
            foreach (var item in model)
            {
                item.MeetingStatusDetails = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                item.InterviewStatusDetails = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
            }
            return Json(new { result = model/*, totalAmount = totalAmount, payableAmount = payableAmount, tds = Tds */});
        }
        [HttpGet]
        public async Task<IActionResult> CustomerDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = await _meetingService.FetchCustomerDetails();
                    foreach (var item in model)
                    {
                        if (item.JobCountry.ToUpper() == "INDIA" || item.JobCountry == "1")
                        {
                            item.JobCountry = "INDIA";
                        }
                        else { item.JobCountry = "OUTSIDE INDIA"; }

                        item.CompanyType = !String.IsNullOrEmpty(item.CompanyType) ? ((CompanyType)Convert.ToInt32(item.CompanyType)).GetDescription() : string.Empty;
                    }
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
        /// <summary>
        /// Download Interviewers Payout list
        /// </summary>
        /// <param name="st"></param>
        /// <param name="se"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public FileContentResult ExportGenerateFNPayout(DateTime st, DateTime se)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payout Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_PayoutDetails.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        var model = _meetingService.FNAllGeneratedPayoutDetails(st, se.AddDays(1));
                        var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        int startColumn = 1; int endColumn = 12;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 25;
                        }
                        Sheet.Cells[1, 1].Value = "S.No.";
                        Sheet.Cells[1, 2].Value = "Interviewer Name";
                        Sheet.Cells[1, 3].Value = "Email id";
                        Sheet.Cells[1, 4].Value = "PAN Number";
                        Sheet.Cells[1, 5].Value = "A/C holder Name";
                        Sheet.Cells[1, 6].Value = "A/C Number";
                        Sheet.Cells[1, 7].Value = "IFSC";
                        Sheet.Cells[1, 8].Value = "Total";
                        Sheet.Cells[1, 9].Value = "TDS";
                        Sheet.Cells[1, 10].Value = "Balance";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var interviewer in Interviewers)
                            {
                                Sheet.Cells[i, 1].Value = i - 1;
                                Sheet.Cells[i, 2].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
                                Sheet.Cells[i, 3].Value = interviewer;
                                Sheet.Cells[i, 4].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.PAN).FirstOrDefault();
                                Sheet.Cells[i, 5].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountName).FirstOrDefault();
                                Sheet.Cells[i, 6].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountNumber).FirstOrDefault();
                                Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.IFSCCode).FirstOrDefault();
                                Sheet.Cells[i, 8].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum();
                                Sheet.Cells[i, 9].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                Sheet.Cells[i, 10].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
                                i++;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpGet]
        public FileContentResult ExportFNPayout()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payout Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_PayoutDetails.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        var model = _meetingService.FetchAllPayoutDetails();
                        var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
                        string userEmailId = string.Empty;

                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;
                        int startColumn = 1; int endColumn = 12;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 25;
                        }
                        Sheet.Cells[1, 1].Value = "S.No.";
                        Sheet.Cells[1, 2].Value = "Interviewer Name";
                        Sheet.Cells[1, 3].Value = "Email id";
                        Sheet.Cells[1, 4].Value = "PAN Number";
                        Sheet.Cells[1, 5].Value = "A/C holder Name";
                        Sheet.Cells[1, 6].Value = "A/C Number";
                        Sheet.Cells[1, 7].Value = "IFSC";
                        Sheet.Cells[1, 8].Value = "Total";
                        Sheet.Cells[1, 9].Value = "TDS";
                        Sheet.Cells[1, 10].Value = "Balance";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var interviewer in Interviewers)
                            {
                                Sheet.Cells[i, 1].Value = i - 1;
                                Sheet.Cells[i, 2].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
                                Sheet.Cells[i, 3].Value = interviewer;
                                Sheet.Cells[i, 4].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.PAN).FirstOrDefault();
                                Sheet.Cells[i, 5].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountName).FirstOrDefault();
                                Sheet.Cells[i, 6].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountNumber).FirstOrDefault();
                                Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.IFSCCode).FirstOrDefault();
                                Sheet.Cells[i, 8].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum();
                                Sheet.Cells[i, 9].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                Sheet.Cells[i, 10].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
                                i++;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
