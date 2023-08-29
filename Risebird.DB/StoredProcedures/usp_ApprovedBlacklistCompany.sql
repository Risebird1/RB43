 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApprovedBlacklistCompany'))
BEGIN
    DROP PROCEDURE dbo.usp_ApprovedBlacklistCompany
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
Create Procedure [dbo].[usp_ApprovedBlacklistCompany]
@CompanyName varchar(100),
@ModifyDate datetime,
@ModifyBy varchar(50)
as
begin
update BlackListedCompany set IsApprovedBlacklistCompany=1,ModifyDate=@ModifyDate,ModifyBy=@modifyBy where CompanyName=@CompanyName
Update UserList set IsActive=0 where id in(Select UserId from InterviewerProfile where CompanyName=@CompanyName)
Update InterviewMeetings set Interviewer=null where Interviewer in(Select EmailId from UserList where id in(Select UserId from InterviewerProfile where CompanyName=@CompanyName))
and interviewDate>@ModifyDate
Update TransInterviewer set IsActive=0,IsAvailable=0 where UserId in(Select UserId from InterviewerProfile where CompanyName=@CompanyName)
end 

 