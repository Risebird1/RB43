using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Service for parsing resumes inline (used by manual review flow).
    /// Returns parsed candidate data and updates CandidateResume status.
    /// </summary>
    public interface IResumeParsingService
    {
        /// <summary>
        /// Parses a single resume by CandidateResume ID.
        /// Updates IsResumeParsed, IsResumeParsingFailed, etc. columns.
        /// Returns parsed candidate info (name, email, phone) if successful.
        /// </summary>
        Task<ResumeParseResult> ParseResumeAsync(int candidateResumeId);
    }

    public class ResumeParseResult
    {
        public bool Success { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public int? CandidateId { get; set; }
        public int? CandidateMasterId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
