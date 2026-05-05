IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetMyTickets'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMyTickets
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_GetMyTickets
@RequestedBy varchar(200)
AS
BEGIN
    Select rs.*, ul.MobileNumber, ul.FirstName as Name,
           IRF.id as irfIntId, im.candidateEmail, im.Interviewer
    from RaisedIssues rs
    left join UserList ul on ul.EmailId = rs.RequestedBy
    left join InterviewMeetings im on im.Id = rs.interviewId
    left join IRF on IRF.IRFID = im.IRFId
    where rs.RequestedBy = @RequestedBy
    order by rs.RequestedOn desc
END
GO
