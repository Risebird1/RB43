IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchQCFormsOnDate'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchQCFormsOnDate
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_FetchQCFormsOnDate
@StartDate datetime,
@EndDate datetime
AS
BEGIN
Select im.id as imid,im.IRFId,im.InterviewDate,im.interviewer,im.candidateName,im.candidateEmail,cl.Name as ClientName,ul.EmailId as QCFormedBy,ISNULL(tq.Redflags,0) Redflags 
from Trans_QCFormReport tq join InterviewMeetings im on im.id=tq.Interviewid join UserList ul on ul.id=tq.Userid join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId
where tq.ModifiedOn>=@StartDate and tq.ModifiedOn<@EndDate
END
GO