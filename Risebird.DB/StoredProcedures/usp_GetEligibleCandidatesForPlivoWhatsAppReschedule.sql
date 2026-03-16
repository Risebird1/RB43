-- =============================================
-- Author:      Plivo WhatsApp Integration
-- Create date: 2026-03-14
-- Description: Gets candidates eligible for Plivo WhatsApp RESCHEDULING messages.
--
-- Eligibility mirrors the voice reschedule SP but tracks PlivoWhatsAppLogs.
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEligibleCandidatesForPlivoWhatsAppReschedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEligibleCandidatesForPlivoWhatsAppReschedule
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEligibleCandidatesForPlivoWhatsAppReschedule]
    @Count  INT      = 10,
    @NowIST DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NowIST = ISNULL(@NowIST, GETDATE());

    SELECT TOP (@Count)
        c.id                                                         AS CandidateId,
        c.IRFId,
        irf.IRFId                                                    AS IRFName,
        u.FirstName                                                  AS CandidateName,
        u.EmailId                                                    AS CandidateEmail,
        c.Phone                                                      AS CandidatePhone,
        irf.JDTiltle                                                 AS JobTitle,
        cl.Name                                                      AS ClientName,
        c.VapiCallAttempts                                           AS CallAttempts,
        c.PlivoCXDisposition,

        (
            SELECT TOP 1
                CONVERT(NVARCHAR(50), im2.interviewDate, 106)
            FROM   InterviewMeetings im2
            JOIN   InterviewComments ic2 ON ic2.InterviewId = im2.id
            WHERE  im2.candidateEmail       = u.EmailId
              AND  im2.IRFId               = irf.IRFId
              AND  ISNULL(im2.IsDeleted, 0) = 0
              AND  (
                       ic2.MeetingStatus IN (1, 3, 5, 9)
                    OR im2.confirmationStatus = '2'
                    OR im2.RedoInterview      = 1
                   )
            ORDER BY im2.interviewDate DESC
        )                                                            AS OriginalInterviewDate,

        (
            SELECT COUNT(*)
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail  = u.EmailId
              AND  wl.IRFId           = c.IRFId
              AND  wl.Status          IN ('replied', 'booked')
              AND  wl.MessagePurpose  = 'reschedule'
        )                                                            AS CompletedCallCount

    FROM       Candidate        c
    INNER JOIN IRF              irf ON irf.id  = c.IRFId
    INNER JOIN Client           cl  ON cl.id   = irf.ClientId
    INNER JOIN CandidatesMaster cm  ON cm.id   = c.CandidateMasterId
    INNER JOIN UserList         u   ON u.id    = cm.UserId

    WHERE
        u.EmailId IS NOT NULL
        AND c.Phone IS NOT NULL
        AND LEN(LTRIM(RTRIM(c.Phone))) > 0

        AND (c.Status IS NULL OR c.Status <> 9)
        AND NOT EXISTS (
            SELECT 1 FROM ColdCandidateInfos cci
            WHERE cci.FKCandidateId = c.id AND ISNULL(cci.IsColdCandidate, 0) = 1
        )

        AND (c.PlivoCXDisposition IS NULL OR c.PlivoCXDisposition <> 'uninterested')

        AND irf.EndDate              >= @NowIST
        AND ISNULL(irf.IsPaused, 0)  = 0
        AND ISNULL(irf.IsDeleted, 0) = 0

        -- Has at least one meeting requiring reschedule
        AND EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            JOIN   InterviewComments ic ON ic.InterviewId = im.id
            WHERE  im.candidateEmail       = u.EmailId
              AND  im.IRFId               = irf.IRFId
              AND  ISNULL(im.IsDeleted, 0) = 0
              AND  (
                       ic.MeetingStatus IN (1, 3, 5, 9)
                    OR im.confirmationStatus = '2'
                    OR im.RedoInterview      = 1
                   )
        )

        -- 6a. No future non-cancelled interview
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            WHERE  im.candidateEmail     = u.EmailId
              AND  im.IRFId             = irf.IRFId
              AND  im.confirmationStatus <> '2'
              AND  im.interviewDate      > @NowIST
        )

        -- 6b. No prior WhatsApp reschedule message that already booked a future slot
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail          = u.EmailId
              AND  wl.IRFId                   = c.IRFId
              AND  wl.MessagePurpose          = 'reschedule'
              AND  wl.BookingApiSuccess        = 1
              AND  wl.BookedInterviewDateTime  > GETUTCDATE()
        )

        -- 7. < 2 conclusive (replied/booked) reschedule messages
        AND (
            c.PlivoCXDisposition IS NULL
            OR (
                SELECT COUNT(*)
                FROM   PlivoWhatsAppLogs wl
                WHERE  wl.RecipientEmail = u.EmailId
                  AND  wl.IRFId          = c.IRFId
                  AND  wl.Status         IN ('replied', 'booked')
                  AND  wl.MessagePurpose = 'reschedule'
            ) < 2
        )

        -- 8. Daily cap
        AND (
            SELECT COUNT(*)
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail = u.EmailId
              AND  wl.IRFId          = c.IRFId
              AND  wl.MessagePurpose = 'reschedule'
              AND  CAST(wl.CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
        ) < 4

        -- 9. 2-hour cooldown
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail = u.EmailId
              AND  wl.IRFId          = c.IRFId
              AND  wl.MessagePurpose = 'reschedule'
              AND  wl.CreatedAt      > DATEADD(HOUR, -2, GETUTCDATE())
        )

        -- 10. 4-day retry window
        AND (
            NOT EXISTS (
                SELECT 1 FROM PlivoWhatsAppLogs wl
                WHERE wl.RecipientEmail = u.EmailId AND wl.IRFId = c.IRFId AND wl.MessagePurpose = 'reschedule'
            )
            OR (
                SELECT MIN(wl.CreatedAt) FROM PlivoWhatsAppLogs wl
                WHERE wl.RecipientEmail = u.EmailId AND wl.IRFId = c.IRFId AND wl.MessagePurpose = 'reschedule'
            ) >= DATEADD(DAY, -4, GETUTCDATE())
        )

    ORDER BY
        CASE WHEN c.PlivoCXDisposition IS NULL THEN 0 ELSE 1 END ASC,
        (
            SELECT COUNT(*) FROM PlivoWhatsAppLogs wl
            WHERE wl.RecipientEmail = u.EmailId AND wl.IRFId = c.IRFId
              AND wl.Status IN ('replied', 'booked') AND wl.MessagePurpose = 'reschedule'
        ) ASC,
        c.id DESC;
END
GO
