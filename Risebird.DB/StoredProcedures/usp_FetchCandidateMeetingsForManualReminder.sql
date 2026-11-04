-- C3 Interview Reminder sent by hand (WhatsApp campaigns with a confirm purpose; later the Not Confirmed
-- bulk send): the same columns as usp_FetchCandidateMeetingsForReminders, for the meetings asked for, and
-- only those still booked, unconfirmed and upcoming. interviewDate is stored in IST.
--
--   @MeetingIds : comma-separated InterviewMeetings ids
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchCandidateMeetingsForManualReminder'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateMeetingsForManualReminder
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchCandidateMeetingsForManualReminder]
    @MeetingIds VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IRF.id AS Irfid, im.id AS Interviewid, candidateEmail AS CandidateEmail, im.interviewDate, ul.FirstName, 6 AS Typefunction,
        cm.id candidateid, ul.uniqueid, im.staffingAgency, im.MobileNumber, cl.Name AS ClientName, cl.CompanyLink, cl.LogoLink,
        CASE WHEN irf.SkillSetsevaluate > 1 AND ISNULL(irf.JDTitleTwo, '') != '' THEN irf.JDTiltle + ' and ' + irf.JDTitleTwo ELSE irf.JDTiltle END AS PrimarySkillInvite,
        im.zoomLink AS MeetingLink, im.TimeZonesValue, im.TimeZoneoffset
    FROM InterviewMeetings im
    JOIN IRF ON IRF.IRFId = im.IRFId
    JOIN Client cl ON cl.id = IRF.ClientId
    LEFT JOIN Userlist ul ON ul.EmailId = im.candidateEmail
    LEFT JOIN CandidatesMaster cm ON cm.UserId = ul.id
    WHERE im.id IN (SELECT TRY_CAST(value AS int) FROM STRING_SPLIT(@MeetingIds, ','))
      AND im.interviewDate > DATEADD(minute, 330, GETUTCDATE())
      AND im.confirmationstatus = 1 AND ISNULL(im.IsCandidateResponsed, 0) = 0
END
GO
