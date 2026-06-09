IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerFlags'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerFlags
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create procedure usp_UpdateInterviewerFlags  
@Emailid varchar(150),  
@LeadInterviewer bit=null,  
@IsApproved bit=null,  
@IsActive bit=null,
@IsBlocked bit=null
As  
BEGIN  
Update ip set  
ip.IsLeadInterviewer=Isnull(@LeadInterviewer,ip.IsLeadInterviewer),  
ip.IsApproved=isnull(@IsApproved,ip.IsApproved),
ip.IsDeleted=isnull(@IsBlocked,ip.IsDeleted)
from InterviewerProfile ip join UserList ul on ip.Userid=ul.id  
where ul.EmailId=@Emailid  
Update UserList set IsActive=IsNull(@IsActive,IsActive),
    InactivationType = CASE WHEN @IsActive = 0 THEN 'ManualFlag' ELSE InactivationType END,
    InactivatedBy = CASE WHEN @IsActive = 0 THEN 'Admin' ELSE InactivatedBy END,
    InactivatedAt = CASE WHEN @IsActive = 0 THEN GETDATE() ELSE InactivatedAt END
where EmailId=@Emailid  
if(@IsActive=0)  
begin  
Update InterviewMeetings set Interviewer=null where Interviewer in(Select Emailid from UserList where  EmailId=@Emailid)  
and interviewDate>DateAdd(Minute,330,GetDate())  
Update TransInterviewer set IsActive=0,IsAvailable=0 where UserId in(Select id from UserList where  EmailId=@Emailid)  
end  
END  
GO