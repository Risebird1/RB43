using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class PQIDetails
    {
        public int id { get; set; }
        public int UserId { get; set; }
        public string IRFId { get; set; }
        public string interviewer { get; set; }
        public string Client { get; set; }
        public int TotalInterview { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelected { get; set; }
        public int selectionrate { get; set; }
        public int TotalRejected { get; set; }
        public int rejectionrate { get; set; }
        public string ChannelOwner { get; set; }
        public string CompanyName { get; set; }
        public string Clienthr { get; set; }
        public string JDTiltle { get; set; }
        public int NegativeRating { get; set; }

        public int TotalRedoCount { get; set; }
        public int TotallessComment { get; set; }
        public int TotallessQuestions { get; set; }
        public int TotalLatejoin { get; set; }
        public string NickName { get; set; }
        public string ShortListstatus { get; set; }
        public string ProjectUniqueid { get; set; }
        public int RedMarker { get; set; }
        public string AIVerdict { get; set; }
        public string ResumeLink { get; set; }
        public int TotalFuture { get; set; }
        public int TotalInterviewers { get; set; }
        public int BillingRate { get; set; }
        public bool IsActive { get; set; }
        public int TotalLessduration { get; set; }
        public string TranscriptVerdit { get; set; }
        public bool IsEmployed { get; set; }
        public string AcknowledgementStatus { get; set; }
        public int VideoConfirmation { get; set; }
        public int RedMarkerStatus { get; set; }
        public string RedMarkerReviewComment { get; set; }

        // ---- PLI QC decision on this interviewer and channel (usp_GetQcPageStatus Type PLI; QC Tracking Delta 5) ----
        /// <summary>1 Under review, 2 Reviewed, 3 Removed; null = no decision.</summary>
        public int? PliStatus { get; set; }
        public string PliStatusBy { get; set; }
        public DateTime? PliStatusOn { get; set; }
        public string PliStatusComment { get; set; }
    }

    /// <summary>Latest QC decision of one interviewer (and channel; IRFId 0 = all channels) on a QC page.</summary>
    public class QcPageStatusVM
    {
        public int UserId { get; set; }
        public int IRFId { get; set; }
        public int Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string Comment { get; set; }

        /// <summary>
        /// Puts the PLI decision on each row: the later of the channel's own decision (UserId, IRF.id) and an
        /// interviewer-wide one (IRFId 0, a Remove). Rows without a decision keep PliStatus null.
        /// </summary>
        public static void ApplyPli(IEnumerable<PQIDetails> rows, IEnumerable<QcPageStatusVM> statuses)
        {
            if (rows == null || statuses == null) return;
            var byKey = statuses
                .Where(s => s != null)
                .GroupBy(s => (s.UserId, s.IRFId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedOn ?? DateTime.MinValue).First());
            foreach (var row in rows)
            {
                if (row == null || row.UserId <= 0) continue;
                byKey.TryGetValue((row.UserId, row.id), out var own);
                byKey.TryGetValue((row.UserId, 0), out var all);
                var pick = own == null ? all
                    : all == null ? own
                    : ((all.CreatedOn ?? DateTime.MinValue) > (own.CreatedOn ?? DateTime.MinValue) ? all : own);
                if (pick == null) continue;
                row.PliStatus = pick.Status;
                row.PliStatusBy = pick.CreatedBy;
                row.PliStatusOn = pick.CreatedOn;
                row.PliStatusComment = pick.Comment;
            }
        }
    }
    public class ToalInterviewerInterviewDetails
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public int UserId { get; set; }
        public string interviewer { get; set; }
        public int TotalInterview { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelectMeetings { get; set; }
        public int TotalRejectMeetings { get; set; }
        public string CompanyName { get; set; }
        public bool IsEmployed { get; set; }
        public bool Isavailable { get; set; }
        public string NickName { get; set; }
        public int RedMarker { get; set; }
        public string AIVerdict { get; set; }
        public string ResumeLink { get; set; }
        public int TotalFuture { get; set; }
        public int TranscriptVerdit { get; set; }
        public int SkillChannelFutureAssignedInterview { get; set; }
        public int TotalDeficitMeetings { get; set; }
        public string VideoKYC { get; set; }
        public int RedMarkerStatus { get; set; }
        public string RedMarkerReviewComment { get; set; }
        public bool Isactive { get; set; }
        public string InactivationType { get; set; }
        public string InactivatedBy { get; set; }
        public DateTime? InactivatedAt { get; set; }
    }
    public class RedMarkerDetailsVM
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string Comment { get; set; }
    }
}
