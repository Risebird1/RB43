-- =============================================
-- PlivoCX campaigns: the candidates of one campaign that may be dialled now.
-- Used by Call now and by the PlivoCXCampaignScheduler timer.
--
--   @CampaignId  : the campaign
--   @Take        : how many to return (the free concurrent-call slots)
--   @NowUtc      : current UTC time
--   @DayStartUtc : start of "today" in the campaign's time zone, as UTC - for Attempts/day
--
-- Due = State 'Pending'
--   AND NextAttemptAt is unset or has passed (Min gap)
--   AND Attempts < TotalAttempts
--   AND calls to the candidate from this campaign since @DayStartUtc < AttemptsPerDay
--   AND no other dial holds the candidate (DialLockUntil unset or expired)
--   AND no call to the candidate is still connecting (initiated / triggered in the last 30
--       minutes - older ones never got a webhook and no longer block).
-- The calling window is checked by the scheduler, not here: Call now ignores it.
-- Fewest attempts first, so fresh candidates go before retries.
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDuePlivoCXCampaignCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDuePlivoCXCampaignCandidates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDuePlivoCXCampaignCandidates]
    @CampaignId  INT,
    @Take        INT,
    @NowUtc      DATETIME,
    @DayStartUtc DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InFlightFrom DATETIME = DATEADD(MINUTE, -30, @NowUtc);

    SELECT TOP (@Take)
        cc.Id,
        cc.CampaignId,
        cc.CandidateId,
        cc.CandidateName,
        cc.CandidateEmail,
        cc.CandidatePhone,
        cc.IRFId,
        cc.IRFName,
        cc.JobTitle,
        cc.ClientName,
        cc.InterviewId,
        cc.OriginalInterviewDate,
        cc.[State],
        cc.Attempts,
        cc.LastCallAt,
        cc.NextAttemptAt,
        cc.LastDisposition,
        cc.BookedSlot,
        cc.LastCallLogId
    FROM       dbo.PlivoCXCampaignCandidate cc
    INNER JOIN dbo.PlivoCXCampaign          pc ON pc.Id = cc.CampaignId
    WHERE cc.CampaignId = @CampaignId
      AND cc.[State] = 'Pending'
      AND (cc.NextAttemptAt IS NULL OR cc.NextAttemptAt <= @NowUtc)
      AND cc.Attempts < pc.TotalAttempts
      AND (
            SELECT COUNT(*)
            FROM dbo.PlivoCXCallLogs l
            WHERE l.CampaignId  = cc.CampaignId
              AND l.CandidateId = cc.CandidateId
              AND l.CreatedAt  >= @DayStartUtc
          ) < pc.AttemptsPerDay
      AND (cc.DialLockUntil IS NULL OR cc.DialLockUntil < @NowUtc)
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.PlivoCXCallLogs l
            WHERE l.CandidateId = cc.CandidateId
              AND l.CallStatus IN ('initiated', 'triggered')
              AND l.CreatedAt >= @InFlightFrom
          )
    ORDER BY cc.Attempts ASC, ISNULL(cc.NextAttemptAt, '19000101') ASC, cc.Id ASC;
END
GO
