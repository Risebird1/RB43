using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class TdsController : Controller
    {
        private readonly ITdsComplianceService _tdsService;
        private readonly IAzureStorageHelper   _azureStorageHelper;
        private readonly IFileMimeTypeHelper   _fileMimeTypeHelper;
        private readonly IConfiguration        _configuration;
        private readonly ILogger<TdsController> _logger;

        // Container name to store challan PDFs and Form16A files
        private const string BlobContainer = "resumes";

        public TdsController(
            ITdsComplianceService  tdsService,
            IAzureStorageHelper    azureStorageHelper,
            IFileMimeTypeHelper    fileMimeTypeHelper,
            IConfiguration        configuration,
            ILogger<TdsController> logger)
        {
            _tdsService         = tdsService;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _configuration      = configuration;
            _logger             = logger;
        }

        // ------------------------------------------------------------------ //
        //  Auth guard helper                                                   //
        // ------------------------------------------------------------------ //

        private bool IsFinanceOrAdmin()
        {
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return profile == "Finance" || profile == "Admin";
        }

        private string OperatorEmail()
            => HttpContext.Session.GetString("email")?.Trim().Replace("\"", "") ?? "system";

        // ------------------------------------------------------------------ //
        //  GET: QuarterlyTds                                                   //
        // ------------------------------------------------------------------ //

        [HttpGet]
        public async Task<IActionResult> QuarterlyTds(string year, string quarter)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            // Default to current FY and current quarter
            if (string.IsNullOrWhiteSpace(year))
            {
                var now = DateTime.Today;
                var fyStart = now.Month >= 4 ? now.Year : now.Year - 1;
                year = $"{fyStart}-{fyStart + 1}";
            }
            if (string.IsNullOrWhiteSpace(quarter))
            {
                var month = DateTime.Today.Month;
                quarter = month switch
                {
                    >= 4 and <= 6  => "Quarter 1",
                    >= 7 and <= 9  => "Quarter 2",
                    >= 10 and <= 12 => "Quarter 3",
                    _              => "Quarter 4"
                };
            }

            try
            {
                var vm = await _tdsService.GetComplianceAsync(year, quarter);
                ViewBag.StatusMessage = TempData["StatusMessage"];
                ViewBag.StatusType    = TempData["StatusType"];
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading QuarterlyTds for {Year} {Quarter}", year, quarter);
                TempData["StatusMessage"] = "Error loading TDS data. Please try again.";
                TempData["StatusType"]    = "danger";
                return RedirectToAction("QuarterlyTds", new { year, quarter });
            }
        }

        // ------------------------------------------------------------------ //
        //  POST: SaveTdsChallan                                               //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 10485760)]
        public async Task<IActionResult> SaveTdsChallan(TdsChallanVM vm, IFormFile ChallanPdf)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                // Upload PDF if provided
                if (ChallanPdf != null && ChallanPdf.Length > 0)
                {
                    var mimeType = _fileMimeTypeHelper.GetFileMimeType(ChallanPdf);
                    if (mimeType != "application/pdf")
                    {
                        TempData["StatusMessage"] = "Challan PDF must be a PDF file.";
                        TempData["StatusType"]    = "danger";
                        return RedirectToAction("QuarterlyTds", new { year = vm.FinancialYear, quarter = vm.Quarter });
                    }

                    var maxMb = _configuration.GetValue<int>("Tds:MaxUploadMb", 10);
                    if (ChallanPdf.Length > maxMb * 1024 * 1024)
                    {
                        TempData["StatusMessage"] = $"Challan PDF must be smaller than {maxMb} MB.";
                        TempData["StatusType"]    = "danger";
                        return RedirectToAction("QuarterlyTds", new { year = vm.FinancialYear, quarter = vm.Quarter });
                    }

                    var blobName = $"challan_{vm.FinancialYear}_{vm.Quarter}_W{vm.WeekNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
                        .Replace(" ", "_");
                    await _azureStorageHelper.InsertBlob(blobName, BlobContainer, ChallanPdf);
                    var container = _azureStorageHelper.GetCloudBlobContainer(BlobContainer);
                    vm.ChallanPdfPath = container.GetBlockBlobReference(blobName).Uri.ToString();
                }

                var newId = await _tdsService.SaveChallanAsync(vm, OperatorEmail());
                _logger.LogInformation("Challan saved id={Id} by {Op}", newId, OperatorEmail());

                TempData["StatusMessage"] = vm.id == 0 ? "Challan added successfully." : "Challan updated successfully.";
                TempData["StatusType"]    = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving challan");
                TempData["StatusMessage"] = "Error saving challan. Please try again.";
                TempData["StatusType"]    = "danger";
            }

            return RedirectToAction("QuarterlyTds", new { year = vm.FinancialYear, quarter = vm.Quarter });
        }

        // ------------------------------------------------------------------ //
        //  POST: DeleteTdsChallan                                             //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTdsChallan(int id, string year, string quarter)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                await _tdsService.DeleteChallanAsync(id);
                _logger.LogInformation("Challan deleted id={Id} by {Op}", id, OperatorEmail());
                TempData["StatusMessage"] = "Challan deleted.";
                TempData["StatusType"]    = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting challan id={Id}", id);
                TempData["StatusMessage"] = "Error deleting challan.";
                TempData["StatusType"]    = "danger";
            }

            return RedirectToAction("QuarterlyTds", new { year, quarter });
        }

        // ------------------------------------------------------------------ //
        //  POST: ToggleChallanPaid                                            //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleChallanPaid(int id, bool isPaid, string year, string quarter)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                await _tdsService.ToggleChallanPaidAsync(id, isPaid, year, quarter, OperatorEmail());
                TempData["StatusMessage"] = isPaid ? "Week marked as Paid." : "Week marked as Unpaid.";
                TempData["StatusType"]    = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling challan paid id={Id}", id);
                TempData["StatusMessage"] = "Error updating payment status.";
                TempData["StatusType"]    = "danger";
            }

            return RedirectToAction("QuarterlyTds", new { year, quarter });
        }

        // ------------------------------------------------------------------ //
        //  POST: SaveTdsReturnFiling                                          //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTdsReturnFiling(TdsReturnFilingVM vm)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                await _tdsService.SaveFilingAsync(vm, OperatorEmail());
                _logger.LogInformation("TDS filing saved {FY} {Q} by {Op}", vm.FinancialYear, vm.Quarter, OperatorEmail());
                TempData["StatusMessage"] = "Return filing saved.";
                TempData["StatusType"]    = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving return filing");
                TempData["StatusMessage"] = "Error saving return filing.";
                TempData["StatusType"]    = "danger";
            }

            return RedirectToAction("QuarterlyTds", new { year = vm.FinancialYear, quarter = vm.Quarter });
        }

        // ------------------------------------------------------------------ //
        //  POST: UploadForm16Dump                                             //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> UploadForm16Dump(string year, string quarter, List<IFormFile> files)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            if (files == null || !files.Any())
            {
                TempData["StatusMessage"] = "No files uploaded.";
                TempData["StatusType"]    = "warning";
                return RedirectToAction("QuarterlyTds", new { year, quarter });
            }

            var maxMb = _configuration.GetValue<int>("Tds:MaxUploadMb", 10);

            try
            {
                var result = await _tdsService.ProcessForm16DumpAsync(year, quarter, files,
                    async file =>
                    {
                        var blobName = $"form16_{year}_{quarter}_{file.FileName}".Replace(" ", "_");
                        await _azureStorageHelper.InsertBlob(blobName, BlobContainer, file);
                        var container = _azureStorageHelper.GetCloudBlobContainer(BlobContainer);
                        return container.GetBlockBlobReference(blobName).Uri.ToString();
                    });

                _logger.LogInformation(
                    "Form16A upload: matched={M} unmatched={U} skipped={S} by {Op}",
                    result.Matched.Count, result.Unmatched.Count, result.Skipped.Count, OperatorEmail());

                TempData["Form16Result"]  = System.Text.Json.JsonSerializer.Serialize(result);
                TempData["StatusMessage"] = $"Upload complete: {result.Matched.Count} matched, {result.Unmatched.Count} unmatched, {result.Skipped.Count} skipped.";
                TempData["StatusType"]    = result.Unmatched.Any() || result.Skipped.Any() ? "warning" : "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Form16 dump");
                TempData["StatusMessage"] = "Error processing Form16A files.";
                TempData["StatusType"]    = "danger";
            }

            return RedirectToAction("QuarterlyTds", new { year, quarter });
        }

        // ------------------------------------------------------------------ //
        //  GET: ExportTdsReturn26Q                                            //
        // ------------------------------------------------------------------ //

        [HttpGet]
        public async Task<IActionResult> ExportTdsReturn26Q(string year, string quarter)
        {
            if (!IsFinanceOrAdmin())
                return RedirectToAction("Index", "Home");

            try
            {
                var bytes    = await _tdsService.BuildExport26QAsync(year, quarter);
                var fileName = $"26Q_{year}_{quarter.Replace(" ", "_")}.xlsx";
                _logger.LogInformation("Export 26Q downloaded {File} by {Op}", fileName, OperatorEmail());
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting 26Q");
                TempData["StatusMessage"] = "Error generating export.";
                TempData["StatusType"]    = "danger";
                return RedirectToAction("QuarterlyTds", new { year, quarter });
            }
        }
    }
}
