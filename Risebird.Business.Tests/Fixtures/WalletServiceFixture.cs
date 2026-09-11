using System.Reflection;
using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.Fixtures
{
    /// <summary>
    /// Wallet fixture built around the PO ledger, because that is where the money is: the FIFO
    /// distribution in <c>GetPOHistory</c> decides how much of each purchase order a client has
    /// actually consumed, and every downstream balance, alert and dashboard reads from it.
    ///
    /// Repository results are served through mutable backing fields rather than fixed Moq
    /// setups, so <c>Given*</c> can be called before or after <see cref="CreateSut"/> without a
    /// later setup silently overriding an earlier one. Purchase orders are handed out as fresh
    /// clones per query, matching a real repository and keeping the ledger's in-place
    /// <c>WalletConsumed</c> mutation from leaking between calls.
    /// </summary>
    public sealed class WalletServiceFixture
    {
        private List<ClientPOVM> _purchaseOrders = new();
        private List<WalletMonthDebitVM> _debits = new();

        public Mock<IWalletRepository> Repository { get; } = new(MockBehavior.Loose);
        public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);

        public WalletService CreateSut()
        {
            Logger.Setup(x => x.LogMessage(It.IsAny<LogLevelInfo>(), It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            Repository.Setup(x => x.GetClientPOList(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>()))
                .ReturnsAsync(() => _purchaseOrders.Select(Clone).ToList());

            Repository.Setup(x => x.GetOwnerMonthlyDebits(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(() => _debits.ToList());

            return new WalletService(Repository.Object, Logger.Object);
        }

        public void VerifyExceptionLogged(Times times) =>
            Logger.Verify(
                x => x.LogMessage(LogLevelInfo.Exception, It.IsAny<Exception>(), It.IsAny<string>()), times);

        /// <summary>The purchase orders the repository will serve for any client-PO query.</summary>
        public WalletServiceFixture GivenPurchaseOrders(params ClientPOVM[] purchaseOrders)
        {
            _purchaseOrders = purchaseOrders.ToList();
            return this;
        }

        /// <summary>The monthly debit totals the repository will serve.</summary>
        public WalletServiceFixture GivenMonthlyDebits(params WalletMonthDebitVM[] debits)
        {
            _debits = debits.ToList();
            return this;
        }

        private static ClientPOVM Clone(ClientPOVM source)
        {
            var clone = new ClientPOVM();
            foreach (var property in typeof(ClientPOVM).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (property.CanRead && property.CanWrite)
                    property.SetValue(clone, property.GetValue(source));
            return clone;
        }

        // ── Builders ──────────────────────────────────────────────────────────

        public static ClientPOVM Po(
            decimal credit,
            int year,
            int month,
            int? ownerId = null,
            DateTime? expireDate = null,
            string ownerEmail = null,
            string poNumber = null,
            DateTime? createdOn = null,
            int clientId = 1) => new()
            {
                ClientId = clientId,
                POCreditAmount = credit,
                POYear = year,
                POMonth = month,
                BillingOwnerId = ownerId,
                BillingOwnerEmail = ownerEmail,
                BillingOwnerName = ownerEmail == null ? null : "Owner " + ownerEmail,
                PONumber = poNumber ?? $"PO-{year}-{month:00}",
                ExpireDate = expireDate,
                CreatedOn = createdOn
            };

        public static WalletMonthDebitVM Debit(
            decimal amount, int year, int month, int? ownerId = null, int billingType = 1, int clientId = 1) => new()
            {
                TotalDebit = amount,
                Year = year,
                Month = month,
                BillingOwnerId = ownerId,
                BillingType = billingType,
                ClientId = clientId
            };

        public static ClientPOGroupVM Client(
            string billingModelDesc = "M2 Prepaid",
            int billingType = 3,
            int clientId = 1,
            string clientName = "Acme",
            params ClientPOVM[] purchaseOrders) => new()
            {
                ClientId = clientId,
                ClientName = clientName,
                BillingType = billingType,
                BillingModelDesc = billingModelDesc,
                POs = purchaseOrders.ToList()
            };

        /// <summary>An expiry date in the past, relative to the running clock.</summary>
        public static DateTime Expired => DateTime.Today.AddDays(-1);

        /// <summary>An expiry date inside the 30-day "expiring soon" window.</summary>
        public static DateTime ExpiringSoon => DateTime.Today.AddDays(10);

        /// <summary>An expiry date comfortably outside the alert window.</summary>
        public static DateTime FarFuture => DateTime.Today.AddDays(400);
    }
}
