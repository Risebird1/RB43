-- =============================================
-- PlivoCX campaigns: the "Not-scheduled" candidate source on Create campaign.
--
-- The same list as /Client/NotScheduledMeetings for an internal user
-- (usp_GetClientNotScheduledMeetings with @Clientid = 0), written for speed: that procedure
-- UNIONs every candidate with every meeting across all channels and groups the lot to find the
-- candidates with no meeting (HAVING COUNT(*) = 1), which takes minutes. Here the same rule is a
-- NOT EXISTS per candidate.
--
-- A candidate is on the list when:
--   - their skill channel is active: IRF.EndDate >= now and not paused
--   - they were added to the channel: a CandidateStatus row with Status = 12
--   - they are not cold (ColdCandidateInfos.IsColdCandidate)
--   - there is no InterviewMeetings row at all for their email on that channel
--
-- Scoping, as in the original: Admin and the other internal profiles (idProfile 1, 31, 32) see
-- every channel; anyone else only channels where they are the channel's Risebird HR or are
-- assigned through TransHR.
--   @EmailId : the signed-in user
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignNotScheduled'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignNotScheduled
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignNotScheduled]
    @EmailId VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Role INT, @UserId INT, @HrId INT;
    SELECT @Role = ul.idProfile, @UserId = ul.id FROM dbo.UserList ul WHERE ul.EmailId = @EmailId;
    SELECT @HrId = hr.id FROM dbo.HR hr WHERE hr.UserId = @UserId;
    DECLARE @AllChannels BIT = CASE WHEN @Role IN (1, 31, 32) THEN 1 ELSE 0 END;

    ;WITH Channels AS (
        SELECT irf.id, irf.IRFId, irf.JDTiltle, cl.Name AS ClientName
        FROM       dbo.IRF    irf
        INNER JOIN dbo.Client cl ON cl.id = irf.ClientId
        WHERE irf.EndDate >= GETDATE()
          AND ISNULL(irf.IsPaused, 0) = 0
          AND (
                @AllChannels = 1
             OR irf.RisebirdHr = @UserId
             OR EXISTS (SELECT 1 FROM dbo.TransHR th WHERE th.IRFId = irf.id AND th.HRId = @HrId)
              )
    )
    SELECT
        c.id                                                                 AS CandidateId,
        ch.id                                                                AS IRFId,
        ch.IRFId                                                             AS IRFName,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS CandidateName,
        ul.EmailId                                                           AS CandidateEmail,
        ul.MobileNumber                                                      AS CandidatePhone,
        ch.JDTiltle                                                          AS JobTitle,
        ch.ClientName
    FROM       Channels              ch
    INNER JOIN dbo.Candidate         c  ON c.IRFId = ch.id
    INNER JOIN dbo.CandidatesMaster  cm ON cm.id   = c.CandidateMasterId
    INNER JOIN dbo.UserList          ul ON ul.id   = cm.UserId
    WHERE ul.EmailId IS NOT NULL
      AND EXISTS (
            SELECT 1 FROM dbo.CandidateStatus cs
            WHERE cs.CandidateId = cm.id AND cs.IRFId = ch.id AND cs.Status = 12)
      AND NOT EXISTS (
            SELECT 1 FROM dbo.ColdCandidateInfos cci
            WHERE cci.FKCandidateId = c.id AND cci.IsColdCandidate = 1)
      AND NOT EXISTS (
            SELECT 1 FROM dbo.InterviewMeetings im
            WHERE im.IRFId = ch.IRFId AND im.candidateEmail = ul.EmailId)
    ORDER BY c.id DESC;
END
GO
