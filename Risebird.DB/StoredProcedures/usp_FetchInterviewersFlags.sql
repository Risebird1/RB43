IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewersFlags'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewersFlags
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchInterviewersFlags
@Emailid varchar(150)
As
BEGIN
Select ul.Emailid,ip.isApproved,ul.Isactive,ip.IsInterest as IsInterested,ul.LastLogin,ip.isVerified from 
InterviewerProfile ip join Userlist ul on ul.id=ip.Userid 
where ul.Emailid=@Emailid
END
GO