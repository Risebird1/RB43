namespace Risebird.Entities.ViewModel

{

    /// <summary>DM Approval v4 extensions (see TransInterviewer.cs for InterviewCapacity).</summary>

    public partial class TransInterviewer

    {
        /// <summary>Null when IsSMEVerified was NULL in DB (distinct from explicit false).</summary>
        public bool? IsSMEVerifiedStored { get; set; }
    }

}

