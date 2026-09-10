using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.ViewModel;
using static Risebird.Business.Tests.Fixtures.WalletServiceFixture;

namespace Risebird.Business.Tests.FinanceTests
{
    public class WalletServiceOwnerOpeningBalanceTests
    {
        [Fact]
        public async Task No_purchase_orders_yields_no_opening_balances()
        {
            var sut = new WalletServiceFixture().CreateSut();

            Assert.Empty(await sut.GetOwnerOpeningBalances(1, 6, 2026));
        }

        [Fact]
        public async Task Purchase_orders_without_an_owner_are_ignored()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: null));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerOpeningBalances(1, 6, 2026));
        }

        [Fact]
        public async Task Opening_balance_is_prior_credit_minus_prior_debits()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(400m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, selectedMonth: 6, selectedYear: 2026);

            Assert.Equal(600m, balances[10]);
        }

        [Fact]
        public async Task Credit_and_debits_from_the_selected_month_itself_are_excluded()
        {
            // The opening balance is the state entering the month, so June's own activity
            // must not be counted.
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10),
                    Po(5000m, 2026, 6, ownerId: 10))
                .GivenMonthlyDebits(
                    Debit(100m, 2026, 1, ownerId: 10),
                    Debit(900m, 2026, 6, ownerId: 10));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Equal(900m, balances[10]);
        }

        [Fact]
        public async Task Activity_from_a_previous_year_is_included()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2025, 12, ownerId: 10))
                .GivenMonthlyDebits(Debit(250m, 2025, 12, ownerId: 10));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 1, 2026);

            Assert.Equal(750m, balances[10]);
        }

        [Fact]
        public async Task Activity_from_a_later_year_is_excluded()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10),
                    Po(9000m, 2027, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Equal(1000m, balances[10]);
        }

        [Fact]
        public async Task An_owner_who_over_consumed_carries_a_negative_balance_forward()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(500m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(800m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Equal(-300m, balances[10]);
        }

        [Fact]
        public async Task Each_owner_gets_an_independent_balance()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10),
                    Po(2000m, 2026, 1, ownerId: 20))
                .GivenMonthlyDebits(
                    Debit(100m, 2026, 1, ownerId: 10),
                    Debit(500m, 2026, 1, ownerId: 20));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Equal(900m, balances[10]);
            Assert.Equal(1500m, balances[20]);
        }

        [Fact]
        public async Task Another_owners_debits_do_not_reduce_this_owners_balance()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(900m, 2026, 1, ownerId: 20));
            var sut = fixture.CreateSut();

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Equal(1000m, balances[10]);
        }

        [Fact]
        public async Task An_owner_with_credit_but_no_debits_carries_the_full_amount()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Equal(1000m, (await sut.GetOwnerOpeningBalances(1, 6, 2026))[10]);
        }

        [Fact]
        public async Task A_repository_failure_yields_an_empty_dictionary_rather_than_throwing()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetOwnerMonthlyDebits(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10));

            var balances = await sut.GetOwnerOpeningBalances(1, 6, 2026);

            Assert.Empty(balances);
        }

        [Fact]
        public async Task Reads_owner_scoped_debits_because_opening_balances_are_per_owner()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            await sut.GetOwnerOpeningBalances(1, 6, 2026);

            fixture.Repository.Verify(x => x.GetOwnerMonthlyDebits(1, 1, null), Times.Once);
        }
    }

    public class WalletServiceExpiredNotConsumedTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task Only_the_billing_owner_scope_is_supported(int billingType)
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType));
        }

        [Fact]
        public async Task Client_scope_does_not_even_query_the_repository()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetOwnerExpiredNotConsumed(1, billingType: 3);

            fixture.Repository.Verify(
                x => x.GetClientPOList(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<int?>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task An_expired_purchase_order_with_a_balance_is_reported()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired));
            var sut = fixture.CreateSut();

            var lapsed = await sut.GetOwnerExpiredNotConsumed(1, billingType: 1);

            Assert.Equal(1000m, lapsed[10]);
        }

        [Fact]
        public async Task A_live_purchase_order_is_not_reported()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.FarFuture));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType: 1));
        }

        [Fact]
        public async Task A_purchase_order_with_no_expiry_is_not_reported()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: null));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType: 1));
        }

        [Fact]
        public async Task A_fully_consumed_expired_purchase_order_is_not_reported()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired))
                .GivenMonthlyDebits(Debit(1000m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType: 1));
        }

        [Fact]
        public async Task Only_the_unconsumed_part_of_an_expired_purchase_order_is_reported()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired))
                .GivenMonthlyDebits(Debit(400m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var lapsed = await sut.GetOwnerExpiredNotConsumed(1, billingType: 1);

            Assert.Equal(600m, lapsed[10]);
        }

        [Fact]
        public async Task An_expired_purchase_order_without_an_owner_is_ignored()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: null, expireDate: WalletServiceFixture.Expired));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType: 1));
        }

        [Fact]
        public async Task Several_expired_purchase_orders_for_one_owner_are_summed()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired, poNumber: "a"),
                    Po(500m, 2026, 2, ownerId: 10, expireDate: WalletServiceFixture.Expired, poNumber: "b"));
            var sut = fixture.CreateSut();

            var lapsed = await sut.GetOwnerExpiredNotConsumed(1, billingType: 1);

            Assert.Equal(1500m, lapsed[10]);
        }

        [Fact]
        public async Task Each_owner_is_reported_separately()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10, expireDate: WalletServiceFixture.Expired),
                    Po(300m, 2026, 1, ownerId: 20, expireDate: WalletServiceFixture.Expired));
            var sut = fixture.CreateSut();

            var lapsed = await sut.GetOwnerExpiredNotConsumed(1, billingType: 1);

            Assert.Equal(1000m, lapsed[10]);
            Assert.Equal(300m, lapsed[20]);
        }

        [Fact]
        public async Task A_repository_failure_yields_an_empty_dictionary_rather_than_throwing()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetClientPOList(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Empty(await sut.GetOwnerExpiredNotConsumed(1, billingType: 1));
        }
    }

    public class WalletServiceHighConsumptionTests
    {
        [Fact]
        public async Task No_purchase_orders_yields_no_flagged_owners()
        {
            var sut = new WalletServiceFixture().CreateSut();

            Assert.Empty(await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task An_owner_at_the_default_ninety_percent_threshold_is_flagged()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(900m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Contains((int?)10, await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task An_owner_just_below_the_threshold_is_not_flagged()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(899m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task An_over_consumed_owner_is_flagged()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(1500m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Contains((int?)10, await sut.GetHighConsumptionOwnerIds(1));
        }

        [Theory]
        [InlineData(0.50, 500, true)]
        [InlineData(0.50, 499, false)]
        [InlineData(0.10, 100, true)]
        [InlineData(1.00, 1000, true)]
        [InlineData(1.00, 999, false)]
        public async Task The_threshold_is_configurable(double threshold, int consumed, bool expectFlagged)
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(consumed, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var flagged = await sut.GetHighConsumptionOwnerIds(1, (decimal)threshold);

            Assert.Equal(expectFlagged, flagged.Contains((int?)10));
        }

        [Fact]
        public async Task Only_the_latest_purchase_order_decides_whether_an_owner_is_flagged()
        {
            // An old, fully drained PO must not flag an owner who has since topped up.
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10, poNumber: "old"),
                    Po(10_000m, 2026, 6, ownerId: 10, poNumber: "latest"))
                .GivenMonthlyDebits(Debit(1000m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task An_owner_whose_latest_purchase_order_is_nearly_drained_is_flagged()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10, poNumber: "old"),
                    Po(1000m, 2026, 6, ownerId: 10, poNumber: "latest"))
                .GivenMonthlyDebits(Debit(1950m, 2026, 6, ownerId: 10));
            var sut = fixture.CreateSut();

            // FIFO drains the old PO fully, then 950 of the latest 1000 — 95%.
            Assert.Contains((int?)10, await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task A_zero_credit_latest_purchase_order_is_never_flagged()
        {
            // Guards against a divide-by-zero dressed up as 100% consumption.
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(Po(0m, 2026, 1, ownerId: 10))
                .GivenMonthlyDebits(Debit(500m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            Assert.Empty(await sut.GetHighConsumptionOwnerIds(1));
        }

        [Fact]
        public async Task Owners_are_evaluated_independently()
        {
            var fixture = new WalletServiceFixture()
                .GivenPurchaseOrders(
                    Po(1000m, 2026, 1, ownerId: 10),
                    Po(1000m, 2026, 1, ownerId: 20))
                .GivenMonthlyDebits(
                    Debit(950m, 2026, 1, ownerId: 10),
                    Debit(100m, 2026, 1, ownerId: 20));
            var sut = fixture.CreateSut();

            var flagged = await sut.GetHighConsumptionOwnerIds(1);

            Assert.Contains((int?)10, flagged);
            Assert.DoesNotContain((int?)20, flagged);
        }

        [Fact]
        public async Task A_repository_failure_yields_an_empty_set_rather_than_throwing()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetOwnerMonthlyDebits(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10));

            Assert.Empty(await sut.GetHighConsumptionOwnerIds(1));
        }
    }

    /// <summary>
    /// The remaining wallet surface is repository delegation. Each method gets its happy path
    /// and its documented fallback, because a swallowed write here loses a client's credit.
    /// </summary>
    public class WalletServiceDelegationTests
    {
        [Fact]
        public async Task Get_wallet_returns_the_repository_rows()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetWallet(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new[] { new WalletVM(), new WalletVM() });

            var wallet = await sut.GetWallet(3, clientId: 1);

            Assert.Equal(2, wallet.Count);
        }

        [Fact]
        public async Task Get_wallet_forwards_every_filter()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var start = new DateTime(2026, 01, 01);
            var end = new DateTime(2026, 01, 31);
            fixture.Repository.Setup(x => x.GetWallet(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(Array.Empty<WalletVM>());

            await sut.GetWallet(3, 1, 10, "Credit", start, end);

            fixture.Repository.Verify(x => x.GetWallet(3, 1, 10, "Credit", start, end), Times.Once);
        }

        [Fact]
        public async Task Get_wallet_returns_null_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetWallet(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Null(await sut.GetWallet(3, 1));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1500)]
        [InlineData(-200)]
        public async Task Get_wallet_balance_returns_the_repository_value(int balance)
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetWalletBalance(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(balance);

            Assert.Equal(balance, await sut.GetWalletBalance(3, 1));
        }

        [Fact]
        public async Task Get_wallet_balance_returns_zero_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetWalletBalance(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.GetWalletBalance(3, 1));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Adding_credit_forwards_the_full_transaction_detail()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var createdOn = new DateTime(2026, 09, 09);
            var transactionDate = new DateTime(2026, 09, 08);
            fixture.Repository.Setup(x => x.AddClientWalletBalance(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(true);

            var added = await sut.AddClientWalletBalance(
                3, 1, 5000, "ops@risebird.io", createdOn, 10, "TXN-1", "https://blob/i.pdf",
                transactionDate, "Invoice", 7, 9);

            Assert.True(added);
            fixture.Repository.Verify(x => x.AddClientWalletBalance(
                3, 1, 5000, "ops@risebird.io", createdOn, 10, "TXN-1", "https://blob/i.pdf",
                transactionDate, "Invoice", 7, 9), Times.Once);
        }

        [Fact]
        public async Task Adding_credit_defaults_the_transaction_type_to_manual()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.AddClientWalletBalance(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(true);

            await sut.AddClientWalletBalance(3, 1, 5000, "ops@risebird.io", null);

            fixture.Repository.Verify(x => x.AddClientWalletBalance(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                "Manual", It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task Adding_credit_returns_false_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.AddClientWalletBalance(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.AddClientWalletBalance(3, 1, 5000, "ops@risebird.io", null));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Saving_a_purchase_order_returns_the_new_id()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.SaveClientPO(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(77);

            var id = await sut.SaveClientPO(1, 3, 2, 5000m, 9, 2026, "ops@risebird.io");

            Assert.Equal(77, id);
        }

        [Fact]
        public async Task Saving_a_purchase_order_returns_zero_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.SaveClientPO(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.SaveClientPO(1, 3, 2, 5000m, 9, 2026, "ops@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Getting_the_purchase_order_list_returns_an_empty_list_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetClientPOList(
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            var list = await sut.GetClientPOList(1);

            Assert.NotNull(list);
            Assert.Empty(list);
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Getting_the_purchase_order_list_forwards_its_filters()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetClientPOList(1, month: 9, year: 2026, billingOwnerId: 10, openOnly: true);

            fixture.Repository.Verify(x => x.GetClientPOList(1, 9, 2026, 10, true), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Updating_a_purchase_order_returns_the_repository_result(bool repositoryResult)
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdateClientPO(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(repositoryResult);

            var updated = await sut.UpdateClientPO(7, 1, "PO-1", 5000m, 9, 2026, null, null, null);

            Assert.Equal(repositoryResult, updated);
        }

        [Fact]
        public async Task Updating_a_purchase_order_returns_false_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdateClientPO(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.UpdateClientPO(7, 1, "PO-1", 5000m, 9, 2026, null, null, null));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Updating_a_purchase_order_status_returns_the_repository_result(bool repositoryResult)
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdatePOStatus(7, 1, 2)).ReturnsAsync(repositoryResult);

            Assert.Equal(repositoryResult, await sut.UpdatePOStatus(7, 1, 2));
        }

        [Fact]
        public async Task Updating_a_purchase_order_status_returns_false_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdatePOStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.UpdatePOStatus(7, 1, 2));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Bulk_extending_expiry_returns_the_affected_row_count()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var expireDate = new DateTime(2027, 03, 31);
            fixture.Repository.Setup(x => x.BulkUpdateClientPOExpireDate(
                    It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), It.IsAny<DateTime>()))
                .ReturnsAsync(3);

            Assert.Equal(3, await sut.BulkUpdateClientPOExpireDate(1, new[] { 1, 2, 3 }, expireDate));
        }

        [Fact]
        public async Task Bulk_extending_expiry_returns_zero_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.BulkUpdateClientPOExpireDate(
                    It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.BulkUpdateClientPOExpireDate(1, new[] { 1 }, DateTime.Today));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Cash_credit_history_is_passed_through()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var rows = new List<WalletVM> { new() };
            fixture.Repository.Setup(x => x.GetCashCreditHistory(1)).ReturnsAsync(rows);

            Assert.Same(rows, await sut.GetCashCreditHistory(1));
        }

        [Fact]
        public async Task Saving_an_agreement_returns_the_new_id()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.InsertClientAgreement(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(12);

            var id = await sut.SaveClientAgreement(
                1, 2, DateTime.Today, DateTime.Today.AddYears(1), 1, "https://blob/a.pdf", "note", "ops@risebird.io");

            Assert.Equal(12, id);
        }

        [Fact]
        public async Task Saving_an_agreement_returns_zero_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.InsertClientAgreement(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.Equal(0, await sut.SaveClientAgreement(1, 2, null, null, 1, null, null, "ops@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task Updating_an_agreement_returns_false_and_logs_on_failure()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.UpdateClientAgreement(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            Assert.False(await sut.UpdateClientAgreement(
                12, 1, 2, null, null, 1, "note", "ops@risebird.io"));
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task The_agreement_list_is_passed_through()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var rows = new List<ClientAgreementVM> { new() };
            fixture.Repository.Setup(x => x.GetClientAgreementList(1)).ReturnsAsync(rows);

            Assert.Same(rows, await sut.GetClientAgreementList(1));
        }

        [Fact]
        public async Task The_all_clients_latest_agreement_list_is_passed_through()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var rows = new List<ClientAgreementVM> { new(), new() };
            fixture.Repository.Setup(x => x.GetAllClientsLatestAgreement()).ReturnsAsync(rows);

            Assert.Equal(2, (await sut.GetAllClientsLatestAgreement()).Count);
        }
    }
}
