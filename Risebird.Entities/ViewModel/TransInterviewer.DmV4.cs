using System;

namespace Risebird.Entities.ViewModel

{

    /// <summary>DM Approval v4 extensions (see TransInterviewer.cs for InterviewCapacity).</summary>

    public partial class TransInterviewer

    {
        /// <summary>Null when IsSMEVerified was NULL in DB (distinct from explicit false).</summary>
        public bool? IsSMEVerifiedStored { get; set; }

        /// <summary>UserList.LastLogin — platform sign-in.</summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>Most recent completed interview (MeetingStatus 4,6,7,8), all channels.</summary>
        public DateTime? LastInterviewConductedAt { get; set; }
    }

}

