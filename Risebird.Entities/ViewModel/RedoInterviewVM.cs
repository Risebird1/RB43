
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class RedoInterviewVM
    {
        public int id { get; set; }
        public int interviewid { get; set; }
        [Display(Name = "Candidate Face")]
        public string CandidateFace { get; set; }
        [Display(Name = "Interview Quality")]
        public string Inappropriate { get; set; }
        [Display(Name = "Required Interview Duration")]
        public int RequiredDuration { get; set; }
        [Display(Name = "Actual Interview Duration")]
        public int ActualDuration { get; set; }
        [Display(Name = "Hands On Coding")]
        public string HandsOfCoding { get; set; }
        [Display(Name = "Redo Interview Reason")]
        public string Comments { get; set; }
        [Display(Name = "Rejection Comment")]
        public string RejectionComment { get; set; }
        public string ModifiedBy { get; set; }
        public string type { get; set; }
        public string uniqueid { get; set; }
        public int IRFId { get; set; }
        /// <summary>Tab to return to after the save (Potential Risk by Candidate); optional.</summary>
        public string ReturnTab { get; set; }
        /// <summary>QC Activity page for usp_AddRedoInterviewDetails @SourcePage (PotentialRisk | HighRisk); null otherwise.</summary>
        public string SourcePage { get; set; }

        /// <summary>Maps the redo form's type to its QC Activity page: PotentialRiskRCandidate -&gt; PotentialRisk, HighRisk -&gt; HighRisk.</summary>
        public static string QcSourcePageFor(string type)
        {
            if (string.Equals(type, "PotentialRiskRCandidate", System.StringComparison.OrdinalIgnoreCase)) return "PotentialRisk";
            if (string.Equals(type, "HighRisk", System.StringComparison.OrdinalIgnoreCase)) return "HighRisk";
            return null;
        }
    }
}
