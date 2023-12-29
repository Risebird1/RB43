IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerResumesAndCodedResume'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerResumesAndCodedResume
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_SaveInterviewerResumesAndCodedResume
@Userid int,
@CodedResumeLink varchar(1000),
@ResumeLink varchar(1000),
@IsResumeRedactAttempted bit,
@IsResumeRedactFailed bit
AS
BEGIN
Update ip set ip.CodedResumeLink=@CodedResumeLink, [ip].ResumeLink=@ResumeLink, IsResumeRedactAttempted = @IsResumeRedactAttempted, IsResumeRedactFailed = @IsResumeRedactFailed From InterviewerProfile ip where ip.Userid=@Userid
END
Go