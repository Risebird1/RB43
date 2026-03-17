-- =============================================
-- Author:      Plivo WhatsApp Integration
-- Create date: 2026-03-14
-- Description: Gets eligible candidates for automated Plivo WhatsApp interview scheduling messages.
--
-- Eligibility rules (ALL must be true):
--   1. Valid phone number and email
--   2. Candidate is NOT marked cold (Candidate.Status != 9)
--   3. Candidate disposition is NOT 'uninterested'
--   4a. No future non-cancelled interview already exists in InterviewMeetings
--   4b. No prior WhatsApp schedule message already booked a future slot
--   4c. No confirmed interview in the last 15 days with MeetingStatus NULL or not a
--       reschedule-needed status (1,3,5,9) — prevents re-scheduling duplicates
--   4d. Candidate does NOT have any interview meeting requiring rescheduling
--       (MeetingStatus IN (1,3,5,9), confirmationStatus='2', or RedoInterview=1).
--       Mirrors rule in usp_GetEligibleCandidatesForPlivoWhatsAppReschedule so the
--       two SPs are mutually exclusive — a reschedule candidate never gets a schedule message.
--   5. Replied-message count < 2 (unless disposition is NULL = never reached)
--   6. Hard daily cap: no more than 4 schedule WhatsApp messages today per candidate+IRF
--   7. Minimum 2-hour cooldown between consecutive messages
--   8. 4-day retry window from the very first message attempt
--
-- Note: tracks PlivoWhatsAppLogs independently of PlivoCXCallLogs (voice channel).
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEligibleCandidatesForPlivoWhatsApp'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEligibleCandidatesForPlivoWhatsApp
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEligibleCandidatesForPlivoWhatsApp]
    @Count  INT      = 10,
    @NowIST DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NowIST = ISNULL(@NowIST, GETDATE());

    SELECT TOP (@Count)
        c.id                                                        AS CandidateId,
        c.IRFId,
        irf.IRFId                                                   AS IRFName,
        u.FirstName                                                 AS CandidateName,
        u.EmailId                                                   AS CandidateEmail,
        c.Phone                                                     AS CandidatePhone,
        irf.JDTiltle                                                AS JobTitle,
        cl.Name                                                     AS ClientName,
        c.VapiCallAttempts                                          AS CallAttempts,
        c.PlivoCXDisposition,

        (
            SELECT COUNT(*)
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail  = u.EmailId
              AND  wl.IRFId           = c.IRFId
              AND  wl.Status          IN ('replied', 'booked')
              AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
        )                                                           AS CompletedCallCount

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

        AND (c.PlivoCXDisposition IS NULL OR c.PlivoCXDisposition <> 'uninterested')

        -- 4a. No future non-cancelled interview
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            WHERE  im.candidateEmail     = u.EmailId
              AND  im.IRFId              = irf.IRFId
              AND  im.confirmationStatus <> '2'
              AND  im.interviewDate       > @NowIST
        )

        -- 4b. No prior WhatsApp message that already booked a future slot
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail          = u.EmailId
              AND  wl.IRFId                   = c.IRFId
              AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
              AND  wl.BookingApiSuccess        = 1
              AND  wl.BookedInterviewDateTime  > GETUTCDATE()
        )

        -- 4c. No confirmed interview in last 15 days with valid (non-reschedule-needed) status
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            JOIN   InterviewComments ic ON ic.InterviewId = im.id
            WHERE  im.candidateEmail     = u.EmailId
              AND  im.IRFId              = irf.IRFId
              AND  im.confirmationStatus = '1'
              AND  im.interviewDate      >= DATEADD(DAY, -15, @NowIST)
              AND  (
                       ic.MeetingStatus IS NULL
                    OR ic.MeetingStatus NOT IN (1, 3, 5, 9)
                   )
        )

        -- 4d. Exclude candidates who already need RESCHEDULING.
        --     If a candidate has any interview meeting flagged as reschedule-needed
        --     (No Show, Candidate Absent, Tech Issue, Redo, or Cancelled), they belong
        --     to the reschedule flow — not the initial schedule flow.
        --     Mirrors the EXISTS rule in usp_GetEligibleCandidatesForPlivoWhatsAppReschedule
        --     so the two SPs are mutually exclusive.
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            JOIN   InterviewComments ic ON ic.InterviewId = im.id
            WHERE  im.candidateEmail       = u.EmailId
              AND  im.IRFId               = irf.IRFId
              AND  ISNULL(im.IsDeleted, 0) = 0
              AND  (
                       ic.MeetingStatus IN (1, 3, 5, 9)   -- No Show / Candidate Absent / Tech Issue / Redo
                    OR im.confirmationStatus = '2'          -- Cancelled
                    OR im.RedoInterview      = 1            -- Explicit redo flag
                   )
        )

        -- 5. < 2 conclusive (replied/booked) messages unless never reached
        AND (
            c.PlivoCXDisposition IS NULL
            OR (
                SELECT COUNT(*)
                FROM   PlivoWhatsAppLogs wl
                WHERE  wl.RecipientEmail = u.EmailId
                  AND  wl.IRFId          = c.IRFId
                  AND  wl.Status         IN ('replied', 'booked')
                  AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
            ) < 2
        )

        -- 6. Daily cap: at most 4 schedule messages today
        AND (
            SELECT COUNT(*)
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail = u.EmailId
              AND  wl.IRFId          = c.IRFId
              AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
              AND  CAST(wl.CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
        ) < 4

        -- 7. 2-hour cooldown
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail = u.EmailId
              AND  wl.IRFId          = c.IRFId
              AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
              AND  wl.CreatedAt      > DATEADD(HOUR, -2, GETUTCDATE())
        )

        -- 8. 4-day retry window
        AND (
            NOT EXISTS (
                SELECT 1
                FROM   PlivoWhatsAppLogs wl
                WHERE  wl.RecipientEmail = u.EmailId
                  AND  wl.IRFId          = c.IRFId
                  AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
            )
            OR (
                SELECT MIN(wl.CreatedAt)
                FROM   PlivoWhatsAppLogs wl
                WHERE  wl.RecipientEmail = u.EmailId
                  AND  wl.IRFId          = c.IRFId
                  AND  ISNULL(wl.MessagePurpose, 'schedule') = 'schedule'
            ) >= DATEADD(DAY, -4, GETUTCDATE())
        )

    ORDER BY
        CASE WHEN c.PlivoCXDisposition IS NULL THEN 0 ELSE 1 END ASC,
        (
            SELECT COUNT(*)
            FROM   PlivoWhatsAppLogs wl
            WHERE  wl.RecipientEmail = u.EmailId
              AND  wl.IRFId          = c.IRFId
              AND  wl.Status         IN ('replied', 'booked')
        ) ASC,
        c.id DESC;
END
GO
