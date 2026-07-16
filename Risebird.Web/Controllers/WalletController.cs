using Amazon.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using OfficeOpenXml;
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
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IWebHostEnvironment _env;

        public WalletController(IWalletService walletService, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger, IClientService clientService, IIRFService irfService, IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper, IWebHostEnvironment env)
        {
            _walletService = walletService;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _env = env;
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
        /// <param name="startDate">Optional filter: transactions from this date (inclusive)</param>
        /// <param name="endDate">Optional filter: transactions up to this date (inclusive). Must be &gt;= startDate when both provided.</param>
        /// <returns>Wallet transaction list as partial view</returns>
        [HttpPost]
        public async Task<IActionResult> GetWalletDetails(int billingType, int? clientId = null, int? billingOwnerId = null, string actiontype = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                try
                {
                    // Validate start date < end date when both provided
                    if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                    {
                        ViewBag.DateFilterError = "Start date must be less than or equal to end date.";
                        ViewBag.actiontype = actiontype;
                        return PartialView("GetWalletDetails", new List<WalletVM>());
                    }

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
                    ViewBag.actiontype = actiontype;
                    var model = await _walletService.GetWallet(billingType, clientId, billingOwnerId, actiontype, startDate, endDate);

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

        /// <summary>
        /// Export wallet transaction data (same as index page) to Excel, with or without filters.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportWalletDetails(int billingType, int? clientId = null, int? billingOwnerId = null, string actiontype = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (_env.IsDevelopment())
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            }
            var ep = new ExcelPackage();
            var sheet = ep.Workbook.Worksheets.Add("Wallet Details");
            var fileName = $"Risebird_Wallet_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
            {
                sheet.Cells[1, 1].Value = "Data export failed. Please sign in.";
                return File(ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            try
            {
                if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                {
                    sheet.Cells[1, 1].Value = "Start date must be less than or equal to end date.";
                    return File(ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

                if (billingType != 1 && billingType != 3)
                {
                    sheet.Cells[1, 1].Value = "Invalid billing type.";
                    return File(ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName == "Client")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    if (client != null && billingType == 3)
                        clientId = client.ClientId;
                    else if (client != null && billingType == 1)
                        billingOwnerId = client.UserId;
                }

                var model = await _walletService.GetWallet(billingType, clientId, billingOwnerId, actiontype, startDate, endDate);
                var list = model?.ToList() ?? new List<WalletVM>();

                foreach (var item in list)
                {
                    if (item.CreatedOn.HasValue)
                        item.CreatedOnstring = item.CreatedOn.Value.ToShortDateString();
                }

                var isCreditOnly = string.Equals(actiontype, "CREDIT", StringComparison.OrdinalIgnoreCase);
                int col = 1, row = 1;

                if (isCreditOnly)
                {
                    sheet.Cells[row, col++].Value = "Transaction Date";
                    sheet.Cells[row, col++].Value = "Transaction Id";
                    sheet.Cells[row, col++].Value = "Transaction Type";
                    sheet.Cells[row, col++].Value = "Points";
                    sheet.Cells[row, col].Value = "Wallet Entry Date";
                    row = 2;
                    foreach (var item in list)
                    {
                        col = 1;
                        sheet.Cells[row, col++].Value = item.TransactionDate.HasValue ? item.TransactionDate.Value.ToString("MM/dd/yyyy HH:mm") : "N/A";
                        sheet.Cells[row, col++].Value = item.TransactionId ?? "N/A";
                        sheet.Cells[row, col++].Value = item.Credit > 0 ? "CREDIT" : (item.Debit > 0 ? "DEBIT" : "-");
                        sheet.Cells[row, col++].Value = item.Credit > 0 ? item.Credit : (item.Debit > 0 ? item.Debit : 0);
                        sheet.Cells[row, col].Value = !string.IsNullOrEmpty(item.CreatedOnstring) ? item.CreatedOnstring : (item.CreatedOn.HasValue ? item.CreatedOn.Value.ToString("MM/dd/yyyy") : "N/A");
                        row++;
                    }
                }
                else
                {
                    sheet.Cells[row, col++].Value = "Interview Date";
                    sheet.Cells[row, col++].Value = "IRF Name";
                    sheet.Cells[row, col++].Value = "Candidate Name";
                    sheet.Cells[row, col++].Value = "Candidate Email";
                    sheet.Cells[row, col++].Value = "Report URL";
                    sheet.Cells[row, col++].Value = "Meeting Status";
                    sheet.Cells[row, col++].Value = "Transaction Type";
                    sheet.Cells[row, col++].Value = "Points";
                    sheet.Cells[row, col].Value = "Wallet Entry Date";
                    row = 2;
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    foreach (var item in list)
                    {
                        col = 1;
                        sheet.Cells[row, col++].Value = item.InterviewDate.HasValue ? item.InterviewDate.Value.ToString("MM/dd/yyyy HH:mm") : "N/A";
                        sheet.Cells[row, col++].Value = item.IRFName ?? "N/A";
                        sheet.Cells[row, col++].Value = item.CandidateName ?? "N/A";
                        sheet.Cells[row, col++].Value = item.CandidateEmail ?? "N/A";
                        var reportUrl = (item.InterviewId > 0 && item.IRFId > 0)
                            ? $"{baseUrl}{Url.Action("ClientReport", "Interview", new { interviewId = item.InterviewId, irfId = item.IRFId })}"
                            : "N/A";
                        sheet.Cells[row, col++].Value = reportUrl;
                        var meetingStatusStr = "N/A";
                        if (item.MeetingStatus > 0 && Enum.IsDefined(typeof(MeetingStatus), item.MeetingStatus))
                            meetingStatusStr = ((MeetingStatus)item.MeetingStatus).GetDescription();
                        sheet.Cells[row, col++].Value = meetingStatusStr;
                        sheet.Cells[row, col++].Value = item.Credit > 0 ? "CREDIT" : (item.Debit > 0 ? "DEBIT" : "-");
                        sheet.Cells[row, col++].Value = item.Credit > 0 ? item.Credit : (item.Debit > 0 ? item.Debit : 0);
                        sheet.Cells[row, col].Value = !string.IsNullOrEmpty(item.CreatedOnstring) ? item.CreatedOnstring : (item.CreatedOn.HasValue ? item.CreatedOn.Value.ToString("MM/dd/yyyy") : "N/A");
                        row++;
                    }
                }

                if (list.Count == 0)
                    sheet.Cells[2, 1].Value = "No transactions found.";

                var maxCol = isCreditOnly ? 5 : 9;
                for (var c = 1; c <= maxCol; c++)
                    sheet.Column(c).Width = 18;

                return File(ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                sheet.Cells[1, 1].Value = "Data export failed.";
                return File(ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddWalletBalance(int Clientid)
        {
            var clientBillingType = await _clientService.GetClientBillingType(Clientid);
            ViewBag.BillingType = clientBillingType;//3: BillingType :Client
            ViewBag.Clientid = Clientid;
            if (clientBillingType == 1)
            {
                //BillingType :BilingOwner
                ViewBag.ClientUser = new SelectList(_irfService.GetClientUser(Clientid), "id", "EmailId");
            }
            return PartialView();
        }
        [HttpPost]
        public async Task<JsonResult> AddWalletBalance(int Clientid, int billingType, int CreditAmount, DateTime? CreatedOn, int? billingOwnerId = null, string transactionId = null, DateTime? transactionDate = null)
        {
            var emailid = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
            }


            string invoicePDF = null;
            // Handle PDF file upload
            IFormFile file = null;
            if (Request.Form.Files["file"] != null)
            {
                file = Request.Form.Files["file"];
            }
            else if (Request.Form.Files.Count > 0)
            {
                file = Request.Form.Files[0];
            }

            if (file != null && file.Length > 0)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                if (fileMimeType == "application/pdf")
                {
                    var fileName = file.FileName.Replace(" ", "_");
                    // Generate unique filename with timestamp
                    var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}";
                    await _azureStorageHelper.InsertBlob(uniqueFileName, "invoicefiles", file);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("invoicefiles");
                    var blob = containerRef.GetBlockBlobReference(uniqueFileName);
                    invoicePDF = blob.Uri.ToString();
                }
                else
                {
                    return Json("Invalid file type. Only PDF files are allowed.");
                }
            }

            var result = await _walletService.AddClientWalletBalance(billingType, Clientid, CreditAmount, emailid, CreatedOn, billingOwnerId, transactionId, invoicePDF, transactionDate, "Manual");
            if (result)
            {
                return Json("Credit amount added Successfully");
            }
            else
            {
                return Json("Fail to added Credit amount, Please try again.");
            }
        }

        #region Client Wallet Dashboard (Finance)

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientWallet(int clientId, int? month = null, int? year = null, string scope = "month")
        {
            if (!IsFinanceOrAdmin()) return RedirectToAction("SignOut", "Session");

            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;
            var allDetails = string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase);
            var dashboard = await _walletService.GetClientWalletDashboard(clientId, selectedMonth, selectedYear, allDetails);
            if (string.IsNullOrEmpty(dashboard.ClientName))
            {
                TempData["ClientWalletMessage"] = "Error: Client not found.";
                return RedirectToAction("List", "Client");
            }

            ViewBag.ClientUsers = dashboard.BillingType == 1
                ? new SelectList(_irfService.GetClientUser(clientId), "id", "EmailId")
                : null;

            if (dashboard.BillingType == 1)
            {
                // Identify billing owners whose latest PO is >= 90% consumed — used for warning badge
                ViewBag.HighConsumptionOwnerIds = await _walletService.GetHighConsumptionOwnerIds(clientId);

                // PO Opening Balance = gross PO credit registered before the selected period.
                // This shows how much credit was already in the pool entering this month.
                // For the "all-time" scope, opening balance is zero (no prior period exists).
                if (!allDetails)
                {
                    var allPOs = await _walletService.GetClientPOList(clientId);
                    ViewBag.POOpeningBalances = allPOs
                        .Where(p => p.BillingOwnerId.HasValue
                                 && (p.POYear < selectedYear
                                     || (p.POYear == selectedYear && p.POMonth < selectedMonth)))
                        .GroupBy(p => p.BillingOwnerId!.Value)
                        .ToDictionary(g => g.Key, g => g.Sum(p => p.POCreditAmount));
                }
            }

            if (TempData["ClientWalletMessage"] != null)
            {
                ViewBag.Message = TempData["ClientWalletMessage"].ToString();
            }

            return View(dashboard);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPOHistory(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, string ownerEmail = null)
        {
            if (!IsFinanceOrAdmin()) return PartialView("_POHistory", new List<ClientPOVM>());
            var billingType = await _clientService.GetClientBillingType(clientId);
            var list = await _walletService.GetPOHistory(clientId, billingType, month, year, billingOwnerId, ownerEmail);
            ViewBag.BillingType   = billingType;
            ViewBag.FilterMonth   = month;
            ViewBag.FilterYear    = year;
            ViewBag.OwnerFiltered = billingOwnerId.HasValue || !string.IsNullOrEmpty(ownerEmail);
            ViewBag.FifoApplied   = true;   // FIFO always calculated — view should use WalletConsumed
            return PartialView("_POHistory", list);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCashCreditHistory(int clientId)
        {
            if (!IsFinanceOrAdmin()) return PartialView("_CashCreditHistory", new List<WalletVM>());
            var billingType = await _clientService.GetClientBillingType(clientId);
            var list = await _walletService.GetCashCreditHistory(clientId);
            ViewBag.BillingType = billingType;
            return PartialView("_CashCreditHistory", list);
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> AddCashCreditPoints(int clientId, int creditAmount, DateTime? transactionDate = null, string transactionRefNumber = null, int? billingOwnerId = null, int? clientPOId = null)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });

            var billingType = await _clientService.GetClientBillingType(clientId);
            if (billingType == 1 && (!billingOwnerId.HasValue || billingOwnerId.Value <= 0))
            {
                return Json(new { success = false, message = "Billing owner is required for this client." });
            }
            if (creditAmount <= 0) return Json(new { success = false, message = "Enter a valid credit amount." });

            if (clientPOId.HasValue && clientPOId.Value > 0)
            {
                var openPOs = await _walletService.GetClientPOList(clientId, openOnly: true);
                var po = openPOs.FirstOrDefault(x => x.id == clientPOId.Value);
                if (po == null)
                {
                    return Json(new { success = false, message = "Selected PO not found or already fully credited." });
                }
                if (po.PendingCashCredit > 0 && creditAmount > po.PendingCashCredit)
                {
                    return Json(new { success = false, message = $"Cash credit cannot exceed pending PO amount (₹ {po.PendingCashCredit:N0})." });
                }
                if (billingType == 1 && po.BillingOwnerId != billingOwnerId)
                {
                    return Json(new { success = false, message = "Cash credit billing owner must match the selected PO." });
                }
            }

            var emailid = GetSessionEmail();
            var result = await _walletService.AddClientWalletBalance(
                billingType, clientId, creditAmount, emailid, DateTime.Now,
                billingType == 1 ? billingOwnerId : null,
                transactionRefNumber, null, transactionDate, "CashCredit", clientPOId);

            return Json(new { success = result, message = result ? "Cash credit added to wallet successfully." : "Failed to add cash credit." });
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetOpenClientPOs(int clientId, int? billingOwnerId = null)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });
            var list = await _walletService.GetClientPOList(clientId, billingOwnerId: billingOwnerId, openOnly: true);
            return Json(new
            {
                success = true,
                items = list.Select(x => new
                {
                    x.id,
                    label = string.IsNullOrEmpty(x.PONumber)
                        ? $"{x.PeriodLabel} · ₹ {x.POCreditAmount:N0} (Pending ₹ {x.PendingCashCredit:N0})"
                        : $"{x.PONumber} · {x.PeriodLabel} · Pending ₹ {x.PendingCashCredit:N0}",
                    x.POCreditAmount,
                    x.CashCreditReceived,
                    pending = x.PendingCashCredit,
                    x.BillingOwnerId,
                    x.Status,
                    x.StatusDesc,
                    x.StatusCss,
                    x.PONumber,
                    x.PeriodLabel,
                    x.Remarks
                })
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetClientPOStatusInfo(int clientId, int? billingOwnerId = null)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });
            var list = await _walletService.GetClientPOList(clientId, billingOwnerId: billingOwnerId);
            return Json(new
            {
                success = true,
                items = list.Select(x => new
                {
                    x.id,
                    x.PONumber,
                    x.PeriodLabel,
                    x.POCreditAmount,
                    x.CashCreditReceived,
                    pending     = x.PendingCashCredit,
                    x.Status,
                    x.StatusDesc,
                    x.StatusCss,
                    x.BillingOwnerId,
                    x.BillingOwnerEmail,
                    x.Remarks,
                    x.CreatedBy,
                    createdOn = x.CreatedOn?.ToString("dd MMM yyyy")
                })
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> AddPOCreditPoints(int clientId, int creditAmount, int month, int year,
            DateTime? transactionDate = null, string transactionRefNumber = null, int? billingOwnerId = null, IFormFile poFile = null)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });

            var client = await _clientService.GetClient(clientId);
            if (client == null) return Json(new { success = false, message = "Client not found." });
            if (client.BillingModel != (int)BillingModelEnum.M2 && client.BillingModel != (int)BillingModelEnum.M3)
            {
                return Json(new { success = false, message = "PO credit is only available for M2 and M3 billing models." });
            }

            var billingType = await _clientService.GetClientBillingType(clientId);
            if (billingType == 1 && (!billingOwnerId.HasValue || billingOwnerId.Value <= 0))
            {
                return Json(new { success = false, message = "Billing owner is required for this client." });
            }
            if (creditAmount <= 0) return Json(new { success = false, message = "Enter a valid PO credit amount." });

            var invoicePDF = await UploadPdfAsync(poFile);
            if (poFile != null && poFile.Length > 0 && invoicePDF == null)
            {
                return Json(new { success = false, message = "Only PDF files are allowed." });
            }

            var emailid = GetSessionEmail();
            var poId = await _walletService.SaveClientPO(
                clientId, billingType, client.BillingModel, creditAmount, month, year, emailid,
                billingType == 1 ? billingOwnerId : null,
                transactionRefNumber, invoicePDF, "PO credit registered — cash credit to be added later in wallet");

            return Json(new
            {
                success = poId > 0,
                message = poId > 0
                    ? "PO registered successfully. Add cash credit later to post amount into wallet."
                    : "Failed to register PO."
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> UploadPODocument(int clientId, int month, int year, string transactionRefNumber = null,
            int? billingOwnerId = null, IFormFile poFile = null)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });

            var client = await _clientService.GetClient(clientId);
            if (client == null) return Json(new { success = false, message = "Client not found." });
            if (client.BillingModel != (int)BillingModelEnum.M2 && client.BillingModel != (int)BillingModelEnum.M3)
            {
                return Json(new { success = false, message = "PO upload is only available for M2 and M3 billing models." });
            }

            var billingType = await _clientService.GetClientBillingType(clientId);
            if (billingType == 1 && (!billingOwnerId.HasValue || billingOwnerId.Value <= 0))
            {
                return Json(new { success = false, message = "Billing owner is required for this client." });
            }
            if (poFile == null || poFile.Length == 0)
            {
                return Json(new { success = false, message = "Please select a PO PDF file." });
            }

            var invoicePDF = await UploadPdfAsync(poFile);
            if (invoicePDF == null)
            {
                return Json(new { success = false, message = "Only PDF files are allowed." });
            }

            var emailid = GetSessionEmail();
            var poId = await _walletService.SaveClientPO(
                clientId, billingType, client.BillingModel, 0, month, year, emailid,
                billingType == 1 ? billingOwnerId : null,
                transactionRefNumber, invoicePDF, "PO document uploaded without credit amount");

            return Json(new
            {
                success = poId > 0,
                message = poId > 0 ? "PO document saved successfully." : "Failed to upload PO document."
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> UpdatePOStatus(int poId, int clientId, int status)
        {
            if (!IsFinanceOrAdmin()) return Json(new { success = false, message = "Unauthorized." });

            // Only allow setting to Complete (4) or Paid (5)
            if (status != 4 && status != 5)
                return Json(new { success = false, message = "Invalid status. Allowed values: 4 (Complete), 5 (Paid)." });

            var result = await _walletService.UpdatePOStatus(poId, clientId, status);
            var label = status == 4 ? "Complete" : "Paid";
            return Json(new
            {
                success = result,
                message = result ? $"Invoice marked as {label}." : "Failed to update invoice status."
            });
        }

        #region Wallet Details — All Clients Summary (Finance)

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> WalletDetails(
            int? month = null, int? year = null,
            int? fromMonth = null, int? fromYear = null,
            int? toMonth = null, int? toYear = null)
        {
            if (!IsFinanceOrAdmin()) return RedirectToAction("SignOut", "Session");

            int fm, fy, tm, ty;

            if (fromMonth.HasValue || fromYear.HasValue || toMonth.HasValue || toYear.HasValue)
            {
                // Range filter submitted
                fm = fromMonth ?? DateTime.Now.Month;
                fy = fromYear  ?? DateTime.Now.Year;
                tm = toMonth   ?? fm;
                ty = toYear    ?? fy;
            }
            else
            {
                // Legacy single-month filter (or default)
                fm = month ?? DateTime.Now.Month;
                fy = year  ?? DateTime.Now.Year;
                tm = fm;
                ty = fy;
            }

            // Ensure from <= to (swap if inverted)
            if (new DateTime(fy, fm, 1) > new DateTime(ty, tm, 1))
                (fm, fy, tm, ty) = (tm, ty, fm, fy);

            var model = await _walletService.GetAllClientsWalletSummary(fm, fy, tm, ty);
            return View(model);
        }

        #endregion

        private bool IsFinanceOrAdmin()
        {
            if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                return false;
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return profileName == "Admin" || profileName == "Finance";
        }

        private string GetSessionEmail()
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            return emailid?.Replace("\"", "") ?? string.Empty;
        }

        private async Task<string> UploadPdfAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file);
            if (fileMimeType != "application/pdf") return null;

            var fileName = file.FileName.Replace(" ", "_");
            var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}";
            await _azureStorageHelper.InsertBlob(uniqueFileName, "invoicefiles", file);
            var containerRef = _azureStorageHelper.GetCloudBlobContainer("invoicefiles");
            var blob = containerRef.GetBlockBlobReference(uniqueFileName);
            return blob.Uri.ToString();
        }

        #endregion
    }
}

