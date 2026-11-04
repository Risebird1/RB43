-- =============================================
-- PlivoCX and WhatsApp campaigns: the "Reschedule" candidate source on Create campaign.
--
-- The same list as /Client/ReScheduleMeetings for an internal user
-- (usp_GetClientReScheduledMeetings with @Clientid = 0), written for speed: that procedure groups
-- every meeting of every active channel before it applies the date range (and, for non-admins,
-- UNIONs four copies of the data and never applies the date range at all), then joins the
-- meetings back and looks up each candidate's status. That runs past the web request timeout.
--
-- A candidate is on the list when, on an active channel (IRF.EndDate >= now, not paused, not
-- deleted), their latest meeting (by interview date, ignoring deleted meetings and meetings
-- with no InterviewComments row, as the original does):
--   - is dated between @Startdate and @Enddate
--   - needs a reschedule: a comment with MeetingStatus 1, 3, 5 or 9, confirmationStatus = 2,
--     or RedoInterview = 1
-- and they are not cold (ColdCandidateInfos.IsColdCandidate).
--
-- Only meetings from @Startdate on are read: a latest meeting inside the range is always among
-- them, and when there are none the candidate's latest meeting is before the range anyway.
--
-- Scoping, as in the original: Admin and the other internal profiles (idProfile 1, 31, 32) see
-- every channel; anyone else only channels where they are the channel's Risebird HR or are
-- assigned through TransHR.
--   @EmailId   : the signed-in user
--   @Startdate : first meeting date (inclusive)
--   @Enddate   : last meeting date (inclusive; callers pass the day after the chosen end date)
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignReschedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignReschedule
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignReschedule]
    @EmailId   VARCHAR(150),
    @Startdate DATETIME,
    @Enddate   DATETIME
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
          AND ISNULL(irf.IsDeleted, 0) = 0
          AND (
                @AllChannels = 1
             OR irf.RisebirdHr = @UserId
             OR EXISTS (SELECT 1 FROM dbo.TransHR th WHERE th.IRFId = irf.id AND th.HRId = @HrId)
              )
    ),
    Latest AS (
        SELECT
            ch.id AS ChannelId, ch.IRFId AS IRFName, ch.JDTiltle, ch.ClientName,
            -- Only columns in IX_InterviewMeetings_IRFId_InterviewDate (IRFId, interviewDate) INCLUDE (confirmationStatus,
            -- candidateEmail, IsDeleted, ...): RedoInterview is read below, for the latest meeting only.
            im.id AS InterviewId, im.candidateEmail, im.interviewDate, im.confirmationStatus,
            ROW_NUMBER() OVER (PARTITION BY ch.id, im.candidateEmail ORDER BY im.interviewDate DESC, im.id DESC) AS rn
        FROM       Channels              ch
        INNER JOIN dbo.InterviewMeetings im ON im.IRFId = ch.IRFId
        WHERE im.interviewDate >= @Startdate
          AND ISNULL(im.IsDeleted, 0) = 0
          AND EXISTS (SELECT 1 FROM dbo.InterviewComments ic WHERE ic.InterviewId = im.id)
    )
    SELECT
        c.id                                                                   AS CandidateId,
        l.ChannelId                                                            AS IRFId,
        l.IRFName,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS CandidateName,
        ul.EmailId                                                             AS CandidateEmail,
        ul.MobileNumber                                                        AS CandidatePhone,
        l.JDTiltle                                                             AS JobTitle,
        l.ClientName,
        CONVERT(VARCHAR(11), l.interviewDate, 106)                             AS LastInterviewDate  -- dd mon yyyy
    FROM       Latest               l
    INNER JOIN dbo.UserList         ul ON ul.EmailId = l.candidateEmail
    INNER JOIN dbo.CandidatesMaster cm ON cm.UserId = ul.id
    INNER JOIN dbo.Candidate        c  ON c.IRFId = l.ChannelId AND c.CandidateMasterId = cm.id
    WHERE l.rn = 1
      AND l.interviewDate <= @Enddate
      AND (
            l.confirmationStatus = 2
         OR EXISTS (SELECT 1 FROM dbo.InterviewMeetings r WHERE r.id = l.InterviewId AND r.RedoInterview = 1)
         OR EXISTS (SELECT 1 FROM dbo.InterviewComments ic
                    WHERE ic.InterviewId = l.InterviewId AND ic.MeetingStatus IN (1, 3, 5, 9))
          )
      AND NOT EXISTS (
            SELECT 1 FROM dbo.ColdCandidateInfos cci
            WHERE cci.FKCandidateId = c.id AND cci.IsColdCandidate = 1)
    ORDER BY l.interviewDate DESC;
END
GO
