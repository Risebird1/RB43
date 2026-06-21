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
--   4c. No confirmed interview (confirmationStatus = '1') within the last 15 days
--       where MeetingStatus is NULL (not yet evaluated), or is not a reschedule-needed
--       status (reschedule-needed: 1, 3, 5, 9). Prevents re-scheduling a candidate who
--       recently had or is currently scheduled for a valid interview.
--   4d. Candidate does NOT have any interview meeting requiring rescheduling
--       (MeetingStatus IN (1,3,5,9), confirmationStatus='2', or RedoInterview=1).
--       Mirrors rule 5 of usp_GetEligibleCandidatesForPlivoCXReschedule so the two
--       SPs are mutually exclusive — a reschedule candidate never gets a schedule call.
--   5. Completed call count in PlivoCXCallLogs is < 2, UNLESS disposition is NULL
--      (disposition NULL = never been reached; always worth attempting)
--   6. Hard daily cap: no more than 4 schedule calls (any status) made today
--      for this candidate + IRF.  Prevents unbounded retries on busy/no-answer days.
--   7. Minimum 2-hour cooldown between consecutive calls.
--      The most recent schedule call must have been made at least 2 hours ago.
--   8. 4-day retry window from the very first call attempt.
--      If the earliest schedule call to this candidate+IRF is older than 4 days, stop.
--
-- Performance (2026-06-17):
--   CandidatePool pre-filters cheap predicates.
--   RelevantCallLogs / RelevantMeetings narrow rows before aggregation.
--   See migration: Migrations/Optimize_GetEligibleCandidatesForPlivoCXCalls.sql
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
    @Count  INT      = 10,
    @NowIST DATETIME = NULL   -- caller passes DateTime.Now (IST); falls back to GETDATE() if omitted
AS
BEGIN
    SET NOCOUNT ON;

    -- InterviewMeetings.interviewDate is stored in Indian Standard Time (IST).
    SET @NowIST = ISNULL(@NowIST, GETDATE());

    DECLARE @NowUtc        DATETIME = GETUTCDATE();
    DECLARE @TodayUtcStart DATETIME = CAST(CAST(@NowUtc AS DATE) AS DATETIME);
    DECLARE @TomorrowUtc   DATETIME = DATEADD(DAY, 1, @TodayUtcStart);
    DECLARE @CooldownFrom  DATETIME = DATEADD(HOUR, -2, @NowUtc);
    DECLARE @WindowFrom    DATETIME = DATEADD(DAY, -4, @NowUtc);
    DECLARE @RecentFromIST DATETIME = DATEADD(DAY, -15, @NowIST);

    ;WITH CandidatePool AS (
        SELECT
            c.id              AS CandidateId,
            c.IRFId           AS CandidateIrfIntId,
            c.Phone,
            c.VapiCallAttempts,
            c.PlivoCXDisposition,
            u.FirstName,
            u.EmailId,
            irf.IRFId         AS IrfBusinessId,
            irf.JDTiltle      AS JobTitle,
            cl.Name           AS ClientName
        FROM       dbo.Candidate        c
        INNER JOIN dbo.IRF              irf ON irf.id = c.IRFId
        INNER JOIN dbo.Client           cl  ON cl.id  = irf.ClientId
        INNER JOIN dbo.CandidatesMaster cm  ON cm.id  = c.CandidateMasterId
        INNER JOIN dbo.UserList         u   ON u.id   = cm.UserId
        WHERE  u.EmailId IS NOT NULL
          AND  NULLIF(LTRIM(RTRIM(c.Phone)), '') IS NOT NULL
          AND  (c.Status IS NULL OR c.Status <> 9)
          AND  (c.PlivoCXDisposition IS NULL OR c.PlivoCXDisposition <> 'uninterested')
    ),
    -- Only log rows that can affect eligibility (completed, schedule-purpose, or active booking)
    RelevantCallLogs AS (
        SELECT
            cp.EmailId,
            cp.CandidateIrfIntId,
            pcl.CallStatus,
            pcl.CallPurpose,
            pcl.CreatedAt,
            pcl.BookingApiSuccess,
            pcl.BookedInterviewDateTime,
            IsScheduleCall = CASE
                WHEN ISNULL(pcl.CallPurpose, N'schedule') = N'schedule' THEN 1
                ELSE 0
            END
        FROM       dbo.PlivoCXCallLogs pcl
        INNER JOIN CandidatePool       cp
            ON  cp.EmailId           = pcl.RecipientEmail
            AND cp.CandidateIrfIntId = pcl.IRFId
        WHERE  pcl.RecipientEmail IS NOT NULL
          AND  pcl.IRFId IS NOT NULL
          AND  (
                   pcl.CallStatus = 'completed'
                OR ISNULL(pcl.CallPurpose, N'schedule') = N'schedule'
                OR (pcl.BookingApiSuccess = 1 AND pcl.BookedInterviewDateTime > @NowUtc)
               )
    ),
    CallStats AS (
        SELECT
            rcl.EmailId           AS RecipientEmail,
            rcl.CandidateIrfIntId AS IRFId,
            CompletedCallCount = SUM(CASE WHEN rcl.CallStatus = 'completed' THEN 1 ELSE 0 END),
            ScheduleCallsToday = SUM(CASE
                WHEN rcl.IsScheduleCall = 1
                 AND rcl.CreatedAt >= @TodayUtcStart
                 AND rcl.CreatedAt <  @TomorrowUtc
                THEN 1 ELSE 0 END),
            HasRecentScheduleCall = MAX(CASE
                WHEN rcl.IsScheduleCall = 1 AND rcl.CreatedAt > @CooldownFrom
                THEN 1 ELSE 0 END),
            HasExpiredRetryWindow = MAX(CASE
                WHEN rcl.IsScheduleCall = 1 AND rcl.CreatedAt < @WindowFrom
                THEN 1 ELSE 0 END),
            HasFutureBooking = MAX(CASE
                WHEN rcl.IsScheduleCall = 1
                 AND rcl.BookingApiSuccess = 1
                 AND rcl.BookedInterviewDateTime > @NowUtc
                THEN 1 ELSE 0 END)
        FROM RelevantCallLogs rcl
        GROUP BY rcl.EmailId, rcl.CandidateIrfIntId
    ),
    -- Only meetings that can trigger a block (future, recent, cancelled, redo, or bad status)
    RelevantMeetings AS (
        SELECT
            cp.EmailId,
            cp.IrfBusinessId,
            im.confirmationStatus,
            im.interviewDate,
            im.IsDeleted,
            im.RedoInterview,
            ic.MeetingStatus
        FROM       dbo.InterviewMeetings im
        INNER JOIN CandidatePool         cp
            ON  cp.EmailId       = im.candidateEmail
            AND cp.IrfBusinessId = im.IRFId
        LEFT JOIN  dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE  im.candidateEmail IS NOT NULL
          AND  im.IRFId IS NOT NULL
          AND  (
                   im.interviewDate > @NowIST
                OR im.interviewDate >= @RecentFromIST
                OR im.confirmationStatus = '2'
                OR im.RedoInterview = 1
                OR ic.MeetingStatus IN (1, 3, 5, 9)
               )
    ),
    MeetingBlocks AS (
        SELECT
            rm.EmailId       AS candidateEmail,
            rm.IrfBusinessId AS IRFId,
            HasFutureMeeting = MAX(CASE
                WHEN rm.confirmationStatus <> '2'
                 AND rm.interviewDate > @NowIST
                THEN 1 ELSE 0 END),
            HasRecentValidConfirmed = MAX(CASE
                WHEN rm.confirmationStatus = '1'
                 AND rm.interviewDate >= @RecentFromIST
                 AND (rm.MeetingStatus IS NULL OR rm.MeetingStatus NOT IN (1, 3, 5, 9))
                THEN 1 ELSE 0 END),
            NeedsReschedule = MAX(CASE
                WHEN ISNULL(rm.IsDeleted, 0) = 0
                 AND (
                        rm.MeetingStatus IN (1, 3, 5, 9)
                     OR rm.confirmationStatus = '2'
                     OR rm.RedoInterview = 1
                     )
                THEN 1 ELSE 0 END)
        FROM RelevantMeetings rm
        GROUP BY rm.EmailId, rm.IrfBusinessId
    )
    SELECT TOP (@Count)
        cp.CandidateId,
        cp.CandidateIrfIntId                                           AS IRFId,
        cp.IrfBusinessId                                               AS IRFName,
        cp.FirstName                                                   AS CandidateName,
        cp.EmailId                                                     AS CandidateEmail,
        cp.Phone                                                       AS CandidatePhone,
        cp.JobTitle,
        cp.ClientName,
        cp.VapiCallAttempts                                            AS CallAttempts,
        cp.PlivoCXDisposition,
        ISNULL(cs.CompletedCallCount, 0)                               AS CompletedCallCount
    FROM       CandidatePool cp
    LEFT JOIN  CallStats     cs ON cs.RecipientEmail = cp.EmailId
                               AND cs.IRFId          = cp.CandidateIrfIntId
    LEFT JOIN  MeetingBlocks mb ON mb.candidateEmail = cp.EmailId
                               AND mb.IRFId          = cp.IrfBusinessId
    WHERE  ISNULL(mb.HasFutureMeeting, 0)          = 0
      AND  ISNULL(cs.HasFutureBooking, 0)          = 0
      AND  ISNULL(mb.HasRecentValidConfirmed, 0)     = 0
      AND  ISNULL(mb.NeedsReschedule, 0)             = 0
      AND  (
               cp.PlivoCXDisposition IS NULL
            OR ISNULL(cs.CompletedCallCount, 0) < 2
           )
      AND  ISNULL(cs.ScheduleCallsToday, 0)        < 4
      AND  ISNULL(cs.HasRecentScheduleCall, 0)       = 0
      AND  ISNULL(cs.HasExpiredRetryWindow, 0)       = 0
    ORDER BY
        CASE WHEN cp.PlivoCXDisposition IS NULL THEN 0 ELSE 1 END ASC,
        ISNULL(cs.CompletedCallCount, 0) ASC,
        cp.CandidateId DESC
    OPTION (RECOMPILE);
END
GO
