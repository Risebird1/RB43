-- ============================================================
-- usp_GetPlivoWhatsAppLogDetails
-- ============================================================
-- Purpose : Paginated, filterable WhatsApp message log for
--           PlivoWhatsAppInterview/Index.
--
-- Parameters:
--   @Page        – 1-based page number  (default 1)
--   @PageSize    – rows per page         (default 50)
--   @Status      – filter by log Status  (NULL = all)
--   @Purpose     – filter by MessagePurpose (NULL = all)
--   @Meeting     – filter by MeetingCreated: 'YES' | 'NO' (NULL = all)
--   @Disposition – filter by CandidateDisposition (NULL = all)
-- ============================================================

CREATE OR ALTER PROCEDURE [dbo].[usp_GetPlivoWhatsAppLogDetails]
    @Page        INT           = 1,
    @PageSize    INT           = 50,
    @Status      NVARCHAR(50)  = NULL,
    @Purpose     NVARCHAR(20)  = NULL,
    @Meeting     NVARCHAR(3)   = NULL,
    @Disposition NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    WITH base AS
    (
        SELECT
            wl.Id                           AS LogId,
            wl.RecipientEmail               AS CandidateEmail,
            wl.RecipientName                AS CandidateName,
            wl.RecipientPhone,
            irf.IRFId,
            irf.JDTiltle,
            wl.ClientName,
            wl.TriggeredBy,
            wl.MessagePurpose,
            wl.CandidateDisposition,
            wl.PlivoCXConversationId,
            wl.MessageStatus,
            wl.ConversationTranscript,
            wl.ConversationSummary,
            wl.AIReportedInterviewDate,
            wl.AIReportedInterviewTime,
            wl.AIReportedBooked,
            wl.ConversationEndedAt,
            wl.Status,
            wl.InterviewBooked,
            wl.BookingApiSuccess,
            wl.BookingApiResponse,
            wl.BookedInterviewDate,
            wl.BookedInterviewTime,
            wl.BookedInterviewDateTime,
            wl.CreatedAt                    AS MessageSentAt,
            wl.UpdatedAt,

            -- Did the message produce an InterviewMeeting?
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

        FROM      PlivoWhatsAppLogs wl
        JOIN      IRF               irf ON irf.id = wl.IRFId
        LEFT JOIN InterviewMeetings  im
               ON im.candidateEmail    = wl.RecipientEmail
              AND im.IRFId             = irf.IRFId
              AND im.RequestType       = CASE ISNULL(wl.MessagePurpose, 'schedule')
                                             WHEN 'reschedule' THEN 'reschedule_voiceAgent'
                                             ELSE                   'schedule_voiceAgent'
                                        END
              AND wl.BookedInterviewDateTime IS NOT NULL
              AND CAST(im.interviewDate AS SMALLDATETIME)
                = CAST(wl.BookedInterviewDateTime AS SMALLDATETIME)

        WHERE
            (@Status      IS NULL OR wl.Status             = @Status)
            AND (@Purpose     IS NULL OR wl.MessagePurpose     = @Purpose)
            AND (@Disposition IS NULL OR wl.CandidateDisposition = @Disposition)
    )
    SELECT
        COUNT(*)                                                                              OVER () AS TotalCount,
        SUM(CASE WHEN Status = 'completed'                                             THEN 1 ELSE 0 END) OVER () AS OverallReplied,
        SUM(CASE WHEN MeetingCreated = 'YES'                                           THEN 1 ELSE 0 END) OVER () AS OverallMeetingCreated,
        SUM(CASE WHEN Status = 'trigger-failed'                                        THEN 1 ELSE 0 END) OVER () AS OverallFailed,

        *

    FROM   base
    WHERE  (@Meeting IS NULL OR MeetingCreated = @Meeting)

    ORDER BY MessageSentAt DESC
    OFFSET  (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
