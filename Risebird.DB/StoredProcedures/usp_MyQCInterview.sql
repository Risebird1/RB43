IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyQCInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_MyQCInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_MyQCInterview
@emailId varchar(100)
As
Begin
select distinct im.interviewDate,irf.JDLink,irf.id as irfIntId,irf.IRFId,im.interviewer,im.candidateEmail,im.id as imid
from
TechQualityDetails tqoi
Join InterviewMeetings im on tqoi.Interviewid=im.id
join IRF irf on im.IRFId=irf.IRFId
where tqoi.QcInterviewer=@emailId and tqoi.IsActive=1
END
GO