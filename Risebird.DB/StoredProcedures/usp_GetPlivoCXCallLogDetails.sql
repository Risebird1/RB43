-- ============================================================
-- usp_GetPlivoCXCallLogDetails
-- ============================================================
-- Purpose : Paginated, filterable call-log detail for
--           PlivoCXInterviewCall/Index.
--
-- Returns :
--   • TotalCount            – total rows matching current filters (window fn)
--   • OverallCompleted      – filtered "completed" calls          (window fn)
--   • OverallMeetingCreated – filtered calls that produced a meeting (window fn)
--   • OverallNoAnswer       – filtered failed/no-answer calls      (window fn)
--   • One data row per call log, ordered newest-first.
--
-- Parameters :
--   @Page        – 1-based page number  (default 1)
--   @PageSize    – rows per page         (default 50)
--   @Status      – filter by CallStatus  (NULL = all)
--   @Purpose     – filter by CallPurpose (NULL = all)
--   @Meeting     – filter by MeetingCreated: 'YES' | 'NO' (NULL = all)
--   @Disposition – filter by CandidateDisposition (NULL = all)
--   @DateFrom    – filter calls on or after this date (NULL = no lower bound)
--   @DateTo      – filter calls on or before this date (NULL = no upper bound)
--
-- Run to deploy:
--   Execute this script against productProdDB in SSMS.
-- ============================================================

CREATE OR ALTER PROCEDURE [dbo].[usp_GetPlivoCXCallLogDetails]
    @Page        INT           = 1,
    @PageSize    INT           = 50,
    @Status      NVARCHAR(50)  = NULL,
    @Purpose     NVARCHAR(20)  = NULL,
    @Meeting     NVARCHAR(3)   = NULL,
    @Disposition NVARCHAR(50)  = NULL,
    @DateFrom    DATE          = NULL,
    @DateTo      DATE          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Build the full result set first (with derived MeetingCreated column)
    -- so that the MeetingCreated filter can be applied cleanly in WHERE.
    WITH base AS
    (
        SELECT
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

            -- ── What the AI extracted ─────────────────────────────────────────────
            CASE WHEN im.id IS NOT NULL
                 THEN CAST(1 AS BIT)
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
            im.dtCreatedOn                  AS MeetingCreatedOn,
            im.RequestType                  AS MeetingRequestType

        FROM      PlivoCXCallLogs   pcl
        JOIN      IRF               irf ON irf.id = pcl.IRFId
        LEFT JOIN InterviewMeetings  im
               ON im.candidateEmail    = pcl.RecipientEmail
              AND im.IRFId             = irf.IRFId
              -- Only match a meeting that was created by the voice agent for the same purpose.
              -- RequestType is NULL for all non-voice-agent meetings, so this eliminates
              -- spurious matches that happened to share the same booked date/time.
              AND im.RequestType       = CASE ISNULL(pcl.CallPurpose, 'schedule')
                                             WHEN 'reschedule' THEN 'reschedule_voiceAgent'
                                             ELSE                   'schedule_voiceAgent'
                                        END
              -- Datetime guard: pcl must have attempted a booking, and the meeting slot
              -- must match the exact minute the AI booked.
              AND pcl.BookedInterviewDateTime IS NOT NULL
              AND CAST(im.interviewDate          AS SMALLDATETIME)
                = CAST(pcl.BookedInterviewDateTime AS SMALLDATETIME)

        WHERE
            (@Status      IS NULL OR pcl.CallStatus           = @Status)
            AND (@Purpose     IS NULL OR pcl.CallPurpose          = @Purpose)
            AND (@Disposition IS NULL OR pcl.CandidateDisposition  = @Disposition)
            AND (@DateFrom    IS NULL OR CAST(pcl.CreatedAt AS DATE) >= @DateFrom)
            AND (@DateTo      IS NULL OR CAST(pcl.CreatedAt AS DATE) <= @DateTo)
    )
    SELECT
        -- ── Pagination / aggregate metadata (window functions over filtered set) ──
        COUNT(*)                                                                        OVER () AS TotalCount,
        SUM(CASE WHEN CallStatus = 'completed'                                   THEN 1 ELSE 0 END) OVER () AS OverallCompleted,
        SUM(CASE WHEN MeetingCreated = 'YES'                                     THEN 1 ELSE 0 END) OVER () AS OverallMeetingCreated,
        SUM(CASE WHEN CallStatus IN ('no-answer','failed','trigger-failed')       THEN 1 ELSE 0 END) OVER () AS OverallNoAnswer,

        -- ── All base columns ──────────────────────────────────────────────────────
        *

    FROM   base
    WHERE  (@Meeting IS NULL OR MeetingCreated = @Meeting)

    ORDER BY CallInitiatedAt DESC
    OFFSET  (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
