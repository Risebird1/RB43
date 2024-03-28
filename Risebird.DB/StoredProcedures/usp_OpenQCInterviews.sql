IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenQCInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenQCInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_OpenQCInterviews
@emailID varchar(100)
AS
BEGIN
select distinct im.interviewDate,irf.JDLink,irf.id as irfIntId,irf.IRFId,im.interviewer,im.candidateEmail,im.id as imid
from
TechQualityDetails tqoi
Join InterviewMeetings im on tqoi.Interviewid=im.id
join IRF irf on im.IRFId=irf.IRFId 
join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId 
join [dbo].[UserList] ul on tirf.UserId = ul.id   
where tqoi.IsActive=1 and tqoi.QcInterviewer is null
and im.confirmationStatus = 1
and tirf.IsActive=1
and ul.EmailId=@emailID
and im.interviewer!=@emailID
order by interviewDate desc
END
GO