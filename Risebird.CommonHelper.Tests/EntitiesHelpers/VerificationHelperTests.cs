using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    public class PanVerificationHelperTests
    {
        private static TransInterviewerAccDetails Acc(string pan) => new() { PAN = pan };

        [Theory]
        [InlineData("abcde1234f", "ABCDE1234F")]
        [InlineData("  ABCDE1234F  ", "ABCDE1234F")]
        [InlineData("AbCdE1234f", "ABCDE1234F")]
        public void Normalizes_to_trimmed_upper_case(string raw, string expected) =>
            Assert.Equal(expected, PanVerificationHelper.NormalizePan(raw));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Normalizes_blank_input_to_an_empty_string(string raw) =>
            Assert.Equal(string.Empty, PanVerificationHelper.NormalizePan(raw));

        [Fact]
        public void Normalization_is_idempotent()
        {
            var once = PanVerificationHelper.NormalizePan("  abcde1234f ");
            Assert.Equal(once, PanVerificationHelper.NormalizePan(once));
        }

        [Fact]
        public void A_null_side_is_treated_as_changed_so_verification_re_runs()
        {
            Assert.True(PanVerificationHelper.CorePanChanged(null, Acc("ABCDE1234F")));
            Assert.True(PanVerificationHelper.CorePanChanged(Acc("ABCDE1234F"), null));
            Assert.True(PanVerificationHelper.CorePanChanged(null, null));
        }

        [Theory]
        [InlineData("ABCDE1234F", "ABCDE1234F")]
        [InlineData("ABCDE1234F", "abcde1234f")]
        [InlineData("ABCDE1234F", "  ABCDE1234F  ")]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData("", "   ")]
        public void Equivalent_pans_are_not_a_change(string previous, string current) =>
            Assert.False(PanVerificationHelper.CorePanChanged(Acc(previous), Acc(current)));

        [Theory]
        [InlineData("ABCDE1234F", "ABCDE1234G")]
        [InlineData(null, "ABCDE1234F")]
        [InlineData("ABCDE1234F", null)]
        [InlineData("", "ABCDE1234F")]
        public void A_different_pan_is_a_change(string previous, string current) =>
            Assert.True(PanVerificationHelper.CorePanChanged(Acc(previous), Acc(current)));

        [Fact]
        public void Other_fields_do_not_affect_the_pan_comparison()
        {
            var previous = new TransInterviewerAccDetails { PAN = "ABCDE1234F", AccountNumber = "1", IFSCCode = "A" };
            var current = new TransInterviewerAccDetails { PAN = "ABCDE1234F", AccountNumber = "2", IFSCCode = "B" };

            Assert.False(PanVerificationHelper.CorePanChanged(previous, current));
        }
    }

    public class BankAccountVerificationHelperTests
    {
        private static TransInterviewerAccDetails Acc(
            string accountNumber = "123456789", string ifsc = "HDFC0001234", string name = "Ada Lovelace") =>
            new() { AccountNumber = accountNumber, IFSCCode = ifsc, AccountName = name };

        [Theory]
        [InlineData("  123456789  ", "123456789")]
        [InlineData("123456789", "123456789")]
        public void Account_number_normalization_only_trims(string raw, string expected) =>
            Assert.Equal(expected, BankAccountVerificationHelper.NormalizeAccountNumber(raw));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_account_number_normalizes_to_empty(string raw) =>
            Assert.Equal(string.Empty, BankAccountVerificationHelper.NormalizeAccountNumber(raw));

        [Theory]
        [InlineData("hdfc0001234", "HDFC0001234")]
        [InlineData("  hdfc0001234 ", "HDFC0001234")]
        public void Ifsc_normalization_trims_and_upper_cases(string raw, string expected) =>
            Assert.Equal(expected, BankAccountVerificationHelper.NormalizeIfsc(raw));

        [Theory]
        [InlineData("  Ada Lovelace  ", "Ada Lovelace")]
        [InlineData("ada lovelace", "ada lovelace")]
        public void Name_normalization_only_trims(string raw, string expected) =>
            Assert.Equal(expected, BankAccountVerificationHelper.NormalizeName(raw));

        [Fact]
        public void A_null_side_is_treated_as_changed_so_verification_re_runs()
        {
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(null, Acc()));
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(Acc(), null));
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(null, null));
        }

        [Fact]
        public void Identical_details_are_not_a_change() =>
            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(Acc(), Acc()));

        [Fact]
        public void Whitespace_only_differences_are_not_a_change() =>
            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc("  123456789 ", " hdfc0001234 ", "  Ada Lovelace "),
                Acc("123456789", "HDFC0001234", "Ada Lovelace")));

        [Fact]
        public void Ifsc_case_differences_are_not_a_change() =>
            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(ifsc: "hdfc0001234"), Acc(ifsc: "HDFC0001234")));

        [Fact]
        public void Account_name_case_differences_are_not_a_change() =>
            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(name: "ADA LOVELACE"), Acc(name: "ada lovelace")));

        [Fact]
        public void Account_number_case_is_compared_ordinally()
        {
            // Account numbers are digits in practice, but the comparison is Ordinal (not
            // OrdinalIgnoreCase) so any alphanumeric difference counts as a change.
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(accountNumber: "ab123"), Acc(accountNumber: "AB123")));
        }

        [Fact]
        public void A_changed_account_number_is_a_change() =>
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(accountNumber: "111"), Acc(accountNumber: "222")));

        [Fact]
        public void A_changed_ifsc_is_a_change() =>
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(ifsc: "HDFC0001234"), Acc(ifsc: "ICIC0005678")));

        [Fact]
        public void A_changed_account_name_is_a_change() =>
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(name: "Ada Lovelace"), Acc(name: "Grace Hopper")));

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "   ")]
        [InlineData(null, "   ")]
        public void Blank_variants_of_the_same_field_are_equivalent(string previous, string current) =>
            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(accountNumber: previous, ifsc: previous, name: previous),
                Acc(accountNumber: current, ifsc: current, name: current)));

        [Fact]
        public void Going_from_blank_to_populated_is_a_change() =>
            Assert.True(BankAccountVerificationHelper.CoreBankDetailsChanged(
                Acc(accountNumber: null), Acc(accountNumber: "123")));

        [Fact]
        public void Non_core_fields_such_as_upi_and_verification_state_do_not_count()
        {
            var previous = new TransInterviewerAccDetails
            {
                AccountNumber = "1", IFSCCode = "A", AccountName = "N",
                UPIId = "old@upi", IsBankVerified = true, NameAtBank = "Old"
            };
            var current = new TransInterviewerAccDetails
            {
                AccountNumber = "1", IFSCCode = "A", AccountName = "N",
                UPIId = "new@upi", IsBankVerified = false, NameAtBank = "New"
            };

            Assert.False(BankAccountVerificationHelper.CoreBankDetailsChanged(previous, current));
        }
    }
}
