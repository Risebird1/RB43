IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerCodedResume'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerCodedResume
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_SaveInterviewerCodedResume
@Userid int,
@CodedResumeLink varchar(1000),
@IsResumeRedactAttempted bit=null,
@IsResumeRedactFailed bit=null
AS
BEGIN
Update ip set ip.CodedResumeLink=@CodedResumeLink, IsResumeRedactAttempted = @IsResumeRedactAttempted, IsResumeRedactFailed = @IsResumeRedactFailed From InterviewerProfile ip where ip.Userid=@Userid
END
Go