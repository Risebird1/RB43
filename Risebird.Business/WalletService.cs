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

        public async Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null, int? poQuantity = null, DateTime? addedDate = null, DateTime? expireDate = null)
        {
            try
            {
                return await _walletRepository.SaveClientPO(clientId, billingType, billingModel, poCreditAmount, poMonth, poYear, createdBy, billingOwnerId, poNumber, poDocumentUrl, remarks, poQuantity, addedDate, expireDate);
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
        /// Returns per billing owner the total unconsumed PO credit on expired POs.
        /// Uses the same all-time FIFO ledger as PO History.
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetOwnerExpiredNotConsumed(int clientId, int billingType)
        {
            var result = new Dictionary<int, decimal>();
            if (billingType != 1) return result;

            try
            {
                var allPos = await GetPOHistory(clientId, billingType, null, null);
                foreach (var po in allPos.Where(p => p.IsExpired && p.BillingOwnerId.HasValue))
                {
                    var remaining = Math.Max(0m, po.WalletRemaining);
                    if (remaining <= 0) continue;

                    var ownerId = po.BillingOwnerId!.Value;
                    result[ownerId] = result.TryGetValue(ownerId, out var existing)
                        ? existing + remaining
                        : remaining;
                }
            }
            catch { /* non-critical */ }

            return result;
        }

        /// <summary>Owner/client wallet metrics for report pages (opening = 0 for all-time).</summary>
        public static void EnrichAlertRowMetrics(
            POConsumptionAlertRowVM row,
            IEnumerable<ClientPOVM> ownerPOs,
            decimal openingBalance = 0m)
        {
            var list = ownerPOs?.ToList() ?? new List<ClientPOVM>();
            row.POOpeningBalance = openingBalance;
            row.OwnerTotalCredit = list.Sum(p => p.POCreditAmount);
            row.OwnerTotalConsumed = list.Sum(p => p.WalletConsumed);
            row.ExpiredNotConsumed = list.Where(p => p.IsExpired).Sum(p => Math.Max(0m, p.WalletRemaining));
        }

        public static void EnrichAlertRowMetrics(
            POConsumptionAlertRowVM row,
            ClientPOGroupVM client,
            int? billingOwnerId)
        {
            var ownerPOs = billingOwnerId.HasValue
                ? client.POs.Where(p => p.BillingOwnerId == billingOwnerId)
                : client.POs.AsEnumerable();
            EnrichAlertRowMetrics(row, ownerPOs);
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
                ApplyFifo(sortedPOs, debit.TotalDebit, debit.Year, debit.Month);
        }

        /// <summary>
        /// Drains <paramref name="amount"/> from the PO pool using FIFO (oldest PO first).
        /// POs expired before the debit month are skipped. If total debits exceed eligible PO
        /// credit, overflow is added to the last eligible PO (or the chronologically last PO).
        /// </summary>
        private static void ApplyFifo(List<ClientPOVM> sortedPOs, decimal amount, int debitYear, int debitMonth)
        {
            var rem = amount;
            ClientPOVM lastEligible = null;

            foreach (var po in sortedPOs)
            {
                if (rem <= 0) break;
                if (!po.IsEligibleForDebitMonth(debitYear, debitMonth))
                    continue;

                lastEligible = po;
                var available = po.POCreditAmount - po.WalletConsumed;
                if (available <= 0) continue;
                var consume = Math.Min(rem, available);
                po.WalletConsumed += consume;
                rem -= consume;
            }

            if (rem > 0 && sortedPOs.Count > 0)
            {
                var overflowTarget = lastEligible ?? sortedPOs[sortedPOs.Count - 1];
                overflowTarget.WalletConsumed += rem;
            }
        }

        public async Task<int> BulkUpdateClientPOExpireDate(int clientId, IEnumerable<int> poIds, DateTime expireDate)
        {
            try
            {
                return await _walletRepository.BulkUpdateClientPOExpireDate(clientId, poIds, expireDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        /// <summary>
        /// Returns alert rows for latest POs that are expired or expiring within 30 days
        /// and still have a remaining balance (M2/M3 clients only).
        /// </summary>
        public async Task<List<POConsumptionAlertRowVM>> GetPOExpiryAlerts(List<ClientPOGroupVM> allSummary = null)
        {
            var alerts = new List<POConsumptionAlertRowVM>();
            try
            {
                allSummary ??= await GetAllClientsPOSummary();
                foreach (var client in allSummary)
                {
                    if (!client.BillingModelDesc.StartsWith("M2", StringComparison.OrdinalIgnoreCase)
                        && !client.BillingModelDesc.StartsWith("M3", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (client.BillingType == 1)
                    {
                        foreach (var ownerGroup in client.POs.GroupBy(p => new { p.BillingOwnerId, p.BillingOwnerName, p.BillingOwnerEmail }))
                        {
                            var latestPO = ownerGroup
                                .OrderByDescending(p => p.POYear).ThenByDescending(p => p.POMonth)
                                .ThenByDescending(p => p.CreatedOn ?? DateTime.MinValue)
                                .FirstOrDefault();
                            TryAddExpiryAlert(alerts, client, latestPO, ownerGroup.Key.BillingOwnerId,
                                ownerGroup.Key.BillingOwnerName, ownerGroup.Key.BillingOwnerEmail);
                        }
                    }
                    else
                    {
                        var latestPO = client.POs
                            .OrderByDescending(p => p.POYear).ThenByDescending(p => p.POMonth)
                            .ThenByDescending(p => p.CreatedOn ?? DateTime.MinValue)
                            .FirstOrDefault();
                        TryAddExpiryAlert(alerts, client, latestPO, null, null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return alerts
                .OrderBy(a => a.IsExpired ? 0 : 1)
                .ThenBy(a => a.ExpireDate)
                .ToList();
        }

        private static void TryAddExpiryAlert(
            List<POConsumptionAlertRowVM> alerts,
            ClientPOGroupVM client,
            ClientPOVM latestPO,
            int? billingOwnerId,
            string billingOwnerName,
            string billingOwnerEmail)
        {
            if (latestPO == null || !latestPO.ExpireDate.HasValue) return;
            if (!latestPO.IsExpired && !latestPO.IsExpiringSoon) return;
            if (latestPO.WalletRemaining <= 0) return;

            alerts.Add(new POConsumptionAlertRowVM
            {
                ClientId          = client.ClientId,
                ClientName        = client.ClientName,
                BillingType       = client.BillingType,
                BillingModelDesc  = client.BillingModelDesc,
                BillingOwnerId    = billingOwnerId,
                BillingOwnerName  = billingOwnerName,
                BillingOwnerEmail = billingOwnerEmail,
                PONumber          = latestPO.PONumber,
                POMonth           = latestPO.POMonth,
                POYear            = latestPO.POYear,
                POCreditAmount    = latestPO.POCreditAmount,
                WalletConsumed    = latestPO.WalletConsumed,
                ExpireDate        = latestPO.ExpireDate,
                AlertType         = "Expiry"
            });
            EnrichAlertRowMetrics(alerts[alerts.Count - 1], client, billingOwnerId);
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

        public async Task<bool> UpdateClientPO(int poId, int clientId, string poNumber, decimal poCreditAmount, int poMonth, int poYear, int? poQuantity, DateTime? addedDate, DateTime? expireDate, string poDocumentUrl = null, bool updateDocument = false)
        {
            try
            {
                return await _walletRepository.UpdateClientPO(poId, clientId, poNumber, poCreditAmount, poMonth, poYear, poQuantity, addedDate, expireDate, poDocumentUrl, updateDocument);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
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

        public async Task<int> SaveClientAgreement(int clientId, int agreementType, DateTime? startDate, DateTime? expireDate, int status, string agreementFileUrl, string remarks, string createdBy)
        {
            try
            {
                return await _walletRepository.InsertClientAgreement(clientId, agreementType, startDate, expireDate, status, agreementFileUrl, remarks, createdBy);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateClientAgreement(int agreementId, int clientId, int agreementType, DateTime? startDate, DateTime? expireDate, int status, string remarks, string modifiedBy, string agreementFileUrl = null, bool updateDocument = false)
        {
            try
            {
                return await _walletRepository.UpdateClientAgreement(agreementId, clientId, agreementType, startDate, expireDate, status, remarks, modifiedBy, agreementFileUrl, updateDocument);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<ClientAgreementVM>> GetClientAgreementList(int clientId)
        {
            try
            {
                var list = await _walletRepository.GetClientAgreementList(clientId);
                foreach (var agreement in list) MapAgreementType(agreement);
                return list;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<ClientAgreementVM>();
            }
        }

        public async Task<List<ClientAgreementVM>> GetAllClientsLatestAgreement()
        {
            try
            {
                var list = await _walletRepository.GetAllClientsLatestAgreement();
                foreach (var agreement in list) MapAgreementType(agreement);
                return list;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<ClientAgreementVM>();
            }
        }

        private static void MapAgreementType(ClientAgreementVM agreement)
        {
            agreement.AgreementTypeDesc = ((AgreementTypeEnum)agreement.AgreementType).ToString();
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

                // Flag owners whose latest PO is expired with remaining balance
                if (dashboard.BillingType == 1 && dashboard.PORecords.Any())
                {
                    foreach (var owner in dashboard.OwnerWallets)
                    {
                        var latestPO = dashboard.PORecords
                            .Where(p => p.BillingOwnerId == owner.BillingOwnerId)
                            .OrderByDescending(p => p.POYear).ThenByDescending(p => p.POMonth)
                            .ThenByDescending(p => p.CreatedOn ?? DateTime.MinValue)
                            .FirstOrDefault();
                        if (latestPO != null && latestPO.IsExpired && owner.Remaining > 0)
                        {
                            owner.State = "PO expired";
                            owner.StateCss = "badge-danger";
                        }
                        else if (latestPO != null && latestPO.IsExpiringSoon && owner.Remaining > 0)
                        {
                            owner.State = "Expiring soon";
                            owner.StateCss = "badge-warning";
                        }
                    }
                }

                foreach (var po in dashboard.PORecords)
                {
                    MapPOStatus(po);
                }

                var agreements = await _walletRepository.GetClientAgreementList(clientId);
                var latestAgreement = agreements.OrderByDescending(a => a.CreatedOn).ThenByDescending(a => a.id).FirstOrDefault();
                if (latestAgreement != null)
                {
                    MapAgreementType(latestAgreement);
                    dashboard.LatestAgreement = latestAgreement;
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

        /// <summary>
        /// Single-call PO Summary: fetches all PO + debit rows in one SP round-trip,
        /// applies per-client / per-owner FIFO in memory, and returns grouped results
        /// sorted by descending overall consumption %.
        /// </summary>
        public async Task<List<ClientPOGroupVM>> GetAllClientsPOSummary()
        {
            var (allPOs, allDebits) = await _walletRepository.GetAllClientsPOData();

            // Map status labels (same helper used in GetPOHistory)
            foreach (var po in allPOs) MapPOStatus(po);

            var result = new List<ClientPOGroupVM>();

            // Group POs by client
            var byClient = allPOs.GroupBy(p => p.ClientId);

            foreach (var clientGroup in byClient)
            {
                var clientId   = clientGroup.Key;
                var clientPOs  = clientGroup.ToList();
                var billingType = clientPOs.First().BillingType;
                var clientName  = clientPOs.First().ClientName ?? clientId.ToString();
                var billingModel = clientPOs.First().BillingModel;

                // Client debits from all-clients debit set
                var clientDebits = allDebits.Where(d => d.ClientId == clientId).ToList();

                // Run FIFO per owner (BT=1) or client-wide (BT=3)
                if (billingType == 1)
                {
                    foreach (var ownerGroup in clientPOs.GroupBy(p => p.BillingOwnerId))
                    {
                        var ownerPOs = ownerGroup
                            .OrderBy(p => p.POYear).ThenBy(p => p.POMonth)
                            .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue).ToList();
                        var ownerDebits = clientDebits.Where(d => d.BillingOwnerId == ownerGroup.Key);
                        RunFifoLedger(ownerPOs, ownerDebits, null, null);
                    }
                }
                else
                {
                    var sortedPOs = clientPOs
                        .OrderBy(p => p.POYear).ThenBy(p => p.POMonth)
                        .ThenBy(p => p.CreatedOn ?? DateTime.MaxValue).ToList();
                    RunFifoLedger(sortedPOs, clientDebits, null, null);
                }

                string modelDesc = System.Enum.IsDefined(typeof(BillingModelEnum), billingModel)
                    ? ((BillingModelEnum)billingModel).GetDescription()
                    : (billingModel > 0 ? $"M{billingModel}" : "—");

                result.Add(new ClientPOGroupVM
                {
                    ClientId         = clientId,
                    ClientName       = clientName,
                    BillingType      = billingType,
                    BillingModelDesc = modelDesc,
                    POs              = clientPOs
                });
            }

            return result
                .OrderByDescending(c => c.TotalCredit > 0 ? c.TotalConsumed / c.TotalCredit : 0)
                .ToList();
        }
    }
}

