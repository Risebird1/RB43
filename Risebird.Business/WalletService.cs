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

namespace Risebird.Business
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public WalletService(IWalletRepository walletRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _walletRepository = walletRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var resultdata = await _walletRepository.GetWallet(billingType, clientId, billingOwnerId, action, startDate, endDate);
                return resultdata?.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null)
        {
            try
            {
                return await _walletRepository.GetWalletBalance(billingType, clientId, billingOwnerId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionRefNumber = null, string invoicePDF = null, DateTime? transactionDate = null, string transactionType = "Manual", int? clientPOId = null, int? invoiceId = null)
        {
            try
            {
                return await _walletRepository.AddClientWalletBalance(billingType, clientId, CreditAmount, loginedUser, CreatedOn, billingOwnerId, transactionRefNumber, invoicePDF, transactionDate, transactionType, clientPOId, invoiceId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null)
        {
            try
            {
                return await _walletRepository.SaveClientPO(clientId, billingType, billingModel, poCreditAmount, poMonth, poYear, createdBy, billingOwnerId, poNumber, poDocumentUrl, remarks);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<ClientPOVM>> GetClientPOList(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, bool openOnly = false)
        {
            try
            {
                var list = await _walletRepository.GetClientPOList(clientId, month, year, billingOwnerId, openOnly);
                foreach (var po in list)
                {
                    MapPOStatus(po);
                }
                return list;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<ClientPOVM>();
            }
        }

        public async Task<List<ClientPOVM>> GetPOHistory(int clientId, int billingType, int? month = null, int? year = null, int? billingOwnerId = null, string ownerEmail = null)
        {
            bool isOwnerScope = billingOwnerId.HasValue || !string.IsNullOrEmpty(ownerEmail);

            // For the per-owner view, always fetch ALL client POs and filter in C# by email.
            // This is resilient to BillingOwnerId int-ID mismatches across different user entities
            // while BillingOwnerEmail is the stable, canonical owner identifier.
            var allPos = await GetClientPOList(clientId, null, null, null);
            foreach (var po in allPos)
                MapPOStatus(po);

            List<ClientPOVM> list;
            if (isOwnerScope)
            {
                // Match by email (preferred) or fall back to ID
                list = allPos.Where(p =>
                    (!string.IsNullOrEmpty(ownerEmail) &&
                     string.Equals(p.BillingOwnerEmail, ownerEmail, StringComparison.OrdinalIgnoreCase))
                    ||
                    (string.IsNullOrEmpty(ownerEmail) && billingOwnerId.HasValue &&
                     p.BillingOwnerId == billingOwnerId.Value)
                ).ToList();
            }
            else
            {
                list = allPos;
            }

            if (!list.Any()) return list;

            try
            {
                if (isOwnerScope)
                {
                    // ── Per-owner view ────────────────────────────────────────────────────────
                    // Use the BillingOwnerId from the matched PO records to query debits.
                    // This ensures the debit query uses the same ID that was stored in Wallet.
                    var resolvedOwnerId = billingOwnerId
                        ?? list.Select(p => p.BillingOwnerId).FirstOrDefault(id => id.HasValue);

                    var ownerDebits = await _walletRepository.GetOwnerMonthlyDebits(
                        clientId, billingType, resolvedOwnerId);

                    var sortedPOs = list
                        .OrderBy(p => p.POYear)
                        .ThenBy(p => p.POMonth)
                        .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue)
                        .ToList();

                    RunFifoLedger(sortedPOs, ownerDebits, month, year);
                }
                else if (billingType == 1)
                {
                    // ── Overall, Billing-Owner scope: independent FIFO ledger per owner ───────
                    var allDebits = await _walletRepository.GetOwnerMonthlyDebits(clientId, billingType);

                    foreach (var ownerGroup in list.GroupBy(p => p.BillingOwnerId))
                    {
                        var ownerId    = ownerGroup.Key;
                        var ownerPOs   = ownerGroup
                            .OrderBy(p => p.POYear)
                            .ThenBy(p => p.POMonth)
                            .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue)
                            .ToList();
                        var ownerDebits = allDebits.Where(d => d.BillingOwnerId == ownerId);
                        RunFifoLedger(ownerPOs, ownerDebits, month, year);
                    }
                }
                else
                {
                    // ── Overall, Client scope: single shared pool ────────────────────────────
                    var allDebits = await _walletRepository.GetOwnerMonthlyDebits(clientId, billingType);
                    var sortedPOs = list
                        .OrderBy(p => p.POYear)
                        .ThenBy(p => p.POMonth)
                        .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue)
                        .ToList();
                    RunFifoLedger(sortedPOs, allDebits, month, year);
                }
            }
            catch { /* non-critical — WalletConsumed stays 0 */ }

            return list;
        }

        /// <summary>
        /// Returns the net PO Opening Balance per billing owner at the start of the selected month.
        ///   Opening Balance = Σ(PO credit before month) − Σ(wallet debits before month)
        /// A value of zero means the owner had no carry-forward credit entering the month.
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetOwnerOpeningBalances(int clientId, int selectedMonth, int selectedYear)
        {
            var result = new Dictionary<int, decimal>();
            try
            {
                // All POs for the client (no period filter — we filter in memory below)
                var allPOs = await GetClientPOList(clientId, null, null, null);

                // All monthly debit totals (BillingType 1 = Billing Owner scope)
                var allDebits = await _walletRepository.GetOwnerMonthlyDebits(clientId, billingType: 1);

                var ownerIds = allPOs
                    .Where(p => p.BillingOwnerId.HasValue)
                    .Select(p => p.BillingOwnerId!.Value)
                    .Distinct();

                foreach (var ownerId in ownerIds)
                {
                    // PO credit registered strictly before the 1st of selectedMonth
                    var prePOCredit = allPOs
                        .Where(p => p.BillingOwnerId == ownerId
                                 && (p.POYear < selectedYear
                                     || (p.POYear == selectedYear && p.POMonth < selectedMonth)))
                        .Sum(p => p.POCreditAmount);

                    // Wallet debits that occurred strictly before the 1st of selectedMonth
                    var preConsumed = allDebits
                        .Where(d => d.BillingOwnerId == ownerId
                                 && (d.Year < selectedYear
                                     || (d.Year == selectedYear && d.Month < selectedMonth)))
                        .Sum(d => d.TotalDebit);

                    // Net carry-forward (can be negative if debits exceeded credit)
                    result[ownerId] = prePOCredit - preConsumed;
                }
            }
            catch { /* non-critical — returns empty dict on error */ }

            return result;
        }

        /// <summary>
        /// Returns the BillingOwnerIds of all billing owners whose LATEST PO has had
        /// >= <paramref name="threshold"/> of its credit consumed (default 90 %).
        /// Consumption is computed using the same FIFO ledger as GetPOHistory so the
        /// values are accurate and consistent with what the user sees in the PO History popup.
        /// Only meaningful for BillingType = 1 (Billing Owner scope).
        /// </summary>
        public async Task<HashSet<int?>> GetHighConsumptionOwnerIds(int clientId, decimal threshold = 0.90m)
        {
            var result = new HashSet<int?>();
            try
            {
                // Fetch ALL POs for the client (no owner / period filter) and all debits
                var allPos = await GetClientPOList(clientId, null, null, null);
                if (!allPos.Any()) return result;

                foreach (var po in allPos) MapPOStatus(po);

                var allDebits = await _walletRepository.GetOwnerMonthlyDebits(clientId, billingType: 1);

                // Run an independent FIFO ledger per owner (all-time view — no month cutoff)
                foreach (var ownerGroup in allPos.GroupBy(p => p.BillingOwnerId))
                {
                    var ownerPOs = ownerGroup
                        .OrderBy(p => p.POYear)
                        .ThenBy(p => p.POMonth)
                        .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue)
                        .ToList();

                    var ownerDebits = allDebits.Where(d => d.BillingOwnerId == ownerGroup.Key);
                    RunFifoLedger(ownerPOs, ownerDebits, null, null);   // all-time state

                    // "Latest PO" = last in the chronologically sorted list
                    var latestPO = ownerPOs.Last();
                    if (latestPO.POCreditAmount > 0
                        && latestPO.WalletConsumed / latestPO.POCreditAmount >= threshold)
                    {
                        result.Add(ownerGroup.Key);
                    }
                }
            }
            catch { /* non-critical — return empty set on any error */ }

            return result;
        }

        /// <summary>
        /// Applies the carry-forward FIFO ledger to a sorted PO list using the supplied debits.
        /// Always replays debits month-by-month up to the cutoff date so the result represents
        /// the cumulative state as of that point (never just a single month in isolation).
        ///   Month-filter → cutoff = that month (shows "snapshot as of month-end")
        ///   All-data     → cutoff = latest PO month (shows all-time state)
        /// </summary>
        private static void RunFifoLedger(
            List<ClientPOVM> sortedPOs,
            IEnumerable<WalletMonthDebitVM> debits,
            int? filterMonth, int? filterYear)
        {
            // Determine the cutoff: include all debits up to and including this month
            var cutoff = filterMonth.HasValue && filterYear.HasValue
                ? new DateTime(filterYear.Value, filterMonth.Value, 1)
                : (sortedPOs.Any()
                    ? sortedPOs.Max(p => new DateTime(p.POYear, p.POMonth, 1))
                    : DateTime.MinValue);

            // Replay each month's debits chronologically (oldest first) up to the cutoff
            var orderedDebits = debits
                .Where(d => new DateTime(d.Year, d.Month, 1) <= cutoff)
                .OrderBy(d => d.Year)
                .ThenBy(d => d.Month);

            foreach (var debit in orderedDebits)
                ApplyFifo(sortedPOs, debit.TotalDebit);
        }

        /// <summary>
        /// Drains <paramref name="amount"/> from the PO pool using FIFO (oldest PO first).
        /// Each PO absorbs as much as possible without exceeding its credit amount.
        /// Any excess beyond total PO credit is silently ignored.
        /// </summary>
        private static void ApplyFifo(List<ClientPOVM> sortedPOs, decimal amount)
        {
            var rem = amount;
            foreach (var po in sortedPOs)
            {
                if (rem <= 0) break;
                var available = po.POCreditAmount - po.WalletConsumed;
                if (available <= 0) continue;
                var consume = Math.Min(rem, available);
                po.WalletConsumed += consume;
                rem -= consume;
            }
        }

        public async Task<List<WalletVM>> GetCashCreditHistory(int clientId)
        {
            try
            {
                return await _walletRepository.GetCashCreditHistory(clientId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WalletVM>();
            }
        }

        public async Task<bool> UpdatePOStatus(int poId, int clientId, int status)
        {
            try
            {
                return await _walletRepository.UpdatePOStatus(poId, clientId, status);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        private static void MapPOStatus(ClientPOVM po)
        {
            var mfi = new System.Globalization.DateTimeFormatInfo();
            po.PeriodLabel = mfi.GetMonthName(po.POMonth) + " " + po.POYear;
            switch (po.Status)
            {
                case 0:
                    po.StatusDesc = "Awaiting Cash";
                    po.StatusCss = "badge-warning";
                    break;
                case 1:
                    po.StatusDesc = "Partial Cash";
                    po.StatusCss = "badge-info";
                    break;
                case 2:
                    po.StatusDesc = "Cash Credited";
                    po.StatusCss = "badge-success";
                    break;
                case 3:
                    po.StatusDesc = "Document Only";
                    po.StatusCss = "badge-secondary";
                    break;
                case 4:
                    po.StatusDesc = "Complete";
                    po.StatusCss = "badge-primary";
                    break;
                case 5:
                    po.StatusDesc = "Paid";
                    po.StatusCss = "badge-success";
                    break;
                default:
                    po.StatusDesc = "Unknown";
                    po.StatusCss = "badge-secondary";
                    break;
            }
        }

        public async Task<ClientWalletDashboardVM> GetClientWalletDashboard(int clientId, int month, int year, bool allDetails = false)
        {
            try
            {
                var dashboard = await _walletRepository.GetClientWalletDashboard(clientId, month, year, allDetails);
                if (dashboard == null)
                {
                    dashboard = new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
                }

                dashboard.IsAllDetailsView = allDetails;
                dashboard.SupportsPO = dashboard.BillingModel == (int)BillingModelEnum.M2
                    || dashboard.BillingModel == (int)BillingModelEnum.M3;

                if (Enum.IsDefined(typeof(BillingModelEnum), dashboard.BillingModel))
                {
                    dashboard.BillingModelDesc = ((BillingModelEnum)dashboard.BillingModel).GetDescription();
                }

                var mfi = new System.Globalization.DateTimeFormatInfo();
                dashboard.PeriodLabel = mfi.GetMonthName(month) + " " + year;
                dashboard.DetailsPeriodLabel = allDetails ? "All Time" : dashboard.PeriodLabel;

                if (dashboard.POCreditLimit > 0)
                {
                    dashboard.POCreditUtilizationPercent = (int)Math.Min(100, Math.Round(dashboard.POConsumed / dashboard.POCreditLimit * 100));
                }

                foreach (var owner in dashboard.OwnerWallets)
                {
                    if (owner.POCredit <= 0)
                    {
                        owner.State = dashboard.BillingModel == (int)BillingModelEnum.M2 ? "Awaiting PO" : "No PO";
                        owner.StateCss = "badge-warning";
                    }
                    else if (owner.Remaining <= 0)
                    {
                        owner.State = "PO exhausted";
                        owner.StateCss = "badge-danger";
                    }
                    else
                    {
                        owner.State = "Active";
                        owner.StateCss = "badge-success";
                    }
                }

                foreach (var po in dashboard.PORecords)
                {
                    MapPOStatus(po);
                }

                return dashboard;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
            }
        }

        public async Task<AllClientsWalletVM> GetAllClientsWalletSummary(int fromMonth, int fromYear, int toMonth, int toYear)
        {
            var vm = await _walletRepository.GetAllClientsWalletSummary(fromMonth, fromYear, toMonth, toYear);

            var mfi = new System.Globalization.DateTimeFormatInfo();
            bool isSingleMonth = fromMonth == toMonth && fromYear == toYear;
            vm.PeriodLabel = isSingleMonth
                ? mfi.GetMonthName(fromMonth) + " " + fromYear
                : mfi.GetMonthName(fromMonth) + " " + fromYear + " – " + mfi.GetMonthName(toMonth) + " " + toYear;

            foreach (var row in vm.Clients)
            {
                row.BillingModelDesc = System.Enum.IsDefined(typeof(BillingModelEnum), row.BillingModel)
                    ? ((BillingModelEnum)row.BillingModel).GetDescription()
                    : "—";
            }

            return vm;
        }
    }
}

