using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Generates stable candidate coded IDs (same initials + numeric suffix pattern as interviewer NickNames).
    /// </summary>
    public static class CandidatePseudonymHelper
    {
        public static string BuildNickName(string firstName, string lastName, int seedId, ISet<string> takenNickNames)
        {
            takenNickNames = takenNickNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var firstPart = TakeInitials(firstName, 2, "Ri");
            var lastPart = TakeInitials(lastName, 2, "se");
            var suffix = (Math.Abs(seedId) % 9000 + 1000).ToString(CultureInfo.InvariantCulture);
            var nick = firstPart + lastPart + suffix;

            var attempt = 0;
            while (takenNickNames.Contains(nick) && attempt < 10)
            {
                attempt++;
                suffix = ((Math.Abs(seedId) + attempt) % 9000 + 1000).ToString(CultureInfo.InvariantCulture);
                nick = firstPart + lastPart + suffix;
            }

            if (takenNickNames.Contains(nick))
            {
                nick = firstPart + lastPart + Guid.NewGuid().ToString("N").Substring(0, 4);
            }

            return nick;
        }

        public static string BuildNickNameFromEmail(string candidateEmail, int seedId, ISet<string> takenNickNames)
        {
            if (string.IsNullOrWhiteSpace(candidateEmail))
            {
                return BuildNickName(null, null, seedId, takenNickNames);
            }

            var localPart = candidateEmail.Split('@')[0];
            var tokens = localPart
                .Replace('.', ' ')
                .Replace('_', ' ')
                .Replace('-', ' ')
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var first = tokens.Length > 0 ? tokens[0] : localPart;
            var last = tokens.Length > 1 ? tokens[1] : string.Empty;
            return BuildNickName(first, last, seedId, takenNickNames);
        }

        private static string TakeInitials(string value, int count, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var cleaned = new string(value.Trim().Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                return fallback;
            }

            if (cleaned.Length >= count)
            {
                return Capitalize(cleaned.Substring(0, count));
            }

            return Capitalize(cleaned.PadRight(count, 'x'));
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
        }
    }
}
