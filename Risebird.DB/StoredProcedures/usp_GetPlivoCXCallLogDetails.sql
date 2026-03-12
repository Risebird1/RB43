-- ============================================================
-- usp_GetPlivoCXCallLogDetails
-- ============================================================
-- Purpose : Paginated call-log detail for PlivoCXInterviewCall/Index.
--           Joins PlivoCXCallLogs → IRF → InterviewMeetings (only
--           the meeting that matches the exact slot the AI booked).
--
-- Returns :
--   • TotalCount          – total rows across all pages (window fn)
--   • OverallCompleted    – total "completed" calls         (window fn)
--   • OverallMeetingCreated – calls that produced a meeting  (window fn)
--   • OverallNoAnswer     – failed / no-answer calls         (window fn)
--   • One data row per call log, ordered newest-first.
--
-- Parameters :
--   @Page     – 1-based page number  (default 1)
--   @PageSize – rows per page         (default 50)
--
-- Run to deploy:
--   Execute this script against productProdDB in SSMS.
-- ============================================================

CREATE OR ALTER PROCEDURE [dbo].[usp_GetPlivoCXCallLogDetails]
    @Page     INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        -- ── Pagination / aggregate metadata (window functions) ────────────────
        COUNT(*)                                                                    OVER () AS TotalCount,
        SUM(CASE WHEN pcl.CallStatus = 'completed'                           THEN 1 ELSE 0 END) OVER () AS OverallCompleted,
        SUM(CASE WHEN im.id IS NOT NULL                                       THEN 1 ELSE 0 END) OVER () AS OverallMeetingCreated,
        SUM(CASE WHEN pcl.CallStatus IN ('no-answer','failed','trigger-failed') THEN 1 ELSE 0 END) OVER () AS OverallNoAnswer,

        -- ── Call identity ─────────────────────────────────────────────────────
        pcl.Id                          AS CallLogId,
        pcl.RecipientEmail              AS CandidateEmail,
        pcl.RecipientName               AS CandidateName,
        pcl.RecipientPhone,
        irf.IRFId,
        irf.JDTiltle,
        pcl.ClientName,
        pcl.TriggeredBy,
        pcl.PlivoCXConversationId,
        pcl.CallPurpose,
        pcl.CandidateDisposition,

        -- ── Call outcome ──────────────────────────────────────────────────────
        pcl.CallStatus,
        pcl.DurationSeconds,
        pcl.HangupCause,
        pcl.CreatedAt                   AS CallInitiatedAt,
        pcl.CallEndedAt,

        -- AIReportedBooked is derived from whether the meeting JOIN found a
        -- matching row, not from the stored pcl.InterviewBooked flag which
        -- may lag or be inaccurate.
        CASE WHEN im.id IS NOT NULL THEN CAST(1 AS BIT)
             ELSE CAST(0 AS BIT) END    AS AIReportedBooked,
        pcl.BookedInterviewDate,
        pcl.BookedInterviewTime,
        pcl.BookedInterviewDateTime,
        pcl.BookingApiSuccess,
        pcl.BookingApiResponse,
        pcl.CallTranscript,
        pcl.ConversationSummary,

        -- ── Did the call produce an InterviewMeeting? ─────────────────────────
        CASE WHEN im.id IS NOT NULL THEN 'YES' ELSE 'NO' END AS MeetingCreated,
        im.id                           AS MeetingId,
        im.interviewDate                AS MeetingScheduledAt,
        im.interviewSlot,
        im.interviewDuration,
        im.confirmationStatus           AS MeetingConfirmationStatus,
        im.interviewer,
        im.risebirdHR,
        im.ClientHrContact,
        im.isAutoCancelled,
        im.CancellationReason,
        im.IsCandidateResponsed,
        im.CandidateResponse,
        im.dtCreatedOn                  AS MeetingCreatedOn

    FROM      PlivoCXCallLogs   pcl
    JOIN      IRF               irf ON irf.id = pcl.IRFId
    LEFT JOIN InterviewMeetings  im
           ON im.candidateEmail    = pcl.RecipientEmail
          AND im.IRFId             = irf.IRFId
          -- Only match a meeting whose date+time equals the slot the AI booked.
          -- SMALLDATETIME truncates to the minute so sub-second differences are
          -- ignored. The IS NOT NULL guard prevents matching when no booking
          -- was attempted.
          AND pcl.BookedInterviewDateTime IS NOT NULL
          AND CAST(im.interviewDate          AS SMALLDATETIME)
            = CAST(pcl.BookedInterviewDateTime AS SMALLDATETIME)

    ORDER BY pcl.CreatedAt DESC
    OFFSET  (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
