IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updateInterviewerIRFStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_updateInterviewerIRFStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_updateInterviewerIRFStatus  
@interviewId int,
@action varchar(100) = null,
@timeNow datetime = null
AS  
BEGIN  
declare @interviewerUserId int  
declare @irfId int  
declare @IRFName varchar(50)
declare @Interviewer varchar(200)
set @interviewerUserId = (select ul.id from UserList ul join InterviewMeetings im on im.interviewer = ul.EmailId where im.id = @InterviewId)  
Select @irfId=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid  
Select @IRFName=IRFID,@Interviewer=interviewer from InterviewMeetings where id=@Interviewid  

if(@action is not null and @action='Rejected')
begin

Update TransInterviewer Set ClientAction='Rejected',isActive=0 Where Userid=@interviewerUserId and IRFId=@irfId  

update InterviewMeetings set Interviewer=null  where interviewdate>@timeNow and IRFId=@IRFName and Interviewer=@Interviewer

end
else
begin

Update TransInterviewer Set ClientAction='Shortlisted' Where Userid=@interviewerUserId and IRFId=@irfId  

end
END  
GO