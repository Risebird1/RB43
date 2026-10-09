using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Decides whether a blob left behind by the AI screening intake can be deleted.
    ///
    /// The intake writes two kinds of blob and keeps neither tidy. A draft expires out of the
    /// distributed cache after AIScreening:DraftLifetimeHours, but its uploads stay in storage for
    /// ever, so every abandoned intake leaks a job description and a voice recording.
    ///
    /// The whole of this file exists because one of those two containers is shared. Instruction
    /// audio is written to "audios" under a prefix nothing else uses, and is never referenced once
    /// intake finishes - it is unambiguously disposable. Job descriptions go to "resumes", which
    /// also holds real candidate resumes uploaded under their own file names by AccountController,
    /// and a job description that became a channel is referenced for ever by IRF.JDLink.
    ///
    /// So a job description is only ever deleted when BOTH hold: its name matches the narrow shape
    /// the intake generates, and nothing in IRF points at it. Neither test is sufficient alone -
    /// the name test does not protect a referenced JD, and the reference test does not protect a
    /// candidate resume, because a resume was never in IRF.JDLink to begin with.
    /// </summary>
    public static class AIScreeningBlobRetention
    {
        /// <summary>
        /// Job description blobs are named "{draftId}_{originalFileName}", and a draft id is
        /// "d_" followed by five hex characters (see AIScreeningIntakeService.CreateDraftAsync).
        ///
        /// Anchored and exact-width on purpose. A loose prefix like "d_" would match a candidate
        /// resume called "d_final_v2.pdf" sitting in the same container.
        /// </summary>
        private static readonly Regex JdBlobNamePattern =
            new Regex(@"^d_[0-9a-fA-F]{5}_.+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Instruction recordings: "ai_screening_instruction_{draftId}{extension}".</summary>
        public const string InstructionAudioPrefix = "ai_screening_instruction_";

        /// <summary>Default days a blob survives after its draft expired. Conservative on purpose.</summary>
        public const int DefaultRetentionDays = 7;

        public static bool IsScreeningJdBlob(string blobName) =>
            !string.IsNullOrWhiteSpace(blobName) && JdBlobNamePattern.IsMatch(blobName.Trim());

        public static bool IsScreeningInstructionAudioBlob(string blobName) =>
            !string.IsNullOrWhiteSpace(blobName)
            && blobName.Trim().StartsWith(InstructionAudioPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The blob's own name, from a stored url. Compared by name rather than by whole url
        /// because IRF.JDLink is written without a SAS token but is handed around with one, and
        /// the host differs between environments; the name is the part that identifies the blob.
        /// Returns null when the url yields nothing usable, which the caller reads as "cannot
        /// prove this is unreferenced" rather than as "no reference".
        /// </summary>
        public static string BlobNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var value = url.Trim();

            var query = value.IndexOf('?');
            if (query >= 0) value = value.Substring(0, query);

            var fragment = value.IndexOf('#');
            if (fragment >= 0) value = value.Substring(0, fragment);

            // A url ending in '/' names a container, not a blob. Trimming the slash first would
            // make the container's own name look like a blob name.
            if (value.Length == 0 || value.EndsWith("/", StringComparison.Ordinal)) return null;

            var slash = value.LastIndexOf('/');
            var name = slash >= 0 ? value.Substring(slash + 1) : value;
            if (string.IsNullOrWhiteSpace(name)) return null;

            try { name = Uri.UnescapeDataString(name); }
            catch (UriFormatException) { /* keep the escaped form rather than lose the name */ }

            return name.Trim();
        }

        /// <summary>
        /// Builds the set of blob names that something still points at. Names are compared
        /// case-insensitively; Azure blob names are case-sensitive, so this errs towards keeping a
        /// blob, which is the direction to err in.
        /// </summary>
        public static HashSet<string> ToReferencedBlobNames(IEnumerable<string> referencedUrls)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (referencedUrls == null) return names;

            foreach (var url in referencedUrls)
            {
                var name = BlobNameFromUrl(url);
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }

            return names;
        }

        /// <summary>
        /// Whether one blob may be deleted.
        /// </summary>
        /// <param name="lastModifiedUtc">
        /// Null means the age is unknown, which is never treated as old enough.
        /// </param>
        /// <param name="referencedBlobNames">
        /// From <see cref="ToReferencedBlobNames"/>. Null is treated as "the reference list could
        /// not be read", and nothing is deleted - a purge that cannot see what is in use must do
        /// nothing rather than guess.
        /// </param>
        public static bool IsPurgeable(
            string blobName,
            DateTimeOffset? lastModifiedUtc,
            DateTimeOffset nowUtc,
            int retentionDays,
            HashSet<string> referencedBlobNames)
        {
            if (referencedBlobNames == null) return false;
            if (string.IsNullOrWhiteSpace(blobName)) return false;

            var name = blobName.Trim();

            var isJd = IsScreeningJdBlob(name);
            var isAudio = IsScreeningInstructionAudioBlob(name);
            if (!isJd && !isAudio) return false;

            if (!lastModifiedUtc.HasValue) return false;

            // A negative or zero retention would delete blobs the moment they are written, including
            // ones belonging to an intake still in progress. Fall back to the default rather than
            // honour it.
            var days = retentionDays > 0 ? retentionDays : DefaultRetentionDays;
            if (lastModifiedUtc.Value > nowUtc.AddDays(-days)) return false;

            // Applies to both kinds. Instruction audio should never appear here, but if some future
            // caller does store the url, the blob stops being disposable and this notices.
            return !referencedBlobNames.Contains(name);
        }
    }
}
