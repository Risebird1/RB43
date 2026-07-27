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
using X.PagedList.Extensions;

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
        private readonly IClientService _clientService;
        private readonly ICashfreePayoutService _cashfreePayoutService;
        private readonly IWalletService _walletService;

        public FinanceController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IInterviewerService interviewerService, IWhatsAppHelper whatsAppHelper, IConfiguration configuration, IAzureStorageHelper azureStorageHelper, IProjectService projectService, IFileMimeTypeHelper fileMimeTypeHelper, IClientService clientService, ICashfreePayoutService cashfreePayoutService, IWalletService walletService)
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
            _clientService = clientService;
            _cashfreePayoutService = cashfreePayoutService;
            _walletService = walletService;
        }
        /// <summary>
        /// Interviewer or QC check payment status and request for payment with details
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        public async Task<IActionResult> RBPayment(string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Interviewer")
                {
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        type = "Pending";
                    }
                    ViewBag.Type = type;
                    var model = await _meetingService.RBDetails(emailid);
                    if (model == null)
                    {
                        model = new List<RBInterviewDetails>();
                    }
                    if (type == "Paid")
                    {
                        model = model.Where(x => x.Status == "Paid").ToList();
                    }
                    else
                    {
                        model = model.Where(x => x.Status != "Paid").ToList();
                    }
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
                    var verificationdate = new DateTime(2023, 05, 01);
                    var verificationenddate = new DateTime(2024, 10, 01);
                    foreach (var item in model)
                    {
                        int i = 0;
                        //var _onemoth = item.Date.AddMonths(1).DayOfWeek;
                        //int diff = (7 + (_onemoth - DayOfWeek.Monday)) % 7;
                        //var _firstdate = item.Date.AddMonths(1).AddDays(-1 * diff).Date;
                        //var _lastdate = _firstdate.AddDays(6);
                        //item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        int interviewerbillingcycle = 3;
                        if (item.InterviewerBillingCycle == 1 || item.InterviewerBillingCycle == 2)
                        {
                            interviewerbillingcycle = item.InterviewerBillingCycle;
                        }
                        if (1 <= item.Date.AddMonths(interviewerbillingcycle).Day && item.Date.AddMonths(interviewerbillingcycle).Day <= 7)
                        {
                            var _firstdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(-(item.Date.AddMonths(interviewerbillingcycle).Day - 1));
                            var _lastdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(7 - item.Date.AddMonths(interviewerbillingcycle).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (7 < item.Date.AddMonths(interviewerbillingcycle).Day && item.Date.AddMonths(interviewerbillingcycle).Day <= 14)
                        {
                            var _firstdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(-(item.Date.AddMonths(interviewerbillingcycle).Day - 7));
                            var _lastdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(14 - item.Date.AddMonths(interviewerbillingcycle).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (14 < item.Date.AddMonths(interviewerbillingcycle).Day && item.Date.AddMonths(interviewerbillingcycle).Day <= 21)
                        {
                            var _firstdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(-(item.Date.AddMonths(interviewerbillingcycle).Day - 14));
                            var _lastdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(21 - item.Date.AddMonths(interviewerbillingcycle).Day);
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        else if (21 < item.Date.AddMonths(interviewerbillingcycle).Day && item.Date.AddMonths(interviewerbillingcycle).Day <= 31)
                        {
                            var _firstdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(-(item.Date.AddMonths(interviewerbillingcycle).Day - 21));
                            var _lastdate = item.Date.AddMonths(interviewerbillingcycle).AddDays(-(item.Date.AddMonths(interviewerbillingcycle).Day + 1));
                            item.ExpectedDate = _firstdate.ToShortDateString() + " - " + _lastdate.ToShortDateString();
                        }
                        if (string.IsNullOrWhiteSpace(item.MeetingStatusDetails))
                        {
                            item.MeetingStatusDetails = "";
                        }
                        //if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatusDetails != "No Show(Interviewer)" && item.MeetingStatusDetails != "No Show")
                        if (item.CrossCheckvalue != null && item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5 && item.MeetingStatus != 9)
                        {
                            item.MeetingStatusDetails = "Unassign Meeting within 1/2 hr";
                            item.InterviewStatusDetails = "";
                            item.VideoDuration = 0;
                        }
                        if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                        {
                            item.VideoDuration = item.VideoDuration - 5;
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
        public FileContentResult ExportRBDetails(string type)
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
                    if (profileName == "Interviewer")
                    {
                        var verificationdate = new DateTime(2023, 05, 01);
                        var verificationenddate = new DateTime(2024, 10, 01);
                        if (string.IsNullOrWhiteSpace(type))
                        {
                            type = "Pending";
                        }
                        var model = _meetingService.RBDetails(emailid).Result;
                        if (type == "Paid")
                        {
                            model = model.Where(x => x.Status == "Paid").ToList();
                        }
                        else
                        {
                            model = model.Where(x => x.Status != "Paid").ToList();
                        }
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
                        Sheet.Cells[1, 5].Value = "Base Pay";
                        Sheet.Cells[1, 6].Value = "Late Report Penalty";
                        Sheet.Cells[1, 7].Value = "Variable Pay";
                        Sheet.Cells[1, 8].Value = "QC Amt";
                        Sheet.Cells[1, 9].Value = "Payment Deductions";
                        Sheet.Cells[1, 10].Value = "Video Rewards";
                        Sheet.Cells[1, 11].Value = "Amount";
                        Sheet.Cells[1, 12].Value = "Video Link";
                        Sheet.Cells[1, 13].Value = "Video Duration";
                        Sheet.Cells[1, 14].Value = "Date";
                        Sheet.Cells[1, 15].Value = "Status";
                        Sheet.Cells[1, 16].Value = "Location";
                        Sheet.Cells[1, 17].Value = "Meeting Status";

                        int i = 2;
                        if (model.Count() > 0)
                        {
                            foreach (var item in model)
                            {
                                int _location = 0;
                                Sheet.Cells[i, 1].Value = item.Comment;
                                Sheet.Cells[i, 2].Value = item.InterviewId;
                                Sheet.Cells[i, 3].Value = item.Type;
                                Sheet.Cells[i, 4].Value = item.Rate;
                                Sheet.Cells[i, 5].Value = item.PrePoints;
                                Sheet.Cells[i, 6].Value = item.Penaltyamount;
                                Sheet.Cells[i, 7].Value = item.CandidateRatingAmount + item.ClientRatingAmount;
                                Sheet.Cells[i, 8].Value = item.QCPoints;
                                Sheet.Cells[i, 9].Value = item.PaymentSLA;
                                Sheet.Cells[i, 10].Value = item.VideoReward;
                                Sheet.Cells[i, 11].Value = item.Points;
                                Sheet.Cells[i, 14].Value = item.Date;
                                Sheet.Cells[i, 15].Value = item.Status;
                                if (string.IsNullOrEmpty(item.PreferredLocation))
                                {
                                    Sheet.Cells[i, 16].Value = item.PreferredLocation;
                                }
                                else if (int.TryParse(item.PreferredLocation, out _location))
                                {
                                    if (_location == 1)
                                    {
                                        Sheet.Cells[i, 16].Value = "India";
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 16].Value = "OutSide India";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 16].Value = item.PreferredLocation;
                                }
                                if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5 && item.MeetingStatus != 9)
                                {
                                    item.MeetingStatusDetails = "Unassign Meeting within 1/2 hr";
                                    item.InterviewStatusDetails = "";
                                }
                                else
                                {
                                    Sheet.Cells[i, 17].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                                }
                                if (item.CrossCheckvalue.Contains("Penalty") && item.MeetingStatus != 5)
                                {
                                    Sheet.Cells[i, 12].Value = "No Video";
                                    Sheet.Cells[i, 13].Value = "NA";
                                }
                                else if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    Sheet.Cells[i, 12].Value = "No Video";
                                    Sheet.Cells[i, 13].Value = "NA";
                                }
                                else if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 12].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                        Sheet.Cells[i, 12].Value = "Video";
                                        Sheet.Cells[i, 12].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 12].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                        if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                                        {
                                            item.VideoDuration = item.VideoDuration - 5;
                                        }
                                        Sheet.Cells[i, 13].Value = (item.VideoDuration.HasValue && item.VideoDuration.Value > 0) ? item.VideoDuration.Value.ToString() : string.Empty;
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 12].Value = "No Video";
                                        Sheet.Cells[i, 13].Value = "NA";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 12].Value = "No Video";
                                    Sheet.Cells[i, 13].Value = "NA";
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
                if (profileName == "Interviewer")
                {

                    var model = await _meetingService.RBUpdateStatusDetails(emailid);
                    //Send a mail to FN
                    Mail _mail = new Mail
                    {
                        TO = "finance@risebird.io",
                        Subject = "Interviewer Submitted Payment details"
                    };
                    var emailsend = await _notificationHelper.RBPayNotification(_mail, emailid);
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
                    if (string.IsNullOrEmpty(year))
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
            var verificationdate = new DateTime(2023, 05, 01);
            var verificationenddate = new DateTime(2024, 10, 01);
            foreach (var item in model)
            {
                item.MeetingStatusDetails = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                item.InterviewStatusDetails = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
                if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                {
                    item.VideoDuration = item.VideoDuration - 5;
                }
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
        public async Task<JsonResult> SendQuerybyFN(string emailid, string query)
        {
            //send a mail to interviewer
            Mail _mail = new Mail
            {
                TO = emailid,
                CC = "finance@risebird.io",
                Subject = "RiseBird: Finance Team has query regarding you payment request"
            };
            var emailsend = await _notificationHelper.RBFNQueryNotification(_mail, query);
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
            if (resultset.Count > 0)
            {
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
                if (string.IsNullOrEmpty(paymenthistory))
                    paymenthistory = "0";
                var model = await _meetingService.RBConfirmPayment(id, index, st, se.AddDays(1), Convert.ToInt32(paymenthistory));
                //Start:Send email to Interviewer
                Mail _mail = new Mail
                {
                    TO = email,
                    CC = "finance@risebird.io",
                    Subject = "RiseBird: Finance Team has done payment"
                };
                var emailsend = await _notificationHelper.RBFNPaymentConfirmationNotification(_mail, amount, sb);
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
            }
            else
            {
                return Json("Failed to pay the payment");
            }

            //return RedirectToAction("FNPayment");
        }
        /// <summary>
        /// Finance Team triggers a Cashfree bank payout for an interviewer.
        /// Returns structured JSON: { success, message, payoutId }.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ConfirmPaymentViaCashfree(int id, int index, string email, string amount, DateTime st, DateTime se)
        {
            try
            {
                var (success, message) = await _meetingService.ConfirmPaymentViaCashfree(id, index, email, amount, st, se);

                if (success)
                {
                    // Extract payoutId from message e.g. "Payment initiated via Cashfree. Reference: RB_123_..."
                    var payoutId = string.Empty;
                    var refIndex = message.IndexOf("Reference:", StringComparison.OrdinalIgnoreCase);
                    if (refIndex >= 0)
                        payoutId = message.Substring(refIndex + "Reference:".Length).Trim();

                    // Fire-and-forget: build notification table and send email + WhatsApp
                    //try
                    //{
                    //    var resultset = await _meetingService.RBInterviewerDetails(id, index);
                    //    if (resultset != null && resultset.Count > 0)
                    //    {
                    //        if (index == 2)
                    //        {
                    //            var advances = resultset.Where(x => x.Status == "Advance").ToList();
                    //            resultset = resultset.Where(x => x.Date >= st && x.Date <= se.AddDays(1)).ToList();
                    //            resultset = resultset.Union(advances).ToList();
                    //        }

                    //        StringBuilder sb = new StringBuilder();
                    //        sb.Append("<table cellspacing='0' width='100%'><thead><tr><th>Date</th><th>Payment for</th><th>Type</th><th>Status</th><th>Total Amount</th><th>Payable Amount</th></tr></thead><tbody>");
                    //        foreach (var item in resultset)
                    //        {
                    //            sb.Append("<tr>");
                    //            sb.Append("<td>" + item.Date.ToString() + "</td>");
                    //            sb.Append("<td>" + item.Comment + "</td>");
                    //            sb.Append("<td>" + item.Type + "</td>");
                    //            sb.Append("<td>" + (item.Status == "Advance" ? item.Status : "Paid") + "</td>");
                    //            sb.Append("<td>" + item.Points + "</td>");
                    //            sb.Append("<td>" + (item.Status != "Advance" ? Math.Round(item.Points - item.Points * .1) : (object)item.Points) + "</td>");
                    //            sb.Append("</tr>");
                    //        }
                    //        sb.Append("</tbody></table>");

                    //        var payableDisplay = (int)Math.Round(
                    //            resultset.Where(x => x.Status != "Advance").Sum(x => x.Points * 0.9)
                    //            - resultset.Where(x => x.Status == "Advance").Sum(x => (double)x.Points));

                    //        Mail _mail = new Mail
                    //        {
                    //            TO      = email,
                    //            CC      = "finance@risebird.io",
                    //            Subject = "RiseBird: Finance Team has done payment via Cashfree"
                    //        };
                    //        await _notificationHelper.RBFNPaymentConfirmationNotification(_mail, payableDisplay.ToString(), sb);

                    //        WhatsAppVM _whatsApp = new WhatsAppVM
                    //        {
                    //            Emailid       = email,
                    //            InterviewDate = payableDisplay.ToString(),
                    //            Event         = "Payment Notification"
                    //        };
                    //        _whatsAppHelper.WhatsAppSendOnAssignation(_whatsApp);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    //}

                    return Json(new { success = true, message, payoutId });
                }

                return Json(new { success = false, message });
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        /// <summary>
        /// Returns all interviewers from the current session-cached FNPayment list
        /// that have a pending payable amount AND a verified bank account.
        /// Used to populate the Batch Payout modal with the full (unpaginated) dataset.
        /// </summary>
        [HttpGet]
        public JsonResult GetAllBatchPayoutCandidates()
        {
            try
            {
                var sessionData = HttpContext.Session.GetString("financemodel");
                if (string.IsNullOrEmpty(sessionData))
                    return Json(new { success = false, message = "Session expired. Please reload the page.", data = new List<object>() });

                var allItems = JsonConvert.DeserializeObject<List<Risebird.Entities.ViewModel.RBInterviewDetails>>(sessionData);
                var candidates = allItems
                    .Where(x => x.Points > 0 && x.IsBankVerified)
                    .Select(x => new { x.id, email = x.Comment, amount = x.Points, x.IsBankVerified })
                    .ToList();

                return Json(new { success = true, data = candidates });
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Json(new { success = false, message = "Failed to load candidates.", data = new List<object>() });
            }
        }

        /// <summary>
        /// Processes Cashfree bank payouts for multiple interviewers in a single request.
        /// Accepts a comma-separated list of interviewer IDs; each is paid using the same
        /// index/date-range context that was active on the FNPayment page.
        /// Returns JSON: { results: [{ id, email, success, message }] }
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> BatchConfirmPaymentViaCashfree(string ids, int index, DateTime? st, DateTime? se)
        {
            var results = new List<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                    return Json(new { results, error = "No interviewer IDs provided." });

                var idList = ids.Split(',')
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .Select(int.Parse)
                                .ToList();

                var sessionData = HttpContext.Session.GetString("financemodel");
                List<Risebird.Entities.ViewModel.RBInterviewDetails> allItems = null;
                if (!string.IsNullOrEmpty(sessionData))
                    allItems = JsonConvert.DeserializeObject<List<Risebird.Entities.ViewModel.RBInterviewDetails>>(sessionData);

                var stVal = st ?? DateTime.MinValue;
                var seVal = se ?? DateTime.MinValue;

                foreach (var id in idList)
                {
                    var item = allItems?.FirstOrDefault(x => x.id == id);
                    if (item == null)
                    {
                        results.Add(new { id, email = "", success = false, message = "Interviewer not found in session." });
                        continue;
                    }
                    try
                    {
                        var (success, message) = await _meetingService.ConfirmPaymentViaCashfree(
                            id, index, item.Comment, item.Points.ToString(), stVal, seVal);
                        results.Add(new { id, email = item.Comment, success, message });
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        results.Add(new { id, email = item.Comment, success = false, message = "Unexpected error during payout." });
                    }
                }

                return Json(new { results });
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Json(new { results, error = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Re-initiates a Cashfree payout for a PaymentHistory row whose PayoutStatus is TRANSFER_FAILED.
        /// Returns structured JSON: { success, message, payoutId }.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> RetryPaymentViaCashfree(int paymentHistoryId)
        {
            try
            {
                if (paymentHistoryId <= 0)
                    return Json(new { success = false, message = "Invalid payment history id." });

                var (success, message) = await _meetingService.RetryPaymentViaCashfree(paymentHistoryId);
                if (!success)
                    return Json(new { success = false, message });

                var payoutId = string.Empty;
                var refIndex = message.IndexOf("Reference:", StringComparison.OrdinalIgnoreCase);
                if (refIndex >= 0)
                    payoutId = message.Substring(refIndex + "Reference:".Length).Trim();

                return Json(new { success = true, message, payoutId });
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        /// <summary>
        /// Refreshes the Cashfree transfer status for a previously initiated payout.
        /// Returns structured JSON: { success, payoutId, status, amount, utr, updated } on success.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetPayoutStatus(string payoutId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payoutId))
                    return Json(new { success = false, message = "payoutId is required." });

                var result = await _meetingService.GetCashfreePayoutStatus(payoutId);
                if (!result.Success)
                    return Json(new { success = false, message = result.ErrorMessage ?? "Failed to fetch payout status." });

                return Json(new
                {
                    success  = true,
                    payoutId = result.PayoutId,
                    status   = result.Status,
                    amount   = result.Amount,
                    utr      = result.Utr,
                    updated  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return Json(new { success = false, message = "An unexpected error occurred while fetching payout status." });
            }
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
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper().Trim();
                    model = model.Where(x => (!string.IsNullOrEmpty(x.Comment) ? x.Comment.ToUpper().Contains(searchValue) : x.Comment == "")).ToList();
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
            if (string.IsNullOrEmpty(paymenthistory))
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
                var emailsend = await _notificationHelper.RBFNPaymentConfirmationNotification(_mail, Amount.ToString(), sb);
                WhatsAppVM _whatsApp = new WhatsAppVM();
                _whatsApp.Emailid = Interviewer;
                _whatsApp.InterviewDate = Amount.ToString();
                _whatsApp.Event = "Payment Notification";
                await _whatsAppHelper.WhatsAppSendOnAssignation(_whatsApp);
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
                //var totalqcamount = await _meetingService.FNFetchToalQCAmountStatus(st, se.AddDays(1));
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
                //int qcamount = Convert.ToInt32((from ad in totalqcamount
                //                                select Math.Round(ad.Points * .9)).ToList().Sum());
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
                return Json(new { result = model.Skip((Page - 1) * 10).Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count, currentpage = Page, totalAmount = totalAmount, payableAmount = payableAmount, tds = Tds, totalpaidamount = totalpaidamount, totalinterviewamount = totalinterviewamount, totaldroppedamount = totaldroppedamount, totalNoShowamount = totalNoShowamount, pendingAmount = pendingAmount });
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
                        var verificationdate = new DateTime(2023, 05, 01);
                        var verificationenddate = new DateTime(2024, 10, 01);
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
                        Sheet.Cells[1, 16].Value = "Base Pay";
                        Sheet.Cells[1, 17].Value = "Late Report Penalty";
                        Sheet.Cells[1, 18].Value = "Surcharge";
                        Sheet.Cells[1, 19].Value = "Candidate Rating Amount";
                        Sheet.Cells[1, 20].Value = "Client Rating Amount";
                        Sheet.Cells[1, 21].Value = "QC Amount";
                        Sheet.Cells[1, 22].Value = "Payment SLA";
                        Sheet.Cells[1, 23].Value = "Video Reward";
                        Sheet.Cells[1, 24].Value = "Amount";
                        Sheet.Cells[1, 25].Value = "Channel Owner";
                        Sheet.Cells[1, 26].Value = "Emergency";
                        Sheet.Cells[1, 27].Value = "Min Video Duration";
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
                                    if (string.IsNullOrEmpty(item.PreferredLocation))
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
                                    if (item.Comment.Contains("Penalty") && item.MeetingStatus != 5 && item.MeetingStatus != 9)
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
                                        }
                                        catch (Exception)
                                        {
                                            Sheet.Cells[i, 13].Value = "No Video";
                                        }
                                    }
                                    else
                                    {
                                        Sheet.Cells[i, 13].Value = "No Video";
                                    }
                                    if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                                    {
                                        item.VideoDuration = item.VideoDuration - 5;
                                    }
                                    Sheet.Cells[i, 14].Value = (item.VideoDuration.HasValue && item.VideoDuration.Value > 0) ? item.VideoDuration.Value.ToString() : string.Empty;
                                    Sheet.Cells[i, 15].Value = item.Rate;
                                    Sheet.Cells[i, 16].Value = item.PrePoints;
                                    Sheet.Cells[i, 17].Value = item.Penaltyamount;
                                    Sheet.Cells[i, 18].Value = item.Surcharge;
                                    Sheet.Cells[i, 19].Value = item.CandidateRatingAmount;
                                    Sheet.Cells[i, 20].Value = item.ClientRatingAmount;
                                    Sheet.Cells[i, 21].Value = item.QCPoints;
                                    Sheet.Cells[i, 22].Value = item.PaymentSLA;
                                    Sheet.Cells[i, 23].Value = item.VideoReward;
                                    Sheet.Cells[i, 24].Value = item.Points;
                                    if (item.Points == 0)
                                    {
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    }
                                    Sheet.Cells[i, 25].Value = item.ChannelOwner;
                                    Sheet.Cells[i, 26].Value = item.IsEmergency == true ? "Y" : "N";
                                    Sheet.Cells[i, 27].Value = (item.MaxVideoDuration.HasValue && item.MaxVideoDuration.Value > 0) ? item.MaxVideoDuration.Value.ToString() : string.Empty;
                                    i++;
                                }
                                Sheet.Cells[i, 23].Value = "Sub Total";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum());
                                i++;
                                Sheet.Cells[i, 23].Value = "10% TDS";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                i++;
                                Sheet.Cells[i, 23].Value = "Total";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
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
                        var verificationdate = new DateTime(2023, 05, 01);
                        var verificationenddate = new DateTime(2024, 10, 01);
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
                        Sheet.Cells[1, 16].Value = "Base Pay";
                        Sheet.Cells[1, 17].Value = "Penalty";
                        Sheet.Cells[1, 18].Value = "Surcharge";
                        Sheet.Cells[1, 19].Value = "Candidate Rating Amount";
                        Sheet.Cells[1, 20].Value = "Client Rating Amount";
                        Sheet.Cells[1, 21].Value = "QC Amount";
                        Sheet.Cells[1, 22].Value = "Payment SLA";
                        Sheet.Cells[1, 23].Value = "Video Reward";
                        Sheet.Cells[1, 24].Value = "Amount";
                        Sheet.Cells[1, 25].Value = "Channel Owner";
                        Sheet.Cells[1, 26].Value = "Emergency";
                        Sheet.Cells[1, 27].Value = "Min Video Duration";
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
                                    if (string.IsNullOrEmpty(item.PreferredLocation))
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
                                    if (item.Comment.Contains("Penalty") && item.MeetingStatus != 5 && item.MeetingStatus != 9)
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
                                            if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                                            {
                                                item.VideoDuration = item.VideoDuration - 5;
                                            }
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
                                    Sheet.Cells[i, 16].Value = item.PrePoints;
                                    Sheet.Cells[i, 17].Value = item.Penaltyamount;
                                    Sheet.Cells[i, 18].Value = item.Surcharge;
                                    Sheet.Cells[i, 19].Value = item.CandidateRatingAmount;
                                    Sheet.Cells[i, 20].Value = item.ClientRatingAmount;
                                    Sheet.Cells[i, 21].Value = item.QCPoints;
                                    Sheet.Cells[i, 22].Value = item.PaymentSLA;
                                    Sheet.Cells[i, 23].Value = item.VideoReward;
                                    Sheet.Cells[i, 24].Value = item.Points;
                                    if (item.Points == 0)
                                    {
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        Sheet.Cells["A" + i.ToString() + ":Z" + i.ToString()].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                    }
                                    Sheet.Cells[i, 25].Value = item.ChannelOwner;
                                    Sheet.Cells[i, 26].Value = item.IsEmergency == true ? "Y" : "N";
                                    Sheet.Cells[i, 27].Value = (item.MaxVideoDuration.HasValue && item.MaxVideoDuration.Value > 0) ? item.MaxVideoDuration.Value.ToString() : string.Empty;
                                    i++;
                                }
                                Sheet.Cells[i, 23].Value = "Sub Total";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum());
                                i++;
                                Sheet.Cells[i, 23].Value = "10% TDS";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
                                i++;
                                Sheet.Cells[i, 23].Value = "Total";
                                Sheet.Cells[i, 24].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
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
                    //var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                    //foreach (var item in model)
                    //{
                    //    if (!string.IsNullOrEmpty(item.AssessmentGroupId))
                    //    {
                    //        var AssessmentGroup = item.AssessmentGroupId.Split(",");
                    //        string groupdetail = "";
                    //        foreach (var group in AssessmentGroup)
                    //        {
                    //            if (group != "")
                    //            {
                    //                int groupid = Convert.ToInt32(group);
                    //                if (string.IsNullOrEmpty(groupdetail))
                    //                {
                    //                    groupdetail = AssessmentsGroups.Where(x => x.id == groupid)?.FirstOrDefault()?.ToString();
                    //                }
                    //                else
                    //                {
                    //                    groupdetail = groupdetail + ", " + AssessmentsGroups.Where(x => x.id == groupid)?.FirstOrDefault()?.ToString();
                    //                }
                    //            }
                    //        }
                    //        item.AssessmentGroupId = groupdetail;
                    //    }
                    //}
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
                    else
                    {
                        var StartDate = DateTime.Now.AddDays(-30);
                        var EndDate = DateTime.Now.AddDays(1);
                        model = model.Where(t => t.Date >= StartDate && t.Date <= EndDate).ToList();
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
        public IActionResult PaymentHistory(CandidateTimelineFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("PaymentHistoryfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("PaymentHistory");
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
                        //var AssessmentsGroups = _interviewerService.FetchAssessmentGroups();
                        //foreach (var item in model)
                        //{
                        //    if (!string.IsNullOrEmpty(item.AssessmentGroupId))
                        //    {
                        //        var AssessmentGroup = item.AssessmentGroupId.Split(",");
                        //        string groupdetail = "";
                        //        foreach (var group in AssessmentGroup)
                        //        {
                        //            int groupid = Convert.ToInt32(group);
                        //            if (string.IsNullOrEmpty(groupdetail))
                        //            {
                        //                groupdetail = AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                        //            }
                        //            else
                        //            {
                        //                groupdetail = groupdetail + ", " + AssessmentsGroups.Where(x => x.id == groupid).FirstOrDefault().ToString();
                        //            }
                        //        }
                        //        item.AssessmentGroupId = groupdetail;
                        //    }
                        //}
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
                        else
                        {
                            var StartDate = DateTime.Now.AddDays(-30);
                            var EndDate = DateTime.Now.AddDays(1);
                            model = model.Where(t => t.Date >= StartDate && t.Date <= EndDate).ToList();
                        }
                        int startColumn = 1; int endColumn = 20;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 15;
                        }
                        Sheet.Cells[1, 1].Value = "Interviewer";
                        Sheet.Cells[1, 2].Value = "Email-Id";
                        Sheet.Cells[1, 3].Value = "PAN";
                        Sheet.Cells[1, 4].Value = "Name as Pan";
                        Sheet.Cells[1, 5].Value = "Name as Bank Account";
                        Sheet.Cells[1, 6].Value = "Total Amount";
                        Sheet.Cells[1, 7].Value = "TDS deduction";
                        Sheet.Cells[1, 8].Value = "Amount";
                        Sheet.Cells[1, 9].Value = "Status";
                        Sheet.Cells[1, 10].Value = "Comment";
                        Sheet.Cells[1, 11].Value = "Payment Date";
                        Sheet.Cells[1, 12].Value = "Transaction Date";
                        Sheet.Cells[1, 13].Value = "Transfer Id";
                        Sheet.Cells[1, 14].Value = "UTR";
                        Sheet.Cells[1, 15].Value = "Cf Transfer Id";
                        Sheet.Cells[1, 16].Value = "Payment Status";
                        int i = 2;
                        if (model.Count() > 0)
                        {
                            foreach (var item in model)
                            {
                                Sheet.Cells[i, 1].Value = item.FirstName;
                                Sheet.Cells[i, 2].Value = item.InterviewerEmailid;
                                Sheet.Cells[i, 3].Value = item.PAN;
                                Sheet.Cells[i, 4].Value = item.NameAtPan;
                                Sheet.Cells[i, 5].Value = item.NameAtBank;
                                Sheet.Cells[i, 8].Value = item.Points;
                                if (item.Status == "Advance")
                                {
                                    Sheet.Cells[i, 6].Value = item.Points;
                                    Sheet.Cells[i, 7].Value = 0;
                                }
                                else
                                {
                                    Sheet.Cells[i, 6].Value = item.TotalAmount;
                                    Sheet.Cells[i, 7].Value = item.TotalAmount - item.Points;
                                }
                                Sheet.Cells[i, 9].Value = item.Status;
                                Sheet.Cells[i, 10].Value = item.Comment;
                                Sheet.Cells[i, 11].Value = item.Date.ToString();
                                Sheet.Cells[i, 12].Value = item.TransactionDate?.ToString("yyyy-MM-dd HH:mm:ss");
                                Sheet.Cells[i, 13].Value = item.TransferId;
                                Sheet.Cells[i, 14].Value = item.TransferUtr;
                                Sheet.Cells[i, 15].Value = item.CfTransferId;
                                Sheet.Cells[i, 16].Value = item.PayoutStatus;
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

        #region GenerateInvoice

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GenerateInvoice(int? clientId = null, int? month = null, int? year = null, int? billingOwnerId = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var clients = _projectService.GetClients()
                        .Where(x => !string.IsNullOrEmpty(x.Name))
                        .OrderBy(x => x.Name)
                        .ToList();
                    ViewBag.ClientList = new SelectList(clients, "id", "Name");
                    ViewBag.CurrentMonth = month ?? DateTime.Now.Month;
                    ViewBag.CurrentYear  = year ?? DateTime.Now.Year;
                    ViewBag.PreFillClientId = clientId ?? 0;
                    ViewBag.PreFillBillingOwnerId = billingOwnerId ?? 0;
                    return View(new GenerateInvoiceVM());
                }
                return RedirectToAction("SignOut", "Session");
            }
            return RedirectToAction("SignOut", "Session");
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetClientInvoiceData(int clientId, int month, int year, int billingOwnerId = 0)
        {
            try
            {
                var dataTask          = _meetingService.GetClientInvoiceFormData(clientId, month, year, billingOwnerId);
                var invoiceNumTask    = _meetingService.GetNextInvoiceNumber(month, year);
                var billingOwnersTask = _meetingService.GetClientBillingOwners(clientId);

                await Task.WhenAll(dataTask, invoiceNumTask, billingOwnersTask);

                var data          = dataTask.Result;
                data.InvoiceNumber = invoiceNumTask.Result;
                var billingOwners = billingOwnersTask.Result;

                var subtotal      = data.LineItems?.Sum(x => x.LineTotal) ?? 0;
                var gstAmount     = data.GSTApplicable ? Math.Round(subtotal * 18 / 100, 2) : 0;
                var invoiceValue  = subtotal + gstAmount;
                var tdsAmount     = Math.Round(subtotal * 10 / 100, 2);
                var netReceivable = invoiceValue - tdsAmount;

                return Json(new
                {
                    invoiceNumber      = data.InvoiceNumber,
                    billingOwnerEmail  = data.BillingOwnerEmail,
                    billingOwnerId     = data.BillingOwnerId,
                    billingOwners      = billingOwners,
                    creditPeriod       = data.PaymentTerm > 0 ? data.PaymentTerm + " Days" : string.Empty,
                    gstNumber          = data.GSTNumber,
                    gstApplicable      = data.GSTApplicable,
                    billingType              = data.BillingType,
                    interviewerBillingType   = data.InterviewerBillingType,
                    billingCurrency          = string.IsNullOrEmpty(data.BillingCurrency) ? "INR" : data.BillingCurrency,
                    perInterviewRate         = data.PerInterviewRate,
                    aiPerInterviewRate       = data.AIPerInterviewRate,
                    lineItems                = data.LineItems,
                    subtotal           = subtotal,
                    gstAmount          = gstAmount,
                    invoiceValue       = invoiceValue,
                    tdsAmount          = tdsAmount,
                    netReceivable      = netReceivable
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { error = "Failed to load invoice data." });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> CheckDuplicateInvoice(int clientId, int billingOwnerId, int month, int year)
        {
            try
            {
                var existing = await _meetingService.CheckDuplicateInvoice(clientId, billingOwnerId, month, year);
                if (existing != null && existing.id > 0)
                {
                    System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                    return Json(new
                    {
                        isDuplicate   = true,
                        invoiceNumber = existing.InvoiceNumber,
                        createdBy     = existing.CreatedBy,
                        createdOn     = existing.CreatedOn?.ToString("dd MMM yyyy")
                    });
                }
                return Json(new { isDuplicate = false });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { isDuplicate = false });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ViewInvoice(int invoiceId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var invoice = await _meetingService.GetGeneratedInvoiceById(invoiceId);
                    if (invoice == null)
                    {
                        TempData["InvoiceListMessage"] = "Error: Invoice not found.";
                        return RedirectToAction("InvoiceList");
                    }
                    if (TempData["ViewInvoiceMsg"] != null)
                        ViewBag.Message = TempData["ViewInvoiceMsg"].ToString();
                    return View(invoice);
                }
                return RedirectToAction("SignOut", "Session");
            }
            return RedirectToAction("SignOut", "Session");
        }

        /// <summary>
        /// Printable / downloadable tax invoice PDF for a generated invoice.
        /// Shows SEZ supply statement at top when TaxScenario is SEZ without/with Tax.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InvoicePdf(int invoiceId)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var invoice = await _meetingService.GetGeneratedInvoiceById(invoiceId);
                    if (invoice == null)
                    {
                        TempData["InvoiceListMessage"] = "Error: Invoice not found.";
                        return RedirectToAction("InvoiceList");
                    }

                    ViewBag.SellerName = _configuration["RisebirdDetails:AccountName"]
                        ?? TdsConstants.DeductorName;
                    ViewBag.SellerAddress = _configuration["RisebirdDetails:Address"];
                    ViewBag.SellerGSTIN = _configuration["RisebirdDetails:GSTIN"];
                    ViewBag.SellerPAN = _configuration["RisebirdDetails:PAN"];
                    ViewBag.SellerTAN = _configuration["RisebirdDetails:TAN"];
                    ViewBag.SellerEmail = "finance@risebird.io";
                    ViewBag.SellerStateName = _configuration["RisebirdDetails:StateName"];
                    ViewBag.SellerStateCode = _configuration["RisebirdDetails:StateCode"];
                    ViewBag.LUTBondNo = _configuration["RisebirdDetails:LUTBondNo"];
                    ViewBag.LUTValidFrom = _configuration["RisebirdDetails:LUTValidFrom"];
                    ViewBag.LUTValidTo = _configuration["RisebirdDetails:LUTValidTo"];
                    ViewBag.HSNSAC = _configuration["RisebirdDetails:HSNSAC"] ?? "998399";

                    try
                    {
                        var client = await _clientService.GetClient(invoice.ClientId);
                        ViewBag.BuyerAddress = client?.OfficeAddress;
                    }
                    catch { /* buyer address optional */ }

                    return View(invoice);
                }
                return RedirectToAction("SignOut", "Session");
            }
            return RedirectToAction("SignOut", "Session");
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetInvoiceById(int invoiceId)
        {
            try
            {
                var invoice = await _meetingService.GetGeneratedInvoiceById(invoiceId);
                if (invoice == null) return Json(new { error = "Invoice not found." });
                return Json(invoice);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { error = "Failed to load invoice." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> UpdateInvoiceDetails(int invoiceId, string gstNumber, string creditPeriod,
            string irn, string eInvoiceFileLink, int status, decimal subtotal, decimal gstAmount, decimal invoiceValue,
            decimal tdsAmount, decimal netReceivable, string lineItemsJson, int taxScenario = 0,
            decimal? cashCreditAmount = null, string cashTransactionRef = null, DateTime? cashTransactionDate = null,
            string latestRemark = null)
        {
            try
            {
                List<InvoiceLineItemVM> lineItems = null;
                if (!string.IsNullOrWhiteSpace(lineItemsJson))
                {
                    try { lineItems = JsonConvert.DeserializeObject<List<InvoiceLineItemVM>>(lineItemsJson); }
                    catch { lineItems = new List<InvoiceLineItemVM>(); }
                }

                const int StatusPaid     = 5;
                const int BillingModelM1 = 1;

                // When marking as Paid (status=5), post InvoiceValue (base amount) to the client wallet
                if (status == StatusPaid)
                {
                    var invoice = await _meetingService.GetGeneratedInvoiceById(invoiceId);
                    if (invoice != null && invoice.BillingModel != BillingModelM1)
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                        var billingType = await _clientService.GetClientBillingType(invoice.ClientId);
                        int? billingOwnerId = billingType == 1 ? invoice.BillingOwnerId : (int?)null;
                        // Credit = InvoiceValue (Subtotal + GST — full invoice value).
                        // Passing invoiceId ensures existing wallet row is updated, not duplicated.
                        await _walletService.AddClientWalletBalance(
                            billingType, invoice.ClientId, (int)subtotal,
                            emailid, DateTime.Now, billingOwnerId,
                            cashTransactionRef, null, cashTransactionDate, "CashCredit",
                            invoiceId: invoiceId);
                    }
                }

                // Save cashCreditAmount on the invoice record for reference
                decimal? savedCashAmount = (cashCreditAmount.HasValue && cashCreditAmount.Value > 0)
                    ? (decimal?)cashCreditAmount.Value : null;
                var success = await _meetingService.UpdateInvoiceDetails(invoiceId, gstNumber, creditPeriod, irn, eInvoiceFileLink, status,
                    subtotal, gstAmount, invoiceValue, tdsAmount, netReceivable, lineItems, latestRemark, cashTransactionRef, savedCashAmount, taxScenario);
                return Json(new
                {
                    success,
                    message     = success ? "Invoice updated successfully." : "Failed to update invoice.",
                    redirectUrl = success ? Url.Action("ViewInvoice", "Finance", new { invoiceId }) : (string)null
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> SaveInvoiceExpectedDate(int invoiceId, string expectedDate)
        {
            try
            {
                DateTime? date = null;
                if (!string.IsNullOrWhiteSpace(expectedDate) &&
                    DateTime.TryParse(expectedDate, out DateTime parsed))
                {
                    date = parsed.Date;
                }
                var success = await _meetingService.SaveInvoiceExpectedDate(invoiceId, date);
                return Json(new { success, message = success ? "Expected date saved." : "Failed to save expected date." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [Authorize]
        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<JsonResult> UploadEInvoiceFile(IFormFile[] files)
        {
            try
            {
                if (files == null || files.Length == 0)
                    return Json(new { success = false, message = "No file(s) received." });

                var allowed = new[] { "application/pdf", "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

                var fileUrls   = new List<string>();
                var fileNames  = new List<string>();
                var timestamp  = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                    if (!allowed.Contains(fileMimeType))
                        return Json(new { success = false, message = $"'{file.FileName}' is not allowed. Only PDF or Word documents are supported." });

                    var uniqueFileName = $"einvoice_{timestamp}_{file.FileName.Replace(" ", "_")}";
                    await _azureStorageHelper.InsertBlob(uniqueFileName, "invoicefiles", file);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("invoicefiles");
                    var blob         = containerRef.GetBlockBlobReference(uniqueFileName);

                    fileUrls.Add(blob.Uri.ToString());
                    fileNames.Add(file.FileName);
                }

                if (fileUrls.Count == 0)
                    return Json(new { success = false, message = "No valid files were uploaded." });

                return Json(new { success = true, fileUrls, fileNames, combinedUrl = string.Join("|", fileUrls) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveGeneratedInvoice(GenerateInvoiceVM model, string LineItemsJson,
            int? CashCreditAmount = null, string CashTransactionRef = null, DateTime? CashTransactionDate = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                var emailid     = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    model.CreatedBy = emailid;
                    if (!string.IsNullOrWhiteSpace(LineItemsJson))
                    {
                        try { model.LineItems = JsonConvert.DeserializeObject<List<InvoiceLineItemVM>>(LineItemsJson); }
                        catch { model.LineItems = new List<InvoiceLineItemVM>(); }
                    }

                    // Attach transaction fields to the model so the SP saves them in one shot
                    if (CashCreditAmount.HasValue && CashCreditAmount.Value > 0)
                        model.CashCreditAmount = CashCreditAmount;
                    if (!string.IsNullOrWhiteSpace(CashTransactionRef))
                        model.UTRTrxn = CashTransactionRef;

                    var invoiceId = await _meetingService.SaveGeneratedInvoice(model);
                    if (invoiceId > 0)
                    {
                        // When saved as Paid (status=5), post InvoiceValue (base amount) to client wallet
                        if (model.Status == 5)
                        {
                            try
                            {
                                var billingType    = await _clientService.GetClientBillingType(model.ClientId);
                                int? billingOwnerId = billingType == 1 ? model.BillingOwnerId : (int?)null;
                                // Credit = InvoiceValue (base amount).
                                // Passing invoiceId ensures existing wallet row is updated, not duplicated.
                                await _walletService.AddClientWalletBalance(
                                    billingType, model.ClientId, (int)model.Subtotal,
                                    emailid, DateTime.Now, billingOwnerId,
                                    CashTransactionRef, null, CashTransactionDate, "CashCredit",
                                    invoiceId: invoiceId);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }

                        TempData["InvoiceListMessage"] = "Success: Invoice saved successfully.";
                        return RedirectToAction("InvoiceList");
                    }

                    TempData["GenerateInvoiceMsg"] = invoiceId == -1
                        ? "Error: An invoice has already been generated for this Client, Billing Owner, Month and Year. Duplicate invoices are not allowed."
                        : "Error: Could not save invoice.";
                    return RedirectToAction("GenerateInvoice");
                }
                return RedirectToAction("SignOut", "Session");
            }
            return RedirectToAction("SignOut", "Session");
        }

        #endregion

        #region InvoiceList

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InvoiceList(string tab = "monthly")
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    // Normalise — only three valid values
                    if (tab != "unpaid" && tab != "paid") tab = "monthly";
                    ViewBag.ActiveTab = tab;

                    // ── Unpaid list is always loaded (drives the badge on every tab) ──
                    var unpaidList = await _meetingService.GetUnpaidInvoiceList();
                    unpaidList.ForEach(x => x.InvoiceModel = Enum.IsDefined(typeof(BillingModelEnum), x.BillingModel)
                        ? ((BillingModelEnum)x.BillingModel).GetDescription()
                        : string.Empty);
                    ViewBag.UnpaidList       = unpaidList;
                    ViewBag.UnpaidCount      = unpaidList.Count;
                    ViewBag.UnpaidTotalGross = unpaidList.Sum(x => x.GrossAmount);

                    // ── Monthly data: skip when Paid tab is active ──
                    var model = new List<InvoiceListVM>();
                    if (tab != "paid")
                    {
                        var prevMonth = DateTime.Now.AddMonths(-1).Month;
                        var prevYear  = DateTime.Now.AddMonths(-1).Year;
                        int month = prevMonth;
                        int year  = prevYear;

                        if (HttpContext.Session.GetString("InvoiceListFilter") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InvoiceListFilter>(HttpContext.Session.GetString("InvoiceListFilter"));
                            if (filter.Month != null && filter.Year != null)
                            {
                                month = Convert.ToInt32(filter.Month);
                                year  = Convert.ToInt32(filter.Year);
                            }
                            model = await _meetingService.GetInvoiceList(month, year);
                            if (model == null) model = new List<InvoiceListVM>();
                            model.ForEach(x => x.InvoiceModel = Enum.IsDefined(typeof(BillingModelEnum), x.BillingModel)
                                ? ((BillingModelEnum)x.BillingModel).GetDescription()
                                : string.Empty);
                            filter.Client = filter.Client == null
                                ? model.Select(x => x.ClientName).ToArray()
                                : filter.Client;
                            filter.Status = filter.Status == null
                                ? model.Select(x => x.Status.ToString()).Distinct().ToArray()
                                : filter.Status;
                            model = model.Where(x =>
                                filter.Client.Contains(x.ClientName) &&
                                filter.Status.Contains(x.Status.ToString())).ToList();
                        }
                        else
                        {
                            model = await _meetingService.GetInvoiceList(month, year);
                            if (model == null) model = new List<InvoiceListVM>();
                            model.ForEach(x => x.InvoiceModel = Enum.IsDefined(typeof(BillingModelEnum), x.BillingModel)
                                ? ((BillingModelEnum)x.BillingModel).GetDescription()
                                : string.Empty);
                        }

                        System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                        ViewBag.SelectedMonth = month;
                        ViewBag.SelectedYear  = year;
                        ViewBag.DateResult    = mfi.GetMonthName(month) + " " + year;
                        ViewBag.TotalGross    = model.Sum(x => x.GrossAmount);
                        ViewBag.ClientList    = _projectService.GetClients().Select(x => x.Name).Where(x => !string.IsNullOrEmpty(x)).Distinct();
                        ViewBag.MonthlyLoaded = true;
                    }
                    else
                    {
                        // Paid tab active — provide safe stubs so the view renders without null errors
                        ViewBag.SelectedMonth = 0;
                        ViewBag.SelectedYear  = 0;
                        ViewBag.DateResult    = string.Empty;
                        ViewBag.TotalGross    = 0m;
                        ViewBag.ClientList    = Enumerable.Empty<string>();
                        ViewBag.MonthlyLoaded = false;
                    }

                    // ── Paid data: skip when Monthly or Unpaid tab is active ──
                    int paidFromYear = 0, paidFromMonth = 0, paidToYear = 0, paidToMonth = 0;
                    var paidFilterJson = HttpContext.Session.GetString("PaidInvoiceFilter");
                    if (!string.IsNullOrEmpty(paidFilterJson))
                    {
                        try
                        {
                            var pf = JsonConvert.DeserializeObject<dynamic>(paidFilterJson);
                            paidFromYear  = pf.FromYear  != null ? (int)pf.FromYear  : 0;
                            paidFromMonth = pf.FromMonth != null ? (int)pf.FromMonth : 0;
                            paidToYear    = pf.ToYear    != null ? (int)pf.ToYear    : 0;
                            paidToMonth   = pf.ToMonth   != null ? (int)pf.ToMonth   : 0;
                        }
                        catch { /* ignore bad session data */ }
                    }
                    ViewBag.PaidFromYear  = paidFromYear; 
                    ViewBag.PaidFromMonth = paidFromMonth;
                    ViewBag.PaidToYear    = paidToYear;
                    ViewBag.PaidToMonth   = paidToMonth;

                    if (tab == "paid")
                    {
                        var paidList = await _meetingService.GetPaidInvoiceList(paidFromYear, paidFromMonth, paidToYear, paidToMonth);
                        paidList.ForEach(x => x.InvoiceModel = Enum.IsDefined(typeof(BillingModelEnum), x.BillingModel)
                            ? ((BillingModelEnum)x.BillingModel).GetDescription()
                            : string.Empty);
                        ViewBag.PaidList       = paidList;
                        ViewBag.PaidCount      = paidList.Count;
                        ViewBag.PaidTotalGross = paidList.Sum(x => x.GrossAmount);
                        ViewBag.PaidLoaded     = true;
                    }
                    else
                    {
                        ViewBag.PaidList       = new List<InvoiceListVM>();
                        ViewBag.PaidCount      = 0;
                        ViewBag.PaidTotalGross = 0m;
                        ViewBag.PaidLoaded     = false;
                    }

                    if (TempData["InvoiceListMessage"] != null)
                    {
                        ViewBag.Message = TempData["InvoiceListMessage"].ToString();
                        TempData.Remove("InvoiceListMessage");
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
        public IActionResult InvoiceList(InvoiceListFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("InvoiceListFilter", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("InvoiceList");
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
        public IActionResult ResetInvoiceList()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InvoiceListFilter");
                    return RedirectToAction("InvoiceList");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public IActionResult FilterPaidInvoices(int fromYear, int fromMonth, int toYear, int toMonth)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var pf = new { FromYear = fromYear, FromMonth = fromMonth, ToYear = toYear, ToMonth = toMonth };
                HttpContext.Session.SetString("PaidInvoiceFilter", JsonConvert.SerializeObject(pf));
                return RedirectToAction("InvoiceList", new { tab = "paid" });
            }
            return RedirectToAction("SignOut", "Session");
        }

        [HttpGet]
        public IActionResult ResetPaidInvoiceFilter()
        {
            HttpContext.Session.Remove("PaidInvoiceFilter");
            return RedirectToAction("InvoiceList", new { tab = "paid" });
        }

        #endregion

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
            var verificationdate = new DateTime(2023, 05, 01);
            var verificationenddate = new DateTime(2024, 10, 01);
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
                item.CandidateRatingAmount = item.CandidateRatingAmount + item.ClientRatingAmount;
                if (item.Date >= verificationdate && item.Date < verificationenddate && @item.VideoDuration > 5 && (item.MeetingStatus == 4 || item.MeetingStatus == 6 || item.MeetingStatus == 7 || item.MeetingStatus == 8))
                {
                    item.VideoDuration = item.VideoDuration - 5;
                }
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

                        item.CompanyType = !string.IsNullOrEmpty(item.CompanyType) ? ((CompanyType)Convert.ToInt32(item.CompanyType)).GetDescription() : string.Empty;
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
        //[Authorize]
        //[HttpGet]
        //public FileContentResult ExportGenerateFNPayout(DateTime st, DateTime se)
        //{
        //    ExcelPackage Ep = new ExcelPackage();
        //    ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payout Details");
        //    string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_PayoutDetails.xlsx";
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "Finance")
        //            {
        //                var model = _meetingService.FNAllGeneratedPayoutDetails(st, se.AddDays(1));
        //                var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
        //                string userEmailId = string.Empty;

        //                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
        //                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
        //                Sheet.Cells["A1:Z1"].Style.WrapText = true;
        //                int startColumn = 1; int endColumn = 12;
        //                for (int a = startColumn; a <= endColumn; a++)
        //                {
        //                    Sheet.Column(a).Width = 25;
        //                }
        //                Sheet.Cells[1, 1].Value = "S.No.";
        //                Sheet.Cells[1, 2].Value = "Interviewer Name";
        //                Sheet.Cells[1, 3].Value = "Email id";
        //                Sheet.Cells[1, 4].Value = "PAN Number";
        //                Sheet.Cells[1, 5].Value = "A/C holder Name";
        //                Sheet.Cells[1, 6].Value = "A/C Number";
        //                Sheet.Cells[1, 7].Value = "IFSC";
        //                Sheet.Cells[1, 8].Value = "Total";
        //                Sheet.Cells[1, 9].Value = "TDS";
        //                Sheet.Cells[1, 10].Value = "Balance";
        //                int i = 2;
        //                if (model.Result.Count() > 0)
        //                {
        //                    foreach (var interviewer in Interviewers)
        //                    {
        //                        Sheet.Cells[i, 1].Value = i - 1;
        //                        Sheet.Cells[i, 2].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
        //                        Sheet.Cells[i, 3].Value = interviewer;
        //                        Sheet.Cells[i, 4].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.PAN).FirstOrDefault();
        //                        Sheet.Cells[i, 5].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountName).FirstOrDefault();
        //                        Sheet.Cells[i, 6].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountNumber).FirstOrDefault();
        //                        Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.IFSCCode).FirstOrDefault();
        //                        Sheet.Cells[i, 8].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum();
        //                        Sheet.Cells[i, 9].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
        //                        Sheet.Cells[i, 10].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
        //                        i++;
        //                    }
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }
        //                else
        //                {
        //                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                    Rng.Value = "No data available in table !!!";
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }
        //            }
        //            else
        //            {
        //                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                Rng.Value = "Data export failed !!!";
        //                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //            }
        //        }
        //        else
        //        {
        //            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //            Rng.Value = "Data export failed !!!";
        //            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //        Rng.Value = "Data export failed !!!";
        //        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //    }
        //}
        //[Authorize]
        //[HttpGet]
        //public FileContentResult ExportFNPayout()
        //{
        //    ExcelPackage Ep = new ExcelPackage();
        //    ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Payout Details");
        //    string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_PayoutDetails.xlsx";
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            var profileName = HttpContext.Session.GetString("profileName")?.Trim();
        //            profileName = profileName.Replace("\"", "");
        //            var emailid = HttpContext.Session.GetString("email")?.Trim();
        //            emailid = emailid.Replace("\"", "");
        //            if (profileName == "Admin" || profileName == "Finance")
        //            {
        //                var model = _meetingService.FetchAllPayoutDetails();
        //                var Interviewers = model.Result.OrderBy(x => x.InterviewerEmailid).Select(x => x.InterviewerEmailid).Distinct().ToList();
        //                string userEmailId = string.Empty;

        //                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
        //                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
        //                Sheet.Cells["A1:Z1"].Style.WrapText = true;
        //                int startColumn = 1; int endColumn = 12;
        //                for (int a = startColumn; a <= endColumn; a++)
        //                {
        //                    Sheet.Column(a).Width = 25;
        //                }
        //                Sheet.Cells[1, 1].Value = "S.No.";
        //                Sheet.Cells[1, 2].Value = "Interviewer Name";
        //                Sheet.Cells[1, 3].Value = "Email id";
        //                Sheet.Cells[1, 4].Value = "PAN Number";
        //                Sheet.Cells[1, 5].Value = "A/C holder Name";
        //                Sheet.Cells[1, 6].Value = "A/C Number";
        //                Sheet.Cells[1, 7].Value = "IFSC";
        //                Sheet.Cells[1, 8].Value = "Total";
        //                Sheet.Cells[1, 9].Value = "TDS";
        //                Sheet.Cells[1, 10].Value = "Balance";
        //                int i = 2;
        //                if (model.Result.Count() > 0)
        //                {
        //                    foreach (var interviewer in Interviewers)
        //                    {
        //                        Sheet.Cells[i, 1].Value = i - 1;
        //                        Sheet.Cells[i, 2].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.FirstName).FirstOrDefault();
        //                        Sheet.Cells[i, 3].Value = interviewer;
        //                        Sheet.Cells[i, 4].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.PAN).FirstOrDefault();
        //                        Sheet.Cells[i, 5].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountName).FirstOrDefault();
        //                        Sheet.Cells[i, 6].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.AccountNumber).FirstOrDefault();
        //                        Sheet.Cells[i, 7].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.IFSCCode).FirstOrDefault();
        //                        Sheet.Cells[i, 8].Value = model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum();
        //                        Sheet.Cells[i, 9].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100;
        //                        Sheet.Cells[i, 10].Value = (model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) - ((model.Result.Where(x => x.InterviewerEmailid == interviewer).Select(x => x.Points).Sum()) * 10 / 100);
        //                        i++;
        //                    }
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }
        //                else
        //                {
        //                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                    Rng.Value = "No data available in table !!!";
        //                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //                }
        //            }
        //            else
        //            {
        //                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //                Rng.Value = "Data export failed !!!";
        //                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //            }
        //        }
        //        else
        //        {
        //            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //            Rng.Value = "Data export failed !!!";
        //            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
        //        Rng.Value = "Data export failed !!!";
        //        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //    }
        //}
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> FNRevenue(string year, int month, string fromYear = null, int fromMonth = 0, string toYear = null, int toMonth = 0)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    //Start:Year list
                    List<string> _listyear = new List<string>();
                    var currentyear = DateTime.Now.Year;
                    var counter = currentyear - 2020;
                    for (int i = 0; i < counter; i++)
                    {
                        _listyear.Add(DateTime.Now.AddYears(-i).Year.ToString());
                    }
                    var selectListforsize = new List<SelectListItem>();
                    if (string.IsNullOrEmpty(year))
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
                    ViewBag.RevenueYears = selectListforsize;
                    //End:Year list

                    // Determine if range mode or single month mode
                    bool isRangeMode = fromMonth > 0 && toMonth > 0 && !string.IsNullOrEmpty(fromYear) && !string.IsNullOrEmpty(toYear);

                    int resolvedFromMonth, resolvedFromYear, resolvedToMonth, resolvedToYear;
                    if (isRangeMode)
                    {
                        resolvedFromMonth = fromMonth;
                        resolvedFromYear  = Convert.ToInt32(fromYear);
                        resolvedToMonth   = toMonth;
                        resolvedToYear    = Convert.ToInt32(toYear);
                    }
                    else
                    {
                        if (month == 0) month = DateTime.Now.Month;
                        resolvedFromMonth = resolvedToMonth = month;
                        resolvedFromYear  = resolvedToYear  = Convert.ToInt32(year);
                    }

                    ViewBag.RevenueMonth    = isRangeMode ? fromMonth  : (month == 0 ? DateTime.Now.Month : month);
                    ViewBag.IsRangeMode     = isRangeMode;
                    ViewBag.FromMonth       = resolvedFromMonth;
                    ViewBag.FromYear        = resolvedFromYear.ToString();
                    ViewBag.ToMonth         = resolvedToMonth;
                    ViewBag.ToYear          = resolvedToYear.ToString();

                    DateTime stdate = new DateTime(resolvedFromYear, resolvedFromMonth, 1);
                    DateTime endate = new DateTime(resolvedToYear, resolvedToMonth, 1).AddMonths(1);

                    var model = await _meetingService.FetchRevenueDetails(stdate, endate, resolvedFromYear, resolvedFromMonth, resolvedToYear, resolvedToMonth);
                    decimal totalamount = 0;
                    foreach (var item in model)
                    {
                        // Use the SQL-computed BaseAmount which respects InterviewerBillingType
                        // (Skill Channel rate / Interviewer rate / Client rate) and the
                        // Turing 30% partial-billing rule for MeetingStatus 1/3/9.
                        var baseAmount = (int)Math.Round(item.BaseAmount);
                        totalamount = totalamount + item.BaseAmount;
                        if (item.GSTApplicable)
                        {
                            item.GST = "Yes";
                            item.GSTAmount = (baseAmount * 18) / 100;
                        }
                        else
                        {
                            item.GST = "No";
                            item.GSTAmount = 0;
                        }
                        item.SystemInvoiceAmount = item.GSTAmount + baseAmount;
                        item.TDSAmount = (baseAmount * 10) / 100;
                        //if (item.ReceivedAmount > 0)
                        //{
                        //    item.PaymentStatus = "Received";
                        //}
                        //else
                        //{
                        //    item.PaymentStatus = "Pending";
                        //}
                            var denominator = item.BillingType == 1
                            ? (item.TotalBillingOwnerCount > 0 ? item.TotalBillingOwnerCount : 1)
                            : 1;
                        item.InvoiceRatio = $"{item.GeneratedInvoiceCount}/{denominator}";
                    }
                    ViewBag.TotalRevenue = Math.Round(totalamount, 2);
                    if (model != null)
                    {
                        ViewBag.PendingInvoiceAmount       = model.Sum(x => x.GeneratedInvoiceAmount) - model.Sum(x => x.ReceivedAmount);
                        ViewBag.InvoiceAmount              = model.Sum(x => x.GeneratedInvoiceAmount);
                        ViewBag.TotalSystemInvoiceAmount   = model.Sum(x => x.SystemInvoiceAmount);
                        ViewBag.PendingAmount              = model.Sum(x => x.SystemInvoiceAmount) - model.Sum(x => x.ReceivedAmount);

                        // Per-currency breakdown for the header summary.
                        // ExpandoObject is used (not anonymous type) so dynamic access works in Razor views.
                        ViewBag.RevenueByCurrency = model
                            .GroupBy(x => string.IsNullOrEmpty(x.BillingCurrency) ? "INR" : x.BillingCurrency)
                            .OrderBy(g => g.Key)
                            .Select(g =>
                            {
                                dynamic row          = new System.Dynamic.ExpandoObject();
                                row.Currency         = g.Key;
                                row.TotalRevenue     = Math.Round(g.Sum(x => x.BaseAmount), 2);
                                row.SystemInvoiceAmount  = (decimal)g.Sum(x => x.SystemInvoiceAmount);
                                row.InvoiceAmount        = g.Sum(x => x.GeneratedInvoiceAmount);
                                row.PendingInvoiceAmount = g.Sum(x => x.GeneratedInvoiceAmount) - g.Sum(x => x.ReceivedAmount);
                                row.PendingAmount        = (decimal)g.Sum(x => x.SystemInvoiceAmount) - g.Sum(x => x.ReceivedAmount);
                                return row;
                            })
                            .ToList();
                    }
                    else
                    {
                        ViewBag.PendingInvoiceAmount       = 0;
                        ViewBag.InvoiceAmount              = 0;
                        ViewBag.TotalSystemInvoiceAmount   = 0;
                        ViewBag.PendingAmount              = 0;
                        ViewBag.RevenueByCurrency          = new List<object>();
                    }
                    if (TempData["RevenueMessage"] != null)
                    {
                        ViewBag.Message = TempData["RevenueMessage"].ToString();
                        TempData.Remove("RevenueMessage");
                    }
                    var relizationstatus = from Invoicerealized s in Enum.GetValues(typeof(Invoicerealized))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.RelizationStatus = new SelectList(relizationstatus, "ID", "Name");
                    var submitionstatus = from Invoicesubmition s in Enum.GetValues(typeof(Invoicesubmition))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.SubmitionStatus = new SelectList(submitionstatus, "ID", "Name");
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
        [HttpGet]
        public async Task<FileContentResult> ExportGenerateFNRevenue(string year, int month)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Revenue Details");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_RevenueDetails.xlsx";
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
                        int curyear = Convert.ToInt32(year);
                        DateTime stdate = new DateTime(curyear, month, 1);
                        DateTime endate = stdate.AddMonths(1);
                        var model = await _meetingService.FetchAllRevenueDetails(stdate, endate);
                        if (model != null)
                        {
                            model = model.OrderBy(x => x.Name).ToList();
                        }
                        Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                        Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                        Sheet.Cells["A1:Z1"].Style.WrapText = true;

                        int startColumn = 1; int endColumn = 25;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 25;
                        }
                        Sheet.Cells[1, 1].Value = "IRFID";
                        Sheet.Cells[1, 2].Value = "Client Name";
                        Sheet.Cells[1, 3].Value = "Per Interview Rate";
                        Sheet.Cells[1, 4].Value = "Total Interviews";
                        Sheet.Cells[1, 5].Value = "Amount";
                        Sheet.Cells[1, 6].Value = "Billing Owner";
                        int i = 2;
                        if (model.Count() > 0)
                        {
                            foreach (var item in model)
                            {
                                Sheet.Cells[i, 1].Value = item.IRFId;
                                Sheet.Cells[i, 2].Value = item.Name;
                                Sheet.Cells[i, 3].Value = item.PerInterviewRate;
                                Sheet.Cells[i, 4].Value = item.TotalCount;
                                Sheet.Cells[i, 5].Value = item.BaseAmount;
                                Sheet.Cells[i, 6].Value = item.BillingOwner;
                                i++;
                            }
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
        public async Task<IActionResult> FNRevenueByClient(int id, string year, int month)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "Finance")
                {
                    var model = new List<FnRevenueVM>();
                    var billingType = await _clientService.GetClientBillingType(id);
                    int curyear = Convert.ToInt32(year);
                    DateTime stdate = new DateTime(curyear, month, 1);
                    DateTime endate = stdate.AddMonths(1);
                    model = await _meetingService.FetchRevenueDetailsByClient(id, stdate, endate, billingType, year, month);
                    model = model.Select(x =>
                    {
                        x.CurrentStatusDesc = x.InvoiceStatus switch
                        {
                            1 => "Generated",
                            2 => "PO Pending",
                            3 => "Approval Pending",
                            4 => "Invoice Realisation Pending",
                            5 => "Paid",
                            _ => string.Empty
                        };
                        return x;
                    }).ToList();
                    ViewBag.BillingType = billingType;
                    ViewBag.Year = year;
                    ViewBag.Month = month;
                    ViewBag.ClientId = id;
                    var Statuses = from Revenueenum s in Enum.GetValues(typeof(Revenueenum))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.CurrentStatus = new SelectList(Statuses, "ID", "Name");
                    var relizationstatus = from Invoicerealized s in Enum.GetValues(typeof(Invoicerealized))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.RelizationStatus = new SelectList(relizationstatus, "ID", "Name");
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
        public async Task<JsonResult> FNRevenueStatusDetails(int id, string year, int month, int billingType, string Detailid)
        {
            var model = await _meetingService.FetchRevenueStatusDetails(id, year, month, billingType, Detailid);
            if (model == null)
            {
                model = new FnRevenueVM();
            }
            return Json(new { currentStatus = model.CurrentStatus, poPending = model.POPendingAmount, invoiceamount = model.InvoiceAmount, invoicerealization = model.invoicerealization, invoiceSubmitted = model.invoiceSubmitted });
        }
        [HttpPost]
        public async Task<JsonResult> SaveRevenueStatusDetails(int id, string year, int month, int billingType, string Detailid, int currentStatus, int invoiceamount, int poPending, int invoicerealization, int invoicesubmit)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (currentStatus == 1)
                {
                    poPending = 0;
                }
                else
                {
                    invoiceamount = 0;
                }
                var model = await _meetingService.SaveRevenueStatusDetails(id, year, month, billingType, Detailid, currentStatus, invoiceamount, poPending, emailid, invoicerealization, invoicesubmit);
                if (model)
                {
                    return Json("Save Successfully!");
                }
                return Json("Fail to save, Please try again!");
            }
            else
            {
                return Json("Fail to save, Please relogin again!");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> CompletedInterviewExcelById(int id, string year, int month)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Completed Interviews");
            string fileName = "";
            //string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_CompletedInterview.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "Finance")
                    {
                        int curyear = Convert.ToInt32(year);
                        DateTime stdate = new DateTime(curyear, month, 1);
                        DateTime endate = stdate.AddMonths(1);
                        var model = await _meetingService.FetchCompletedInterviewsByClientId(id, stdate, endate, year, month);
                        var client = model.FirstOrDefault().Client;
                        fileName = "Risebird" + "_" + client + DateTime.Now.ToString("dd/MM/yyyy") + "_CompletedInterview.xlsx";
                        int startColumn = 1; int endColumn = 15;
                        for (int a = startColumn; a <= endColumn; a++)
                        {
                            Sheet.Column(a).Width = 15;
                        }
                        Sheet.Cells[1, 1].Value = "Interview Date";
                        Sheet.Cells[1, 2].Value = "Candidate Email";
                        Sheet.Cells[1, 3].Value = "Candidate Name";
                        Sheet.Cells[1, 4].Value = "Meeting Status";
                        Sheet.Cells[1, 5].Value = "Interview Status";
                        Sheet.Cells[1, 6].Value = "Interviewer Comment";
                        Sheet.Cells[1, 7].Value = "Video Link";
                        Sheet.Cells[1, 8].Value = "Client HR";
                        int i = 2;
                        if (model.Count() > 0)
                        {
                            foreach (var item in model)
                            {
                                if (item.InterviewStatus.HasValue)
                                {
                                    item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                                }
                                if (item.MeetingStatus.HasValue)
                                {
                                    item.MeetingStatusStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                                }
                                Sheet.Cells[i, 1].Value = item.interviewDate;
                                Sheet.Cells[i, 2].Value = item.candidateEmail;
                                Sheet.Cells[i, 3].Value = item.candidateName;
                                Sheet.Cells[i, 4].Value = item.MeetingStatusStatusDesc;
                                Sheet.Cells[i, 5].Value = item.intervieweStatusStatDesc;
                                Sheet.Cells[i, 6].Value = item.InterviewerComment;
                                if (!string.IsNullOrEmpty(item.VideoLink))
                                {
                                    try
                                    {
                                        Sheet.Cells[i, 7].Hyperlink = new Uri(item.VideoLink, UriKind.Absolute);
                                        Sheet.Cells[i, 7].Value = "Video Link";
                                        Sheet.Cells[i, 7].Style.Font.UnderLine = true;
                                        Sheet.Cells[i, 7].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                    }
                                    catch (Exception)
                                    {
                                        Sheet.Cells[i, 7].Value = "No Video";
                                    }
                                }
                                else
                                {
                                    Sheet.Cells[i, 7].Value = "No Video";
                                }
                                Sheet.Cells[i, 8].Value = item.ClientHrContact;
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
    }
}
