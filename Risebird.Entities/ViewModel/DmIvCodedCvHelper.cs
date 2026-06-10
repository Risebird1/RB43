using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// DM Approval coded-CV link semantics: OpenAI ResumeSummary JSON blobs vs legacy Affinda/redacted URLs.
    /// </summary>
    public static class DmIvCodedCvHelper
    {
        /// <summary>
        /// True when the link points at an OpenAI-generated ResumeSummary JSON blob we store in blob storage.
        /// </summary>
        public static bool IsOpenAiResumeSummaryLink(string codedResumeLink)
        {
            if (string.IsNullOrWhiteSpace(codedResumeLink)) return false;
            var path = codedResumeLink.Split('?')[0].Trim().ToLowerInvariant();
            return path.IndexOf("resumesummary_", StringComparison.Ordinal) >= 0
                || path.EndsWith(".json", StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the link only when it is a displayable OpenAI coded CV; legacy Affinda/XML/PDF links are ignored.
        /// </summary>
        public static string GetDisplayCodedCvLink(string codedResumeLink)
        {
            if (!IsOpenAiResumeSummaryLink(codedResumeLink)) return null;
            return codedResumeLink.Trim();
        }

        public static bool HasDisplayCodedCv(string codedResumeLink) =>
            !string.IsNullOrWhiteSpace(GetDisplayCodedCvLink(codedResumeLink));
    }
}
