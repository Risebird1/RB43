IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMyRaisedIssues'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMyRaisedIssues
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns all tickets raised by a specific user (for the "My Tickets" page).
Create Procedure usp_GetMyRaisedIssues
@RequestedBy varchar(200)
AS
BEGIN
    SELECT
        rs.*,
        ul.MobileNumber,
        ul.FirstName AS Name,
        IRF.id       AS irfIntId,
        im.candidateEmail,
        im.Interviewer
    FROM RaisedIssues rs
    LEFT JOIN UserList ul           ON ul.EmailId  = rs.RequestedBy
    LEFT JOIN InterviewMeetings im  ON im.Id       = rs.interviewId
    LEFT JOIN IRF                   ON IRF.IRFID   = im.IRFId
    WHERE rs.RequestedBy = @RequestedBy
    ORDER BY rs.RequestedOn DESC;
END
GO
