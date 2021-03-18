using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using OfficeOpenXml;
using Risebird.Entities.Enum;
using X.PagedList;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;

namespace Risebird.Web.Controllers
{
    public class FinanceController : Controller
    {
        private readonly IMeetingService _meetingService;
        private static List<RBInterviewDetails> _modelDetails;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private static string SearchValue;


        public FinanceController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
        }
        /// <summary>
        /// Interviewer or QC check payment status and request for payment with details
        /// </summary>
        /// <returns></returns>
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
                    var sum = model.Where(x => x.Status == "Submitted" || x.Status == "Pending").Sum(x => x.Points) - model.Where(x => x.Status == "Advance").Sum(x => x.Points);
                    if (sum > 0)
                    {
                        ViewBag.submit = "true";
                    }
                    else
                    {
                        ViewBag.submit = "false";
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
                        Sheet.Cells[1, 4].Value = "Amount";
                        Sheet.Cells[1, 5].Value = "Date";
                        Sheet.Cells[1, 6].Value = "Status";
                        Sheet.Cells[1, 7].Value = "Location";
                        Sheet.Cells[1, 8].Value = "Meeting Status";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var item in model.Result)
                            {
                                Sheet.Cells[i, 1].Value = item.Comment;
                                Sheet.Cells[i, 2].Value = item.InterviewId;
                                Sheet.Cells[i, 3].Value = item.Type;
                                Sheet.Cells[i, 4].Value = item.Points;
                                Sheet.Cells[i, 5].Value = item.Date;
                                Sheet.Cells[i, 6].Value = item.Status;
                                Sheet.Cells[i, 7].Value = item.PreferredLocation;
                                Sheet.Cells[i, 8].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
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
        public async Task<IActionResult> FNPayment(int? Page, string searchValue)
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

                    if (String.IsNullOrEmpty(searchValue))
                    {
                        if (!String.IsNullOrEmpty(SearchValue))
                        {
                            searchValue = SearchValue;
                        }
                    }
                    if (!String.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToUpper();
                        model = model.Where(x => (!String.IsNullOrEmpty(x.Comment) ? x.Comment.ToUpper().Contains(searchValue) : x.Comment == "")).ToList();
                    }
                    ViewBag.Searchvalue = searchValue;
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
                    _modelDetails = model;

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
        public async Task<JsonResult> GetInterviewerPaymentDetails(int id, int flag)
        {
            var model = await _meetingService.RBInterviewerDetails(id, flag);
            foreach (var item in model)
            {
                item.MeetingStatusDetails = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                item.InterviewStatusDetails = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
            }
            return Json(model);
        }
        /// <summary>
        /// Admin or Finance Team may add points in interviewer or QC account
        /// </summary>
        /// <param name="id"></param>
        /// <param name="Points"></param>
        /// <param name="Comment"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> AddPointsforInterviewer(int id, int Points, string Comment)
        {
            var model = await _meetingService.RBAddPoints(id, Points, Comment);
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
        public async Task<IActionResult> ConfirmPayment(int id, int index, string email, string amount)
        {
            var resultset = await _meetingService.RBInterviewerDetails(id, index);
            //Start: Create PDF File
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_RBpayment.pdf";
            Document doc = new Document(iTextSharp.text.PageSize.A4, 10, 10, 42, 35);
            PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(fileName, FileMode.Create));
            doc.Open();
            PdfPTable table = new PdfPTable(5);
            PdfPCell cell = new PdfPCell();
            table.AddCell("Date");
            table.AddCell("Comment");
            table.AddCell("Type");
            table.AddCell("Status");
            table.AddCell("Amount");
            foreach (var item in resultset)
            {
                item.MeetingStatusDetails = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
                item.InterviewStatusDetails = Enum.GetName(typeof(InterviewStatus), item.InterviewStatus);
                table.AddCell(item.Date.ToString());
                table.AddCell(item.Comment);
                table.AddCell(item.Type);
                table.AddCell(item.Status);
                table.AddCell(item.Points.ToString());
            }
            doc.Add(table);
            doc.Close();
            //End: Create PDF File
            var model = await _meetingService.RBConfirmPayment(id, index);
            //Start:Send email to Interviewer
            Mail _mail = new Mail
            {
                TO = email,
                CC = "finance@risebird.io",
                Subject = "RiseBird: Finance Team has done payment"
            };
            var emailsend = _notificationHelper.RBFNPaymentConfirmationNotification(_mail, amount, fileName);
            //End:Send email to Interviewer
            if (emailsend == "Mail sent successfully.")
            {

            }
            System.IO.File.Delete(fileName);
            return RedirectToAction("FNPayment");
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
                var model = _modelDetails;
                SearchValue = searchValue;
                if (!String.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToUpper();
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
            var model = await _meetingService.InsertAdvancePayment(Interviewer, Comment, Amount);
            var result = "";
            if (model == "Success")
            {
                result = "Advance Payment is done successfully.";
                Mail _mail = new Mail
                {
                    TO = Interviewer,
                    CC = "finance@risebird.io",
                    Subject = "RiseBird: Finance Team done advance payment"
                };
                var emailsend = _notificationHelper.RBFNPaymentConfirmationNotification(_mail, Amount.ToString(), "");
            }
            else if (model == "Failed")
            {
                result = "Interviewer Email-id is wrong.";
            }
            else
            {
                result = "Fail to done the advance payment.";
            }
            return Json(result);
        }
        [HttpPost]
        public async Task<JsonResult> FNGenerateInvoiceJson(DateTime st, DateTime se)
        {
            try
            {
                int Page = 1;
                var model = await _meetingService.FNGenerateInvoiceDetails(st, se);
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
                _modelDetails = model;
                return Json(new { result = model.Take(10), startPage = Pagestart, endPage = Pageend, totalPage = a, totalentry = model.Count });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("failed");
            }
        }
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
                        var model = _meetingService.FNAllGeneratedInvoiceDetails(st, se);
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
                        Sheet.Cells[1, 4].Value = "Amount";
                        Sheet.Cells[1, 5].Value = "Date";
                        Sheet.Cells[1, 6].Value = "Status";
                        Sheet.Cells[1, 7].Value = "Location";
                        Sheet.Cells[1, 8].Value = "Meeting Status";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var item in model.Result)
                            {
                                Sheet.Cells[i, 1].Value = item.Comment;
                                Sheet.Cells[i, 2].Value = item.InterviewId;
                                Sheet.Cells[i, 3].Value = item.Type;
                                Sheet.Cells[i, 4].Value = item.Points;
                                Sheet.Cells[i, 5].Value = item.Date;
                                Sheet.Cells[i, 6].Value = item.Status;
                                Sheet.Cells[i, 7].Value = item.PreferredLocation;
                                Sheet.Cells[i, 8].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
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
                        Sheet.Cells[1, 4].Value = "Amount";
                        Sheet.Cells[1, 5].Value = "Date";
                        Sheet.Cells[1, 6].Value = "Status";
                        Sheet.Cells[1, 7].Value = "Location";
                        Sheet.Cells[1, 8].Value = "Meeting Status";
                        int i = 2;
                        if (model.Result.Count() > 0)
                        {
                            foreach (var item in model.Result)
                            {
                                Sheet.Cells[i, 1].Value = item.Comment;
                                Sheet.Cells[i, 2].Value = item.InterviewId;
                                Sheet.Cells[i, 3].Value = item.Type;
                                Sheet.Cells[i, 4].Value = item.Points;
                                Sheet.Cells[i, 5].Value = item.Date;
                                Sheet.Cells[i, 6].Value = item.Status;
                                Sheet.Cells[i, 7].Value = item.PreferredLocation;
                                Sheet.Cells[i, 8].Value = Enum.GetName(typeof(MeetingStatus), item.MeetingStatus);
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
