IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewerDetailsByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerDetailsByInterviewId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInterviewerDetailsByInterviewId]
    @interviewid INT
AS
BEGIN
    SELECT
        im.interviewer,
        ul.EmailId,
        im.interviewDate,
        Cl.Name AS ClientName,
        IRF.CalendlyLink,
        IRF.JDTiltle,
        (ISNULL(uli.FirstName, '') + ISNULL(uli.LastName, '')) AS CandidateName
    FROM InterviewMeetings im
    JOIN IRF ON im.IRFId = irf.IRFId
    LEFT JOIN Client cl ON IRF.ClientId = cl.id
    LEFT JOIN Userlist ul ON ul.id = irf.RisebirdHr
    LEFT JOIN Userlist uli ON uli.EmailId = im.candidateEmail
    WHERE im.id = @interviewid
END
GO
