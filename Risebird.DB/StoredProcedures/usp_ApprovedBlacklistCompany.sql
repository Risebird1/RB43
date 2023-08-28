 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApprovedBlacklistCompany'))
BEGIN
    DROP PROCEDURE dbo.usp_ApprovedBlacklistCompany
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

      
ALTER Procedure [dbo].[usp_ApprovedBlacklistCompany]
@CompanyName varchar(100),
@ModifyDate datetime,
@ModifyBy varchar(50)
as
begin
update BlackListedCompany set IsApprovedBlacklistCompany=1,ModifyDate=@ModifyDate,ModifyBy=@modifyBy where CompanyName=@CompanyName
declare @userId int=(select UserId from InterviewerProfile where CompanyName=@CompanyName)
 update UserList set IsActive=1 where id=@userId
 declare @EmailId varchar(100)=(select distinct EmailId from UserList where id=@userId)
 declare @interviewer varchar(100)=(select distinct Interviewer from InterviewMeetings where interviewer=@EmailId and interviewDate>GETDATE())
 update InterviewMeetings set interviewer=NULL where interviewer=@interviewer
 update TransInterviewer set IsActive=1 where Userid=@userId
end  

 