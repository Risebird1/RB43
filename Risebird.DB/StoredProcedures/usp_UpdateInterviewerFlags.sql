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
@Verified bit=null,
@IsApproved bit=null
As
BEGIN
Update ip set
ip.isVerified=Isnull(@Verified,ip.isVerified),
ip.IsApproved=isnull(@IsApproved,ip.IsApproved)
from InterviewerProfile ip join UserList ul on ip.Userid=ul.id
where ul.EmailId=@Emailid
END
GO