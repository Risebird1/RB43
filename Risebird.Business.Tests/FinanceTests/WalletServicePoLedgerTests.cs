using Moq;
using Risebird.Business.Tests.Fixtures;
using Risebird.Entities.ViewModel;
using static Risebird.Business.Tests.Fixtures.WalletServiceFixture;

namespace Risebird.Business.Tests.FinanceTests
{
    /// <summary>
    /// The FIFO ledger decides how much of each purchase order a client has consumed. Every
    /// wallet balance, PO History popup and expiry alert reads from it, so each rule gets its
    /// own case: oldest PO drains first, expired POs are skipped for later debit months,
    /// overflow lands on the last eligible PO, and a month filter is a cumulative snapshot
    /// rather than a single month in isolation.
    /// </summary>
    public class WalletServicePoHistoryClientScopeTests
    {
        private const int ClientScope = 3;

        [Fact]
        public async Task No_purchase_orders_yields_an_empty_ledger()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Empty(history);
        }

        [Fact]
        public async Task An_empty_po_list_short_circuits_before_reading_debits()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetPOHistory(1, ClientScope);

            fixture.Repository.Verify(
                x => x.GetOwnerMonthlyDebits(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task A_single_debit_drains_the_only_purchase_order()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(Debit(400m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(400m, Assert.Single(history).WalletConsumed);
            Assert.Equal(600m, history[0].WalletRemaining);
        }

        [Fact]
        public async Task Debits_drain_the_oldest_purchase_order_first()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 2, poNumber: "newer"),
                Po(1000m, 2026, 1, poNumber: "older"));
            fixture.GivenMonthlyDebits(Debit(600m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            var older = history.Single(p => p.PONumber == "older");
            var newer = history.Single(p => p.PONumber == "newer");
            Assert.Equal(600m, older.WalletConsumed);
            Assert.Equal(0m, newer.WalletConsumed);
        }

        [Fact]
        public async Task A_debit_larger_than_the_oldest_po_spills_into_the_next_one()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, poNumber: "first"),
                Po(1000m, 2026, 2, poNumber: "second"));
            fixture.GivenMonthlyDebits(Debit(1500m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(1000m, history.Single(p => p.PONumber == "first").WalletConsumed);
            Assert.Equal(500m, history.Single(p => p.PONumber == "second").WalletConsumed);
        }

        [Fact]
        public async Task Same_month_purchase_orders_are_ordered_by_creation_time()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(500m, 2026, 1, poNumber: "later", createdOn: new DateTime(2026, 01, 20)),
                Po(500m, 2026, 1, poNumber: "earlier", createdOn: new DateTime(2026, 01, 05)));
            fixture.GivenMonthlyDebits(Debit(500m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(500m, history.Single(p => p.PONumber == "earlier").WalletConsumed);
            Assert.Equal(0m, history.Single(p => p.PONumber == "later").WalletConsumed);
        }

        [Fact]
        public async Task A_purchase_order_with_no_creation_time_sorts_last_within_its_month()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(500m, 2026, 1, poNumber: "unknown-date", createdOn: null),
                Po(500m, 2026, 1, poNumber: "dated", createdOn: new DateTime(2026, 01, 05)));
            fixture.GivenMonthlyDebits(Debit(500m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(500m, history.Single(p => p.PONumber == "dated").WalletConsumed);
            Assert.Equal(0m, history.Single(p => p.PONumber == "unknown-date").WalletConsumed);
        }

        [Fact]
        public async Task Debits_are_replayed_oldest_month_first()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, poNumber: "first"),
                Po(1000m, 2026, 2, poNumber: "second"));
            fixture.GivenMonthlyDebits(
                Debit(700m, 2026, 2),
                Debit(700m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            // 1400 total against a 1000 + 1000 pool, FIFO: first fills, then second takes 400.
            Assert.Equal(1000m, history.Single(p => p.PONumber == "first").WalletConsumed);
            Assert.Equal(400m, history.Single(p => p.PONumber == "second").WalletConsumed);
        }

        [Fact]
        public async Task Over_consumption_beyond_the_pool_lands_on_the_last_eligible_po()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(100m, 2026, 1, poNumber: "first"),
                Po(100m, 2026, 2, poNumber: "second"));
            fixture.GivenMonthlyDebits(Debit(500m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(100m, history.Single(p => p.PONumber == "first").WalletConsumed);
            Assert.Equal(400m, history.Single(p => p.PONumber == "second").WalletConsumed);
        }

        [Fact]
        public async Task Over_consumption_leaves_a_negative_remaining_rather_than_clamping_to_zero()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(100m, 2026, 1));
            fixture.GivenMonthlyDebits(Debit(250m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(250m, history[0].WalletConsumed);
            Assert.Equal(-150m, history[0].WalletRemaining);
        }

        [Fact]
        public async Task A_debit_of_zero_consumes_nothing()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(Debit(0m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(0m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task A_zero_credit_purchase_order_is_skipped_and_the_debit_moves_on()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(0m, 2026, 1, poNumber: "empty"),
                Po(500m, 2026, 2, poNumber: "funded"));
            fixture.GivenMonthlyDebits(Debit(300m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(0m, history.Single(p => p.PONumber == "empty").WalletConsumed);
            Assert.Equal(300m, history.Single(p => p.PONumber == "funded").WalletConsumed);
        }
    }

    public class WalletServicePoHistoryExpiryTests
    {
        private const int ClientScope = 3;

        [Fact]
        public async Task An_unexpiring_purchase_order_absorbs_debits_from_any_month()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, expireDate: null));
            fixture.GivenMonthlyDebits(Debit(400m, 2026, 12));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 12, year: 2026);

            Assert.Equal(400m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task A_purchase_order_absorbs_a_debit_from_the_month_it_expires_in()
        {
            // Eligibility is ExpireDate >= the 1st of the debit month, so a PO expiring
            // mid-month still absorbs that whole month's charges.
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, expireDate: new DateTime(2026, 03, 15)));
            fixture.GivenMonthlyDebits(Debit(400m, 2026, 3));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 3, year: 2026);

            Assert.Equal(400m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task A_purchase_order_that_expired_before_the_debit_month_is_skipped()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, expireDate: new DateTime(2026, 02, 28), poNumber: "lapsed"),
                Po(1000m, 2026, 2, poNumber: "live"));
            fixture.GivenMonthlyDebits(Debit(300m, 2026, 3));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 3, year: 2026);

            Assert.Equal(0m, history.Single(p => p.PONumber == "lapsed").WalletConsumed);
            Assert.Equal(300m, history.Single(p => p.PONumber == "live").WalletConsumed);
        }

        [Fact]
        public async Task An_expired_purchase_order_still_absorbs_debits_from_before_it_lapsed()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, expireDate: new DateTime(2026, 02, 28), poNumber: "lapsed"),
                Po(1000m, 2026, 2, poNumber: "live"));
            fixture.GivenMonthlyDebits(
                Debit(300m, 2026, 1),
                Debit(400m, 2026, 3));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 3, year: 2026);

            Assert.Equal(300m, history.Single(p => p.PONumber == "lapsed").WalletConsumed);
            Assert.Equal(400m, history.Single(p => p.PONumber == "live").WalletConsumed);
        }

        [Fact]
        public async Task When_every_purchase_order_has_lapsed_the_overflow_lands_on_the_last_one()
        {
            // No eligible PO for the debit month, so the chronologically last PO absorbs it —
            // which is how an over-consumed, fully expired client shows up in the ledger.
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(500m, 2026, 1, expireDate: new DateTime(2026, 01, 31), poNumber: "first"),
                Po(500m, 2026, 2, expireDate: new DateTime(2026, 02, 28), poNumber: "second"));
            fixture.GivenMonthlyDebits(Debit(200m, 2026, 6));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 6, year: 2026);

            Assert.Equal(0m, history.Single(p => p.PONumber == "first").WalletConsumed);
            Assert.Equal(200m, history.Single(p => p.PONumber == "second").WalletConsumed);
        }
    }

    public class WalletServicePoHistoryMonthFilterTests
    {
        private const int ClientScope = 3;

        [Fact]
        public async Task A_month_filter_is_a_cumulative_snapshot_not_a_single_month()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(
                Debit(100m, 2026, 1),
                Debit(200m, 2026, 2),
                Debit(400m, 2026, 3));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 2, year: 2026);

            // January + February, not February alone.
            Assert.Equal(300m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task A_month_filter_excludes_debits_after_the_cutoff()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(
                Debit(100m, 2026, 1),
                Debit(900m, 2026, 12));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 1, year: 2026);

            Assert.Equal(100m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task The_cutoff_month_itself_is_included()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(Debit(250m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: 2, year: 2026);

            Assert.Equal(250m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task Without_a_filter_the_cutoff_is_the_latest_purchase_order_month()
        {
            // A debit dated after the newest PO is outside the all-time cutoff and is ignored.
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            fixture.GivenMonthlyDebits(
                Debit(100m, 2026, 1),
                Debit(500m, 2026, 6));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope);

            Assert.Equal(100m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task A_year_without_a_month_falls_back_to_the_all_time_cutoff()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 3));
            fixture.GivenMonthlyDebits(Debit(100m, 2026, 1));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, ClientScope, month: null, year: 2026);

            Assert.Equal(100m, history[0].WalletConsumed);
        }
    }

    public class WalletServicePoHistoryOwnerScopeTests
    {
        private const int OwnerScope = 1;

        [Fact]
        public async Task An_owner_id_filters_the_ledger_to_that_owners_purchase_orders()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, ownerId: 10, poNumber: "ten"),
                Po(1000m, 2026, 1, ownerId: 20, poNumber: "twenty"));
            fixture.GivenMonthlyDebits(Debit(300m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope, billingOwnerId: 10);

            Assert.Equal("ten", Assert.Single(history).PONumber);
            Assert.Equal(300m, history[0].WalletConsumed);
        }

        [Fact]
        public async Task An_owner_email_is_preferred_over_the_owner_id_for_matching()
        {
            // BillingOwnerEmail is the stable identifier; ids can drift across user entities.
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, ownerId: 99, ownerEmail: "owner@risebird.io", poNumber: "matched"),
                Po(1000m, 2026, 1, ownerId: 10, ownerEmail: "other@risebird.io", poNumber: "unmatched"));
            fixture.GivenMonthlyDebits(Debit(200m, 2026, 1, ownerId: 99));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope, ownerEmail: "owner@risebird.io");

            Assert.Equal("matched", Assert.Single(history).PONumber);
        }

        [Fact]
        public async Task Owner_email_matching_is_case_insensitive()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, ownerEmail: "Owner@Risebird.IO"));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope, ownerEmail: "owner@risebird.io");

            Assert.Single(history);
        }

        [Fact]
        public async Task An_unmatched_owner_email_yields_an_empty_ledger()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 10, ownerEmail: "owner@risebird.io"));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope, ownerEmail: "nobody@risebird.io");

            Assert.Empty(history);
        }

        [Fact]
        public async Task Owner_scope_resolves_the_debit_owner_id_from_the_matched_purchase_orders()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 42, ownerEmail: "owner@risebird.io"));
            var sut = fixture.CreateSut();

            await sut.GetPOHistory(1, OwnerScope, ownerEmail: "owner@risebird.io");

            fixture.Repository.Verify(x => x.GetOwnerMonthlyDebits(1, OwnerScope, 42), Times.Once);
        }

        [Fact]
        public async Task Owner_scope_prefers_an_explicitly_supplied_owner_id_for_the_debit_query()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1, ownerId: 42, ownerEmail: "owner@risebird.io"));
            var sut = fixture.CreateSut();

            await sut.GetPOHistory(1, OwnerScope, billingOwnerId: 7, ownerEmail: "owner@risebird.io");

            fixture.Repository.Verify(x => x.GetOwnerMonthlyDebits(1, OwnerScope, 7), Times.Once);
        }

        [Fact]
        public async Task Owner_scope_overall_runs_an_independent_ledger_per_owner()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(1000m, 2026, 1, ownerId: 10, poNumber: "ten"),
                Po(1000m, 2026, 1, ownerId: 20, poNumber: "twenty"));
            fixture.GivenMonthlyDebits(
                Debit(300m, 2026, 1, ownerId: 10),
                Debit(800m, 2026, 1, ownerId: 20));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope);

            Assert.Equal(300m, history.Single(p => p.PONumber == "ten").WalletConsumed);
            Assert.Equal(800m, history.Single(p => p.PONumber == "twenty").WalletConsumed);
        }

        [Fact]
        public async Task One_owners_debits_never_drain_another_owners_credit()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(100m, 2026, 1, ownerId: 10, poNumber: "ten"),
                Po(5000m, 2026, 1, ownerId: 20, poNumber: "twenty"));
            fixture.GivenMonthlyDebits(Debit(900m, 2026, 1, ownerId: 10));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, OwnerScope);

            // Owner 10 over-consumes its own small PO; owner 20 is untouched.
            Assert.Equal(900m, history.Single(p => p.PONumber == "ten").WalletConsumed);
            Assert.Equal(0m, history.Single(p => p.PONumber == "twenty").WalletConsumed);
        }

        [Fact]
        public async Task Client_scope_pools_every_purchase_order_regardless_of_owner()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(
                Po(100m, 2026, 1, ownerId: 10, poNumber: "ten"),
                Po(100m, 2026, 2, ownerId: 20, poNumber: "twenty"));
            fixture.GivenMonthlyDebits(Debit(150m, 2026, 2));
            var sut = fixture.CreateSut();

            var history = await sut.GetPOHistory(1, billingType: 3);

            Assert.Equal(100m, history.Single(p => p.PONumber == "ten").WalletConsumed);
            Assert.Equal(50m, history.Single(p => p.PONumber == "twenty").WalletConsumed);
        }

        [Fact]
        public async Task A_failure_reading_debits_leaves_consumption_at_zero_rather_than_throwing()
        {
            var fixture = new WalletServiceFixture();
            fixture.GivenPurchaseOrders(Po(1000m, 2026, 1));
            var sut = fixture.CreateSut();
            fixture.Repository.Setup(x => x.GetOwnerMonthlyDebits(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("sql down"));

            var history = await sut.GetPOHistory(1, billingType: 3);

            Assert.Single(history);
            Assert.Equal(0m, history[0].WalletConsumed);
        }
    }

    public class WalletServiceAlertRowMetricsTests
    {
        [Fact]
        public void Metrics_sum_credit_and_consumption_across_the_owners_purchase_orders()
        {
            var row = new POConsumptionAlertRowVM();
            var pos = new[]
            {
                new ClientPOVM { POCreditAmount = 1000m, WalletConsumed = 400m },
                new ClientPOVM { POCreditAmount = 500m, WalletConsumed = 100m }
            };

            WalletService.EnrichAlertRowMetrics(row, pos);

            Assert.Equal(1500m, row.OwnerTotalCredit);
            Assert.Equal(500m, row.OwnerTotalConsumed);
        }

        [Fact]
        public void Opening_balance_defaults_to_zero_for_all_time_reports()
        {
            var row = new POConsumptionAlertRowVM();

            WalletService.EnrichAlertRowMetrics(row, Array.Empty<ClientPOVM>());

            Assert.Equal(0m, row.POOpeningBalance);
        }

        [Fact]
        public void An_explicit_opening_balance_is_carried_onto_the_row()
        {
            var row = new POConsumptionAlertRowVM();

            WalletService.EnrichAlertRowMetrics(row, Array.Empty<ClientPOVM>(), openingBalance: 250m);

            Assert.Equal(250m, row.POOpeningBalance);
        }

        [Fact]
        public void A_negative_opening_balance_is_preserved_because_owners_can_carry_a_deficit()
        {
            var row = new POConsumptionAlertRowVM();

            WalletService.EnrichAlertRowMetrics(row, Array.Empty<ClientPOVM>(), openingBalance: -100m);

            Assert.Equal(-100m, row.POOpeningBalance);
        }

        [Fact]
        public void Only_expired_purchase_orders_count_toward_expired_not_consumed()
        {
            var row = new POConsumptionAlertRowVM();
            var pos = new[]
            {
                new ClientPOVM { POCreditAmount = 1000m, WalletConsumed = 400m, ExpireDate = WalletServiceFixture.Expired },
                new ClientPOVM { POCreditAmount = 1000m, WalletConsumed = 100m, ExpireDate = WalletServiceFixture.FarFuture }
            };

            WalletService.EnrichAlertRowMetrics(row, pos);

            Assert.Equal(600m, row.ExpiredNotConsumed);
        }

        [Fact]
        public void An_over_consumed_expired_purchase_order_contributes_zero_not_a_negative()
        {
            var row = new POConsumptionAlertRowVM();
            var pos = new[]
            {
                new ClientPOVM { POCreditAmount = 100m, WalletConsumed = 400m, ExpireDate = WalletServiceFixture.Expired }
            };

            WalletService.EnrichAlertRowMetrics(row, pos);

            Assert.Equal(0m, row.ExpiredNotConsumed);
        }

        [Fact]
        public void A_null_purchase_order_sequence_is_treated_as_empty()
        {
            var row = new POConsumptionAlertRowVM();

            WalletService.EnrichAlertRowMetrics(row, (IEnumerable<ClientPOVM>)null);

            Assert.Equal(0m, row.OwnerTotalCredit);
            Assert.Equal(0m, row.OwnerTotalConsumed);
            Assert.Equal(0m, row.ExpiredNotConsumed);
        }

        [Fact]
        public void Usable_remaining_is_opening_plus_credit_minus_consumed_minus_expired()
        {
            var row = new POConsumptionAlertRowVM();
            var pos = new[]
            {
                new ClientPOVM { POCreditAmount = 1000m, WalletConsumed = 200m, ExpireDate = WalletServiceFixture.Expired },
                new ClientPOVM { POCreditAmount = 1000m, WalletConsumed = 300m }
            };

            WalletService.EnrichAlertRowMetrics(row, pos, openingBalance: 100m);

            // 100 + 2000 - 500 - 800 = 800
            Assert.Equal(800m, row.UsableRemaining);
        }

        [Fact]
        public void The_client_overload_scopes_to_one_billing_owner()
        {
            var row = new POConsumptionAlertRowVM();
            var client = Client("M2", 1, 1, "Acme",
                new ClientPOVM { BillingOwnerId = 10, POCreditAmount = 1000m, WalletConsumed = 100m },
                new ClientPOVM { BillingOwnerId = 20, POCreditAmount = 5000m, WalletConsumed = 900m });

            WalletService.EnrichAlertRowMetrics(row, client, billingOwnerId: 10);

            Assert.Equal(1000m, row.OwnerTotalCredit);
            Assert.Equal(100m, row.OwnerTotalConsumed);
        }

        [Fact]
        public void The_client_overload_with_no_owner_covers_every_purchase_order()
        {
            var row = new POConsumptionAlertRowVM();
            var client = Client("M2", 3, 1, "Acme",
                new ClientPOVM { BillingOwnerId = 10, POCreditAmount = 1000m, WalletConsumed = 100m },
                new ClientPOVM { BillingOwnerId = 20, POCreditAmount = 5000m, WalletConsumed = 900m });

            WalletService.EnrichAlertRowMetrics(row, client, billingOwnerId: null);

            Assert.Equal(6000m, row.OwnerTotalCredit);
            Assert.Equal(1000m, row.OwnerTotalConsumed);
        }

        [Fact]
        public void The_client_overload_always_uses_a_zero_opening_balance()
        {
            var row = new POConsumptionAlertRowVM { POOpeningBalance = 999m };
            var client = Client("M2", 3, 1, "Acme");

            WalletService.EnrichAlertRowMetrics(row, client, null);

            Assert.Equal(0m, row.POOpeningBalance);
        }

        [Fact]
        public void An_unmatched_owner_yields_zeroed_metrics()
        {
            var row = new POConsumptionAlertRowVM();
            var client = Client("M2", 1, 1, "Acme",
                new ClientPOVM { BillingOwnerId = 10, POCreditAmount = 1000m });

            WalletService.EnrichAlertRowMetrics(row, client, billingOwnerId: 999);

            Assert.Equal(0m, row.OwnerTotalCredit);
        }
    }

    public class WalletServicePoExpiryAlertTests
    {
        private static ClientPOVM AlertablePo(
            decimal credit = 1000m,
            decimal consumed = 100m,
            DateTime? expireDate = null,
            int? ownerId = null,
            string ownerEmail = null,
            int year = 2026,
            int month = 1,
            string poNumber = "PO-1") => new()
            {
                POCreditAmount = credit,
                WalletConsumed = consumed,
                ExpireDate = expireDate ?? WalletServiceFixture.ExpiringSoon,
                BillingOwnerId = ownerId,
                BillingOwnerEmail = ownerEmail,
                BillingOwnerName = ownerEmail == null ? null : "Owner",
                POYear = year,
                POMonth = month,
                PONumber = poNumber
            };

        [Fact]
        public async Task An_expiring_purchase_order_with_a_balance_raises_an_alert()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM> { Client("M2 Prepaid", 3, 1, "Acme", AlertablePo()) };

            var alerts = await sut.GetPOExpiryAlerts(summary);

            var alert = Assert.Single(alerts);
            Assert.Equal(1, alert.ClientId);
            Assert.Equal("Acme", alert.ClientName);
            Assert.Equal("Expiry", alert.AlertType);
            Assert.Equal(900m, alert.WalletRemaining);
        }

        [Fact]
        public async Task An_already_expired_purchase_order_with_a_balance_raises_an_alert()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M3", 3, 1, "Acme", AlertablePo(expireDate: WalletServiceFixture.Expired))
            };

            Assert.Single(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task A_purchase_order_expiring_far_in_the_future_raises_nothing()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Acme", AlertablePo(expireDate: WalletServiceFixture.FarFuture))
            };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task A_purchase_order_with_no_expiry_raises_nothing()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var po = AlertablePo();
            po.ExpireDate = null;
            var summary = new List<ClientPOGroupVM> { Client("M2", 3, 1, "Acme", po) };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task A_fully_consumed_purchase_order_raises_nothing_even_when_expiring()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Acme", AlertablePo(credit: 1000m, consumed: 1000m))
            };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task An_over_consumed_purchase_order_raises_nothing()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Acme", AlertablePo(credit: 1000m, consumed: 1500m))
            };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Theory]
        [InlineData("M2")]
        [InlineData("M2 Prepaid")]
        [InlineData("m2 prepaid")]
        [InlineData("M3")]
        [InlineData("M3 Postpaid")]
        [InlineData("m3")]
        public async Task Only_m2_and_m3_billing_models_are_alerted_on(string billingModelDesc)
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM> { Client(billingModelDesc, 3, 1, "Acme", AlertablePo()) };

            Assert.Single(await sut.GetPOExpiryAlerts(summary));
        }

        [Theory]
        [InlineData("M1")]
        [InlineData("M4")]
        [InlineData("Pay as you go")]
        [InlineData("")]
        public async Task Other_billing_models_are_skipped(string billingModelDesc)
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM> { Client(billingModelDesc, 3, 1, "Acme", AlertablePo()) };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task A_client_with_no_purchase_orders_raises_nothing()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM> { Client("M2", 3, 1, "Acme") };

            Assert.Empty(await sut.GetPOExpiryAlerts(summary));
        }

        [Fact]
        public async Task Only_the_latest_purchase_order_per_client_is_considered()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Acme",
                    AlertablePo(year: 2026, month: 1, poNumber: "old", expireDate: WalletServiceFixture.Expired),
                    AlertablePo(year: 2026, month: 6, poNumber: "latest"))
            };

            var alert = Assert.Single(await sut.GetPOExpiryAlerts(summary));
            Assert.Equal("latest", alert.PONumber);
        }

        [Fact]
        public async Task Owner_scoped_clients_raise_one_alert_per_billing_owner()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 1, 1, "Acme",
                    AlertablePo(ownerId: 10, ownerEmail: "a@x.io", poNumber: "ten"),
                    AlertablePo(ownerId: 20, ownerEmail: "b@x.io", poNumber: "twenty"))
            };

            var alerts = await sut.GetPOExpiryAlerts(summary);

            Assert.Equal(2, alerts.Count);
            Assert.Contains(alerts, a => a.BillingOwnerId == 10);
            Assert.Contains(alerts, a => a.BillingOwnerId == 20);
        }

        [Fact]
        public async Task An_owner_scoped_alert_carries_the_owner_identity()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 1, 1, "Acme", AlertablePo(ownerId: 10, ownerEmail: "a@x.io"))
            };

            var alert = Assert.Single(await sut.GetPOExpiryAlerts(summary));

            Assert.Equal(10, alert.BillingOwnerId);
            Assert.Equal("a@x.io", alert.BillingOwnerEmail);
            Assert.Equal("Owner", alert.BillingOwnerName);
        }

        [Fact]
        public async Task A_client_scoped_alert_carries_no_owner_identity()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM> { Client("M2", 3, 1, "Acme", AlertablePo(ownerId: 10)) };

            var alert = Assert.Single(await sut.GetPOExpiryAlerts(summary));

            Assert.Null(alert.BillingOwnerId);
            Assert.Null(alert.BillingOwnerEmail);
        }

        [Fact]
        public async Task Expired_alerts_sort_before_merely_expiring_ones()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Soon", AlertablePo(expireDate: WalletServiceFixture.ExpiringSoon)),
                Client("M2", 3, 2, "Gone", AlertablePo(expireDate: WalletServiceFixture.Expired))
            };

            var alerts = await sut.GetPOExpiryAlerts(summary);

            Assert.Equal("Gone", alerts[0].ClientName);
            Assert.Equal("Soon", alerts[1].ClientName);
        }

        [Fact]
        public async Task Within_a_group_alerts_sort_by_expiry_date()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Later", AlertablePo(expireDate: DateTime.Today.AddDays(20))),
                Client("M2", 3, 2, "Sooner", AlertablePo(expireDate: DateTime.Today.AddDays(3)))
            };

            var alerts = await sut.GetPOExpiryAlerts(summary);

            Assert.Equal("Sooner", alerts[0].ClientName);
            Assert.Equal("Later", alerts[1].ClientName);
        }

        [Fact]
        public async Task Each_alert_is_enriched_with_the_owner_totals()
        {
            var sut = new WalletServiceFixture().CreateSut();
            var summary = new List<ClientPOGroupVM>
            {
                Client("M2", 3, 1, "Acme",
                    AlertablePo(credit: 1000m, consumed: 100m, year: 2026, month: 6),
                    AlertablePo(credit: 500m, consumed: 50m, year: 2026, month: 1,
                        expireDate: WalletServiceFixture.FarFuture))
            };

            var alert = Assert.Single(await sut.GetPOExpiryAlerts(summary));

            Assert.Equal(1500m, alert.OwnerTotalCredit);
            Assert.Equal(150m, alert.OwnerTotalConsumed);
        }

        [Fact]
        public async Task A_failure_building_the_alerts_is_logged_and_yields_an_empty_list()
        {
            var fixture = new WalletServiceFixture();
            var sut = fixture.CreateSut();
            var summary = new List<ClientPOGroupVM> { null };

            var alerts = await sut.GetPOExpiryAlerts(summary);

            Assert.Empty(alerts);
            fixture.VerifyExceptionLogged(Times.Once());
        }

        [Fact]
        public async Task An_empty_summary_yields_an_empty_list()
        {
            var sut = new WalletServiceFixture().CreateSut();

            Assert.Empty(await sut.GetPOExpiryAlerts(new List<ClientPOGroupVM>()));
        }
    }
}
