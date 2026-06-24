namespace Risebird.Entities.ViewModel
{
    /// <summary>Admin-only resolution of a candidate coded ID back to real identity.</summary>
    public class CandidatePseudonymResolveVM
    {
        public string NickName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public int? CandidateMasterId { get; set; }
    }
}
