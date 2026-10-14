using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Turns a candidate email as it arrives from resume parsing, Excel or an API into the one address it contains,
    /// or null when no address can be trusted.
    ///
    /// Resume text glues the address to its neighbours, and these were being saved and mailed as-is (SendGrid bounce
    /// list, Oct 2026):
    ///   "|amansambhariya9@gmail.com", "7775812827|%20ritesh@gmail.com", "email%20id%20-%20joshi@gmail.com",
    ///   "%c2%a0swapnil@gmail.com", "anushathotaoﬃcial@gmail.com" (PDF ligature), "Antara Deb &lt;antara@x.com&gt;",
    ///   "kapoor.raunak91@gmail.comprofessional", "ykumar945@gmail.comlinkedin", "saurabhpantse@gmail.commob".
    ///
    /// Junk is only cut where it is clearly junk. When the address itself is damaged ("bobbykboseo?ce@gmail.com", a
    /// ligature the PDF lost; "amitrajupōg@gmail.com") the result is null rather than a guess, because a guess is
    /// someone else's mailbox.
    /// </summary>
    public static class CandidateEmailNormalizer
    {
        public const int MaxLength = 150;

        // Longest first, so "commob" is read as "com" + "mob" and never as "co" + "mmob".
        private static readonly string[] Tlds = { "com", "org", "net", "edu", "gov", "biz", "info", "in", "co", "io", "ai", "us", "uk" };

        // Words that follow the address in resume headers ("Email: x@gmail.com Professional Summary", "| LinkedIn", "Mob:").
        private static readonly HashSet<string> GluedWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "professional", "profile", "summary", "objective", "experience", "education", "skills", "career",
            "linkedin", "linked", "github", "portfolio", "website", "web", "www", "http", "https", "skype",
            "mob", "mobile", "mobileno", "phone", "phoneno", "ph", "cell", "tel", "contact", "contactno", "number", "no",
            "email", "mail", "address", "location", "dob", "nationality", "personal", "about"
        };

        // Free-mail domains: anything glued straight after them is junk, whatever the word.
        private static readonly string[] FreeMailDomains =
        {
            "gmail.com", "googlemail.com", "yahoo.co.in", "yahoo.com", "yahoo.in", "ymail.com", "rediffmail.com",
            "outlook.com", "hotmail.com", "icloud.com"
        };

        // Characters that separate fields in a resume header ("Mob: 98.. | x@gmail.com • LinkedIn").
        private const string Separators = "|:;,/\\=`#()[]{}<>\"*~•·‣◦▪●■►➢–—";

        // A mobile number glued in front: "+91-9711126040garima@", "918349667240sahus@", "+918310076701.sahoo@".
        // Digits and dashes (ten or more digits, checked in code), then the real local part starting with a letter.
        private static readonly Regex GluedPhone = new Regex(@"^\+?[\d\-]+[._]?(?=[a-z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        // A label left in front of the address once the phone number is cut ("+9195..emailsharma.shivam@").
        private static readonly Regex GluedLabel = new Regex(@"^(?:e-?mail(?:id)?|mail(?:id)?)[._\-]?(?=[a-z0-9]{3})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Characters an address is built from; a run of them around '@' is a candidate address.
        private static readonly Regex AddressRun = new Regex(@"[a-z0-9._+\-]+@[a-z0-9.\-]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex LocalPart = new Regex(@"^[a-z0-9_+\-]+(\.[a-z0-9_+\-]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex DomainLabel = new Regex(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex TopLevelLabel = new Regex(@"^[a-z]{2,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>The single clean, lower-case address in <paramref name="raw"/>, or null.</summary>
        public static string Normalize(string raw)
        {
            var found = FindAll(raw);
            // Two different addresses in one field: we cannot know which one is the candidate's.
            return found.Count == 1 ? found[0] : null;
        }

        /// <summary>
        /// The first clean address in free text such as a whole resume (which may also list referees), or null.
        /// </summary>
        public static string ExtractFirst(string text) => FindAll(text).FirstOrDefault();

        /// <summary>True when <paramref name="email"/> is already a clean address (what Normalize would store).</summary>
        public static bool IsValid(string email) => email != null && Normalize(email) == email;

        private static List<string> FindAll(string raw)
        {
            var found = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return found;
            }

            var text = Decode(raw);
            // NFKC turns ligatures and full-width characters into plain letters ("ﬃ" -> "ffi").
            text = text.Normalize(NormalizationForm.FormKC);
            // Zero-width spaces, joiners, BOM, soft hyphen (Unicode "format" characters) vanish; NBSP and friends become spaces.
            text = new string(text
                .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Format)
                .Select(c => char.IsWhiteSpace(c) ? ' ' : c)
                .ToArray());
            text = text.ToLowerInvariant();

            foreach (Match m in AddressRun.Matches(text))
            {
                var address = Clean(text, m);
                if (address != null && !found.Contains(address))
                {
                    found.Add(address);
                }
            }
            return found;
        }

        private static string Decode(string value)
        {
            // Twice, for values that were encoded twice ("%2520").
            for (var i = 0; i < 2 && value.Contains('%'); i++)
            {
                try
                {
                    value = Uri.UnescapeDataString(value);
                }
                catch (UriFormatException)
                {
                    break;
                }
            }
            return WebUtility.HtmlDecode(value);
        }

        private static string Clean(string text, Match m)
        {
            if (!StartsCleanly(text, m.Index))
            {
                return null;
            }

            var at = m.Value.IndexOf('@');
            var local = m.Value.Substring(0, at);
            var phone = GluedPhone.Match(local);
            // Only when it is clearly a phone number (+, dashes, or a country code); "9876543210rahul@" may be real.
            if (phone.Success && phone.Value.Count(char.IsDigit) >= 10
                && (phone.Value.StartsWith("+") || phone.Value.Contains('-') || phone.Value.Count(char.IsDigit) >= 12))
            {
                local = GluedLabel.Replace(local.Substring(phone.Length), string.Empty);
            }
            local = local.TrimStart('.', '-', '_', '+');
            var domain = m.Value.Substring(at + 1).Trim('.', '-');
            if (local.Length == 0 || !LocalPart.IsMatch(local))
            {
                return null;
            }

            // "x@gmailcom", "x@outlookcom": the dot got lost. "x@gmail": so did the TLD, but gmail has only one domain
            // (unlike outlook / hotmail / yahoo, which also have country domains, so those stay refused).
            if (domain == "gmail")
            {
                domain = "gmail.com";
            }
            else if (!domain.Contains('.') && domain.EndsWith("com") && FreeMailDomains.Contains(domain.Substring(0, domain.Length - 3) + ".com"))
            {
                domain = domain.Substring(0, domain.Length - 3) + ".com";
            }
            domain = CutGluedWord(domain);
            var labels = domain.Split('.');
            if (labels.Length < 2 || labels.Any(l => !DomainLabel.IsMatch(l)) || !TopLevelLabel.IsMatch(labels[labels.Length - 1]))
            {
                return null;
            }

            var address = local + "@" + domain;
            return address.Length <= MaxLength ? address : null;
        }

        /// <summary>
        /// The address must start at a field boundary: a separator ("|", ":", space, "•" ...), or junk that itself starts
        /// at one ("'x@", "?x@" at the start of the value). A letter or digit before the junk ("pa!thra@", "v?rakhi@",
        /// "amitrajupōg@") means the address was cut out of a damaged word, and what is left is someone else's mailbox.
        /// </summary>
        private static bool StartsCleanly(string text, int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c) || Separators.IndexOf(c) >= 0)
                {
                    return true;
                }
                if (char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static string CutGluedWord(string domain)
        {
            foreach (var freeMail in FreeMailDomains)
            {
                if (domain.Length > freeMail.Length && domain.StartsWith(freeMail, StringComparison.Ordinal)
                    && domain.Substring(freeMail.Length).All(c => c >= 'a' && c <= 'z'))
                {
                    return freeMail;
                }
            }

            var lastDot = domain.LastIndexOf('.');
            if (lastDot < 0)
            {
                return domain;
            }
            var last = domain.Substring(lastDot + 1);
            if (Tlds.Contains(last))
            {
                return domain;
            }
            foreach (var tld in Tlds.OrderByDescending(t => t.Length))
            {
                if (last.StartsWith(tld, StringComparison.Ordinal) && GluedWords.Contains(last.Substring(tld.Length)))
                {
                    return domain.Substring(0, lastDot + 1) + tld;
                }
            }
            return domain;
        }
    }
}
