IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchChannelOwnerandInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchChannelOwnerandInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchChannelOwnerandInterviewer]
    @interviewid INT
AS
BEGIN
    SELECT
        CASE WHEN ISNULL(uli.IsActive, 0) = 1 THEN im.interviewer ELSE NULL END AS interviewer,
        ul.EmailId,
        im.interviewDate,
        Cl.Name AS ClientName,
        IRF.CalendlyLink,
        IRF.JDTiltle,
        IRF.IRFId,
        IRF.id AS irfIntId,
        im.candidateEmail,
        im.candidateName AS CandidateName,
        im.ZoomMeetingId,
        im.MeetingPlatForm
    FROM InterviewMeetings im
    JOIN IRF ON im.IRFId = irf.IRFId
    LEFT JOIN Client cl ON IRF.ClientId = cl.id
    LEFT JOIN Userlist ul ON ul.id = irf.RisebirdHr
    LEFT JOIN UserList uli ON uli.EmailId = im.interviewer
    WHERE im.id = @interviewid
      AND confirmationStatus = 2
END
GO
