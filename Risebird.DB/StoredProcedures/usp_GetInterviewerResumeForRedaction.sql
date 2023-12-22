IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerResumeForRedaction'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerResumeForRedaction
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 
CREATE PROCEDURE usp_GetInterviewerResumeForRedaction
AS
BEGIN

select top 10 ipr.id, ResumeLink, ul.FirstName+ space(1) + ul.LastName as [FirstName] from InterviewerProfile ipr
join UserList ul on ul.id = ipr.UserId
where ResumeLink is not null and ResumeLink != '' and (CodedResumeLink is null or CodedResumeLink = '')
and ul.IsActive = 1 and ( ipr.IsResumeRedactAttempted is null ) --and ipr.IsResumeRedactFailed
order by id desc
END   

GO

