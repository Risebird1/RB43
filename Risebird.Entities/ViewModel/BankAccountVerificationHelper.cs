using System;

namespace Risebird.Entities.ViewModel
{
    public static class BankAccountVerificationHelper
    {
        public static bool CoreBankDetailsChanged(TransInterviewerAccDetails previous, TransInterviewerAccDetails current)
        {
            if (previous == null || current == null)
                return true;

            return !string.Equals(NormalizeAccountNumber(previous.AccountNumber), NormalizeAccountNumber(current.AccountNumber), StringComparison.Ordinal)
                || !string.Equals(NormalizeIfsc(previous.IFSCCode), NormalizeIfsc(current.IFSCCode), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(NormalizeName(previous.AccountName), NormalizeName(current.AccountName), StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeAccountNumber(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        public static string NormalizeIfsc(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

        public static string NormalizeName(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
