IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updateInterviewerIRFStatusByIRFid'))
BEGIN
    DROP PROCEDURE dbo.usp_updateInterviewerIRFStatusByIRFid
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_updateInterviewerIRFStatusByIRFid  
@irfId int,
@Interviewer varchar(200),
@action varchar(100) = null,
@timeNow datetime = null
AS  
BEGIN  
declare @interviewerUserId int  
declare @IRFName varchar(50)
set @interviewerUserId = (select id from UserList where Emailid=@Interviewer)  
Select @IRFName=IRFID from IRF where id=@irfId 

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