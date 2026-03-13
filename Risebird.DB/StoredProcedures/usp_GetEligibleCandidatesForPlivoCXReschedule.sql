-- =============================================
-- Author:      Plivo CX Integration
-- Create date: 2026-03-10
-- Description: Gets candidates eligible for automated Plivo CX RESCHEDULING calls.
--
-- A candidate is eligible for a reschedule call when ALL of the following are true:
--
--   1. Valid phone number and email
--   2. Candidate is NOT cold (Candidate.Status != 9, IsColdCandidate != 1)
--   3. Candidate disposition is NOT 'uninterested'
--   4. IRF is still active (EndDate >= today, not paused, not deleted)
--   5. Has at least one interview meeting that requires rescheduling:
--        MeetingStatus IN (1, 3, 5, 9)  OR  confirmationStatus = '2' (Cancelled)
--        OR RedoInterview = 1
--   6a. No future non-cancelled interview already exists in InterviewMeetings
--       (confirmationStatus <> '2' covers pending AND confirmed).
--   6b. No prior Plivo reschedule call already successfully posted a future slot
--       (BookingApiSuccess = 1 AND BookedInterviewDateTime > now).
--   7. Fewer than 2 completed reschedule calls (CallPurpose = 'reschedule') in
--      PlivoCXCallLogs, unless disposition is NULL (never conclusively reached)
--   8. Hard daily cap: no more than 4 reschedule calls (any status) made today
--      for this candidate + IRF.
--   9. Minimum 2-hour cooldown between consecutive calls.
--      The most recent reschedule call must have been made at least 2 hours ago.
--  10. 4-day retry window from the very first reschedule call attempt.
--      If the earliest reschedule call to this candidate+IRF is older than 4 days, stop.
--
-- The SP also returns OriginalInterviewDate (most recent meeting needing reschedule)
-- so the AI agent can reference it in conversation.
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEligibleCandidatesForPlivoCXReschedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEligibleCandidatesForPlivoCXReschedule
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEligibleCandidatesForPlivoCXReschedule]
    @Count INT = 10
AS
BEGIN
    SET NOCOUNT ON;

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

        -- Most recent failed/cancelled interview date — passed to the AI agent as context
        (
            SELECT TOP 1
                CONVERT(NVARCHAR(50), im2.interviewDate, 106)   -- e.g. "07 Mar 2026"
            FROM   InterviewMeetings  im2
            JOIN   InterviewComments  ic2 ON ic2.InterviewId = im2.id
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

        -- Live count of completed reschedule calls for this candidate + IRF
        (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallStatus     = 'completed'
              AND  pcl.CallPurpose    = 'reschedule'
        )                                                            AS CompletedCallCount

    FROM       Candidate        c
    INNER JOIN IRF              irf ON irf.id  = c.IRFId
    INNER JOIN Client           cl  ON cl.id   = irf.ClientId
    INNER JOIN CandidatesMaster cm  ON cm.id   = c.CandidateMasterId
    INNER JOIN UserList         u   ON u.id    = cm.UserId

    WHERE
        -- 1. Valid contact info
        u.EmailId IS NOT NULL
        AND c.Phone IS NOT NULL
        AND LEN(LTRIM(RTRIM(c.Phone))) > 0

        -- 2. Not cold (either column marker)
        AND (c.Status IS NULL OR c.Status <> 9)
        AND NOT EXISTS (
            SELECT 1
            FROM   ColdCandidateInfos cci
            WHERE  cci.FKCandidateId      = c.id
              AND  ISNULL(cci.IsColdCandidate, 0) = 1
        )

        -- 3. Disposition is not 'uninterested'
        AND (c.PlivoCXDisposition IS NULL OR c.PlivoCXDisposition <> 'uninterested')

        -- 4. IRF is still active
        AND irf.EndDate              >= GETDATE()
        AND ISNULL(irf.IsPaused, 0)  = 0
        AND ISNULL(irf.IsDeleted, 0) = 0

        -- 5. Has at least one interview meeting that requires rescheduling
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

        -- 6a. No future non-cancelled interview meeting already exists.
        --     confirmationStatus = '2' = Cancelled; pending and confirmed both block.
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            WHERE  im.candidateEmail     = u.EmailId
              AND  im.IRFId             = irf.IRFId
              AND  im.confirmationStatus <> '2'
              AND  im.interviewDate      > GETDATE()
        )

        -- 6b. No prior Plivo reschedule call already successfully posted a future
        --     booking to the Schedule API.  Covers the lag before the meeting appears.
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoCXCallLogs pcl2
            WHERE  pcl2.RecipientEmail         = u.EmailId
              AND  pcl2.IRFId                  = c.IRFId
              AND  pcl2.CallPurpose             = 'reschedule'
              AND  pcl2.BookingApiSuccess        = 1
              AND  pcl2.BookedInterviewDateTime  > GETDATE()
        )

        -- 7. Stop after 2 completed reschedule calls (unless disposition is NULL,
        --    meaning we have never had a conclusive conversation yet)
        AND (
            c.PlivoCXDisposition IS NULL
            OR (
                SELECT COUNT(*)
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  pcl.CallStatus     = 'completed'
                  AND  pcl.CallPurpose    = 'reschedule'
            ) < 2
        )

        -- 8. Hard daily cap: at most 4 reschedule calls per candidate + IRF today
        --    (any call status counts — prevents looping on busy/no-answer)
        AND (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallPurpose    = 'reschedule'
              AND  CAST(pcl.CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
        ) < 4

        -- 9. Minimum 2-hour cooldown between consecutive calls.
        --    Block if ANY reschedule call to this candidate+IRF was made in the last 2 hours.
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallPurpose    = 'reschedule'
              AND  pcl.CreatedAt      > DATEADD(HOUR, -2, GETUTCDATE())
        )

        -- 10. Stop retrying after 4 days from the very first reschedule call attempt.
        --     Passes if: no reschedule call has ever been made (fresh candidate), OR the
        --     earliest call is within the last 4 days (still inside the retry window).
        AND (
            NOT EXISTS (
                SELECT 1
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  pcl.CallPurpose    = 'reschedule'
            )
            OR (
                SELECT MIN(pcl.CreatedAt)
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  pcl.CallPurpose    = 'reschedule'
            ) >= DATEADD(DAY, -4, GETUTCDATE())
        )

    ORDER BY
        -- Never-reached candidates first
        CASE WHEN c.PlivoCXDisposition IS NULL THEN 0 ELSE 1 END ASC,

        -- Within each group, fewer completed reschedule calls go first
        (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallStatus     = 'completed'
              AND  pcl.CallPurpose    = 'reschedule'
        ) ASC,

        -- Most recently added candidates take priority within the same bucket
        c.id DESC;
END
GO
