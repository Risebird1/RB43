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
@IsActive bit=null
As
BEGIN
Update ip set
ip.IsLeadInterviewer=Isnull(@LeadInterviewer,ip.IsLeadInterviewer),
ip.IsApproved=isnull(@IsApproved,ip.IsApproved)
from InterviewerProfile ip join UserList ul on ip.Userid=ul.id
where ul.EmailId=@Emailid
Update UserList set IsActive=IsNull(@IsActive,IsActive) where EmailId=@Emailid
END
GO