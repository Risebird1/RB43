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

        /// <summary>Completed interviews on this skill channel (MeetingStatus 4,6,7,8).</summary>
        public int TotalCompletedMeetings { get; set; }

        /// <summary>Selected outcomes on this skill channel.</summary>
        public int TotalSelected { get; set; }

        /// <summary>Rejected outcomes on this skill channel.</summary>
        public int TotalRejected { get; set; }
        /// <summary>Most recent activation on this skill channel (null = never activated).</summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>UserList.MobileNumber — ops-only on DM Approval.</summary>
        public string MobileNumber { get; set; }

        /// <summary>When the row was added to this skill channel.</summary>
        public DateTime? AddedAt { get; set; }
    }

}
