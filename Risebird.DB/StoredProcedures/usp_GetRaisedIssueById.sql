IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRaisedIssueById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRaisedIssueById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns a single ticket with supplementary data.
Create Procedure usp_GetRaisedIssueById
@id int
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
    WHERE rs.id = @id;
END
GO
