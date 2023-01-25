IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerResume'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerResume
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_SaveInterviewerResume
@Emailid varchar(150),
@ResumeLink varchar(1000)
AS
BEGIN
UPdate ip set ip.ResumeLink=@ResumeLink From UserList ul join InterviewerProfile ip on ip.Userid=ul.id  where ul.Emailid=@Emailid
END
GO