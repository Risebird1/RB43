-- =============================================
-- Author:      Plivo CX Integration
-- Create date: 2026-03-06
-- Description: Gets eligible candidates for automated Plivo CX interview scheduling calls.
--
-- Eligibility rules (ALL must be true):
--   1. Valid phone number and email
--   2. Candidate is NOT marked cold (Candidate.Status != 9)
--   3. Candidate disposition is NOT 'uninterested'
--      (NULL / 'interested' / 'cant_say' are all eligible)
--   4a. No future non-cancelled interview already exists in InterviewMeetings
--       (confirmationStatus <> '2' covers pending AND confirmed — blocks re-calls
--        even before the scheduler marks the meeting as fully confirmed)
--   4b. No prior Plivo schedule call already successfully posted a future slot
--       to the Schedule API (BookingApiSuccess = 1 AND BookedInterviewDateTime > now)
--       Covers the gap between the API call and the meeting appearing in InterviewMeetings.
--   5. Completed call count in PlivoCXCallLogs is < 2, UNLESS disposition is NULL
--      (disposition NULL = never been reached; always worth attempting)
--   6. Hard daily cap: no more than 4 schedule calls (any status) made today
--      for this candidate + IRF.  Prevents unbounded retries on busy/no-answer days.
--   7. Minimum 2-hour cooldown between consecutive calls.
--      The most recent schedule call must have been made at least 2 hours ago.
--   8. 4-day retry window from the very first call attempt.
--      If the earliest schedule call to this candidate+IRF is older than 4 days, stop.
--
-- Attempt tracking:
--   VapiCallAttempts on Candidate is still incremented per attempt (via
--   usp_IncrementCandidateVapiCallAttempts) but is NOT used to gate eligibility here.
--   Completed-call count is derived live from PlivoCXCallLogs.CallStatus = 'completed'.
--
-- Schema notes:
--   Candidate.IRFId (INT)       → references IRF.id (INT primary key)
--   IRF.IRFId (VARCHAR)         = business identifier used in InterviewMeetings
--   Candidate.CandidateMasterId → CandidatesMaster.id → UserList.id → EmailId
--   PlivoCXCallLogs joins on (RecipientEmail = UserList.EmailId AND IRFId = Candidate.IRFId)
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEligibleCandidatesForPlivoCXCalls'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEligibleCandidatesForPlivoCXCalls
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEligibleCandidatesForPlivoCXCalls]
    @Count INT = 10
AS
BEGIN
    SET NOCOUNT ON;

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

        -- Live count of completed Plivo CX calls for this candidate + IRF
        (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallStatus     = 'completed'
        )                                                           AS CompletedCallCount

    FROM       Candidate        c
    INNER JOIN IRF              irf ON irf.id  = c.IRFId
    INNER JOIN Client           cl  ON cl.id   = irf.ClientId
    INNER JOIN CandidatesMaster cm  ON cm.id   = c.CandidateMasterId
    INNER JOIN UserList         u   ON u.id    = cm.UserId

    WHERE
        -- 1. Must have valid email and phone
        u.EmailId IS NOT NULL
        AND c.Phone IS NOT NULL
        AND LEN(LTRIM(RTRIM(c.Phone))) > 0

        -- 2. Not cold (Status = 9 means cold)
        AND (c.Status IS NULL OR c.Status <> 9)

        -- 3. Disposition is not 'uninterested'
        AND (c.PlivoCXDisposition IS NULL OR c.PlivoCXDisposition <> 'uninterested')

        -- 4a. No future non-cancelled interview meeting already exists.
        --     confirmationStatus = '2' = Cancelled; everything else (pending, confirmed)
        --     is enough to block another call.
        AND NOT EXISTS (
            SELECT 1
            FROM   InterviewMeetings im
            WHERE  im.candidateEmail     = u.EmailId
              AND  im.IRFId              = irf.IRFId
              AND  im.confirmationStatus <> '2'
              AND  im.interviewDate       > GETDATE()
        )

        -- 4b. No prior Plivo call already successfully posted a future booking to
        --     the Schedule API.  Covers the lag before the meeting row appears.
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoCXCallLogs pcl2
            WHERE  pcl2.RecipientEmail         = u.EmailId
              AND  pcl2.IRFId                  = c.IRFId
              AND  ISNULL(pcl2.CallPurpose, 'schedule') = 'schedule'
              AND  pcl2.BookingApiSuccess       = 1
              AND  pcl2.BookedInterviewDateTime > GETDATE()
        )

        -- 5. Stop after 2 completed calls (unless disposition is NULL, meaning
        --    we have never had a conclusive conversation with this candidate yet)
        AND (
            c.PlivoCXDisposition IS NULL
            OR (
                SELECT COUNT(*)
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  pcl.CallStatus     = 'completed'
            ) < 2
        )

        -- 6. Hard daily cap: at most 4 schedule calls per candidate + IRF today
        --    (any call status counts — prevents looping on busy/no-answer)
        AND (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  ISNULL(pcl.CallPurpose, 'schedule') = 'schedule'
              AND  CAST(pcl.CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE)
        ) < 4

        -- 7. Minimum 2-hour cooldown between consecutive calls.
        --    Block if ANY schedule call to this candidate+IRF was made in the last 2 hours.
        AND NOT EXISTS (
            SELECT 1
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  ISNULL(pcl.CallPurpose, 'schedule') = 'schedule'
              AND  pcl.CreatedAt      > DATEADD(HOUR, -2, GETUTCDATE())
        )

        -- 8. Stop retrying after 4 days from the very first call attempt.
        --    Passes if: no call has ever been made (fresh candidate), OR the earliest
        --    call is within the last 4 days (still inside the retry window).
        AND (
            NOT EXISTS (
                SELECT 1
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  ISNULL(pcl.CallPurpose, 'schedule') = 'schedule'
            )
            OR (
                SELECT MIN(pcl.CreatedAt)
                FROM   PlivoCXCallLogs pcl
                WHERE  pcl.RecipientEmail = u.EmailId
                  AND  pcl.IRFId          = c.IRFId
                  AND  ISNULL(pcl.CallPurpose, 'schedule') = 'schedule'
            ) >= DATEADD(DAY, -4, GETUTCDATE())
        )

    ORDER BY
        -- Never-reached candidates first (NULL disposition = no conclusive call yet)
        CASE WHEN c.PlivoCXDisposition IS NULL THEN 0 ELSE 1 END ASC,

        -- Within each group, fewer completed calls go first
        (
            SELECT COUNT(*)
            FROM   PlivoCXCallLogs pcl
            WHERE  pcl.RecipientEmail = u.EmailId
              AND  pcl.IRFId          = c.IRFId
              AND  pcl.CallStatus     = 'completed'
        ) ASC,

        -- Most recently added candidates take priority within the same bucket
        c.id DESC;
END
GO
